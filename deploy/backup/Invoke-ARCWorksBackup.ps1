[CmdletBinding()]
param(
    [ValidateSet('DatabaseOnly', 'Full', 'Maintenance')]
    [string]$Mode = 'Full',
    [string]$ConfigPath = 'C:\ProgramData\ARCWorks\Backup\backup.config.psd1',
    [switch]$KeepSuccessfulStaging
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$mutex = [Threading.Mutex]::new($false, 'Global\ARCWorksBackup')
$lockHeld = $false
$runRoot = $null
$logPath = $null
$script:Config = $null

function Write-Log([string]$Message, [ValidateSet('INFO', 'WARN', 'ERROR')][string]$Level = 'INFO') {
    $line = '{0:o} [{1}] {2}' -f [DateTimeOffset]::Now, $Level, $Message
    Write-Host $line
    if ($script:logPath) { Add-Content -LiteralPath $script:logPath -Value $line -Encoding utf8 }
}

function Assert-RequiredPath([string]$Path, [string]$Description) {
    if (-not (Test-Path -LiteralPath $Path)) { throw "$Description is missing: $Path" }
}

function Assert-Container([string]$Name) {
    $state = & docker inspect $Name --format '{{.State.Status}}|{{if .State.Health}}{{.State.Health.Status}}{{else}}none{{end}}' 2>$null
    if ($LASTEXITCODE -ne 0 -or -not $state) { throw "Required container is unavailable: $Name" }
    $parts = $state.Trim() -split '\|', 2
    if ($parts[0] -ne 'running') { throw "Container $Name is not running." }
    if ($parts[1] -notin @('healthy', 'none')) { throw "Container $Name health is $($parts[1])." }
}

function Resolve-Container([string]$ExplicitName, [string]$Service) {
    if (-not [string]::IsNullOrWhiteSpace($ExplicitName)) { return $ExplicitName }

    $instanceId = [string]$script:Config.InstanceId
    if ([string]::IsNullOrWhiteSpace($instanceId)) {
        throw "No explicit container was configured and InstanceId is empty for service '$Service'."
    }

    $matches = @(& docker ps --filter "label=com.arcworks.instance=$instanceId" --filter "label=com.arcworks.service=$Service" --format '{{.Names}}') |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
    if ($matches.Count -ne 1) {
        throw "Expected exactly one running '$Service' container for instance '$instanceId'; found $($matches.Count). Configure an explicit container name if this service is managed separately."
    }
    return [string]$matches[0]
}

function Invoke-BinaryCapture {
    param(
        [Parameter(Mandatory)][string]$FilePath,
        [Parameter(Mandatory)][string[]]$ArgumentList,
        [Parameter(Mandatory)][string]$OutputPath
    )

    $errorPath = "$OutputPath.stderr"
    $start = [Diagnostics.ProcessStartInfo]::new()
    $start.FileName = $FilePath
    $start.UseShellExecute = $false
    $start.RedirectStandardOutput = $true
    $start.RedirectStandardError = $true
    $start.CreateNoWindow = $true
    foreach ($argument in $ArgumentList) { [void]$start.ArgumentList.Add($argument) }

    $process = [Diagnostics.Process]::new()
    $process.StartInfo = $start
    $stream = [IO.File]::Open($OutputPath, [IO.FileMode]::Create, [IO.FileAccess]::Write, [IO.FileShare]::None)
    $exitCode = $null
    try {
        if (-not $process.Start()) { throw "Could not start $FilePath." }
        $copyTask = $process.StandardOutput.BaseStream.CopyToAsync($stream)
        $errorTask = $process.StandardError.ReadToEndAsync()
        $process.WaitForExit()
        [void]$copyTask.GetAwaiter().GetResult()
        $errorText = $errorTask.GetAwaiter().GetResult()
        $exitCode = $process.ExitCode
    } finally {
        $stream.Dispose()
        $process.Dispose()
    }
    if ($errorText) { [IO.File]::WriteAllText($errorPath, $errorText, [Text.UTF8Encoding]::new($false)) }
    if ($exitCode -ne 0) {
        throw "$FilePath exited with code $exitCode. See $errorPath."
    }
    if (Test-Path -LiteralPath $errorPath) { Remove-Item -LiteralPath $errorPath -Force }
}

function Invoke-Restic([string[]]$Arguments) {
    $output = & $script:Config.ResticExe @Arguments 2>&1
    $exitCode = $LASTEXITCODE
    foreach ($line in $output) {
        $text = [string]$line
        if ($text) { Write-Log "restic: $text" }
    }
    if ($exitCode -ne 0) { throw "Restic exited with code $exitCode." }
    return @($output)
}

function Copy-SourceTree {
    param(
        [Parameter(Mandatory)][string]$Source,
        [Parameter(Mandatory)][string]$Destination,
        [string[]]$ExcludeDirectories = @(),
        [string[]]$ExcludeFiles = @()
    )

    Assert-RequiredPath $Source 'Backup source'
    New-Item -ItemType Directory -Path $Destination -Force | Out-Null
    $arguments = @(
        $Source, $Destination, '/E', '/COPY:DAT', '/DCOPY:DAT', '/R:1', '/W:1',
        '/XJ', '/SL', '/FFT', '/NP', '/NFL', '/NDL', '/NJH', '/NJS'
    )
    if ($ExcludeDirectories.Count -gt 0) { $arguments += '/XD'; $arguments += $ExcludeDirectories }
    if ($ExcludeFiles.Count -gt 0) { $arguments += '/XF'; $arguments += $ExcludeFiles }

    & robocopy.exe @arguments | Out-Null
    $exitCode = $LASTEXITCODE
    if ($exitCode -ge 8) { throw "Robocopy failed for $Source with exit code $exitCode." }
    Write-Log "Captured source tree: $Source"
}

function Capture-Databases([string]$DatabaseDirectory) {
    New-Item -ItemType Directory -Path $DatabaseDirectory -Force | Out-Null
    $mariaContainer = Resolve-Container $script:Config.RomsDatabaseContainer 'db'
    $zabbixContainer = Resolve-Container $script:Config.ZabbixDatabaseContainer 'zabbix-db'
    Assert-Container $mariaContainer
    Assert-Container $zabbixContainer
    Write-Log "Using database containers: ROMS=$mariaContainer; Zabbix=$zabbixContainer."

    $mariaDump = Join-Path $DatabaseDirectory 'roms-mariadb.sql'
    $mariaCommand = 'exec mariadb-dump --single-transaction --quick --routines --events --triggers --hex-blob --default-character-set=utf8mb4 -uroot -p"$MARIADB_ROOT_PASSWORD" "$MARIADB_DATABASE"'
    Invoke-BinaryCapture -FilePath 'docker.exe' -ArgumentList @('exec', $mariaContainer, 'sh', '-c', $mariaCommand) -OutputPath $mariaDump
    if ((Get-Item -LiteralPath $mariaDump).Length -lt 1024) { throw 'MariaDB dump is unexpectedly small.' }
    if (-not (Select-String -LiteralPath $mariaDump -Pattern 'CREATE TABLE' -SimpleMatch -Quiet)) { throw 'MariaDB dump has no CREATE TABLE statements.' }
    Write-Log "Validated MariaDB dump ($((Get-Item -LiteralPath $mariaDump).Length) bytes)."

    $postgresDump = Join-Path $DatabaseDirectory 'zabbix-postgresql.dump'
    $postgresCommand = 'export PGPASSWORD="$(cat /run/secrets/postgres_password)"; exec pg_dump --format=custom --compress=9 --username="$(cat /run/secrets/postgres_user)" --dbname="$POSTGRES_DB"'
    Invoke-BinaryCapture -FilePath 'docker.exe' -ArgumentList @('exec', $zabbixContainer, 'sh', '-c', $postgresCommand) -OutputPath $postgresDump
    if ((Get-Item -LiteralPath $postgresDump).Length -lt 1024) { throw 'PostgreSQL dump is unexpectedly small.' }
    $header = [IO.File]::ReadAllBytes($postgresDump)[0..4]
    if ([Text.Encoding]::ASCII.GetString($header) -ne 'PGDMP') { throw 'PostgreSQL custom dump header is invalid.' }
    Write-Log "Validated PostgreSQL dump ($((Get-Item -LiteralPath $postgresDump).Length) bytes)."
}

function Capture-Metadata([string]$MetadataDirectory) {
    New-Item -ItemType Directory -Path $MetadataDirectory -Force | Out-Null

    Get-Volume | Where-Object DriveLetter | Select-Object DriveLetter, FileSystemLabel, FileSystem, HealthStatus, Size, SizeRemaining |
        ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $MetadataDirectory 'volumes.json') -Encoding utf8NoBOM
    & docker ps --format '{{json .}}' | Set-Content -LiteralPath (Join-Path $MetadataDirectory 'docker-containers.jsonl') -Encoding utf8NoBOM
    & docker volume ls --format '{{json .}}' | Set-Content -LiteralPath (Join-Path $MetadataDirectory 'docker-volumes.jsonl') -Encoding utf8NoBOM
    & docker image ls --digests --format '{{json .}}' | Set-Content -LiteralPath (Join-Path $MetadataDirectory 'docker-images.jsonl') -Encoding utf8NoBOM
    & wsl.exe --list --verbose | Set-Content -LiteralPath (Join-Path $MetadataDirectory 'wsl-distributions.txt') -Encoding utf8NoBOM

    foreach ($source in @($script:Config.RomsRoot, $script:Config.MonitoringRoot, $script:Config.PortfolioRoot)) {
        if (Test-Path -LiteralPath (Join-Path $source '.git')) {
            $name = ([IO.Path]::GetFileName($source) -replace '[^A-Za-z0-9_.-]', '_')
            & git -C $source status --short --branch | Set-Content -LiteralPath (Join-Path $MetadataDirectory "$name-git-status.txt") -Encoding utf8NoBOM
            & git -C $source log -1 --format='%H %cI %s' | Set-Content -LiteralPath (Join-Path $MetadataDirectory "$name-git-head.txt") -Encoding utf8NoBOM
        }
    }
    Write-Log 'Captured redacted infrastructure metadata.'
}

function Capture-FullSources([string]$SourcesDirectory) {
    $commonExclude = @(
        'bin', 'obj', 'node_modules', '.dotnet', '.dotnet-home', '.nuget-packages',
        'TestResults', 'playwright-report', '.artifacts', 'tmp', 'cache', '__pycache__',
        'backups', 'Docker'
    )
    Copy-SourceTree -Source $script:Config.RomsRoot -Destination (Join-Path $SourcesDirectory 'roms') -ExcludeDirectories $commonExclude -ExcludeFiles @('*.pyc')
    Copy-SourceTree -Source $script:Config.MonitoringRoot -Destination (Join-Path $SourcesDirectory 'monitoring') -ExcludeDirectories ($commonExclude + @('postgres', 'logs', 'downloads')) -ExcludeFiles @('*.db', '*.log', '*.msi')
    Copy-SourceTree -Source $script:Config.PortfolioRoot -Destination (Join-Path $SourcesDirectory 'portfolio') -ExcludeDirectories ($commonExclude + @('logs')) -ExcludeFiles @('*.log')

    $codexExcludeDirectories = @(
        '.sandbox', '.sandbox-bin', '.sandbox-secrets', '.tmp', 'ambient-suggestions',
        'browser', 'cache', 'computer-use', 'node_repl', 'plugins', 'process_manager',
        'thread-writer-locks', 'tmp', 'vendor_imports', 'visualizations'
    )
    $codexDestination = Join-Path $SourcesDirectory 'codex-continuity'
    Copy-SourceTree -Source $script:Config.CodexRoot -Destination $codexDestination -ExcludeDirectories $codexExcludeDirectories -ExcludeFiles @('auth.json*', '*.tmp')

    foreach ($required in @(
        'sessions', 'archived_sessions', 'memories', 'skills',
        'AGENTS.md', '.codex-global-state.json', 'config.toml', 'session_index.jsonl'
    )) {
        if (-not (Test-Path -LiteralPath (Join-Path $codexDestination $required))) {
            throw "Required Codex continuity data was not captured: $required"
        }
    }

    $forbiddenCodexFiles = @(Get-ChildItem -LiteralPath $codexDestination -Recurse -Force -File |
        Where-Object { $_.Name -like 'auth.json*' -or $_.FullName -match '\\.sandbox-secrets(\\|$)' })
    if ($forbiddenCodexFiles.Count -gt 0) {
        throw 'Forbidden Codex authentication or sandbox-secret files entered staging.'
    }
    Write-Log 'Validated required Codex continuity data and secret exclusions.'
}

function New-Manifest([string]$Root) {
    $manifestPath = Join-Path $Root 'SHA256-MANIFEST.csv'
    $records = Get-ChildItem -LiteralPath $Root -Recurse -File -Force |
        Where-Object FullName -ne $manifestPath |
        ForEach-Object {
            $relative = [IO.Path]::GetRelativePath($Root, $_.FullName)
            $hash = Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256
            [pscustomobject]@{
                Path = $relative
                Length = $_.Length
                LastWriteTimeUtc = $_.LastWriteTimeUtc.ToString('o')
                SHA256 = $hash.Hash
            }
        }
    $records = @($records)
    $records | Export-Csv -LiteralPath $manifestPath -Encoding utf8NoBOM
    if ($records.Count -lt 3) { throw 'Manifest contains too few files.' }
    Write-Log "Created SHA-256 manifest for $($records.Count) files."
}

function Invoke-Retention([string]$Repository, [string]$PasswordFile, [switch]$Prune) {
    $arguments = @(
        '-r', $Repository, '--password-file', $PasswordFile,
        'forget', '--host', $script:Config.ResticHost,
        '--keep-hourly', [string]$script:Config.Retention.Hourly,
        '--keep-daily', [string]$script:Config.Retention.Daily,
        '--keep-weekly', [string]$script:Config.Retention.Weekly,
        '--keep-monthly', [string]$script:Config.Retention.Monthly,
        '--keep-yearly', [string]$script:Config.Retention.Yearly,
        '--group-by', 'host,paths,tags'
    )
    if ($Prune) { $arguments += '--prune' }
    Invoke-Restic $arguments | Out-Null
}

function Invoke-RepositoryMaintenance([switch]$FullRead) {
    foreach ($repository in @(
        @{ Path = $script:Config.LocalRepository; Password = $script:Config.LocalPasswordFile },
        @{ Path = $script:Config.ReplicationRepository; Password = $script:Config.ReplicationPasswordFile }
    )) {
        Invoke-Retention -Repository $repository.Path -PasswordFile $repository.Password -Prune
        $checkArguments = @('-r', $repository.Path, '--password-file', $repository.Password, 'check')
        if ($FullRead) { $checkArguments += '--read-data' } else { $checkArguments += @('--read-data-subset', '10%') }
        Invoke-Restic $checkArguments | Out-Null
    }
}

function Remove-SuccessfulStaging([string]$Path) {
    $staging = [IO.Path]::GetFullPath($script:Config.StagingRoot).TrimEnd('\') + '\'
    $target = [IO.Path]::GetFullPath($Path).TrimEnd('\') + '\'
    if (-not $target.StartsWith($staging, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to delete staging outside $staging"
    }
    if ([IO.Path]::GetFileName($Path) -notmatch '^\d{8}T\d{6}Z-(DatabaseOnly|Full)$') {
        throw "Refusing to delete unexpected staging path: $Path"
    }
    Remove-Item -LiteralPath $Path -Recurse -Force
    Write-Log "Removed completed staging run: $Path"
}

try {
    if (-not $mutex.WaitOne([TimeSpan]::FromSeconds(1))) { throw 'Another ARCWorks backup is already running.' }
    $lockHeld = $true

    Assert-RequiredPath $ConfigPath 'Runtime configuration'
    $script:Config = Import-PowerShellDataFile -LiteralPath $ConfigPath
    if ($script:Config.SchemaVersion -ne 1) { throw 'Unsupported backup configuration schema.' }
    if (-not $script:Config.ContainsKey('InstanceId')) { $script:Config.InstanceId = $script:Config.ResticHost }
    if (-not $script:Config.ContainsKey('RomsDatabaseContainer')) { $script:Config.RomsDatabaseContainer = '' }
    if (-not $script:Config.ContainsKey('ZabbixDatabaseContainer')) { $script:Config.ZabbixDatabaseContainer = '' }

    foreach ($path in @($script:Config.ControlRoot, $script:Config.StagingRoot, $script:Config.LocalRepository, $script:Config.ReplicationRepository)) {
        Assert-RequiredPath $path 'Backup path'
    }
    foreach ($path in @($script:Config.ResticExe, $script:Config.LocalPasswordFile, $script:Config.ReplicationPasswordFile)) {
        Assert-RequiredPath $path 'Backup control file'
    }

    $logDirectory = Join-Path $script:Config.ControlRoot 'logs'
    $stateDirectory = Join-Path $script:Config.ControlRoot 'state'
    New-Item -ItemType Directory -Path $logDirectory, $stateDirectory -Force | Out-Null
    $runId = [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssZ')
    $script:logPath = Join-Path $logDirectory "$runId-$Mode.log"
    Write-Log "Starting $Mode backup run $runId."

    if ($Mode -eq 'Maintenance') {
        Invoke-RepositoryMaintenance
        @{ TimestampUtc = [DateTime]::UtcNow.ToString('o'); Mode = $Mode; Status = 'Success' } |
            ConvertTo-Json | Set-Content -LiteralPath (Join-Path $stateDirectory 'last-maintenance.json') -Encoding utf8NoBOM
        Write-Log 'Repository maintenance completed.'
        return
    }

    $script:runRoot = Join-Path $script:Config.StagingRoot "$runId-$Mode"
    New-Item -ItemType Directory -Path $script:runRoot -Force | Out-Null
    Capture-Databases (Join-Path $script:runRoot 'databases')
    Capture-Metadata (Join-Path $script:runRoot 'metadata')
    if ($Mode -eq 'Full') { Capture-FullSources (Join-Path $script:runRoot 'sources') }
    New-Manifest $script:runRoot

    $tag = if ($Mode -eq 'DatabaseOnly') { 'database-only' } else { 'daily-full' }
    Invoke-Restic @(
        '-r', $script:Config.LocalRepository,
        '--password-file', $script:Config.LocalPasswordFile,
        'backup', $script:runRoot,
        '--host', $script:Config.ResticHost,
        '--tag', 'arcworks', '--tag', $tag
    ) | Out-Null
    Invoke-Restic @('-r', $script:Config.LocalRepository, '--password-file', $script:Config.LocalPasswordFile, 'check') | Out-Null

    Invoke-Restic @(
        '-r', $script:Config.ReplicationRepository,
        '--password-file', $script:Config.ReplicationPasswordFile,
        'copy',
        '--from-repo', $script:Config.LocalRepository,
        '--from-password-file', $script:Config.LocalPasswordFile,
        '--host', $script:Config.ResticHost,
        '--tag', 'arcworks'
    ) | Out-Null
    Invoke-Restic @('-r', $script:Config.ReplicationRepository, '--password-file', $script:Config.ReplicationPasswordFile, 'check') | Out-Null

    $cloudStatus = 'Disabled'
    if ($script:Config.CloudRepository) {
        Assert-RequiredPath $script:Config.CloudPasswordFile 'Cloud repository password file'
        if ($script:Config.CloudCredentialScript) {
            Assert-RequiredPath $script:Config.CloudCredentialScript 'Cloud credential script'
            & $script:Config.CloudCredentialScript
            if ($LASTEXITCODE -ne 0) { throw 'Cloud credential setup failed.' }
        }
        Invoke-Restic @(
            '-r', $script:Config.CloudRepository,
            '--password-file', $script:Config.CloudPasswordFile,
            'copy', '--from-repo', $script:Config.ReplicationRepository,
            '--from-password-file', $script:Config.ReplicationPasswordFile,
            '--host', $script:Config.ResticHost,
            '--tag', 'arcworks'
        ) | Out-Null
        Invoke-Restic @('-r', $script:Config.CloudRepository, '--password-file', $script:Config.CloudPasswordFile, 'check') | Out-Null
        $cloudStatus = 'Success'
    } else {
        Write-Log 'Remote cloud repository is not configured; off-site replication was skipped.' 'WARN'
    }

    Invoke-Retention -Repository $script:Config.LocalRepository -PasswordFile $script:Config.LocalPasswordFile
    Invoke-Retention -Repository $script:Config.ReplicationRepository -PasswordFile $script:Config.ReplicationPasswordFile

    $result = [ordered]@{
        TimestampUtc = [DateTime]::UtcNow.ToString('o')
        RunId = $runId
        Mode = $Mode
        Status = 'Success'
        LocalRepository = 'Success'
        ReplicationRepository = 'Success'
        CloudRepository = $cloudStatus
    }
    $result | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $stateDirectory 'last-success.json') -Encoding utf8NoBOM
    $result | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $script:runRoot 'BACKUP-RESULT.json') -Encoding utf8NoBOM
    Write-Log "Backup run $runId completed."

    if (-not $KeepSuccessfulStaging) { Remove-SuccessfulStaging $script:runRoot }
} catch {
    $message = $_.Exception.Message
    Write-Log $message 'ERROR'
    if ($script:Config -and $script:Config.ControlRoot) {
        $failurePath = Join-Path $script:Config.ControlRoot 'state\last-failure.json'
        @{ TimestampUtc = [DateTime]::UtcNow.ToString('o'); Mode = $Mode; Status = 'Failed'; Message = $message; Staging = $script:runRoot } |
            ConvertTo-Json | Set-Content -LiteralPath $failurePath -Encoding utf8NoBOM
    }
    throw
} finally {
    if ($lockHeld) { [void]$mutex.ReleaseMutex() }
    $mutex.Dispose()
}
