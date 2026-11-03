# ARCWorks Normal Restore Drill — Preparation Record

**Status:** Prepared; execution pending operator confirmation
**Scope:** Isolated file/manifest/database comparison only
**Live-system restore:** Prohibited
**Prepared:** 2026-08-08

## Objective

Verify that two consecutive full backup snapshots preserve the expected project
states without overwriting the live application, Docker volumes, or live
databases.

This drill intentionally does **not** start the restored application. Runtime
acceptance will be performed later during the cross-PC recovery drill.

## Recovery boundaries

| Resource | Live location | Drill behavior |
|---|---|---|
| Project | `D:\ARCWorks_Restaurant_Suite` | Read-only comparison target; never overwritten |
| Backup staging | `F:\ARCWorks_Backup_Staging` | Used by the normal backup script |
| Local Restic repository | `H:\ARCWorks_Restic_Local` | Source of isolated restores |
| Replication repository | `G:\ARCWorks_Restic_Replication` | Checked as part of backup completion |
| Restore targets | `I:\ARCWorks_Restore_Tests` | Dedicated isolated directories |
| Live Docker volumes | Docker-managed | Never removed or replaced |
| Live databases | Running MariaDB/PostgreSQL containers | Never imported into |

## Test sequence

### Snapshot 1

1. Record the baseline project, Git, Docker, and database state.
2. Create `docs/restore-drill-marker-1.md` with a unique timestamp and note.
3. Run a full backup using the installed backup configuration.
4. Record the Restic snapshot ID and validation output.
5. Confirm the backup and repository checks succeeded.

### Snapshot 2

1. Remove only `docs/restore-drill-marker-1.md` from the live project.
2. Create `docs/restore-drill-marker-2.md` with a different unique note.
3. Run a second full backup.
4. Record the second Restic snapshot ID and validation output.
5. Confirm the two snapshot IDs are different.

### Isolated comparisons

Restore each selected snapshot into a separate timestamped directory beneath
`I:\ARCWorks_Restore_Tests`. The first snapshot must be selected explicitly;
the restore helper's default `latest` selection is not sufficient after the
second snapshot exists.

For snapshot 1, verify:

- marker 1 exists and matches its recorded SHA-256 hash;
- marker 2 is absent;
- the restored SHA-256 manifest validates;
- expected project/configuration/documentation files exist;
- MariaDB dump validation succeeds in a disposable container;
- PostgreSQL dump validation succeeds in a disposable container.

For snapshot 2, verify the inverse marker state in a different restore target.

### Mandatory interrupted-restore scenario

The snapshot-2 restore is deliberately interrupted during extraction, but only
inside its isolated restore directory. The Restic snapshot and live project are
never interrupted or modified.

1. Start restoring snapshot 2 to a fresh target beneath
   `I:\ARCWorks_Restore_Tests`.
2. Interrupt the restore process using the controlled operator stop method and
   record the time, process exit result, and files already written.
3. Confirm that the target is visibly partial and that the live application,
   live databases, and Restic repositories are unchanged.
4. Re-run the same snapshot restore against that partial target, or use a fresh
   recovery target if the restore tool reports that continuation is unsafe.
5. Validate the completed target using the same manifest, marker, MariaDB, and
   PostgreSQL checks as the ideal restore.
6. Record whether recovery from the interrupted state succeeded, which files
   were rewritten, and whether cleanup of partial data was required.

This creates two required restore outcomes:

| Scenario | Snapshot | Condition | Required result |
|---|---|---|---|
| Ideal restore | Snapshot 1 | No interruption | Marker 1 present; marker 2 absent; manifest and database checks pass |
| Interrupted recovery | Snapshot 2 | Restore stopped mid-operation, then recovered | Marker 2 present; marker 1 absent; final manifest and database checks pass |

The interruption must never be simulated by deleting a Restic snapshot,
stopping the live Docker stack, removing live volumes, or modifying the live
project.

## Stop conditions

Stop immediately and retain staging/evidence if:

- a backup or repository check fails;
- a snapshot ID cannot be recorded;
- the marker state is inconsistent;
- a manifest or database validation fails;
- a command targets the live project or live Docker volume;
- a required backup drive is unavailable.

## Evidence to capture

- UTC start/end times for both backups and restores;
- snapshot IDs and repository names;
- baseline Git commit and working-tree status;
- container names and health status (metadata only);
- marker contents and SHA-256 hashes;
- manifest and database validation results;
- restore target paths;
- warnings, failures, and operator actions.

## Cleanup policy

Do not clean up markers, staging, or isolated restore targets until the evidence
has been written and reviewed. Cleanup must use explicit paths only. The live
application and its databases must remain in their post-snapshot-2 state.

## Execution gate

This preparation record does not authorize execution. Proceed only after the
operator confirms that the normal PC workload is ready and explicitly states:

> PC ready — begin normal restore drill
