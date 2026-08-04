[CmdletBinding(SupportsShouldProcess)]
param(
    [string]$DestinationPath = (Join-Path $PSScriptRoot '..\.env'),
    [string]$RomsHost = 'localhost',
    [string]$AllowedHosts,
    [string]$ComposeProjectName = 'arcworks-resto-main',
    [string]$InstanceId = 'arcworks-resto-main',
    [ValidateRange(1, 2147483647)][int]$DbServerId = 1,
    [ValidateRange(1, 65535)][int]$RomsHostPort = 7070,
    [string]$DatabaseName = 'roms',
    [string]$DatabaseUser = 'roms',
    [switch]$Force
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function ConvertTo-DotEnvValue([string]$Value) {
    return "'" + $Value.Replace("'", "\'") + "'"
}

function New-Secret {
    [Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(36))
}

if ([string]::IsNullOrWhiteSpace($AllowedHosts)) {
    $AllowedHosts = "$RomsHost;app;localhost;127.0.0.1"
}

if ((Test-Path -LiteralPath $DestinationPath) -and -not $Force) {
    Write-Output "Production environment already exists: $DestinationPath"
    exit 0
}

$destination = [IO.Path]::GetFullPath($DestinationPath)
$parent = Split-Path -Parent $destination
if (-not (Test-Path -LiteralPath $parent)) {
    New-Item -ItemType Directory -Path $parent -Force | Out-Null
}

$lines = @(
    "COMPOSE_PROJECT_NAME=$(ConvertTo-DotEnvValue $ComposeProjectName)"
    "INSTANCE_ID=$(ConvertTo-DotEnvValue $InstanceId)"
    "ROMS_HOST=$(ConvertTo-DotEnvValue $RomsHost)"
    "ROMS_HOST_PORT='$RomsHostPort'"
    "ROMS_IMAGE='roms:local'"
    "DB_SERVER_ID='$DbServerId'"
    "DB_NAME=$(ConvertTo-DotEnvValue $DatabaseName)"
    "DB_USER=$(ConvertTo-DotEnvValue $DatabaseUser)"
    "DB_PASSWORD=$(ConvertTo-DotEnvValue (New-Secret))"
    "DB_ROOT_PASSWORD=$(ConvertTo-DotEnvValue (New-Secret))"
    "ADMIN_USERNAME='admin'"
    "ADMIN_PASSWORD=$(ConvertTo-DotEnvValue (New-Secret))"
    "ADMIN_DISPLAY_NAME='ROMS Administrator'"
    "ROMS_ALLOWED_HOSTS=$(ConvertTo-DotEnvValue $AllowedHosts)"
    "CLOUDFLARE_TUNNEL_TOKEN_FILE='./.secrets/cloudflare-tunnel-token'"
    "OLLAMA_VOLUME_NAME=$(ConvertTo-DotEnvValue ($ComposeProjectName + '_ollama'))"
    "MARIADB_VOLUME_NAME=$(ConvertTo-DotEnvValue ($ComposeProjectName + '_mariadb-data'))"
    "DATA_PROTECTION_VOLUME_NAME=$(ConvertTo-DotEnvValue ($ComposeProjectName + '_data-protection-keys'))"
    "MONITOR_VOLUME_NAME=$(ConvertTo-DotEnvValue ($ComposeProjectName + '_monitor-data'))"
    "CADDY_DATA_VOLUME_NAME=$(ConvertTo-DotEnvValue ($ComposeProjectName + '_caddy-data'))"
    "CADDY_CONFIG_VOLUME_NAME=$(ConvertTo-DotEnvValue ($ComposeProjectName + '_caddy-config'))"
    "AI_ENABLED='false'"
)

if ($PSCmdlet.ShouldProcess($destination, 'Create protected production environment')) {
    [IO.File]::WriteAllLines($destination, $lines, [Text.UTF8Encoding]::new($false))

    $identity = [Security.Principal.WindowsIdentity]::GetCurrent().Name
    & icacls.exe $destination /inheritance:r /grant:r "${identity}:(F)" 'SYSTEM:(F)' | Out-Null
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to restrict permissions on $destination."
    }
}

Write-Output "Created protected production environment: $destination"
Write-Output 'Random database and administrator secrets were generated but not printed.'
Write-Output 'Create the ignored Cloudflare token file separately before enabling the edge-tunnel profile.'
