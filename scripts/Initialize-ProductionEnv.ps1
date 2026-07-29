[CmdletBinding()]
param(
    [string]$DatabaseEnvPath = (Join-Path $PSScriptRoot '..\Docker\MariaDB\.env'),
    [string]$DestinationPath = (Join-Path $PSScriptRoot '..\.env'),
    [string]$RomsHost = 'roms.gbserverph.online',
    [switch]$Force
)

$ErrorActionPreference = 'Stop'

function Read-DotEnv([string]$Path) {
    $values = @{}
    foreach ($line in Get-Content -LiteralPath $Path) {
        $trimmed = $line.Trim()
        if (-not $trimmed -or $trimmed.StartsWith('#') -or -not $trimmed.Contains('=')) {
            continue
        }

        $name, $value = $trimmed.Split('=', 2)
        $value = $value.Trim()
        if ($value.Length -ge 2 -and
            (($value.StartsWith("'") -and $value.EndsWith("'")) -or
             ($value.StartsWith('"') -and $value.EndsWith('"')))) {
            $value = $value.Substring(1, $value.Length - 2)
        }
        $values[$name.Trim()] = $value
    }
    return $values
}

function ConvertTo-DotEnvValue([string]$Value) {
    return "'" + $Value.Replace("'", "\'") + "'"
}

if ((Test-Path -LiteralPath $DestinationPath) -and -not $Force) {
    Write-Output "Production environment already exists: $DestinationPath"
    exit 0
}

$database = Read-DotEnv $DatabaseEnvPath
foreach ($required in 'DB_PASSWORD', 'DB_ROOT_PASSWORD') {
    if (-not $database.ContainsKey($required) -or [string]::IsNullOrWhiteSpace($database[$required])) {
        throw "Required value $required is missing from $DatabaseEnvPath."
    }
}

$adminPassword = [Convert]::ToBase64String(
    [Security.Cryptography.RandomNumberGenerator]::GetBytes(36))

$lines = @(
    "ROMS_HOST=$(ConvertTo-DotEnvValue $RomsHost)"
    "ROMS_IMAGE='roms:local'"
    "DB_SERVER_ID='1'"
    "DB_NAME=$(ConvertTo-DotEnvValue ($database['DB_NAME'] ?? 'roms'))"
    "DB_USER=$(ConvertTo-DotEnvValue ($database['DB_USER'] ?? 'roms'))"
    "DB_PASSWORD=$(ConvertTo-DotEnvValue $database['DB_PASSWORD'])"
    "DB_ROOT_PASSWORD=$(ConvertTo-DotEnvValue $database['DB_ROOT_PASSWORD'])"
    "ADMIN_USERNAME='admin'"
    "ADMIN_PASSWORD=$(ConvertTo-DotEnvValue $adminPassword)"
    "ADMIN_DISPLAY_NAME='ROMS Administrator'"
    "INVENTORY_ENABLED='false'"
)

[IO.File]::WriteAllLines(
    [IO.Path]::GetFullPath($DestinationPath),
    $lines,
    [Text.UTF8Encoding]::new($false))

$identity = [Security.Principal.WindowsIdentity]::GetCurrent().Name
& icacls.exe $DestinationPath /inheritance:r /grant:r "${identity}:(F)" 'SYSTEM:(F)' | Out-Null
if ($LASTEXITCODE -ne 0) {
    throw "Failed to restrict permissions on $DestinationPath."
}

Write-Output "Created protected production environment: $DestinationPath"
Write-Output 'The generated administrator password was not printed.'
