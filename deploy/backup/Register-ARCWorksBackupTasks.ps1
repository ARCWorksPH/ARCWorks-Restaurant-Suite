[CmdletBinding(SupportsShouldProcess)]
param(
    [string]$RuntimeRoot = 'C:\ProgramData\ARCWorks\Backup',
    [string]$ScriptSource = $PSScriptRoot,
    [switch]$Enable
)

trap {
    $errorLog = Join-Path $RuntimeRoot 'logs\task-registration-error.log'
    New-Item -ItemType Directory -Path (Split-Path -Parent $errorLog) -Force | Out-Null
    $detail = $_ | Format-List * -Force | Out-String
    [IO.File]::WriteAllText($errorLog, $detail, [Text.UTF8Encoding]::new($false))
    exit 1
}

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function Assert-Administrator {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = [Security.Principal.WindowsPrincipal]$identity
    if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
        throw 'Run task registration from an elevated PowerShell session.'
    }
}

Assert-Administrator

$pwsh = (Get-Command pwsh.exe -ErrorAction Stop).Source
$currentUser = [Security.Principal.WindowsIdentity]::GetCurrent().Name
$principal = New-ScheduledTaskPrincipal -UserId $currentUser -LogonType Interactive -RunLevel Highest
$settings = New-ScheduledTaskSettingsSet -StartWhenAvailable -MultipleInstances IgnoreNew -ExecutionTimeLimit ([TimeSpan]::FromHours(12)) -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries
$runtimeScripts = Join-Path $RuntimeRoot 'scripts'
New-Item -ItemType Directory -Path $runtimeScripts -Force | Out-Null

foreach ($name in @('Invoke-ARCWorksBackup.ps1', 'Test-ARCWorksRestore.ps1')) {
    $source = Join-Path $ScriptSource $name
    if (-not (Test-Path -LiteralPath $source -PathType Leaf)) { throw "Required script is missing: $source" }
    Copy-Item -LiteralPath $source -Destination (Join-Path $runtimeScripts $name) -Force
}

$backupScript = Join-Path $runtimeScripts 'Invoke-ARCWorksBackup.ps1'
$restoreScript = Join-Path $runtimeScripts 'Test-ARCWorksRestore.ps1'
$hourlyTrigger = @(0..15 | ForEach-Object {
    New-ScheduledTaskTrigger -Daily -At ([DateTime]::Today.AddHours(8 + $_))
})

$definitions = @(
    @{
        Name = 'Backup - Hourly Databases'
        Action = New-ScheduledTaskAction -Execute $pwsh -Argument "-NoLogo -NoProfile -NonInteractive -ExecutionPolicy Bypass -File `"$backupScript`" -Mode DatabaseOnly"
        Trigger = $hourlyTrigger
    },
    @{
        Name = 'Backup - Daily Full'
        Action = New-ScheduledTaskAction -Execute $pwsh -Argument "-NoLogo -NoProfile -NonInteractive -ExecutionPolicy Bypass -File `"$backupScript`" -Mode Full"
        Trigger = New-ScheduledTaskTrigger -Daily -At '01:15'
    },
    @{
        Name = 'Backup - Weekly Maintenance'
        Action = New-ScheduledTaskAction -Execute $pwsh -Argument "-NoLogo -NoProfile -NonInteractive -ExecutionPolicy Bypass -File `"$backupScript`" -Mode Maintenance"
        Trigger = New-ScheduledTaskTrigger -Weekly -WeeksInterval 1 -DaysOfWeek Sunday -At '03:30'
    },
    @{
        Name = 'Backup - Weekly Restore Drill'
        Action = New-ScheduledTaskAction -Execute $pwsh -Argument "-NoLogo -NoProfile -NonInteractive -ExecutionPolicy Bypass -File `"$restoreScript`" -ValidateDatabases"
        Trigger = New-ScheduledTaskTrigger -Weekly -WeeksInterval 1 -DaysOfWeek Sunday -At '05:00'
    }
)

foreach ($definition in $definitions) {
    $taskName = "ARCWorks $($definition.Name)"
    if ($PSCmdlet.ShouldProcess($taskName, 'Register scheduled task')) {
        Register-ScheduledTask -TaskName $taskName -Action $definition.Action -Trigger $definition.Trigger -Principal $principal -Settings $settings -Force | Out-Null
        if (-not $Enable) { Disable-ScheduledTask -TaskName $taskName | Out-Null }
    }
}

$staleErrorLog = Join-Path $RuntimeRoot 'logs\task-registration-error.log'
if (Test-Path -LiteralPath $staleErrorLog) { Remove-Item -LiteralPath $staleErrorLog -Force }

Get-ScheduledTask -TaskName 'ARCWorks Backup -*' |
    Select-Object TaskName, State |
    Sort-Object TaskName
