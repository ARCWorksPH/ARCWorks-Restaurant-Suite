# ARCWorks Overwrite and Damaged-Data Recovery Drill — Preparation

**Status:** Prepared; execution pending operator confirmation
**Scope:** Disposable restore copies only
**Live-system access:** Prohibited
**Permanent deletion:** Prohibited
**Prepared:** 2026-08-08

## Objective

Test recovery when a previously restored instance already contains missing,
modified, or unexpected data. The restore must repair expected files and move
unexpected data to a reversible quarantine area instead of deleting it.

This is a data-validation drill. The restored application will not be started.
The cross-PC drill remains the runtime and portability acceptance test.

## Source snapshots

| Scenario | Snapshot | Accepted source |
|---|---|---|
| 1 — overwrite populated instance | `65857103` | Corrected snapshot 1 |
| 2 — recover damaged instance | `6cb302cf` | Corrected snapshot 2 |

The Restic snapshots are read-only inputs. They must not be forgotten, pruned,
modified, or replaced during this drill.

## Disposable boundaries

Proposed isolated targets:

- `I:\ARCWorks_Restore_Tests\overwrite-scenario-1`
- `I:\ARCWorks_Restore_Tests\overwrite-scenario-2`

The accepted evidence targets from the normal drill remain preserved and are
not used as damage targets. Nothing under the live project, live Docker
volumes, live databases, Cloudflare tunnel, or monitoring stack may be used as
a target.

## Scenario 1 — overwrite populated copy

1. Create a disposable copy restored from snapshot `65857103`.
2. Remove one known expected file from the disposable copy.
3. Modify the contents of another known expected file.
4. Restore snapshot `65857103` over the same disposable target.
5. Validate that the removed file returns and the modified file matches the
   snapshot hash.
6. Validate marker 1 is present and marker 2 is absent.
7. Validate the complete SHA-256 manifest and database dumps.

No unexpected-data quarantine is required for Scenario 1 unless the validator
finds extra files; if it does, the same quarantine policy applies.

## Scenario 2 — damaged copy with quarantine

1. Create a separate disposable copy restored from snapshot `6cb302cf`.
2. Simulate damage only inside that copy:
   - remove an expected project file;
   - alter an expected configuration or documentation file;
   - remove one database dump;
   - add an intentionally incorrect extra file.
3. Restore snapshot `6cb302cf` over the damaged target.
4. Restore missing expected files and repair modified expected files.
5. Compare the final target with the snapshot manifest.
6. Move every item not present in the selected snapshot into:

   `I:\ARCWorks_Restore_Tests\overwrite-scenario-2\QUARANTINE\<UTC-timestamp>\`

7. Write a quarantine inventory containing each item's original relative path,
   quarantine path, size, SHA-256 hash, timestamp, and reason.
8. Validate marker 2 is present and marker 1 is absent.
9. Validate the complete SHA-256 manifest and both disposable database dumps.

Quarantine is reversible. No item may be permanently deleted as part of this
drill.

## Required results

| Check | Scenario 1 | Scenario 2 |
|---|---|---|
| Missing expected file restored | Pass | Pass |
| Modified expected file repaired | Pass | Pass |
| Correct marker state | Pass | Pass |
| Manifest mismatches | 0 | 0 after repair |
| MariaDB dump validation | Pass | Pass |
| PostgreSQL dump validation | Pass | Pass |
| Unexpected data | Report if found | Quarantined and reported |
| Permanent deletion | None | None |

## Stop conditions

Stop immediately if:

- the target resolves outside `I:\ARCWorks_Restore_Tests`;
- the selected snapshot cannot be identified exactly;
- a command would touch the live project or Docker volumes;
- a quarantine move fails or crosses the target boundary;
- a manifest or database validation fails unexpectedly;
- a Restic snapshot or repository operation requests deletion or pruning;
- the source snapshot appears damaged.

Retain the target and logs for diagnosis when a stop condition occurs.

## Evidence

The final evidence report will include:

- source snapshot IDs;
- disposable target paths;
- damage operations performed;
- restored and repaired file counts;
- manifest results;
- database table counts;
- quarantined inventory and hashes;
- timestamps and elapsed time;
- confirmation that live services were not touched.

## Execution gate

This preparation document does not authorize execution. Begin only after the
operator explicitly confirms:

> PC ready — begin overwrite and damaged-data recovery drill
