[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateSet('DatabaseOnly', 'Full', 'Maintenance', 'Restore')]
    [string]$Mode,
    [Parameter(Mandatory)]
    [ValidatePattern('^(?:[01]\d|2[0-3]):[0-5]\d$')]
    [string]$RunAt,
    [ValidateSet('Daily', 'Weekly', 'SixHourly')]
    [string]$ScheduleKind = 'Daily',
    [string]$RuntimeRoot = 'C:\ProgramData\ARCWorks\Backup',
    [ValidateRange(60, 3600)][int]$PromptTimeoutSeconds = 1800,
    [switch]$SkipPrompt
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$logDirectory = Join-Path $RuntimeRoot 'logs'
$stateDirectory = Join-Path $RuntimeRoot 'state'
$logPath = Join-Path $logDirectory 'scheduled-backup.log'
$backupScript = Join-Path $RuntimeRoot 'scripts\Invoke-ARCWorksBackup.ps1'
$restoreScript = Join-Path $RuntimeRoot 'scripts\Test-ARCWorksRestore.ps1'
$friendlyName = @{
    DatabaseOnly = 'database backup'
    Full = 'daily full backup'
    Maintenance = 'repository maintenance'
    Restore = 'weekly recovery drill'
}[$Mode]

New-Item -ItemType Directory -Path $logDirectory, $stateDirectory -Force | Out-Null

function Write-ScheduleLog([string]$Message, [ValidateSet('INFO', 'WARN', 'ERROR')][string]$Level = 'INFO') {
    $line = '{0:o} [{1}] {2}' -f [DateTimeOffset]::Now, $Level, $Message
    Add-Content -LiteralPath $logPath -Value $line -Encoding utf8
}

function Get-ScheduledOccurrence {
    $parsed = [DateTime]::ParseExact($RunAt, 'HH:mm', [Globalization.CultureInfo]::InvariantCulture)
    $now = Get-Date
    if ($ScheduleKind -eq 'SixHourly') {
        $reference = $now.Date.Add($parsed.TimeOfDay)
        $candidates = 0..3 | ForEach-Object { $reference.AddHours(6 * $_) }
        $candidate = @($candidates | Where-Object { $_ -gt $now.AddSeconds(15) } | Select-Object -First 1)
        if ($candidate.Count -eq 0) { $candidate = $reference.AddDays(1) }
        return [DateTime]$candidate[0]
    }
    if ($ScheduleKind -eq 'Daily') {
        $candidate = $now.Date.Add($parsed.TimeOfDay)
        if ($candidate -le $now.AddSeconds(-15)) { $candidate = $candidate.AddDays(1) }
        return $candidate
    }

    $daysUntilSunday = ([int][DayOfWeek]::Sunday - [int]$now.DayOfWeek + 7) % 7
    $candidate = $now.Date.AddDays($daysUntilSunday).Add($parsed.TimeOfDay)
    if ($candidate -le $now.AddSeconds(-15)) { $candidate = $candidate.AddDays(7) }
    return $candidate
}

function Show-BackupPrompt([DateTime]$ScheduledAt) {
    Add-Type -AssemblyName System.Windows.Forms
    Add-Type -AssemblyName System.Drawing

    $databaseCanDelay = $Mode -eq 'DatabaseOnly'
    $promptPolicy = if ($databaseCanDelay) {
        'Confirm waits for that time and runs it. Delay skips this occurrence and waits for the next database slot. No response in 30 minutes: the database capture runs.'
    } else {
        'Confirm acknowledges the scheduled maintenance window. There is no Delay option for this operation. No response in 30 minutes: it runs automatically.'
    }
    $state = [hashtable]::Synchronized(@{
        Choice = 'Timeout'
        TimedOut = $false
        Remaining = $PromptTimeoutSeconds
    })
    $form = [Windows.Forms.Form]::new()
    $form.Text = 'ARCWorks backup confirmation'
    $form.StartPosition = 'CenterScreen'
    $form.Size = [Drawing.Size]::new(560, 280)
    $form.MinimumSize = $form.Size
    $form.MaximizeBox = $false
    $form.MinimizeBox = $false
    $form.TopMost = $true
    $form.FormBorderStyle = [Windows.Forms.FormBorderStyle]::FixedDialog

    $label = [Windows.Forms.Label]::new()
    $label.Location = [Drawing.Point]::new(24, 20)
    $label.Size = [Drawing.Size]::new(500, 130)
    $label.AutoSize = $false
    $label.Font = [Drawing.Font]::new('Segoe UI', 10)
    $label.Text = "ARCWorks is preparing a $friendlyName.`r`n`r`nScheduled start: $($ScheduledAt.ToString('yyyy-MM-dd HH:mm'))`r`n`r`n$promptPolicy"

    $countdown = [Windows.Forms.Label]::new()
    $countdown.Location = [Drawing.Point]::new(24, 158)
    $countdown.Size = [Drawing.Size]::new(500, 24)
    $countdown.Font = [Drawing.Font]::new('Segoe UI', 9)

    $confirm = [Windows.Forms.Button]::new()
    $confirm.Text = 'Confirm'
    $confirm.Size = [Drawing.Size]::new(120, 36)
    $confirm.Location = if ($databaseCanDelay) { [Drawing.Point]::new(270, 195) } else { [Drawing.Point]::new(220, 195) }

    $confirm.Add_Click({ $state.Choice = 'Confirm'; $form.Close() })
    if ($databaseCanDelay) {
        $delay = [Windows.Forms.Button]::new()
        $delay.Text = 'Delay'
        $delay.Size = [Drawing.Size]::new(120, 36)
        $delay.Location = [Drawing.Point]::new(405, 195)
        $delay.Add_Click({ $state.Choice = 'Delay'; $form.Close() })
        $form.Controls.AddRange(@($label, $countdown, $confirm, $delay))
    } else {
        $form.Controls.AddRange(@($label, $countdown, $confirm))
    }
    $form.Add_FormClosing({
        if (-not $state.TimedOut -and $state.Choice -eq 'Timeout' -and $_.CloseReason -eq [Windows.Forms.CloseReason]::UserClosing) {
            $state.Choice = if ($databaseCanDelay) { 'Delay' } else { 'Confirm' }
        }
    })

    $timer = [Windows.Forms.Timer]::new()
    $timer.Interval = 1000
    $timer.Add_Tick({
        $state.Remaining--
        $countdown.Text = "Prompt closes in $($state.Remaining) seconds."
        if ($state.Remaining -le 0) {
            $state.TimedOut = $true
            $form.Close()
        }
    })
    $countdown.Text = "Prompt closes in $($state.Remaining) seconds."
    $timer.Start()
    try { [void]$form.ShowDialog() } finally {
        $timer.Stop()
        $timer.Dispose()
        $form.Dispose()
    }
    return $state.Choice
}

function Write-MaintenanceMarker([string]$Status, [DateTime]$ScheduledAt, [string]$Message) {
    $markerPath = Join-Path $stateDirectory 'maintenance-window.json'
    $historyPath = Join-Path $stateDirectory 'last-maintenance-window.json'
    $record = [ordered]@{
        TimestampUtc = [DateTime]::UtcNow.ToString('o')
        ScheduledAtLocal = $ScheduledAt.ToString('o')
        Mode = $Mode
        Status = $Status
        Enforcement = 'Advisory; the ROMS application has no maintenance/read-only gate.'
        Message = $Message
    }
    $record | ConvertTo-Json | Set-Content -LiteralPath $historyPath -Encoding utf8NoBOM
    if ($Status -eq 'Active') {
        $record | ConvertTo-Json | Set-Content -LiteralPath $markerPath -Encoding utf8NoBOM
    } elseif (Test-Path -LiteralPath $markerPath) {
        Remove-Item -LiteralPath $markerPath -Force
    }
}

try {
    $scheduledAt = Get-ScheduledOccurrence
    Write-ScheduleLog "Prompt opened for $Mode; scheduled start $($scheduledAt.ToString('o'))."

    $choice = 'Confirm'
    if (-not $SkipPrompt) {
        try {
            $choice = Show-BackupPrompt $scheduledAt
            if ($choice -eq 'Timeout') { $choice = 'Confirm' }
        } catch {
            Write-ScheduleLog "Interactive prompt was unavailable; proceeding at the scheduled time. $($_.Exception.Message)" 'WARN'
            $choice = 'Confirm'
        }
    }

    if ($choice -ne 'Confirm') {
        Write-ScheduleLog "Operator delayed $Mode scheduled for $($scheduledAt.ToString('o')); no work was started."
        return
    }

    while ((Get-Date) -lt $scheduledAt) {
        $remainingSeconds = [int]($scheduledAt - (Get-Date)).TotalSeconds
        Start-Sleep -Seconds ([Math]::Min(5, [Math]::Max(1, $remainingSeconds)))
    }

    $markerNeeded = $Mode -ne 'DatabaseOnly'
    if ($markerNeeded) {
        try { Write-MaintenanceMarker 'Active' $scheduledAt 'Confirmed backup/recovery window started.' } catch { Write-ScheduleLog "Could not write maintenance marker: $($_.Exception.Message)" 'WARN' }
    }

    if ($Mode -eq 'Restore') {
        & $restoreScript -ValidateDatabases
    } else {
        & $backupScript -Mode $Mode
    }
    if ($markerNeeded) {
        try { Write-MaintenanceMarker 'Completed' $scheduledAt 'Confirmed backup/recovery window completed.' } catch { Write-ScheduleLog "Could not finalize maintenance marker: $($_.Exception.Message)" 'WARN' }
    }
    Write-ScheduleLog "$Mode completed successfully."
} catch {
    if ($Mode -ne 'DatabaseOnly') {
        try { Write-MaintenanceMarker 'Failed' (Get-ScheduledOccurrence) $_.Exception.Message } catch { }
    }
    Write-ScheduleLog $_.Exception.Message 'ERROR'
    throw
}
