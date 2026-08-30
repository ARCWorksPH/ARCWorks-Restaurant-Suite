# ARCWorks Project Storage — Read-Only Drive Audit

**Audit date:** 2026-08-30  
**Scope:** Project-approved volumes `D:`, `F:`, `G:`, `H:`, and `I:`  
**Excluded:** Restricted system/personal volumes `C:` and `E:`  
**Change boundary:** Read-only inspection; no file, volume, partition, service,
container, task, repository, or backup mutation

## Outcome

The approved storage contains about **246.1 GiB** of enumerated file data. It
will fit temporarily on `D:` by raw capacity, but the drives are not ready for
partition changes. Active Docker, PostgreSQL, backup, Restic, Git-worktree, and
uncommitted-file dependencies must be preserved or deliberately migrated first.

No top-level folder is approved for deletion by this audit. Several large
generated or historical areas are candidates for later cleanup, but only after
their unique contents and runtime dependencies are captured.

## Physical layout

| Physical disk | Model | Media | Current project volumes | Capacity | State |
| --- | --- | --- | --- | ---: | --- |
| Disk 0 | ST500DM005 HD502HJ | HDD | `D:` | 465.76 GiB | Healthy |
| Disk 2 | WDC WD10JPCX-24UE4T0 | HDD | `G:` + `I:` | 931.51 GiB | Healthy |
| Disk 3 | WDC WD5000AAKX-08U6AA0 | HDD | `F:` + `H:` | 465.76 GiB | Healthy |

All five approved volumes reported `NOT Dirty`. Windows reported all three
physical disks healthy, but detailed SMART temperature, power-on-hour, and error
counters were unavailable through the current non-elevated query. BitLocker
status was also unavailable and remains **unverified**.

## Volume summary

The file totals exclude `$RECYCLE.BIN`, `System Volume Information`, and any
inaccessible or reparse-point content.

| Volume | Label | Partition size | Free space | Enumerated files | Enumerated data |
| --- | --- | ---: | ---: | ---: | ---: |
| `D:` | WORK BENCH | 465.76 GiB | ~325.85 GiB | 80,347 | 136.874 GiB |
| `F:` | BACKUP-STAGING | 275.33 GiB | ~234.67 GiB | 18,448 | 40.454 GiB |
| `G:` | ENCRYPTED-CLOUD-REPO | 150.15 GiB | ~137.42 GiB | 25,828 | 12.482 GiB |
| `H:` | RESTIC LOCAL REPOSITORY | 190.43 GiB | ~179.31 GiB | 829 | 10.973 GiB |
| `I:` | FINAL DESTINATION | 781.25 GiB | ~735.36 GiB | 30,057 | 45.309 GiB |

## D: — active workbench and Docker storage

### Largest top-level areas

| Path | Files | Data | Classification |
| --- | ---: | ---: | --- |
| `OLD-ARCWorks_Restaurant_Suite-REMAINDER` | 12,709 | 49.343 GiB | Historical plus large Docker VHDX and media; inspect before cleanup |
| `OLD-ARCWorks_Restaurant_Suite` | 33,650 | 43.430 GiB | Registered Git worktree with 343 changes; do not delete |
| `ARCWorks_Restaurant Suite` | 2,657 | 38.646 GiB | Current Docker storage and recovery material; active |
| `COMPLEATED PROJECTS` | 13,990 | 2.238 GiB | Separate ARCWorks products; product-owner scope decision required |
| `ARCWorks_Restaurant_Suite` | 3,480 | 0.663 GiB | Live ROMS Compose worktree with 27 changes; active |
| `ARCWorks_Restaurant_Suite_Gate1_Deploy` | 3,639 | 0.580 GiB | Authoritative clean `main` checkout |
| `ARCWorks_Restaurant_Suite_Codex_Waiter_Shell` | 2,173 | 0.577 GiB | Clean historical Git worktree |
| `ARCWorks_Restaurant_Suite_Codex_Landing_Final` | 1,926 | 0.331 GiB | Active preview Compose worktree with 5 untracked assets |
| Grok handoff/review worktrees | ~3,787 | ~0.588 GiB | Historical Git worktrees; preserve until Git consolidation |
| `ARCWorks_AI_Workbench` | 14 | 0.001 GiB | Active isolated Grok Build pilot |

### Docker VHDX concentration

Seven VHDX files account for approximately **110.718 GiB** on `D:`.

- The currently configured Docker Desktop data path is under
  `D:\ARCWorks_Restaurant Suite\Docker\storage\DockerDesktopWSL\disk\DockerDesktopWSL`.
- Its active `docker_data.vhdx` was approximately 22.589 GiB and changed during
  the scan while Docker was running.
- Another older VHDX in the same parent tree was approximately 13.828 GiB.
- Two historical 37.010 GiB VHDX files have identical size and timestamp, but
  were not hashed because reading 74 GiB during the live scan was unnecessary.
  They are duplicate candidates, not confirmed duplicates.

### Git and runtime constraints

- `D:\ARCWorks_Restaurant_Suite_Gate1_Deploy` is the clean authoritative
  `main` checkout.
- `D:\ARCWorks_Restaurant_Suite` is a checkpoint branch worktree with 27
  changes: 21 untracked entries and six tracked deletions. Its untracked content
  includes restaurant assets, final UI references, Waiter mock-ups, menu images,
  audit files, and design notes.
- Production ROMS containers use
  `D:\ARCWorks_Restaurant_Suite\compose.yaml` and bind paths below that worktree.
- `D:\ARCWorks_Restaurant_Suite_Codex_Landing_Final` has five untracked enhanced
  landing assets and supplies the running preview database Compose project.
- `D:\OLD-ARCWorks_Restaurant_Suite` has 343 changes, primarily 341 tracked
  deletions plus two untracked areas. It cannot be treated as a redundant clone
  until the branch and working-tree state are preserved.
- Clean historical worktrees can eventually be removed with Git worktree
  commands after branch, remote, and evidence verification. They should not be
  deleted directly from Explorer.

## F: — staging and Docker recovery

| Path | Files | Data | Classification |
| --- | ---: | ---: | --- |
| `ARCWorks_Docker_Recovery_20260826` | 4 | 22.613 GiB | Recent Docker recovery copy; preserve until Docker migration is proven |
| `ARCWorks_Backup_Staging` | 4,203 | 15.721 GiB | Active backup staging path; retention cleanup only through backup workflow |
| `.codex` | 11,592 | 1.724 GiB | Old Codex continuity copy; compare with current protected continuity data |
| `OpenAI.Codex_2p2nqsd0c76g0` | 2,648 | 0.396 GiB | Old application package data; likely generated, not yet approved for deletion |
| `KATHANA` | 0 | 0 | Empty; deletion candidate after owner confirms scope |
| `New Text Document.txt` | 1 | 0 | Empty; deletion candidate |

`F:\ARCWorks_Backup_Staging` is hard-coded into the active backup configuration.
Completed staging runs may be removable through the backup script's retention
process, but must not be manually pruned while the backup workflow is being
restructured.

## G: — Restic replication and live PostgreSQL

| Path | Data | Classification |
| --- | ---: | --- |
| `ARCWorks_Restic_Replication` | 10.973 GiB | Healthy replicated Restic repository; preserve |
| `PostgreSQL` | 1.273 GiB | Live PostgreSQL 18 installation and data; preserve/migrate intentionally |
| `NextCloud`, `PostGIS`, `StackBuilder`, root proxy scripts | Small | Infrastructure or historical tools; ownership decision required |

The Windows service `postgresql-x64-18` is running from
`G:\PostgreSQL\bin\pg_ctl.exe` with its data directory at
`G:\PostgreSQL\data`. `pgagent-pg18` is configured for the same installation
and is currently stopped. Disk 2 cannot be repartitioned until PostgreSQL has a
verified logical/physical backup, the service is stopped, and a restore target
is prepared.

The volume label contains the word `ENCRYPTED`, but OS-level encryption could
not be verified. The label is not accepted as encryption evidence.

## H: — local Restic repository

`H:\ARCWorks_Restic_Local` contains 829 repository files totaling 10.973 GiB.
It is hard-coded as the active local Restic repository and must be preserved
until the final backup disk is initialized and verified.

## Restic verification

| Repository | Repository identity | Snapshots | First | Latest | Check |
| --- | --- | ---: | --- | --- | --- |
| `H:\ARCWorks_Restic_Local` | Local/original | 47 | 2026-08-03 12:38 | 2026-08-30 12:02 | Passed; no errors |
| `G:\ARCWorks_Restic_Replication` | Separate replicated repository | 47 copied | 2026-08-03 12:38 | 2026-08-30 12:02 | Passed; no errors |

The configuration hashes differ, proving that `G:` and `H:` are separate Restic
repository identities. Every `G:` snapshot records an `original` source, while
the snapshot time, tree, path, and tag ranges align with `H:`. This is an
intentional replicated backup design, not a redundant-folder deletion target.

Snapshot categories in each repository:

- 27 `database-only`
- 11 `daily-full`
- 9 `hourly-database`
- all 47 tagged `arcworks`

## I: — restore evidence, Codex recovery, and unrelated/server scope

| Path | Files | Data | Classification |
| --- | ---: | ---: | --- |
| `ARCWorks_Restore_Tests` | 6,295 | 28.951 GiB | Generated restore-drill output; archive/delete candidate after evidence preservation |
| `ARK Survival Ascended` | 439 | 12.106 GiB | Separate ARCWorks/server project; owner scope decision required |
| `CODEX_RAW` | 23,098 | 3.496 GiB | Historical Codex continuity recovery; compare before cleanup |
| `Recovered` | 52 | 0.408 GiB | Recovered content; inspect manually before any deletion |
| `CODEX` | 173 | 0.347 GiB | Historical continuity material; compare before cleanup |
| `ark` | 0 | 0 | Empty; deletion candidate after owner confirms scope |

`I:\ARCWorks_Restore_Tests` and `I:\ARCWorks_EaseUS_Images` are configured
backup paths. The restore-test path exists; the EaseUS path does not currently
exist. Restore-test output is generated data, but it also proves earlier recovery
drills. Preserve the concise evidence and at least one accepted restore artifact
before reclaiming its bulk.

## Backup scheduler findings

Active scheduled tasks:

- `ARCWorks Backup - Daily Full`
- `ARCWorks Backup - Every 6 Hours Databases`
- `ARCWorks Backup - Weekly Maintenance`
- `ARCWorks Backup - Weekly Restore Drill`

The backup controller is stored under `C:\ProgramData\ARCWorks\Backup`. That is
a targeted system dependency, not part of the restricted-drive content scan.
Its configuration currently references:

- `F:\ARCWorks_Backup_Staging`
- `H:\ARCWorks_Restic_Local`
- `G:\ARCWorks_Restic_Replication`
- `I:\ARCWorks_EaseUS_Images` — currently missing
- `I:\ARCWorks_Restore_Tests`
- `D:\ARCWorks_Restaurant_Suite`
- `D:\ARCWorks_Monitoring` — currently missing on the host

The latest observed successful runs were:

- Database-only: 2026-08-30 12:02
- Full: 2026-08-29 01:23

The latest Daily Full, Weekly Maintenance, and Weekly Restore Drill task records
showed `0xC000013A`, which means the scheduled PowerShell process was terminated.
The wrapper log shows approval prompts opening without a corresponding completed
run. The six-hour database task is succeeding. This interactive scheduling
behavior is an operational issue to resolve after storage planning; it is not a
reason to discard the repositories, which both passed Restic checks.

## Docker findings

Docker Desktop was running with 14 containers, 11 running, and 21 images during
the audit. Current relevant dependencies include:

- Production ROMS Compose working directory:
  `D:\ARCWorks_Restaurant_Suite`
- Preview database Compose working directory:
  `D:\ARCWorks_Restaurant_Suite_Codex_Landing_Final`
- Production Cloudflare token bind:
  `D:\ARCWorks_Restaurant_Suite\.secrets\cloudflare-tunnel-token`
- Monitoring bind paths under `D:\ARCWorks_Monitoring`, although that host path
  was not present during the scan
- Docker Desktop disk image under
  `D:\ARCWorks_Restaurant Suite\Docker\storage\...`

The Docker inventory also exposed active portfolio binds on restricted `E:`.
No `E:` content was scanned or changed. Those containers must remain outside
the Restaurant Suite drive migration unless the owner separately changes their
scope.

Docker must be stopped cleanly and its active disk image backed up or exported
before its host storage path or drive letter changes.

## Confirmed exact duplicate samples

Only two small, representative duplicate pairs were hashed during this pass:

1. A 207,419,559-byte integration-test hang dump appears in two historical D:
   trees. Both SHA-256 values are
   `1688A5AC6672DE4AA5D649800550C4B352AD1AB9CB6846EC10C0BDBB02D81878`.
2. A 372,948,992-byte LibreOffice installer appears in both F: backup staging
   and I: restore-test output. Both SHA-256 values are
   `F15BA07BFCB0186986CF3171063506F5D207C11F8CC051BA0D135209E9E915F9`.

These hashes prove only those exact file pairs. They do not prove that their
parent directories are duplicates. Backup and restore-test duplication may be
intentional evidence.

## Classification and cleanup candidates

### Protected now

- Active Docker Desktop VHDX and its recovery copy
- Production and preview Compose worktrees
- Any dirty Git worktree
- `F:\ARCWorks_Backup_Staging` until the scheduler is changed
- Both Restic repositories
- `G:\PostgreSQL` while the Windows service and data remain there
- Current project chronicle, evidence, assets, and AI workbench
- `Recovered` content until manually classified

### Likely generated/recoverable, but not yet approved for deletion

- Test hang dumps and `TestResults` directories
- NuGet caches, `bin`, `obj`, `node_modules`, and packaged app copies
- Completed restore-test trees after evidence extraction
- Completed backup staging runs after Restic and retention verification
- Superseded Docker VHDX copies after active/recovery identity is proven
- Empty root folders and zero-byte scratch files

### Owner scope decision required

- `COMPLEATED PROJECTS`
- ARCWorks Authenticator, Speech2Text, System Monitor, and other separate apps
- `I:\ARK Survival Ascended`
- `G:\NextCloud`, `PostGIS`, PostgreSQL, StackBuilder, and proxy scripts
- Historical Codex continuity copies on F: and I:

These may not belong to the Restaurant Suite, but they are clearly related to
other work performed by the owner. They must not be deleted merely because they
fall outside ROMS.

## Safe partition preparation sequence

This is a proposed safety order, not authorization to execute it.

1. Decide which non-ROMS ARCWorks products remain in the two-drive project
   boundary.
2. Capture each dirty Git worktree as a patch, untracked-file manifest, Git
   bundle, and verified archive before removing any worktree.
3. Update the backup source from the old/live worktree to the final authoritative
   layout and run a successful full backup.
4. Resolve the interactive scheduled-task behavior and complete a fresh restore
   drill.
5. Export PostgreSQL logically, preserve its configuration, and test the export.
6. Stop PostgreSQL before moving its data or changing Disk 2.
7. Stop Docker Desktop cleanly; capture its active VHDX and required Compose,
   volume, image, secret, and database recovery material.
8. Copy F:, G:, H:, and I: to temporary D: storage with per-file manifests and
   verification. Do not delete the source partitions yet.
9. Keep Disk 3/H: intact as the second Restic copy while Disk 2 is rebuilt.
10. Rebuild the chosen primary physical disk, copy and verify the authoritative
    project data onto it, and prove Git, Docker, databases, and backups.
11. Only after both temporary D: and the new primary are verified should the
    remaining backup disk be repartitioned.
12. Initialize the final backup disk, run a full backup and repository check,
    then perform a restore drill.
13. Reclaim temporary D: content only after the final primary and backup copies
    are independently verified.

## Audit limitations

- No file was moved, renamed, modified, or deleted.
- No partition, label, or drive letter was changed.
- No service, task, container, or database was stopped.
- Restricted C: and E: content was not scanned. Only targeted system
  configuration and Docker metadata needed to identify dependencies were read.
- The scan did not hash all 246 GiB. Hashing was limited to Restic integrity and
  two representative duplicate pairs.
- Folder size similarity, identical timestamps, and matching names are candidate
  evidence only.
- Visual review of recovered files and business ownership decisions remain
  human tasks.

## Decision gate before cleanup

No deletion or repartitioning should begin until the product owner reviews this
audit and answers these scope questions:

1. Should the final project drives hold only the Restaurant Suite, or all
   ARCWorks software projects?
2. Should the ARK server material on I: be preserved as an active project,
   archived, or excluded?
3. Are PostgreSQL/NextCloud/PostGIS on G: still needed?
4. Should historical Codex continuity data be preserved as part of the project
   diary, reduced to selected sessions, or archived separately?
5. Which physical disk should become the final primary and which should become
   the final backup after migration and reliability review?

