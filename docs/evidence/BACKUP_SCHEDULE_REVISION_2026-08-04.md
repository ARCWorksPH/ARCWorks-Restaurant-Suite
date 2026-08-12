# Backup schedule revision — 2026-08-04

## Decision

The recurring database capture was changed from hourly to four times per day:

| Operation | Prompt | Scheduled start | Behavior |
|---|---:|---:|---|
| MariaDB/PostgreSQL database-only | 23:30 (previous day) | 00:00 | Online transactional capture |
| MariaDB/PostgreSQL database-only | 05:30 | 06:00 | Online transactional capture |
| MariaDB/PostgreSQL database-only | 11:30 | 12:00 | Online transactional capture |
| MariaDB/PostgreSQL database-only | 17:30 | 18:00 | Online transactional capture |
| Daily full capture | 00:45 | 01:15 | Confirmed maintenance window; advisory marker |
| Weekly repository maintenance | Sunday 03:00 | Sunday 03:30 | Confirmed maintenance window; advisory marker |
| Weekly recovery drill | Sunday 04:30 | Sunday 05:00 | Confirmed maintenance window; advisory marker |

Database-only prompts have explicit **Confirm** and **Delay** buttons and a
30-minute countdown. Confirm waits until the scheduled start. Delay records the
decision and skips only that database occurrence; it does not alter the
recurring schedule. Daily full, weekly maintenance, and weekly recovery have a
Confirm-only notice. They run at the scheduled time even when unattended.

## Safety policy

- A database-only prompt timeout proceeds online because the dump is
  transaction-consistent and missing all four daily captures would unnecessarily
  increase recovery-point risk.
- A full, maintenance, or recovery prompt timeout proceeds at the scheduled
  time; these operations intentionally have no skip option.
- Closing a database prompt is treated as Delay. Closing a full or weekly
  prompt is treated as Confirm.
- If a prompt cannot be displayed, the scheduled operation proceeds rather than
  silently removing the protection window.
- The wrapper never stops Docker, WSL, MariaDB, PostgreSQL, or the ROMS app.
  Full and weekly runs write `maintenance-window.json` while active and record
  the outcome in `last-maintenance-window.json`. This is advisory because the
  ROMS application does not currently implement a maintenance/read-only gate.

## Implementation

- `deploy/backup/Invoke-ARCWorksScheduledBackup.ps1` owns the prompt, database
  delay behavior, scheduled-time wait, audit log, and advisory maintenance marker.
- `deploy/backup/Register-ARCWorksBackupTasks.ps1` registers the prompt tasks
  and removes the obsolete `ARCWorks Backup - Hourly Databases` task.
- Runtime copies are installed under
  `C:\ProgramData\ARCWorks\Backup\scripts` by the registration script.

## Verification boundary

Source PowerShell parsing and task-definition inspection are required after
registration. A live prompt should be tested once manually during a safe window;
this document does not claim that a Windows desktop dialog has been accepted by
an operator until that test is observed. The source change is committed and
pushed; live Task Scheduler registration remains pending one elevated PowerShell
run because the current Codex session does not have an elevated Windows token.
