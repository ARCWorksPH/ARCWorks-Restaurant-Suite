# Repartition Readiness — 2026-09-04

## Current decision

No partition operation has been executed. Readiness is evaluated by physical
disk, not only by drive letter, because F:/H: share Disk 3 and G:/I: share
Disk 2.

## Physical-disk matrix

| Physical disk | Current volumes | Readiness | Evidence and blocker |
| --- | --- | --- | --- |
| Disk 3 — WDC WD5000AAKX, 465.76 GiB | F: and H: | **Ready for owner-approved reformat** | No live process, service, scheduled task, or Docker bind uses F:/H:. F: backup/Docker/Codex sets and H: Restic were copied to D: and verified. The staged H: Restic repository passed all 47 snapshots with no errors. |
| Disk 2 — WDC WD10JPCX, 931.51 GiB | G: and I: | **Not ready** | PostgreSQL remains registered on G: until the corrected elevated cleanup passes. I: contains the unrelated 12.1 GiB ARK Survival Ascended server and needs an explicit preserve/delete decision. |
| Disk 0 — ST500DM005, 465.76 GiB | D: | **Do not format** | D: contains active Docker Desktop storage, ROMS/preview/monitoring paths, Git worktrees, and the only consolidated migration staging set. D: is rebuilt last. |
| Disk 1 — TOSHIBA DT01ACA100, 931.51 GiB | E: | **Restricted; never touch in this migration** | Both running Portfolio containers bind live content, configuration, certificates, logs, and media from `E:\ARCANUM VAULT\...`. The Portfolio site is not contained solely in the Docker image. |
| Disk 4 — EDILOCA EN600Pro, 475.90 GiB | C: | **System; never touch** | Windows system/boot drive. |

## Recommended two-drive sequence

1. With explicit owner approval, erase both F: and H: partitions on physical
   Disk 3 and create one new 465.76 GiB backup volume.
2. Copy the complete D: migration set and required live-project sources onto the
   new Disk 3 volume, then verify manifests, Restic, Git state, database dump,
   Docker recovery image, and restore evidence.
3. Resolve I:'s ARK server disposition and complete the corrected elevated
   PostgreSQL/Nextcloud cleanup on G:.
4. Only after Disk 3 holds a verified independent copy, erase G:/I: on physical
   Disk 2 and create one new 931.51 GiB primary project volume.
5. Restore the project to Disk 2; relocate Docker data and normalize the
   authoritative Compose checkout; update backup paths; verify local/public
   health, database data, Portfolio dependencies, and a real restore drill.
6. Reclaim or repurpose temporary D: only after both the new primary and backup
   are independently proven.

## Current D: scope

D: now contains Restaurant Suite sources, historical Restaurant Suite
worktrees/rollbacks, the project AI workbench, the project monitoring stack,
and the migration staging set. These are all part of the current project or its
recovery chain. Two EaseUS marker/driver files remain at the D: root and can be
handled when D: is rebuilt; they are not a reason to risk the current staging
copy.

## Required elevated completion command

```powershell
& "D:\ARCWorks_Restaurant_Suite_Gate1_Deploy\00_PROJECT_CONTROL\storage\Complete-ARCWorksMigrationAdminSteps.ps1" -Execute -RemovePostgreSQL
```

The script is idempotent for the already completed copy/disable/stop steps. It
must end with `ADMIN MIGRATION STEPS: PASS` before G: can be considered clean.

