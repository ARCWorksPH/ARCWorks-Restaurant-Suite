# ARCWorks Overwrite and Damaged-Data Recovery Drill — Evidence

**Date:** 2026-08-08
**Scope:** Disposable restore copies only
**Application runtime restore:** Not performed
**Result:** Passed

## Safety boundary

The live project, live Docker volumes, live MariaDB/PostgreSQL containers,
Cloudflare tunnel, monitoring stack, Restic snapshots, and accepted normal-drill
evidence targets were not modified.

All damage and overwrite operations occurred under:

`I:\ARCWorks_Restore_Tests`

## Scenario 1 — overwrite populated instance

Source snapshot: `65857103`
Target:
`I:\ARCWorks_Restore_Tests\overwrite-scenario-1`

Controlled damage before overwrite:

- Moved `sources\roms\README.md` out of the target.
- Added a simulated damage line to `sources\roms\PROJECT_TIMELINE.md`.

The target was then restored over using snapshot `65857103`.

Results:

- Missing README restored.
- Modified timeline file repaired to its snapshot content.
- Marker 1 present.
- Marker 2 absent.
- Manifest records: 992.
- Manifest mismatches: 0.
- MariaDB tables: 24.
- PostgreSQL tables: 203.

## Scenario 2 — damaged instance with quarantine

Source snapshot: `6cb302cf`
Target:
`I:\ARCWorks_Restore_Tests\overwrite-scenario-2`

Controlled damage before overwrite:

- Moved `sources\roms\README.md` out of the target.
- Moved `databases\zabbix-postgresql.dump` out of the target.
- Added a simulated damage line to `sources\roms\PROJECT_TIMELINE.md`.
- Added `sources\roms\docs\EXTRA-DATA-FOR-QUARANTINE-TEST.txt`.

The target was then restored over using snapshot `6cb302cf`.

Results:

- Missing README restored.
- Missing PostgreSQL dump restored.
- Modified timeline file repaired to its snapshot content.
- Marker 1 absent.
- Marker 2 present.
- Manifest records: 992.
- Manifest mismatches: 0.
- MariaDB tables: 24.
- PostgreSQL tables: 203.
- Extra data found: 1 item.
- Extra data permanently deleted: 0 items.

Quarantine location:

`I:\ARCWorks_Restore_Tests\overwrite-scenario-2\QUARANTINE\20260808T050642Z`

The quarantine inventory records the original relative path, quarantine path,
size, SHA-256 hash, timestamp, and reason. The original damaged files moved to
the temporary damage-holding area are also retained for audit review:

`I:\ARCWorks_Restore_Tests\damage-holding-20260808`

## Conclusion

The overwrite and damaged-data recovery process passed both required scenarios.
Expected missing and modified data was repaired from the selected snapshot.
Unexpected data was detected and quarantined without permanent deletion.
Both database dumps remained restorable in disposable containers.

This drill proves data-level overwrite and quarantine behavior on the current
PC. It does not replace the planned cross-PC runtime recovery drill.
