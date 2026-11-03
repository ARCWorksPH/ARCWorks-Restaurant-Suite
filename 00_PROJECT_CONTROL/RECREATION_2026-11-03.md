# Project main folder recreation — 2026-11-03

**Performed by:** Grok Bot (ARC), owner-approved. **Location:** `H:\ARCWorks_Restaurant Suite-MAIN`

## What was done
1. Copied `D:\ARCWorks_Restaurant_Suite_Gate1_Deploy` with robocopy (copy only; source untouched),
   excluding `.git`, `bin`, `obj`, `node_modules`, `backups`, `.secrets`, `TestResults`, `.env`, `*.sql`.
   Verified 559 files / 77,193,181 bytes in source scope = 559 files / 77,193,181 bytes in destination.
2. Imported unique (SHA-1 content) documentation/design files from older D: trees into
   `docs\legacy-import\<tree>\` preserving relative paths; see `docs\legacy-import\INDEX.md`
   (root tree 82, Codex_Landing_Final 17, Staff-side 9, AI_Workbench 9). Many root-tree items are older
   versions of files that exist here; they are kept for history only.
3. `dotnet restore Roms.slnx` and `dotnet build Roms.slnx -c Release`: **succeeded, 0 warnings, 0 errors**
   (.NET SDK 10.0.403). No tests were run.

## Source of truth
- Files: Gate1_Deploy snapshot (latest; 17 EF migrations through `20260823023044_AddPrivateJournal`,
  matching the live production database: 17 migrations, 30 tables, verified 2026-11-03).
- Git commit: **NOT YET CONFIRMED.** Git 2.56 is installed, but `ROMS-all-refs.bundle` is inside
  `D:\ARCWorks_Restaurant_Project_Archive_20260925`, which still returns *Access is denied* to the
  non-elevated `GBServerPH` session. Expected `main` = `9664410` (Chronicle). This folder is **not a git repo yet**.

## Missing / owner to place
- `.env` (production values) — owner to place; do not commit.
- `.secrets\cloudflare-tunnel-token` (and any other `.secrets\*`) — owner to place.
- Git history — clone from the bundle once archive is readable, then reconcile with this folder.
- Recovery diary `ROMS_REBUILD_AND_CLEANUP_DIARY_20260926.md` — unread (archive access).

## Live runtime (not changed)
`docker compose ls` on 2026-11-03: `arcworks-resto` still runs from `D:\ARCWorks_Restaurant_Suite\compose.yaml`
(older tree); preview from `D:\ARCWorks_Restaurant_Suite_Codex_Landing_Final\compose.yaml`. Switching
production compose to this folder requires owner approval and placing `.env`/secrets first.

## Next steps
1. Owner grants `GBServerPH` explicit Read on the archive folder (Administrators group is deny-only in agent session).
2. `git clone ROMS-all-refs.bundle` to a temp folder, verify `main`=`9664410`, diff vs this folder, then
   move the `.git` in (or re-copy) so this folder becomes the tracked checkout. Record result in the Chronicle.
3. Design and implement backup v2 (see backup analysis), then plan migration of the live compose project here.

## Git outcome — 2026-11-03 17:55 (UTC+8)
- Archive readable after owner ACL fix. Bundle `git\ROMS-all-refs.bundle`: complete history, SHA-1; 56 refs.
- `main` = **`3934c22b3ae57a142577469acaf1f2ea867b6f21`** (2026-09-04, "Merge pull request #31 ops/repartition-readiness-20260904").
  `9664410` (Gate 2H) is an ancestor; later commits are docs/ops only (PRs #28-#31: chronicle, drive audit, storage checkpoint, repartition readiness).
  This matches the diary: Gate1_Deploy was clean `main` at `3934c22` on 2026-09-26.
- No tags. Only `origin/agent/zabbix-monitoring` has a commit not in `main` (1 commit).
- Clone vs this folder (excl. bin/obj/.git/legacy-import): 559 files identical by SHA-1 except
  `00_PROJECT_CONTROL/PROJECT_CHRONICLE.md` (+161 lines, uncommitted 2026-09-26/27 entries) and this file.
- Converted: clone's `.git` moved into this folder; temp clone `H:\ROMS_bundle_clone_tmp` removed.
  `git status`: `main...origin/main`; ` M PROJECT_CHRONICLE.md`; untracked `RECREATION_2026-11-03.md`, `docs/legacy-import/`.
- `origin` currently points at the D: bundle (read-only source). GitHub remote `ARCWorksPH/...` to be re-added by owner decision. Nothing committed or pushed.
