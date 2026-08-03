[CmdletBinding()]
param(
    [string]$ConfigPath = 'C:\ProgramData\ARCWorks\Backup\backup.config.psd1',
    [string]$TargetRoot,
    [switch]$ValidateDatabases,
    [switch]$KeepContainers,
    [ValidateRange(1, 20)][int]$KeepSuccessfulRestores = 2
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function Invoke-Restic([string[]]$Arguments) {
    & $script:Config.ResticExe @Arguments
    if ($LASTEXITCODE -ne 0) { throw "Restic exited with code $LASTEXITCODE." }
}

function Invoke-Docker([string[]]$Arguments) {
    $null = & docker.exe @Arguments
    if ($LASTEXITCODE -ne 0) { throw "Docker exited with code $LASTEXITCODE." }
}

function Wait-ContainerCommand {
    param(
        [Parameter(Mandatory)][string]$Name,
        [Parameter(Mandatory)][string[]]$Command,
        [int]$Seconds = 90
    )
    $deadline = [DateTime]::UtcNow.AddSeconds($Seconds)
    do {
        & docker.exe exec $Name @Command 1>$null 2>$null
        if ($LASTEXITCODE -eq 0) { return }
        Start-Sleep -Seconds 2
    } while ([DateTime]::UtcNow -lt $deadline)
    throw "Restore-test database did not become ready: $Name"
}

function Test-Manifest([string]$RestoreRoot) {
    $runDirectories = @(Get-ChildItem -LiteralPath $RestoreRoot -Directory -Recurse -Force |
        Where-Object { Test-Path -LiteralPath (Join-Path $_.FullName 'SHA256-MANIFEST.csv') })
    if ($runDirectories.Count -ne 1) { throw "Expected one restored run directory; found $($runDirectories.Count)." }

    $runRoot = $runDirectories[0].FullName
    $records = @(Import-Csv -LiteralPath (Join-Path $runRoot 'SHA256-MANIFEST.csv'))
    if ($records.Count -lt 3) { throw 'Restored manifest contains too few records.' }
    foreach ($record in $records) {
        $path = Join-Path $runRoot $record.Path
        if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "Restored file is missing: $($record.Path)" }
        $file = Get-Item -LiteralPath $path
        if ([int64]$record.Length -ne $file.Length) { throw "Length mismatch: $($record.Path)" }
        $actual = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash
        if ($actual -ne $record.SHA256) { throw "SHA-256 mismatch: $($record.Path)" }
    }
    return $runRoot
}

function Test-RestoredDatabases([string]$RunRoot) {
    $databaseRoot = Join-Path $RunRoot 'databases'
    $mariaDump = Join-Path $databaseRoot 'roms-mariadb.sql'
    $postgresDump = Join-Path $databaseRoot 'zabbix-postgresql.dump'
    foreach ($path in @($mariaDump, $postgresDump)) {
        if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "Database dump is missing: $path" }
    }

    $suffix = [Guid]::NewGuid().ToString('N').Substring(0, 8)
    $mariaName = "arcworks-restore-mariadb-$suffix"
    $postgresName = "arcworks-restore-postgres-$suffix"
    $postgresPassword = [Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(24))
    try {
        Invoke-Docker @('run', '-d', '--name', $mariaName, '-e', 'MARIADB_ALLOW_EMPTY_ROOT_PASSWORD=1', '-e', 'MARIADB_DATABASE=restore_test', 'mariadb:11.4')
        Wait-ContainerCommand -Name $mariaName -Command @('mariadb', '-uroot', 'restore_test', '-e', 'SELECT 1')
        Invoke-Docker @('cp', $mariaDump, "${mariaName}:/tmp/roms.sql")
        Invoke-Docker @('exec', $mariaName, 'sh', '-c', 'exec mariadb -uroot restore_test < /tmp/roms.sql')
        $mariaCount = & docker.exe exec $mariaName mariadb -N -uroot restore_test -e "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema='restore_test';"
        if ($LASTEXITCODE -ne 0 -or [int]$mariaCount -lt 1) { throw 'MariaDB restore validation found no tables.' }

        Invoke-Docker @('run', '-d', '--name', $postgresName, '-e', "POSTGRES_PASSWORD=$postgresPassword", '-e', 'POSTGRES_DB=restore_test', 'postgres:16-alpine')
        Wait-ContainerCommand -Name $postgresName -Command @('psql', '--username=postgres', '--dbname=restore_test', '--command', 'SELECT 1')
        Invoke-Docker @('cp', $postgresDump, "${postgresName}:/tmp/zabbix.dump")
        Invoke-Docker @('exec', $postgresName, 'pg_restore', '--exit-on-error', '--no-owner', '--no-privileges', '--username=postgres', '--dbname=restore_test', '/tmp/zabbix.dump')
        $postgresCount = & docker.exe exec $postgresName psql --username=postgres --dbname=restore_test --tuples-only --no-align --command 'SELECT COUNT(*) FROM information_schema.tables WHERE table_schema NOT IN (''pg_catalog'', ''information_schema'');'
        if ($LASTEXITCODE -ne 0 -or [int]$postgresCount -lt 1) { throw 'PostgreSQL restore validation found no application tables.' }

        [pscustomobject]@{ MariaDBTables = [int]$mariaCount; PostgreSQLTables = [int]$postgresCount }
    } finally {
        if (-not $KeepContainers) {
            foreach ($name in @($mariaName, $postgresName)) {
                & docker.exe rm --force $name 2>$null | Out-Null
            }
        }
    }
}

if (-not (Test-Path -LiteralPath $ConfigPath -PathType Leaf)) { throw "Runtime configuration is missing: $ConfigPath" }
$script:Config = Import-PowerShellDataFile -LiteralPath $ConfigPath
if (-not $TargetRoot) { $TargetRoot = $script:Config.RestoreTestRoot }
if (-not $TargetRoot) { throw 'RestoreTestRoot is not configured.' }

$restoreId = [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssZ')
$restoreRoot = Join-Path $TargetRoot $restoreId
if (Test-Path -LiteralPath $restoreRoot) { throw "Restore target already exists: $restoreRoot" }
New-Item -ItemType Directory -Path $restoreRoot -Force | Out-Null

try {
    Invoke-Restic @('-r', $script:Config.LocalRepository, '--password-file', $script:Config.LocalPasswordFile, 'restore', 'latest', '--host', $script:Config.ResticHost, '--tag', 'arcworks', '--target', $restoreRoot)
    $runRoot = Test-Manifest $restoreRoot
    $databaseResult = $null
    if ($ValidateDatabases) { $databaseResult = Test-RestoredDatabases $runRoot }

    $result = [ordered]@{
        TimestampUtc = [DateTime]::UtcNow.ToString('o')
        Status = 'Success'
        Snapshot = 'latest'
        RestoreRoot = $restoreRoot
        Manifest = 'Verified'
        DatabaseValidation = if ($ValidateDatabases) { 'Verified' } else { 'NotRequested' }
        MariaDBTables = if ($databaseResult) { $databaseResult.MariaDBTables } else { $null }
        PostgreSQLTables = if ($databaseResult) { $databaseResult.PostgreSQLTables } else { $null }
    }
    $stateDirectory = Join-Path $script:Config.ControlRoot 'state'
    New-Item -ItemType Directory -Path $stateDirectory -Force | Out-Null
    $resultJson = $result | ConvertTo-Json
    $resultJson | Set-Content -LiteralPath (Join-Path $stateDirectory 'last-restore-test.json') -Encoding utf8NoBOM
    $resultJson | Set-Content -LiteralPath (Join-Path $restoreRoot 'RESTORE-RESULT.json') -Encoding utf8NoBOM

    $successfulRestores = @(Get-ChildItem -LiteralPath $TargetRoot -Directory -Force |
        Where-Object Name -Match '^\d{8}T\d{6}Z$' |
        Where-Object {
            $resultFile = Join-Path $_.FullName 'RESTORE-RESULT.json'
            if (-not (Test-Path -LiteralPath $resultFile -PathType Leaf)) { return $false }
            try { return (Get-Content -LiteralPath $resultFile -Raw | ConvertFrom-Json).Status -eq 'Success' }
            catch { return $false }
        } |
        Sort-Object Name -Descending)
    foreach ($oldRestore in @($successfulRestores | Select-Object -Skip $KeepSuccessfulRestores)) {
        $restorePrefix = [IO.Path]::GetFullPath($TargetRoot).TrimEnd('\') + '\'
        $candidate = [IO.Path]::GetFullPath($oldRestore.FullName).TrimEnd('\') + '\'
        if (-not $candidate.StartsWith($restorePrefix, [StringComparison]::OrdinalIgnoreCase)) {
            throw "Refusing to remove restore data outside $restorePrefix"
        }
        Remove-Item -LiteralPath $oldRestore.FullName -Recurse -Force
    }

    $resultJson
} catch {
    $failure = [ordered]@{ TimestampUtc = [DateTime]::UtcNow.ToString('o'); Status = 'Failed'; RestoreRoot = $restoreRoot; Message = $_.Exception.Message }
    $stateDirectory = Join-Path $script:Config.ControlRoot 'state'
    New-Item -ItemType Directory -Path $stateDirectory -Force | Out-Null
    $failure | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $stateDirectory 'last-restore-test.json') -Encoding utf8NoBOM
    throw
}
