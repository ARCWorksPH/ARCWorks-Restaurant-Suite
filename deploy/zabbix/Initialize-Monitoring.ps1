[CmdletBinding()]
param(
    [string]$MonitoringServer = "192.168.1.2"
)

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent $MyInvocation.MyCommand.Path
$SecretRoot = Join-Path $Root ".secrets"

function New-UrlSafeSecret {
    param([int]$ByteCount = 32)
    $bytes = [byte[]]::new($ByteCount)
    [Security.Cryptography.RandomNumberGenerator]::Fill($bytes)
    return [Convert]::ToBase64String($bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_')
}

function Ensure-SecretFile {
    param([string]$Path, [string]$Value)
    if (-not (Test-Path -LiteralPath $Path)) {
        [IO.File]::WriteAllText($Path, $Value, [Text.UTF8Encoding]::new($false))
    }
}

foreach ($path in @(
    $SecretRoot,
    (Join-Path $Root "data\postgres"),
    (Join-Path $Root "data\agent2"),
    (Join-Path $Root "downloads"),
    (Join-Path $Root "logs")
)) {
    [IO.Directory]::CreateDirectory($path) | Out-Null
}

Ensure-SecretFile (Join-Path $SecretRoot "postgres_user") "zabbix"
Ensure-SecretFile (Join-Path $SecretRoot "postgres_password") (New-UrlSafeSecret 36)
Ensure-SecretFile (Join-Path $SecretRoot "zabbix_admin_password") (New-UrlSafeSecret 28)

try {
    $currentUserSid = [Security.Principal.WindowsIdentity]::GetCurrent().User.Value
    & icacls.exe $SecretRoot /inheritance:r /grant:r "*$currentUserSid`:(OI)(CI)F" "*S-1-5-18:(OI)(CI)F" "*S-1-5-32-544:(OI)(CI)F" | Out-Null
} catch {
    Write-Warning "Could not tighten the secret directory ACL automatically: $($_.Exception.Message)"
}

Push-Location $Root
try {
    docker compose config --quiet
    docker compose pull
    docker compose up -d

    $deadline = (Get-Date).AddMinutes(5)
    do {
        try {
            $response = Invoke-WebRequest -Uri "http://127.0.0.1:8085/" -UseBasicParsing -TimeoutSec 5
            if ($response.StatusCode -eq 200) { break }
        } catch {
            Start-Sleep -Seconds 5
        }
    } while ((Get-Date) -lt $deadline)

    if (-not $response -or $response.StatusCode -ne 200) {
        throw "Zabbix web interface did not become ready within five minutes."
    }

    & (Join-Path $Root "Configure-Zabbix.ps1") -MonitoringServer $MonitoringServer

    $lock = docker image inspect --format '{{index .RepoDigests 0}}' `
        postgres:16-alpine `
        zabbix/zabbix-server-pgsql:alpine-7.0.29 `
        zabbix/zabbix-web-nginx-pgsql:alpine-7.0.29 `
        zabbix/zabbix-agent2:alpine-7.0.29
    [IO.File]::WriteAllLines((Join-Path $Root "image-lock.txt"), $lock, [Text.UTF8Encoding]::new($false))
} finally {
    Pop-Location
}

$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if ($isAdmin) {
    foreach ($rule in @(
        @{ Name = "ARCWorks Zabbix Dashboard (LAN)"; Port = 8085 },
        @{ Name = "ARCWorks Zabbix Active Agents (LAN)"; Port = 10051 }
    )) {
        if (-not (Get-NetFirewallRule -DisplayName $rule.Name -ErrorAction SilentlyContinue)) {
            New-NetFirewallRule -DisplayName $rule.Name -Direction Inbound -Action Allow -Protocol TCP -LocalPort $rule.Port -Profile Private -RemoteAddress LocalSubnet | Out-Null
        }
    }
} else {
    Write-Warning "Run Set-Lan-Firewall.ps1 once as Administrator to allow dashboard and agent traffic from the private LAN."
}

Write-Host "Zabbix is available locally at http://127.0.0.1:8085"
Write-Host "LAN dashboard address: http://$MonitoringServer`:8085"
Write-Host "The generated Admin password is stored in .secrets\zabbix_admin_password and was not printed."
