[CmdletBinding()]
param(
    [switch]$Execute,
    [switch]$RemovePostgreSQL
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$stageRoot = 'D:\ARCWorks_Migration_Staging_20260830'
$evidenceRoot = Join-Path $stageRoot 'evidence'
$logPath = Join-Path $evidenceRoot 'admin-migration-steps.log'

if (-not $Execute) {
    throw 'Safety stop: rerun this script with -Execute after reviewing it.'
}

$identity = [Security.Principal.WindowsIdentity]::GetCurrent()
$principal = [Security.Principal.WindowsPrincipal]::new($identity)
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    throw 'Administrator rights are required. Open PowerShell as Administrator and rerun with -Execute.'
}

if (-not (Test-Path -LiteralPath $stageRoot -PathType Container)) {
    throw "Verified staging root is missing: $stageRoot"
}

New-Item -ItemType Directory -Path $evidenceRoot -Force | Out-Null
Start-Transcript -LiteralPath $logPath -Append | Out-Null

function Get-TreeMeasure([string]$Path) {
    $files = @(Get-ChildItem -LiteralPath $Path -File -Force -Recurse -ErrorAction Stop)
    $measure = $files | Measure-Object -Property Length -Sum
    [pscustomobject]@{
        Files = [long]$files.Count
        Bytes = if ($null -eq $measure.Sum) { [long]0 } else { [long]$measure.Sum }
    }
}

function Invoke-Uninstaller {
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][string[]]$Arguments
    )

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        Write-Warning "Uninstaller is absent: $Path"
        return
    }

    $process = Start-Process -FilePath $Path -ArgumentList $Arguments -Wait -PassThru -WindowStyle Hidden
    if ($process.ExitCode -ne 0) {
        throw "Uninstaller failed with exit code $($process.ExitCode): $Path"
    }
}

function Copy-AndVerify([string]$Source, [string]$Destination) {
    if (-not (Test-Path -LiteralPath $Source)) {
        Write-Warning "Optional source is absent: $Source"
        return
    }

    New-Item -ItemType Directory -Path $Destination -Force | Out-Null
    & robocopy.exe $Source $Destination /E /COPY:DAT /DCOPY:DAT /R:2 /W:1 /XJ /MT:16 /J /NP
    $robocopyExit = $LASTEXITCODE
    if ($robocopyExit -ge 8) {
        throw "Robocopy failed for $Source with exit code $robocopyExit."
    }

    $sourceMeasure = Get-TreeMeasure $Source
    $destinationMeasure = Get-TreeMeasure $Destination
    if ($sourceMeasure.Files -ne $destinationMeasure.Files -or
        $sourceMeasure.Bytes -ne $destinationMeasure.Bytes) {
        throw "Copy verification failed for $Source."
    }

    Write-Host "Verified: $Source ($($sourceMeasure.Files) files, $($sourceMeasure.Bytes) bytes)"
}

try {
    Write-Host '1. Disabling ARCWorks backup schedules during migration...'
    $taskNames = @(
        'ARCWorks Backup - Daily Full',
        'ARCWorks Backup - Every 6 Hours Databases',
        'ARCWorks Backup - Weekly Maintenance',
        'ARCWorks Backup - Weekly Restore Drill'
    )
    foreach ($taskName in $taskNames) {
        $task = Get-ScheduledTask -TaskName $taskName -ErrorAction SilentlyContinue
        if ($task) {
            Disable-ScheduledTask -TaskName $taskName | Out-Null
        }
    }

    Write-Host '2. Stopping standalone PostgreSQL for a consistent physical copy...'
    foreach ($serviceName in @('pgagent-pg18', 'postgresql-x64-18')) {
        $service = Get-Service -Name $serviceName -ErrorAction SilentlyContinue
        if ($service -and $service.Status -ne 'Stopped') {
            Stop-Service -Name $serviceName -Force
            $service.WaitForStatus('Stopped', [TimeSpan]::FromSeconds(60))
        }
    }

    Write-Host '3. Preserving standalone PostgreSQL/PostGIS material before G: is rebuilt...'
    Copy-AndVerify 'G:\PostgreSQL' (Join-Path $stageRoot 'from-G\PostgreSQL')
    Copy-AndVerify 'G:\PostGIS' (Join-Path $stageRoot 'from-G\PostGIS')
    Copy-AndVerify 'G:\StackBuilder' (Join-Path $stageRoot 'from-G\StackBuilder')

    Write-Host '4. Removing confirmed Nextcloud-only Windows data and proxy helpers...'
    $nextcloudTargets = @(
        'G:\NextCloud',
        'G:\__pycache__',
        'G:\delete_proxy.bat',
        'G:\fix_proxy_v6.bat',
        'G:\fix_proxy_wsl_ip.bat',
        'G:\fix_proxy.bat',
        'G:\fw_rules.ps1',
        'G:\nextcloud_proxy.py',
        'G:\scratch_curl.txt',
        'G:\scratch_service.txt',
        'G:\tcp_proxy.py'
    )
    foreach ($target in $nextcloudTargets) {
        $absolute = [IO.Path]::GetFullPath($target)
        if (-not $absolute.StartsWith('G:\', [StringComparison]::OrdinalIgnoreCase) -or $absolute -eq 'G:\') {
            throw "Refusing unsafe deletion target: $absolute"
        }
        if (Test-Path -LiteralPath $absolute) {
            Remove-Item -LiteralPath $absolute -Recurse -Force
        }
    }

    Write-Host '5. Removing obsolete Nextcloud-to-PostgreSQL WSL firewall rules...'
    Get-NetFirewallRule -DisplayName 'PostgreSQL - WSL2 only' -ErrorAction SilentlyContinue |
        Remove-NetFirewallRule

    if ($RemovePostgreSQL) {
        Write-Host '6. Uninstalling the retired standalone PostgreSQL toolchain...'
        Invoke-Uninstaller 'G:\PostgreSQL\uninstall-postgis-bundle-pg18x64-3.6.2-1.exe' @('/S')
        Invoke-Uninstaller 'G:\PostgreSQL\uninstall-pgagent.exe' @('--mode', 'unattended')
        Invoke-Uninstaller 'G:\PostgreSQL\uninstall-postgresql.exe' @('--mode', 'unattended')

        foreach ($target in @('G:\PostgreSQL', 'G:\PostGIS', 'G:\StackBuilder')) {
            $absolute = [IO.Path]::GetFullPath($target)
            if (-not $absolute.StartsWith('G:\', [StringComparison]::OrdinalIgnoreCase) -or $absolute -eq 'G:\') {
                throw "Refusing unsafe PostgreSQL cleanup target: $absolute"
            }
            if (Test-Path -LiteralPath $absolute) {
                Remove-Item -LiteralPath $absolute -Recurse -Force
            }
        }
    }

    Write-Host '7. Verifying active Nextcloud and PostgreSQL residue...'
    $remainingTargets = @($nextcloudTargets | Where-Object { Test-Path -LiteralPath $_ })
    $ubuntuRegistered = [bool]((wsl.exe --list --quiet 2>$null) -match '^Ubuntu$')
    $portProxy = @(netsh.exe interface portproxy show all | Select-String -Pattern 'nextcloud|80|443')
    $nextcloudService = @(Get-CimInstance Win32_Service | Where-Object {
        $_.Name -match 'nextcloud' -or $_.DisplayName -match 'nextcloud' -or $_.PathName -match 'nextcloud'
    })

    if ($remainingTargets.Count -gt 0 -or $ubuntuRegistered -or
        $portProxy.Count -gt 0 -or $nextcloudService.Count -gt 0) {
        throw "Nextcloud cleanup verification failed. Remaining targets: $($remainingTargets -join ', ')"
    }

    if ($RemovePostgreSQL) {
        $remainingServices = @(Get-CimInstance Win32_Service | Where-Object {
            $_.Name -in @('pgagent-pg18', 'postgresql-x64-18') -or $_.PathName -match 'G:\\PostgreSQL'
        })
        $uninstallRoots = @(
            'HKLM:\Software\Microsoft\Windows\CurrentVersion\Uninstall\*',
            'HKLM:\Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\*',
            'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\*'
        )
        $remainingPrograms = @(Get-ItemProperty $uninstallRoots -ErrorAction SilentlyContinue | Where-Object {
            $_.DisplayName -match 'PostgreSQL 18|pgAgent_PG18|PostGIS Bundle 3\.6\.2'
        })
        if ($remainingServices.Count -gt 0 -or $remainingPrograms.Count -gt 0 -or
            (Test-Path -LiteralPath 'G:\PostgreSQL')) {
            throw 'PostgreSQL cleanup verification failed. Review the transcript before formatting G:.'
        }
    }

    Write-Host 'ADMIN MIGRATION STEPS: PASS'
    if ($RemovePostgreSQL) {
        Write-Host 'The retired standalone PostgreSQL toolchain was removed after its staged copy was verified.'
    } else {
        Write-Host 'PostgreSQL remains installed but stopped. Do not repartition G: until the staged copy is independently reviewed.'
    }
} finally {
    Stop-Transcript | Out-Null
}
