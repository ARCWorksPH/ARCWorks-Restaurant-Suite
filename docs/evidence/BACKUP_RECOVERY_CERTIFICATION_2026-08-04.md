# Backup and recovery certification — 2026-08-04

## Outcome

The ARCWorks local backup and recovery control plane passed its first end-to-end certification on the production workstation. Transaction-consistent database dumps, a complete daily file capture, two encrypted Restic repositories, manifest verification, isolated database restoration, and Windows Task Scheduler execution were all exercised.

This certification does **not** claim off-site protection. The remote cloud repository is deliberately disabled until a verified endpoint and independent credentials are configured.

## Certified topology

| Location | Role | Result |
|---|---|---|
| `F:\ARCWorks_Backup_Staging` | Temporary consistency-capture area | Passed; successful staging removed after certification |
| `H:\ARCWorks_Restic_Local` | Primary encrypted Restic repository | Passed |
| `G:\ARCWorks_Restic_Replication` | Secondary local encrypted Restic repository | Passed |
| `I:\ARCWorks_Restore_Tests` | Isolated restore-test target | Passed; latest certified full restore retained |
| Remote/Nextcloud | Independent off-site repository | Not configured; fail-closed |

`F:` and `H:` share one physical disk. `G:` and `I:` share another physical disk. These partitions are useful operational copies but are not four independent failure domains.

## Captured sources

- ROMS MariaDB transactional dump.
- Zabbix PostgreSQL custom-format dump.
- ROMS source, configuration, documentation, and evidence.
- Monitoring and portfolio source/configuration data, excluding live database directories and replaceable caches.
- Daily Codex continuity from `C:\Users\GBServerPH\.codex`:
  - sessions and archived sessions;
  - memories;
  - skills and instructions;
  - `AGENTS.md`, `config.toml`, global state, and session index.
- Sanitized infrastructure metadata.
- SHA-256 manifest.

Codex authentication files matching `auth.json*`, `.sandbox-secrets`, and replaceable interactive/cache data are excluded. The full capture fails if required Codex continuity content is absent.

## Evidence

### Database-only backup and restore

- Primary snapshot: `c3319517`.
- Replication snapshot: `9d3c1abd`.
- MariaDB dump validation: passed.
- PostgreSQL custom-dump validation: passed.
- Restic repository checks: passed.
- Manifest verification: passed.
- Isolated MariaDB restore: 22 tables.
- Isolated PostgreSQL restore: 203 tables.
- Observed recovery-drill duration: approximately 2 minutes 33 seconds.

### Full daily backup and restore

- Primary snapshot: `5350d611`.
- Replication snapshot: `0fb365ff`.
- Manifest: 1,715 records.
- Independent manifest result: 1,715 verified, 0 failures.
- Required Codex continuity paths: all present.
- Forbidden Codex auth/sandbox-secret files found: 0.
- Restored size on I:: approximately 13.403 GiB.
- Restic latest-snapshot stored size: approximately 7.332 GiB per repository.
- Isolated MariaDB restore: 22 tables.
- Isolated PostgreSQL restore: 203 tables.
- Observed full recovery-drill duration: approximately 8 minutes 33 seconds.

The large daily set is intentional. Most bulk comes from AI benchmark recordings, the ROMS walkthrough, and portfolio media/build artifacts. No Docker VHDX, Restic repository, prior staging tree, or live database directory was included.

### Scheduled execution

- Task: `ARCWorks Backup - Hourly Databases`.
- Manual Task Scheduler launch: passed.
- Task Scheduler result: `0`.
- Scheduler-created primary snapshot: `aa7bd027`.
- Scheduler-created replication snapshot: `d4a99ad9`.
- Observed scheduled backup duration: approximately 23 seconds.
- State file advanced and reported `Status = Success`.
- Both live database containers remained healthy.

## Active schedule

| Task | Schedule |
|---|---|
| Hourly database capture | 08:00 through 23:00 daily |
| Full data and Codex continuity capture | 01:15 daily |
| Repository maintenance and bounded data check | 03:30 Sunday |
| Full isolated restore/database drill | 05:00 Sunday |

Tasks run with the logged-in `GBServerPH` interactive token at highest privileges. This is intentional because Docker Desktop named-pipe access is session-bound. Missed tasks use Start When Available, and overlapping task instances are refused.

Successful restore-test retention is bounded to the two newest certified restore directories. Failed restore evidence is retained for diagnosis until reviewed.

## Recovery-key gate

The two Restic password files remain outside Git under the protected runtime control directory. Before declaring off-machine disaster recovery ready, copy both repository passwords to:

1. an approved password manager; and
2. one offline recovery medium.

Repository contents cannot be restored without the corresponding password.

## Nextcloud and off-site boundary

G: is not routed to Nextcloud. Copying G: into a Nextcloud data directory hosted on the same storage would create recursion and no independent failure domain.

If Nextcloud is selected later:

1. run its PostgreSQL dump on the Nextcloud server;
2. capture Nextcloud configuration and data consistently;
3. store that server backup outside its own PostgreSQL volume and Nextcloud data directory;
4. replicate ARCWorks Restic data to a dedicated remote repository path that is not itself part of the source set; and
5. test recovery from the remote copy before calling it off-site protection.

## Remaining gates

- Export both repository passwords to password-manager and offline custody.
- Configure and test a genuinely independent off-site repository.
- Complete the weekly EaseUS image and WinPE boot/restore test.
- Add Zabbix alerts for stale `last-success.json`, failed task results, and repository capacity.
- Begin the modular and portable project migration only after this recovery baseline is preserved.
