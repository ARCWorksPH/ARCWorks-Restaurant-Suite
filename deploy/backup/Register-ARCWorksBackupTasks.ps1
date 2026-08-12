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

foreach ($name in @('Invoke-ARCWorksBackup.ps1', 'Test-ARCWorksRestore.ps1', 'Invoke-ARCWorksScheduledBackup.ps1')) {
    $source = Join-Path $ScriptSource $name
    if (-not (Test-Path -LiteralPath $source -PathType Leaf)) { throw "Required script is missing: $source" }
    Copy-Item -LiteralPath $source -Destination (Join-Path $runtimeScripts $name) -Force
}

$scheduledScript = Join-Path $runtimeScripts 'Invoke-ARCWorksScheduledBackup.ps1'
$sixHourPromptTriggers = @('05:30', '11:30', '17:30', '23:30' | ForEach-Object {
    New-ScheduledTaskTrigger -Daily -At ([DateTime]::ParseExact($_, 'HH:mm', [Globalization.CultureInfo]::InvariantCulture))
})

$definitions = @(
    @{
        Name = 'Backup - Every 6 Hours Databases'
        Action = New-ScheduledTaskAction -Execute $pwsh -Argument "-STA -NoLogo -NoProfile -NonInteractive -ExecutionPolicy Bypass -File `"$scheduledScript`" -Mode DatabaseOnly -ScheduleKind SixHourly -RunAt 00:00"
        Trigger = $sixHourPromptTriggers
    },
    @{
        Name = 'Backup - Daily Full'
        Action = New-ScheduledTaskAction -Execute $pwsh -Argument "-STA -NoLogo -NoProfile -NonInteractive -ExecutionPolicy Bypass -File `"$scheduledScript`" -Mode Full -ScheduleKind Daily -RunAt 01:15"
        Trigger = New-ScheduledTaskTrigger -Daily -At '00:45'
    },
    @{
        Name = 'Backup - Weekly Maintenance'
        Action = New-ScheduledTaskAction -Execute $pwsh -Argument "-STA -NoLogo -NoProfile -NonInteractive -ExecutionPolicy Bypass -File `"$scheduledScript`" -Mode Maintenance -ScheduleKind Weekly -RunAt 03:30"
        Trigger = New-ScheduledTaskTrigger -Weekly -WeeksInterval 1 -DaysOfWeek Sunday -At '03:00'
    },
    @{
        Name = 'Backup - Weekly Restore Drill'
        Action = New-ScheduledTaskAction -Execute $pwsh -Argument "-STA -NoLogo -NoProfile -NonInteractive -ExecutionPolicy Bypass -File `"$scheduledScript`" -Mode Restore -ScheduleKind Weekly -RunAt 05:00"
        Trigger = New-ScheduledTaskTrigger -Weekly -WeeksInterval 1 -DaysOfWeek Sunday -At '04:30'
    }
)

foreach ($definition in $definitions) {
    $taskName = "ARCWorks $($definition.Name)"
    if ($PSCmdlet.ShouldProcess($taskName, 'Register scheduled task')) {
        Register-ScheduledTask -TaskName $taskName -Action $definition.Action -Trigger $definition.Trigger -Principal $principal -Settings $settings -Force | Out-Null
        if (-not $Enable) { Disable-ScheduledTask -TaskName $taskName | Out-Null }
    }
}

$staleTask = 'ARCWorks Backup - Hourly Databases'
if ($PSCmdlet.ShouldProcess($staleTask, 'Unregister obsolete scheduled task') -and (Get-ScheduledTask -TaskName $staleTask -ErrorAction SilentlyContinue)) {
    Unregister-ScheduledTask -TaskName $staleTask -Confirm:$false
}

$staleErrorLog = Join-Path $RuntimeRoot 'logs\task-registration-error.log'
if (Test-Path -LiteralPath $staleErrorLog) { Remove-Item -LiteralPath $staleErrorLog -Force }

Get-ScheduledTask -TaskName 'ARCWorks Backup -*' |
    Select-Object TaskName, State |
    Sort-Object TaskName
