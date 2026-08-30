# Storage Migration Checkpoint — 2026-08-30

## Scope approved by the project owner

1. Preserve all ARCWorks Restaurant Suite data on the temporary D: drive.
2. Retire the unused Nextcloud project and remove its active installation and
   leftovers without disturbing components used by ROMS.
3. Repartition and clean the approved project drives only after preservation is
   verified.
4. Rebuild the project on a new primary-plus-backup layout.

## Completed in the current checkpoint

- Created the ACL-restricted staging root
  `D:\ARCWorks_Migration_Staging_20260830`.
- Captured and validated a live ROMS MariaDB logical dump:
  `database-checkpoint\roms-mariadb-20260830.sql`.
- Copied F: backup staging, Docker recovery, historical Codex continuity, and
  the Codex application package into the staging root.
- Copied both G:/H: Restic repositories. Both staged copies passed `restic
  check --no-cache` with all 47 snapshots and no errors.
- Copied I: restore-test evidence and the `CODEX_RAW`, `Recovered`, and `CODEX`
  continuity collections.
- Copied `C:\ProgramData\ARCWorks\Backup`, including the protected Restic key
  files, into the ACL-restricted staging root.
- Verified exact source/destination file counts and byte totals for every
  completed copy. The verified set contained 49,711 files and approximately
  95.603 GiB before the small ProgramData copy was added.
- Unregistered the Nextcloud-only Ubuntu WSL distribution. Its approximately
  4.753 GiB VHDX was removed. The separate `docker-desktop` distribution and
  active ROMS containers were not changed.

## Confirmed architectural boundary

The retired Nextcloud instance used SQLite at
`G:\NextCloud\data\nextcloud.db`. The standalone Windows PostgreSQL 18 service
on G: is therefore not the active Nextcloud database. It must be preserved and
stopped before G: is repartitioned, but it must not be described as a confirmed
Nextcloud component.

## Administrator step still required

The current Codex process was denied permission to disable scheduled tasks,
stop PostgreSQL, or delete the remaining G: files. Run the reviewed script from
an elevated PowerShell window:

```powershell
& "D:\ARCWorks_Restaurant_Suite_Gate1_Deploy\00_PROJECT_CONTROL\storage\Complete-ARCWorksMigrationAdminSteps.ps1" -Execute
```

The script performs only the following bounded operations:

- disables the four named ARCWorks backup schedules during migration;
- stops `pgagent-pg18` and `postgresql-x64-18`;
- copies and verifies `G:\PostgreSQL`, `G:\PostGIS`, and `G:\StackBuilder` into
  the protected D: staging root;
- removes the confirmed G: Nextcloud data/proxy residue;
- removes the obsolete `PostgreSQL - WSL2 only` firewall rules;
- verifies that no active Ubuntu/Nextcloud target, service, or port proxy
  remains.

PostgreSQL is intentionally not uninstalled by this script. Its staged copy and
ownership must be reviewed before the later G: repartition action.

## Prohibited next actions

Do not delete the original F:/G:/H:/I: preservation sources and do not alter any
partition yet. The administrator script must pass, the staged PostgreSQL copy
must be checked, and the unrelated `I:\ARK Survival Ascended` data must receive
an explicit keep/archive/delete decision first.

