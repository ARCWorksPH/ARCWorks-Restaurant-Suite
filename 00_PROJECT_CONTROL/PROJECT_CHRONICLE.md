# ARCWorks Restaurant Suite — Project Chronicle

**Status:** Authoritative, append-only project decision and progress record  
**Established:** 2026-08-30  
**Product owner:** Gunborg / ARCWorksPH  
**Repository:** `ARCWorksPH/ARCWorks-Restaurant-Suite`  
**Internal code name:** ROMS  

## Purpose

This chronicle is the permanent human-readable record of how the ARCWorks
Restaurant Suite evolves. It records important decisions, implementation
milestones, tests, failures, reversals, external AI contributions, audits,
deployment events, and lessons learned.

Git remains the authoritative source history. Detailed work logs, test output,
screenshots, audits, and recovery records remain the authoritative evidence for
their individual subjects. This chronicle is the single navigation layer that
explains what happened, why it happened, and where the supporting evidence can
be found.

The chronicle is not a substitute for raw evidence and must not silently rewrite
history. If a prior entry is later found to be incomplete or wrong, add a dated
correction instead of deleting the original decision.

## Recording rules

1. Add an entry for every material product, architecture, security, workflow,
   data, UI, deployment, recovery, AI, or project-management decision.
2. Record failed experiments and rejected options as carefully as accepted
   work. A failure that teaches us something is part of the project history.
3. Separate facts that were verified from assumptions, proposals, and work that
   remains unverified.
4. Link to the exact commit, branch, test evidence, audit, screenshot, handoff,
   or recovery record whenever one exists.
5. Never place passwords, tokens, private keys, authenticator seeds, customer
   data, employee private data, or unredacted environment values in this file.
6. Keep exact technical output in a dedicated evidence file and link it here;
   do not turn this chronicle into a copy of every terminal log.
7. Record who or what produced an external contribution: Codex, Grok Build,
   Grok Bot, another model, an auditor, or a human contributor.
8. External AI output is a candidate until independently inspected, integrated,
   tested, and accepted by the product owner.
9. Record production changes separately from source completion. A successful
   build or merge does not prove that production was deployed or accepted.
10. Append future entries in chronological order under **Ongoing chronology**.

## Evidence precedence

If records appear to disagree, use this order:

1. Current source, database migration state, and observed runtime behavior.
2. Git commit and pull-request history.
3. Focused test, deployment, backup, or visual-acceptance evidence.
4. Gate contracts and accepted implementation plans.
5. This chronicle and the dated work log.
6. Roadmaps, proposals, mock-ups, and informal conversation summaries.

A newer proposal does not override an accepted decision unless the product
owner explicitly accepts the replacement.

## Current position — 2026-08-30

### Confirmed

- The premium Chef Doy's Gourmet Restaurant staff-login landing page was
  accepted and deployed.
- Gate 1 single-active-session and duplicate-session protections were completed.
- Gate 2A through Gate 2H have implementation and evidence records covering
  restaurant identity and server time, the restricted Waiter read model,
  attendance and floor eligibility, staff profiles and Today's Team,
  announcements and manager notes, leave requests, the private journal, and the
  final Waiter dashboard integration.
- The Gate 2H corrections were merged into `main` through commit `9664410` on
  2026-08-25. The detailed evidence reports successful preview validation and
  production promotion.
- The repository contains Git history, work logs, gate evidence, audit branches,
  UI checkpoints, recovery documentation, and rollback records.
- The project is not yet considered beta complete. Other operational panels,
  whole-system integration, portability, security review, recovery validation,
  and supervised acceptance remain relevant future work.

### Active but isolated

- A bounded Grok Build pilot for a Kitchen Order Ticket component exists under
  `D:\ARCWorks_AI_Workbench\001-kitchen-order-ticket-card`.
- Grok Build output must remain candidate material until the handoff contract,
  file boundary, source, tests, and visual result are independently reviewed.
- No Grok Build output is authorized to rewrite the application shell, merge to
  `main`, deploy, or receive production secrets.

### Paused

- Grok Bot onboarding is paused until the project files, storage layout,
  production workflow, AI responsibilities, and review boundaries are organized.
- No Grok Bot connector, local-computer access, GitHub access, routine, or
  production access has been approved.

### Immediate project-control priority

1. Preserve and consolidate the project record.
2. Inventory all project-related files across the approved drives.
3. Identify authoritative, historical, generated, candidate, sensitive,
   duplicate, and unknown material.
4. Design one primary project volume and one physically separate backup volume.
5. Copy and verify recoverable data before changing any partition.
6. Define the complete human-and-AI production workflow.
7. Run one controlled component pilot, evaluate it, and only then scale it.

No partition deletion, volume merge, drive-letter change, mass file deletion,
or Grok Bot onboarding is authorized by this entry.

## Historical reconstruction

The following summary was reconstructed from Git history, the work log, gate
evidence, audit material, UI checkpoints, and accepted project decisions. It is
an index, not a replacement for those sources.

### 2026-07-26 — ROMS beta baseline established

**Decision:** Preserve the existing layered .NET/Blazor/MariaDB application as
the product baseline and move toward a controlled private beta.

**Reason:** The waiter-to-kitchen-to-management core was valuable, but database,
browser, Docker, HTTPS, SignalR, backup, and human workflow acceptance required
evidence beyond in-memory tests.

**Result:** Git history begins with `143bd13` (`Establish ROMS beta baseline`).
External Audit #1 considered the core promising while retaining validation
gates.

**Evidence:**

- `docs/audits/2026-07-26-baseline-maturity.md`
- `PROJECT_TIMELINE.md`

### 2026-07-29 to 2026-07-31 — Inventory, workflow, recovery, and operations groundwork

**Decision:** Develop inventory as a controlled independent-item ledger, keep
restaurant data provisional, isolate experimental AI, and create backup and
recovery evidence before relying on the system operationally.

**Reason:** Inferred inventory, recipes, unvalidated production data, and
unrestricted AI introduced unacceptable uncertainty for the first beta.

**Result:** Inventory controls, synthetic workflow testing, Docker and tunnel
operations, MariaDB incident documentation, backup scheduling, Restic evidence,
and model benchmark records were created. Inventory activation remained gated.

**Evidence:**

- `docs/INVENTORY_DATA_ASSESSMENT_2026-07-29.md`
- `docs/MARIADB_DEADLOCK_INCIDENT_2026-07-30.md`
- `docs/SYNTHETIC_RESILIENCE_TESTING_2026-07-30.md`
- `docs/FAILOVER_RUNBOOK.md`
- `docs/evidence/`
- `docs/audits/2026-07-30-architecture-security-product.md`

### 2026-08-02 to 2026-08-04 — Product simplification, security, branding, and portability

**Decision:** Remove recipes from the initial inventory release, retain manual
independent-item operations, harden security and deployment boundaries, adopt
ARCWorks Restaurant Suite branding while preserving compatibility-sensitive
ROMS identifiers, and prepare rather than force portability.

**Reason:** Recipe costing and automatic consumption were too interconnected for
a safe early release. Compatibility names, migrations, cookies, and storage
could not be casually renamed. Portable deployment needed unique instance and
secret identities.

**Result:** Recipe scope was removed, security controls were strengthened,
inventory remained disabled pending operational proof, branding was updated,
and portable deployment and recovery contracts were documented.

**Evidence:**

- `docs/SECURITY_HARDENING_2026-08-02.md`
- `docs/BRANDING_AND_COMPATIBILITY.md`
- `deploy/portable/README.md`
- `docs/audits/2026-08-02-inventory-readiness.md`
- `docs/audits/2026-08-03-qa-compliance-post-merge.md`

### 2026-08-06 to 2026-08-08 — Workflow contract and AI hold

**Decision:** Freeze the deterministic restaurant workflow before further UI
expansion. Move the in-product AI path to a future-version hold and retain it as
an isolated laboratory only.

**Reason:** Workflow correctness, backups, rollback, and acceptance had to lead
the design. An AI feature could not be allowed to obscure or bypass deterministic
authorization and operational rules.

**Result:** Workflow contracts, UI implementation instructions, roadmaps,
recovery drills, and the fail-closed AI hold were documented. External Audit #5
conditionally accepted continued private-beta preparation.

**Evidence:**

- `docs/WORKFLOW_CONTRACT_2026-08-06.md`
- `docs/UI_REDESIGN_IMPLEMENTATION_INSTRUCTIONS_2026-08-06.md`
- `docs/AI_HOLD.md`
- `docs/evidence/OVERWRITE_RECOVERY_DRILL_2026-08-08.md`
- `docs/audits/2026-08-08-arcworks-suite-rebrand-workflow.md`

### 2026-08-11 to 2026-08-12 — Landing-page design and acceptance

**Decision:** Replace the generic staff-login presentation with a restaurant-
first premium landing page using Chef Doy's real branding, layered assets,
responsive composition, and a restrained ARCWorks mark.

**Reason:** Chef Doy's Gourmet Restaurant was the first potential customer and
the demonstration needed to feel like the restaurant's own premium product.

**Result:** Multiple CSS, asset, Grok, and Codex design iterations were tested.
The final landing page was accepted after desktop, landscape, portrait, zoom,
logo, background, and responsive checks. A pre-Waiter UI checkpoint was
preserved with screenshots and hashes.

**Evidence:**

- `docs/UI/LANDING_PAGE_ACCEPTANCE_2026-08-12.md`
- `docs/UI/CODEX_LANDING_PAGE_FINAL_WORK_LOG_2026-08-12.md`
- `docs/UI/evidence/checkpoints/2026-08-12-waiter-before-personal-dashboard/`

### 2026-08-11 to 2026-08-14 — Gate 1 session security

**Decision:** Enforce one active application instance per staff account and
treat duplicated authenticated sessions as a potential compromise.

**Reason:** Restaurant users and devices cannot be assumed to have strong local
security. A copied browser profile or authenticated state must not create an
unlimited number of trusted sessions.

**Result:** Duplicate-session protection became server-owned and fail-safe.
Gate 1 was tested, backed up, merged, and deployed with rollback evidence.

**Evidence:**

- `docs/NOTES/SINGLE_ACTIVE_SESSION_POLICY_2026-08-11.md`
- `docs/testing/GATE_1_SESSION_TRUTH_EVIDENCE_2026-08-13.md`
- `docs/UI/WAITER_SHELL_ACCOUNT_SECURITY_WORK_LOG_2026-08-12.md`

### 2026-08-12 to 2026-08-14 — Waiter dashboard product contract

**Decision:** Make the Waiter dashboard a personal, restaurant-branded calm
space separated from the operational floor. Remove the permanent side panel,
use one clock-in/clock-out control, gate floor entry on attendance state, show
today's staff by restaurant date, keep announcements dismissible, include leave
requests, and keep the private journal personal.

**Reason:** The dashboard should feel like the employee's own preparation space,
not an office control panel. Operational ordering remains behind the explicit
Enter the Floor transition.

**Result:** Landscape and portrait mock-ups, Staff Hub behavior, restaurant
identity requirements, server-time rules, privacy boundaries, and the final
implementation plan were accepted before implementation.

**Evidence:**

- `docs/UI/Gate 2/GATE_2_WAITER_DASHBOARD_FINAL_IMPLEMENTATION_PLAN_2026-08-14.md`
- `docs/UI/Gate 2/WAITER_DASHBOARD_FINAL_REVIEW_CONTRACT_2026-08-14.md`
- `docs/UI/Gate 2/mockups/`

### 2026-08-14 — Gate 2A: restaurant identity and clock

**Decision:** Centralize replaceable restaurant identity and use a server-owned
Asia/Manila restaurant clock with Monday as the start of the work week.

**Reason:** Branding must be replaceable for future restaurants, and client
device time must not control attendance, schedules, or operational events.

**Result:** Modular restaurant configuration, validated local assets, fallback
behavior, server time, and week-boundary tests were implemented.

**Evidence:** `docs/testing/GATE_2A_RESTAURANT_IDENTITY_CLOCK_EVIDENCE_2026-08-14.md`

### 2026-08-14 — Gate 2B: restricted Waiter read model

**Decision:** Provide one self-only Waiter dashboard contract bound to the
authenticated user's immutable identity.

**Reason:** A Waiter must not substitute another employee identifier or receive
manager, payroll, future-roster, journal, or operational-order data through the
dashboard contract.

**Result:** Authorization, identity, timezone, boundary, and empty-state tests
were implemented.

**Evidence:** `docs/testing/GATE_2B_WAITER_DASHBOARD_READ_MODEL_EVIDENCE_2026-08-14.md`

### 2026-08-14 — Gate 2C: attendance and floor gate

**Decision:** Allow Enter the Floor only while a valid attendance record is
active. Keep clock-in/out independent from application login/logout, and
automatically close anomalous attendance after the approved limit for manager
review.

**Reason:** Application authentication and paid attendance are different
responsibilities. Floor access requires a current operational attendance state.

**Result:** Server-side eligibility and end-to-end workflow coverage were added.

**Evidence:** `docs/testing/GATE_2C_ATTENDANCE_FLOOR_GATE_EVIDENCE_2026-08-14.md`

### 2026-08-20 — Gate 2D: profiles and Today's Team

**Decision:** Populate realistic synthetic employee profiles and show staff
scheduled for the current restaurant date in a portrait-focused team carousel.
Names are intentionally not required in the compact dashboard portrait strip.

**Reason:** Realistic sample data was needed to test layout and behavior while
preserving the social, personal nature of the dashboard and avoiding real
employee information.

**Result:** Synthetic profiles, schedules, and Today's Team behavior were
implemented and tested.

**Evidence:** `docs/testing/GATE_2D_PROFILES_TODAYS_TEAM_EVIDENCE_2026-08-20.md`

### 2026-08-23 — Gate 2E: announcements and manager notes

**Decision:** Provide targeted staff announcements and shift-bound manager
notes through the Staff Hub rather than a full employee messaging system.

**Reason:** Staff need operational communication without turning the dashboard
into an uncontrolled chat or permanently plastering old notices on the UI.

**Result:** Communication contracts and focused tests were completed.

**Evidence:** `docs/testing/GATE_2E_ANNOUNCEMENTS_MANAGER_NOTES_EVIDENCE_2026-08-23.md`

### 2026-08-23 — Gate 2F: leave requests

**Decision:** Give employees a structured one-way leave-request channel, with
manager handling to be finalized alongside the Manager panel.

**Reason:** Employees need a clear way to request time off without relying on
informal or uncomfortable conversations.

**Result:** The Waiter-side leave-request foundation and tests were implemented.

**Evidence:** `docs/testing/GATE_2F_LEAVE_REQUESTS_EVIDENCE_2026-08-23.md`

### 2026-08-23 — Gate 2G: private journal

**Decision:** Build an original ARCWorks private journal rather than embedding
SimpleMDE. Store only encrypted journal payloads in the server database; do not
store journal content or telemetry on the user device and do not give managers
or administrators a content-reading bypass.

**Reason:** The journal is personal, portable, and database-backed, while the
server operator must not be able to read its content. Order notes remain part of
the operational ordering system rather than the personal journal.

**Result:** Browser-side encryption, owner-bound opaque persistence, lifecycle
controls, a restricted Markdown editor, and security tests were implemented.

**Evidence:** `docs/testing/GATE_2G_PRIVATE_JOURNAL_EVIDENCE_2026-08-23.md`

### 2026-08-24 to 2026-08-25 — Gate 2H final Waiter UI and corrections

**Decision:** Integrate the accepted Waiter dashboard, Staff Hub, profile,
attendance, floor gate, communications, leave, and journal presentation, then
correct all owner-observed defects before production promotion.

**Reason:** Backend completion was not visual acceptance. The integrated UI had
to be tested against real MariaDB behavior and real browser viewports.

**Result:** The team corrected the MariaDB `@ids` mapping failure, empty journal
JSON handling, legacy navigation chrome, Waiter journal authorization, and
restaurant logo selection. Focused MariaDB and Playwright checks passed across
Android portrait, Android landscape, and desktop. The corrected image was
promoted with a rollback image retained. `main` reached merge commit `9664410`.

**Evidence:**

- `docs/testing/GATE_2H_FINAL_WAITER_UI_EVIDENCE_2026-08-24.md`
- `docs/WORK_LOG.md`
- Git commits `66829b1`, `cdf5273`, `c7466fe`, and `9664410`

### 2026-08-29 — External component-foundry strategy selected

**Context:** Rolling AI usage limits threatened continuity during large coding
turns. Grok Build offered a separate, temporary three-month source of build
capacity but tended to regenerate large projects when given broad assignments.

**Decision:** Use Grok Build as a component foundry: one bounded Lego block at a
time, outside the real repository. Codex retains repository inspection,
architecture, integration, testing, deployment verification, and Git control.

**Reason:** This obtains external build capacity without surrendering the
authoritative application or paying the cost and risk of repeated full-project
generation.

**Expected result:** Smaller handoffs, measurable external output quality,
reduced regeneration waste, and reversible integration.

**Current result:** The first Kitchen Order Ticket card pilot is in progress in
the isolated AI workbench. Its quality and integration cost remain unverified.

**Evidence:**

- `D:\ARCWorks_AI_Workbench\001-kitchen-order-ticket-card\INPUT\VISUAL_HANDOFF.md`
- `D:\ARCWorks_AI_Workbench\001-kitchen-order-ticket-card\REVIEW_CHECKLIST.md`

### 2026-08-30 — Grok Bot evaluated and onboarding paused

**Context:** The SuperGrok subscription includes Grok Bot with a separate usage
pool. Grok Bot can operate as a persistent cloud teammate.

**Proposed role:** An ARCWorks Build Inspector that reviews candidate Grok Build
deliveries against their handoff contract before Codex integration.

**Decision:** Pause all onboarding. Do not install connectors or grant repository,
local-computer, production, database, Docker, or Cloudflare access until project
storage and the complete AI production workflow are organized.

**Reason:** Adding another persistent agent before defining file authority,
permissions, evidence, and handoff boundaries would add risk and clutter.

**Revisit condition:** Complete project-file inventory, primary/backup storage
design, AI role contract, and one controlled component pilot.

### 2026-08-30 — Organization-first operating reset

**Decision:** Temporarily prioritize project organization over feature work.
Create this chronicle first, then audit the approved drives, preserve unique
data, design two final project volumes, and remove only verified redundant or
disposable material.

**Approved storage boundary:**

- Project-eligible physical disks: Disk 0 (`D:`), Disk 2 (`G:` and `I:`), and
  Disk 3 (`F:` and `H:`).
- Restricted personal/system disks: Disk 1 (`E:`) and Disk 4 (`C:`).
- Restricted disks are outside the project migration and cleanup boundary.

**Safety boundary:** The owner's statement that unrelated material on eligible
drives may be deleted does not remove the requirement to identify exact targets,
verify unique data, preserve rollback copies, and prove that scripts, Docker,
Restic, scheduled tasks, and deployments do not depend on a path before deleting
or repartitioning it.

**Current drive observation:** D: has enough apparent free capacity to be a
candidate temporary staging location, but capacity alone does not establish a
safe migration. F:, G:, H:, and I: must be inventoried first, and irreplaceable
data must have at least two verified copies before a partition is removed.

## Audit continuity note

The `main` branch audit index currently lists five completed audits and describes
Audit #6 as requested. Remote branches named `docs/audit-6-landing-ops` and
`docs/audit-7-gate2-waiter` exist and appear to contain later audit work that is
not represented in the current `main` audit index. These branches must be
inspected during file and history consolidation before declaring the final audit
count or deleting any worktree or branch.

## Ongoing chronology

Add all future entries below this line. Use the template that follows and keep
entries in date order.

### 2026-08-30 — Approved-drive read-only audit completed

**Context:** The project owner approved a read-only scan of the project-eligible
`D:`, `F:`, `G:`, `H:`, and `I:` volumes before consolidating storage into one
primary project drive and one backup drive. `C:` and `E:` remain restricted.

**Decision:** Do not delete files or modify partitions yet. Preserve the current
storage state while runtime dependencies, dirty worktrees, backup repositories,
and non-ROMS ARCWorks projects are classified.

**Reason:** The scan found active Docker storage on D:, production and preview
Compose dependencies in dirty Git worktrees, live PostgreSQL data on G:, active
backup staging on F:, separate healthy Restic repositories on G: and H:, and
restore evidence plus other projects on I:. Similar folders are not reliable
proof of redundancy.

**Expected result:** Use the audit to obtain product-owner scope decisions,
preserve unique data, create verified temporary copies, and execute a staged
two-drive migration without losing runtime state or project history.

**Actual result:** Approximately 246.1 GiB across 155,509 files was enumerated.
Both Restic repositories contain 47 aligned snapshots through 2026-08-30 and
passed structural checks. Two representative duplicate pairs were confirmed by
SHA-256. No file, partition, service, task, container, or repository was
modified. Detailed SMART and BitLocker state remain unverified.

**Risks and boundaries:** `D:\ARCWorks_Restaurant_Suite` has 27 changes and
supplies production Compose; the preview worktree has five untracked assets;
the old inventory worktree has 343 changes. PostgreSQL runs from G:. The backup
configuration hard-codes all approved drive letters, and the latest full,
maintenance, and restore task records show terminated interactive runs even
though database-only backups and both Restic repositories are healthy.

**Reversal or revisit condition:** Cleanup may begin only after the product owner
decides the scope of other ARCWorks projects and after dirty Git, Docker,
PostgreSQL, backup, and restore evidence has a verified preservation plan.

**Contributor:** Codex performed the read-only audit; the product owner retains
all deletion, partition, and product-scope authority.

**Evidence:**

- `00_PROJECT_CONTROL/storage/DRIVE_AUDIT_2026-08-30.md`
- `C:\ProgramData\ARCWorks\Backup` targeted task/configuration state
- Restic checks of `G:\ARCWorks_Restic_Replication` and
  `H:\ARCWorks_Restic_Local`

---

### 2026-08-30 — Temporary preservation staged and Nextcloud retirement started

**Context:** The project owner approved consolidation of Restaurant Suite data
onto temporary D: storage and retirement of the unused Nextcloud project before
the approved drives are repartitioned.

**Decision:** Copy and verify before deleting. Keep live D: worktrees and Docker
in their registered locations, stage the project/backup material from F:/G:/H:/I:
under a restricted D: directory, and remove only components proven to be
Nextcloud-specific. Do not classify the standalone PostgreSQL installation as
Nextcloud because the deployed Nextcloud instance used SQLite.

**Reason:** Moving active D: worktrees would break Git and Compose paths, while
copying data before source-drive cleanup preserves rollback. Separating
Nextcloud SQLite from standalone PostgreSQL prevents an unsupported deletion.

**Expected result:** A verified temporary preservation set, no active Nextcloud
runtime, and a bounded elevated step for operations unavailable to the current
process.

**Actual result:** The protected staging set contains a validated live ROMS
MariaDB dump, exact verified copies of the F: backup/Docker/Codex sets, both
healthy 47-snapshot Restic repositories, I: restore/Codex evidence, and the
backup runtime configuration. The Nextcloud-only Ubuntu WSL distribution and
its VHDX were removed without affecting `docker-desktop`. Windows denied
scheduled-task, PostgreSQL-service, and G: recursive-delete operations, so a
reviewable administrator script was created for those remaining bounded steps.

**Risks and boundaries:** No partition may change yet. PostgreSQL must be
stopped and copied consistently. G: Nextcloud data/proxy leftovers remain until
the administrator script passes. The unrelated ARK server on I: still needs a
product-owner disposition before I: can be rebuilt.

**Reversal or revisit condition:** Source-drive cleanup may advance after the
administrator script passes and its evidence log confirms preservation and
Nextcloud residue checks.

**Contributor:** The product owner approved the scope; Codex performed the
preservation, verification, WSL retirement, and bounded admin handoff.

**Evidence:**

- `00_PROJECT_CONTROL/storage/MIGRATION_CHECKPOINT_2026-08-30.md`
- `00_PROJECT_CONTROL/storage/Complete-ARCWorksMigrationAdminSteps.ps1`
- `D:\ARCWorks_Migration_Staging_20260830`

---

### 2026-09-04 — Physical-disk repartition readiness established

**Context:** The owner completed the elevated migration run, approved retirement
of the standalone PostgreSQL stack as part of the abandoned Nextcloud work, and
requested a definitive format-safety assessment.

**Decision:** Evaluate whole physical disks rather than individual drive
letters. Disk 3 (F:/H:) is the first reformat candidate. Disk 2 (G:/I:) remains
blocked by PostgreSQL cleanup and the unrelated ARK server decision. D: remains
the live temporary/runtime disk and E:/C: remain prohibited.

**Reason:** F:/H: share one disk and G:/I: share another. Formatting one
partition is not equivalent to safely rebuilding the physical disk. Docker and
Portfolio mount inspection also proved the running Portfolio site still binds
its files from restricted E:, while ROMS and monitoring depend on D:.

**Expected result:** Rebuild Disk 3 into one verified backup volume first, then
rebuild Disk 2 into the primary project volume only after an independent copy
exists and all Disk 2 blockers are resolved.

**Actual result:** The administrator run successfully disabled the backup tasks,
stopped PostgreSQL/pgAgent, and verified a 24,877-file physical PostgreSQL copy.
It stopped on an empty PostGIS directory before cleanup. The script was fixed,
given an explicit `-RemovePostgreSQL` option, and paired with a physical-disk
readiness matrix. No partition operation was executed.

**Risks and boundaries:** D: cannot be formatted while it holds Docker, ROMS,
monitoring, worktrees, and staging. E: cannot be touched because Portfolio has
live bind mounts there. Disk 2 cannot be touched until the corrected elevated
cleanup passes and the owner decides whether to preserve or delete I:'s ARK
server.

**Reversal or revisit condition:** Readiness must be rescanned immediately before
each destructive disk operation. A new runtime dependency, failed copy check,
or missing backup invalidates the relevant ready status.

**Contributor:** The owner ran the elevated preservation step and approved
PostgreSQL retirement; Codex verified state and prepared the corrected cleanup
and repartition sequence.

**Evidence:**

- `00_PROJECT_CONTROL/storage/REPARTITION_READINESS_2026-09-04.md`
- `00_PROJECT_CONTROL/storage/Complete-ARCWorksMigrationAdminSteps.ps1`
- `C:\Users\GBServerPH\Desktop\Administrator PowerShell.txt`

---

### 2026-09-26 — D: and E: retained as backup drives; ROMS rebuild archive consolidated

**Context:** The owner plans a clean Windows installation and new projects. D:
and E: are designated backup drives and will not be formatted. Historical ROMS
source/worktree copies on D: are consuming workbench space and should be
documented before verified duplicates are removed.

**Decision:** Keep the canonical development checkout at
`D:\ARCWorks_Restaurant_Suite_Gate1_Deploy`, the active ROMS Compose/config
root at `D:\ARCWorks_Restaurant_Suite`, and the owner-added Docker backup tree
at `D:\ARCWorks_Restaurant Suite\Docker\storage`. Keep the tagged Docker
image archive, current production and preview logical dumps, Git bundle,
Portfolio source snapshot, and separate production/preview Data Protection key
exports. Document historical decisions in this chronicle and the D: rebuild
diary. Stage only source/worktree copies that pass archive comparison and
content-hash verification; do not empty the Recycle Bin before recovery checks.

**Reason:** A clean working environment must not remove the only copy of source,
deployment secrets, database contents, Docker recovery data, or required keys.
The canonical chronicle was already established on 2026-08-30 as the
append-only decision diary, so the history should continue there instead of
being recreated from scratch.

**Alternatives considered:** Keep every raw historical worktree indefinitely;
rejected as unnecessary duplication once verified archive copies and Git
history remain. Permanently delete all historical trees before a restore
rehearsal; rejected because Docker VHDX identity and complete restoreability
remain unverified.

**Expected result:** A compact and reviewable D: recovery set, with documented
history and reversible removal of exact duplicate working copies.

**Actual result:** The active stack was observed healthy; 21 Docker images with
23 tag references were saved to a tagged tar and hashed; current production and
preview SQL dumps are present and hashed; the 56-ref Git bundle verified
against the canonical checkout; both named Data Protection key volumes were
exported read-only, tar-listed, and hashed. Three modified Git index files were
preserved in a separate checkpoint and synchronized into the archived old
worktree copy. The tagged Docker image archive loaded successfully. Both SQL
dumps restored into disposable MariaDB containers; the production dump then
restored into a separate Compose database with the production keys and app
image, and the isolated app returned `/health` HTTP 200. Both key archives were
round-trip compared against their source volumes. A SHA-256 comparison matched
3,581 Portfolio non-log files between E: and D:; six active/locked logs were
excluded. A live Docker label check then found that the preview database
uses `D:\ARCWorks_Restaurant_Suite_Codex_Landing_Final\compose.yaml`; that
folder was removed from cleanup candidates pending a verified configuration
relocation. The saved cleanup script now completes preview and lists only the
remaining historical candidates plus the explicitly incomplete Docker copy. A
full content-hash audit of all old trees was stopped after about 9.3 GB was
read; no complete report was produced. Five smaller historical roots have
confirmed Recycle Bin entries. Windows refused to stage the staff-side root
because of an open file. The two large OLD roots and marked-incomplete VHDX
source are absent but have no matching Recycle Bin records; their D: archive
snapshots remain. D: subsequently reported 268.54 GiB used and 197.22 GiB free.

**Risks and boundaries:** No clean-Windows-machine restore rehearsal has yet
been completed. The Docker disk backups still need content-hash and cold-restore
validation. The user-added six-file VHDX tree must remain untouched. The full
content-hash audit did not complete, and the remaining staff-side root is
locked. The remainder's separately stored full VHDX was presumed duplicate by
metadata but not hash-verified; its archived counterpart is not retained under
the remainder snapshot. The primary archived VHDX, user-added Docker backups,
and tested logical rebuild assets remain available.
Drive-letter format decisions remain separate and out of scope for this
cleanup operation; D: and E: are explicitly protected.

**Reversal or revisit condition:** If any candidate file hash differs, retain
the source and reconcile the archive. If the restore rehearsal fails, preserve
or recover the candidate from D: Recycle Bin and update the recovery plan
before proceeding.

**Contributor:** The owner set D:/E: as protected backup drives and requested
the cleanup; Codex checked current Docker, Compose, Git, and archive state,
exported key volumes, and prepared the reversible preflight.

**Evidence:**

- `00_PROJECT_CONTROL/PROJECT_CHRONICLE.md`
- `D:\ARCWorks_Restaurant_Project_Archive_20260925\ROMS_REBUILD_AND_CLEANUP_DIARY_20260926.md`
- `D:\ARCWorks_Restaurant_Project_Archive_20260925\docker-recovery\docker-images-tagged-20260926.tar`
- `D:\ARCWorks_Restaurant_Project_Archive_20260925\database-checkpoint\`
- `D:\ARCWorks_Restaurant_Project_Archive_20260925\git\ROMS-all-refs.bundle`

## Entry template

### YYYY-MM-DD — Short decision or milestone title

**Context:** What problem, observation, opportunity, or request led here?

**Decision:** What was approved, rejected, deferred, or changed?

**Reason:** Why was this chosen?

**Alternatives considered:** What other approaches were evaluated?

**Expected result:** What should happen?

**Actual result:** What was verified? Use `Pending` until evidence exists.

**Risks and boundaries:** What remains prohibited, uncertain, or gated?

**Reversal or revisit condition:** What evidence would justify changing this
decision?

**Contributor:** Product owner, Codex, Grok Build, Grok Bot, auditor, or other.

**Evidence:** Exact commits, branches, files, screenshots, tests, audits, or
deployment records. Do not include secrets.


### 2026-09-26 — D: recovery archive and cleanup checkpoint

**Context:** The owner wants a clean Windows and internal-drive setup for new
projects while preserving the restaurant project and Portfolio on D: and E:.

**Decision:** Retain a documented logical rebuild set and historical project
chronicle on D:. Move only audited redundant worktrees to the Recycle Bin. Keep
D: and E: outside the reformat scope.

**Actual result:** Tagged Docker images, production/preview SQL and Data
Protection keys were archived and hashed. The Git bundle verified with 56
refs. An isolated production Compose rebuild returned `/health` HTTP 200 and
the preview SQL restored in an isolated MariaDB. Portfolio source SHA-256
comparison matched 3,581 non-log files to E:. Final container scan showed the
ROMS, database, Portfolio, tunnel, and monitor containers running; healthy
status was reported for app/database and Portfolio containers. D: reported
268.54 GiB used and 197.22 GiB free.

During cleanup, removed linked-worktree metadata temporarily broke the three
live Git pointer files. Their original contents were checkpointed and all
three pointers were repaired to the preserved shared Git database inside the
archive snapshot. Git HEAD/status now resolve for canonical, production, and
preview trees. That archived `.git` database is a required recovery asset.
Five small worktree directories have confirmed Recycle Bin entries. The
locked staff-side checkout remains. The removed OLD source paths and incomplete
VHDX lack Recycle Bin metadata; archive snapshots remain but those originals
may be permanently deleted.

**Risks and boundaries:** Fresh-Windows setup, restaurant workflow acceptance,
and cold Docker VHDX restoration are unverified. Preserve all Docker disk
backups until verified. Do not empty Recycle Bin before reviewing its entries.
D: and E: remain excluded from formatting.

**Evidence:** `D:\ARCWorks_Restaurant_Project_Archive_20260925\ROMS_REBUILD_AND_CLEANUP_DIARY_20260926.md`, recovery artifacts under `D:\ARCWorks_Restaurant_Project_Archive_20260925`, and final Git/runtime checks on 2026-09-26.

**Contributor:** Codex with owner-directed recovery scope.

### 2026-09-27 — Fresh Docker rebuild rehearsal on H:

**Context:** Owner requested a practical rebuild rehearsal before formatting
other internal drives.

**Decision:** Preserve H:'s existing Restic repository on D:, install a fresh
Docker Desktop engine with its WSL data root on H:, then restore ROMS from the
D: logical backups to an isolated local Compose project.

**Actual result:** The Restic repository was copied from H: to D: (829 files,
11,782,072,559 bytes); Robocopy metadata verification found no differences,
and the `config` file hashes match. A full per-file hash/Restic verification
is still pending, so keep the original H: copy.

A cold copy of the stopped 24,249,368,576-byte Docker VHDX was also saved on
D:; source and copy SHA-256 match
`BC289F365315DEBA714A85FFD91B544AEE5276429E023CF6D10F3B95A77EFA73`.
The archived image tar loaded to a new Docker Desktop 4.91.0 engine whose WSL
data root is `H:\DockerDesktopData`. The source package was copied to
`H:\ROMS-Rebuild-Test-20260927\project`; a fresh database volume restored
from the D: production SQL (30 tables, 17 migrations), and the production key
archive restored to a new volume. The H: app and DB are healthy; local `/` and
`/health` returned HTTP 200 on port 17070. No public tunnel was started.

**Risks and boundaries:** The test ran on the present Windows installation;
a future Windows reformat, user workflows, Portfolio runtime, preview
Compose restart, and external tunnel remain unverified. The production
services are stopped; the local H: rebuild instance is running. Preserve the
cold VHDX, logical backups, and original H: Restic repository.

**Evidence:** `D:\ARCWorks_Restaurant_Project_Archive_20260925\ROMS_REBUILD_AND_CLEANUP_DIARY_20260926.md`, `recovery-test\docker_data-active-coldcopy-20260927.vhdx`, and H: project at `H:\ROMS-Rebuild-Test-20260927\project`.

**Contributor:** Codex with owner-directed rebuild test.

Portfolio static production was also copied from E: (production files and
Media) to an H: test package and started on the fresh H: Docker engine. The
container was healthy; `/` and `/healthz` both returned HTTP 200 at
`127.0.0.1:18081`. The test used a unique local-only Compose project and did
not start a public tunnel.
