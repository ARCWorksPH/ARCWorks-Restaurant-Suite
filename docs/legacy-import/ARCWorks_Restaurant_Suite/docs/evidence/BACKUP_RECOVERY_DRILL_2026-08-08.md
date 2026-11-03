# ARCWorks Normal Restore Drill — Evidence

**Date:** 2026-08-08
**Scope:** Isolated file, manifest, and database comparison
**Application runtime restore:** Not performed by design; reserved for the cross-PC drill
**Result:** Passed after correcting a backup source-path configuration error
## Baseline

- Active project: `D:\ARCWorks_Restaurant_Suite`
- Git commit at baseline: `66eb9b04e46eb224f37fa4624d580362f99fffc5`
- Application health endpoint: HTTP 200
- Live Docker services remained running and healthy throughout the drill.
- Backup destinations were available: `F:`, `G:`, `H:`, and `I:`.

## Configuration issue found and corrected

The installed runtime configuration initially pointed `RomsRoot` to the retired
space-named directory `D:\ARCWorks_Restaurant Suite`, while the active project
was `D:\ARCWorks_Restaurant_Suite`. The first two captures therefore did not
represent the active project and were excluded from acceptance evidence.

Those snapshots were retained and not deleted. The original runtime
configuration was preserved as a protected pre-drill copy before changing only
`RomsRoot` to the active underscore-named path.

## Accepted snapshots

| Point | Local snapshot | Replication snapshot | Source run | Marker expectation |
|---|---|---|---|---|
| Corrected 1 | `65857103` | `e218e97d` | `20260808T041310Z-Full` | Marker 1 present; marker 2 absent |
| Corrected 2 | `6cb302cf` | `b78fee80` | `20260808T041449Z-Full` | Marker 1 absent; marker 2 present |

Both captures included the active project, monitoring source, portfolio source,
Codex continuity data with secret exclusions, redacted infrastructure metadata,
database dumps, and a SHA-256 manifest containing 992 records.

The backup script reported that the remote cloud repository was not configured;
off-site replication was not part of this local drill.

## Snapshot 1 — ideal isolated restore

Restore target:

`I:\ARCWorks_Restore_Tests\20260808T041700Z-corrected-snapshot1`

Results:

- Marker 1: present
- Marker 1 SHA-256: `131CCDC1D49011BCC8F5450D3857482F2B6523BC7BE5C6C7490E528E032CEDBD`
- Marker 2: absent
- Manifest records: 992
- Manifest mismatches: 0
- MariaDB tables restored: 24
- PostgreSQL tables restored: 203
- Database validation used disposable containers only

## Snapshot 2 — interrupted restore recovery

Restore target:

`I:\ARCWorks_Restore_Tests\20260808T042000Z-interrupted-snapshot2`

The restore was intentionally stopped after approximately 12 seconds. The
target contained 227 partial files and the restore process exited with a
controlled interruption. The same snapshot was then restored again against the
partial target. Restic completed successfully and skipped 223 files that were
already present.

Results after recovery:

- Marker 1: absent
- Marker 2: present
- Marker 2 SHA-256: `052A97F6F9A56B5205016CED400760A92A539FDA96D44891B6DEF02247B933F1`
- Manifest records: 992
- Manifest mismatches: 0
- MariaDB tables restored: 24
- PostgreSQL tables restored: 203
- Database validation used disposable containers only

## Safety observations

- The live project was never overwritten.
- Live Docker volumes were never removed.
- Live MariaDB and PostgreSQL containers were never imported into.
- The Cloudflare tunnel was not changed.
- The live application remained healthy at the final check (HTTP 200).
- Disposable validation containers were removed after each database check.
- The first invalid-path snapshots and the old isolated restore target were
  retained for audit review and were not used as acceptance evidence.

## Conclusion

The corrected backup configuration passed both required scenarios:

1. A normal isolated restore from the earlier project state.
2. Recovery after an interrupted isolated restore from the later project state.

The drill proves backup content integrity and logical database recoverability on
the current PC. It does not prove that the restored application can run on a
different machine; that remains the planned cross-PC recovery drill before beta.
