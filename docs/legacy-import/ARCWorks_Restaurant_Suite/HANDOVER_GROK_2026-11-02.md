# ARCWorks Restaurant Suite — Grok Build Handover

**Prepared:** 2026-11-02 (Asia/Singapore)  
**Purpose:** Give a build agent enough verified context to resume work safely.  
**Product:** ARCWorks Restaurant Suite (internal code name: ROMS)

## Start here

Before editing anything, read these records in order:

1. `README.md` — product boundary, local build, configuration.
2. `D:\ARCWorks_Restaurant_Suite_Gate1_Deploy\00_PROJECT_CONTROL\PROJECT_CHRONICLE.md` — append-only decisions and latest handoff/recovery facts.
3. `D:\ARCWorks_Restaurant_Suite_Gate1_Deploy\docs\WORKFLOW_CONTRACT_2026-08-06.md` — accepted business rules and role boundaries.
4. `D:\ARCWorks_Restaurant_Suite_Gate1_Deploy\docs\ROADMAP_2026-08-06.md` — phase status and outstanding acceptance gates.
5. `docs\WORK_LOG.md` and the relevant `docs\testing\` evidence before changing an existing workflow.
6. `D:\ARCWorks_Restaurant_Project_Archive_20260925\ROMS_REBUILD_AND_CLEANUP_DIARY_20260926.md` for the D: recovery assets and the September 27 restore rehearsal.

The D: source root is `D:\ARCWorks_Restaurant_Suite`. A second project tree,
`D:\ARCWorks_Restaurant_Suite_Gate1_Deploy`, contains the detailed control
records and later UI work. Do not assume either tree's `.git` pointer is valid:
on 2026-11-02, `git status` in both roots failed because the pointer refers to
an archived OLD worktree path. Git history was preserved separately as
`D:\ARCWorks_Restaurant_Project_Archive_20260925\git\ROMS-all-refs.bundle`.
Repair/restore a proper Git checkout from the bundle and reconcile its files
with the preserved source trees before making commits. Do not overwrite the
source trees during that recovery step.

## What the product is

ROMS is a single-location, touch-first restaurant operations application for
Waiter, Kitchen, Manager, and Admin/Owner workflows. It is built with .NET 10,
Blazor Interactive Server, ASP.NET Core Identity, EF Core 10, SignalR, and
MariaDB 11.4. Docker Compose is the deployment path; Caddy/Cloudflare Tunnel
and monitoring are operational components.

Implemented areas include table and order workflow, kitchen status flow,
payment boundaries, audit history, role-based management, independent-item
inventory, receiving/counts/adjustments, staff scheduling and attendance,
staff profiles and announcements, leave requests, and a browser-encrypted
personal journal. Consult the workflow contract and gate evidence for exact
permissions, lifecycle rules, and known exclusions.

## Current direction and boundaries

- Finish and accept the deterministic restaurant workflows before expanding
  features or polishing UI.
- The project records say Gate 2H was promoted on 2026-08-25. The September
  27 local rebuild rehearsal proved the archived production image, SQL dump,
  and Data Protection keys could start ROMS in a fresh Docker engine on this
  same Windows host. It was a local smoke check (`/` and `/health` HTTP 200),
  not a fresh-Windows, end-to-end staff acceptance, or public-tunnel test.
- The public tunnel was not started during the rebuild rehearsal. Do not expose
  a clone or change production routing as part of routine build work.
- `AI_HOLD=true` is fail-closed. The app must remain disconnected from the AI
  command gateway and its private network. AI source/lab files are future
  research only; do not enable AI, natural-language SQL, autonomous writes, or
  voice actions.
- Inventory is a manual independent-item ledger. Do not add recipes,
  ingredient auto-deduction, or unapproved opening balances.
- Single-location scope remains. Do not add multi-tenant architecture or
  broad hospitality abstraction without owner approval.
- Owner approval is required before production deployment, tunnel/hostname
  changes, beta enablement, loading real restaurant or employee data, changing
  AI hold, destructive database actions, or disk/backup cleanup.

## Suggested continuation sequence

1. **Restore Git control safely.** Make a new checkout from the D: bundle in a
   separate directory. Inspect refs and working-tree snapshots, identify the
   intended branch/commit, compare against both D: trees, then document which
   tree is authoritative. Keep the archive and source copies untouched until
   the owner accepts the recovered checkout.
2. **Baseline before edits.** Record branch/commit, tracked and untracked
   changes, .NET SDK, Docker/Compose availability, database/container state,
   and secrets/config presence (names only; never print values). Run tests only
   after reviewing the repo guidance and confirming whether test services are
   isolated from production.
3. **Choose one roadmap gate.** The dated roadmap identifies inventory
   acceptance, schedule/report acceptance, recovery acceptance, and staging
   readiness as outstanding gates. Check the Chronicle and later evidence for
   updates before starting; that roadmap is dated and may be stale.
4. **Work in a new branch/worktree.** Keep changes narrow and tied to one
   accepted workflow contract. Add/update tests and evidence with the code.
5. **Validate in layers.** Focused tests, real MariaDB migration/concurrency,
   Docker build, browser/role acceptance, and supervised restaurant acceptance
   are separate evidence levels. Report exactly which ran and which did not.
6. **Review and hand back.** Summarize changed files, behavior, test results,
   risks, rollback path, and any owner decision needed. Record accepted
   milestones and external-agent contributions in the Project Chronicle.

## Safe build entry points

From a valid checkout with .NET 10 installed:

```powershell
dotnet restore Roms.slnx
dotnet test Roms.slnx -m:1
```

For a local run, provide a disposable MariaDB 11.4 database through
`ConnectionStrings__DefaultConnection`, then:

```powershell
dotnet run --project src/Roms.Web
```

For Docker, first read `deploy\portable\README.md` and the Compose/service
configuration. Use a disposable instance manifest and fresh secrets. Do not
reuse production secrets or production database volumes for tests. The
repository README describes the current Compose startup path.

## Recovery material on D:

The archive folder is `D:\ARCWorks_Restaurant_Project_Archive_20260925`.
Important ROMS assets observed there:

- `docker-recovery\docker-images-tagged-20260926.tar`
- `database-checkpoint\roms-production-20260926.sql`
- `docker-recovery\roms-production-data-protection-keys-20260926.tar.gz`
- `git\ROMS-all-refs.bundle`
- `ROMS_REBUILD_AND_CLEANUP_DIARY_20260926.md`

The September 27 rehearsal restored the image tar, production SQL (30 tables,
17 migrations), and key archive into a fresh Docker engine, then served the H:
test copy locally. Preserve D: as the source of recovery material. Never put
passwords, tokens, private keys, raw `.env` values, employee information, or
customer data in prompts, issues, logs, commits, or this handover. The root
source `.env` and `.secrets` are sensitive; inspect only locally and do not
copy them into AI context.

## Reporting format for Grok

Before work: state the checkout path, branch/commit, exact task, files in
scope, and baseline. During work: make a small change and preserve evidence.
After work: list files changed, explain behavior, report commands and actual
results, distinguish unrun checks, and give rollback instructions. Treat all
Grok output as a proposal until a human reviews it and independently verifies
the change. Do not merge, deploy, publish, contact anyone, rotate credentials,
or make destructive changes unless the owner explicitly authorizes that exact
action.

## Evidence links

- Project history: `D:\ARCWorks_Restaurant_Suite_Gate1_Deploy\00_PROJECT_CONTROL\PROJECT_CHRONICLE.md`
- Workflow contract: `D:\ARCWorks_Restaurant_Suite_Gate1_Deploy\docs\WORKFLOW_CONTRACT_2026-08-06.md`
- Roadmap: `D:\ARCWorks_Restaurant_Suite_Gate1_Deploy\docs\ROADMAP_2026-08-06.md`
- Recovery diary: `D:\ARCWorks_Restaurant_Project_Archive_20260925\ROMS_REBUILD_AND_CLEANUP_DIARY_20260926.md`
- Root README: `README.md`

