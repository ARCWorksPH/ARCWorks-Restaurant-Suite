# ARCWorks backup and recovery

This directory contains the sanitized, version-controlled control plane for
the ARCWorks backup system. Runtime passwords, repositories, database dumps,
logs, and restored data are deliberately outside Git.

## Storage topology

| Volume | Role | Physical boundary |
|---|---|---|
| `D:` | Live ROMS, Docker Desktop storage, and project files | Source disk 0 |
| `F:` | Temporary consistency-capture staging | Disk 3, shared with `H:` |
| `H:` | Primary encrypted Restic repository | Disk 3, shared with `F:` |
| `G:` | Secondary encrypted replication repository and future cloud spool | Disk 2, shared with `I:` |
| `I:` | EaseUS images and isolated restore-test targets | Disk 2, shared with `G:` |

Partitions on the same physical disk are not independent copies. The required
off-site copy is a separate Restic repository on a real remote server. It stays
disabled until an endpoint and independently protected credentials are
configured.

## Captured data

- Transaction-consistent ROMS MariaDB dump.
- Zabbix PostgreSQL custom-format dump.
- ROMS source, untracked working evidence, configuration, and documentation.
- Monitoring and portfolio sources, excluding live database files and caches.
- Daily Codex continuity state from `C:\Users\GBServerPH\.codex`, including
  sessions, archived sessions, memories, skills, instructions, configuration,
  and the session index. Authentication files matching `auth.json*`, sandbox
  secrets, replaceable caches, and interactive-control state are excluded.
- Redacted Docker, Git, Windows, drive, and WSL inventories.
- SHA-256 manifest of every staged file.

For portable instances, set `InstanceId`, `RomsRoot`, and the per-instance
backup host in `backup.config.psd1`. The ROMS database container may be left
blank so the backup script resolves the uniquely labelled Compose service
(`com.arcworks.instance` + `com.arcworks.service=db`). Keep Zabbix explicit
when it lives in the separate monitoring stack.

The process never backs up a live database directory as its database recovery
method. Weekly EaseUS imaging protects the whole-machine state separately.

## Schedule

| Task | Default |
|---|---|
| Database-only capture | Every 6 hours at 00:00, 06:00, 12:00, and 18:00; prompt at T-30 minutes |
| Full data capture | Daily at 01:15; prompt at 00:45 |
| Repository maintenance | Sunday at 03:30; prompt at 03:00 |
| Isolated database/full restore drill | Sunday at 05:00; prompt at 04:30 |
| EaseUS/WinPE image | Weekly maintenance window, configured in EaseUS |

Retention begins at 48 hourly, 14 daily, 8 weekly, 12 monthly, and 2 yearly
snapshots. Pruning occurs only in the weekly maintenance task, after successful
replication.

The scheduled wrapper shows a 30-minute prompt. Database-only captures have
Confirm and Delay buttons; Confirm waits until the scheduled time, while Delay
skips that occurrence and leaves the next database slot intact. Daily full,
maintenance, and recovery operations have a Confirm-only notice with no skip
option. They run at the scheduled time even if the prompt is closed or receives
no response. If the prompt cannot be displayed, all scheduled work proceeds at
its scheduled time. The daily full capture fails closed if required Codex continuity content is
missing. Database-only captures do not duplicate the Codex tree; its required
recovery point objective is one successful copy per day.

Full and weekly operations write an advisory maintenance marker under the
runtime `state` directory for dashboards and audit logs. The current ROMS
application has no maintenance/read-only gate, so the scheduler does not stop
Docker containers or reject live orders automatically. Database dumps are
transaction-consistent; operators should avoid planned data changes during the
confirmed full/recovery window until an application gate is added.

`G:` is currently a second local encrypted Restic repository, not a Nextcloud
or off-site destination. A Nextcloud server database must be dumped on that
server and backed up to storage outside its own data directory and database
volume. Synchronizing a repository back into the same Nextcloud storage does
not create an independent backup.

## Bootstrap

Run from an elevated PowerShell 7 session:

```powershell
pwsh -File .\deploy\backup\Initialize-ARCWorksBackup.ps1
pwsh -File .\deploy\backup\Invoke-ARCWorksBackup.ps1 -Mode Full
pwsh -File .\deploy\backup\Test-ARCWorksRestore.ps1 -ValidateDatabases
pwsh -File .\deploy\backup\Register-ARCWorksBackupTasks.ps1
```

Initialization copies the installed Restic binary to a stable operational
path, creates restricted runtime folders, generates two independent repository
passwords without printing them, and initializes the local repositories. The
password files must be copied to a password manager and offline recovery medium
before the backup is considered production-ready.

## Recovery order

1. Connect to the local or remote Restic repository using an offline recovery
   password.
2. Restore a selected snapshot into a clean target, never over production.
3. Verify the SHA-256 manifest.
4. Restore MariaDB and PostgreSQL into disposable containers and validate them.
5. Rebuild the target server, restore configuration and Data Protection keys,
   and repeat private acceptance before reconnecting public traffic.

`Test-ARCWorksRestore.ps1` automates steps 2 through 4 without modifying the
live containers or volumes.

## EaseUS and WinPE

`I:\ARCWorks_EaseUS_Images` is reserved for weekly system/disk images. Create
and boot-test a WinPE rescue USB, include storage/network drivers, run EaseUS
image validation after every backup, and never include `G:` or `I:` as source
volumes in their own image job. A weekly image should be preceded by fresh
database dumps and a graceful Docker/WSL maintenance stop.

## Safety properties

- A global mutex prevents overlapping runs.
- Drive labels, source paths, containers, Restic version, and repository access
  are checked before capture.
- Database dump or manifest failure aborts the snapshot.
- Failed staging data is retained for diagnosis.
- Successful staging is deleted only after local backup, secondary copy, and
  repository checks succeed.
- Cloud replication fails closed when no real endpoint is configured.
- Secrets are never printed or committed.
