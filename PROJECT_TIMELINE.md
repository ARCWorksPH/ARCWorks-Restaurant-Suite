# ARCWorks Restaurant Suite — Project Timeline and Decision Record

This is the navigation index for the project history. It records the major
milestones, decisions, reasons, and acceptance boundaries without replacing the
detailed evidence in `docs/WORK_LOG.md`, audit reports, or test artifacts.

## Current position — 2026-08-06

The project is intentionally focused on the deterministic restaurant core:

1. Waiter → Kitchen → Management
2. Management → independent-item Inventory
3. Management → Staff Schedule → Reports

The AI path is on a future-version hold. The Assistant is hidden, the app has
no command-gateway HTTP client, the app is not attached to the private AI
network, and the Ollama/command-gateway lab is stopped. AI source, contracts,
benchmarks, and the isolated lab remain preserved for a later release. See
[`docs/AI_HOLD.md`](docs/AI_HOLD.md).

The current working branch is `agent/backup-recovery`. This timeline is meant
to be read with the dated [work log](docs/WORK_LOG.md), the [audit timeline](docs/audits/README.md),
and the current [roadmap](docs/ROADMAP_2026-08-06.md).

## Chronological milestones

### 2026-08-12 — Premium staff-login landing page accepted

- Accepted the Chef Doy's Gourmet Restaurant landing page after desktop,
  landscape, portrait/mobile, and zoomed visual inspection.
- Finalized the layered restaurant-first composition, responsive glass login
  card, enhanced atmospheric background, and production 3200 x 1406 Chef
  Doy's wordmark without changing authentication or authorization behavior.
- Preserved the accepted page as the rollback baseline before beginning the
  Waiter/Tables interface phase. Detailed evidence and boundaries are in
  [`docs/UI/LANDING_PAGE_ACCEPTANCE_2026-08-12.md`](docs/UI/LANDING_PAGE_ACCEPTANCE_2026-08-12.md).

### 2026-07-26 — Baseline and beta safety boundary

- Established the layered .NET/Blazor/MariaDB application baseline and the
  waiter-to-payment lifecycle.
- Confirmed that InMemory tests alone were insufficient evidence for MariaDB,
  Docker, SignalR, HTTPS, and browser behavior; real-database and browser
  acceptance were separated from unit coverage.
- Adopted an evidence-first workflow: inspect the real runtime, make reversible
  changes, and record acceptance boundaries before enabling production paths.
- External Audit #1 rated the core positively but required validation before
  inventory activation.

### 2026-07-29 — Inventory, recovery, UI, and AI laboratory groundwork

- Added the provisional restaurant-data sandbox/import path while keeping
  supplied data clearly non-production.
- Built the manual independent-item inventory foundation and kept automatic
  deduction/recipes outside the safe pilot boundary.
- Reworked the UI through documented design iterations and browser checks.
- Isolated Ollama and the command gateway as a private benchmark/laboratory
  path without database credentials or write authority.
- Recovered and redeployed the local application while preserving rollback and
  backup evidence.

### 2026-07-30 — Inventory controls, workflow testing, and audits

- Added negative-stock controls, manager override evidence, and waste/spoilage
  approval workflows.
- Added receiving and witnessed physical-count reconciliation rather than
  relying on inferred stock levels.
- Ran synthetic waiter/kitchen/cashier, stress, adversarial, malformed-input,
  wrong-date, and over-limit tests to identify failure behavior before beta.
- Documented a MariaDB deadlock incident and the transaction evidence needed to
  investigate it.
- External Audit #2 found a solid core but kept inventory, production
  hardening, and beta acceptance gated.

### 2026-07-31 — Operations, backups, tunnels, and model evidence

- Consolidated Docker cleanup, Cloudflare tunnel routing, and operational
  recovery documentation.
- Built the certified backup/recovery path with database dumps, staging,
  Restic repositories, integrity checks, and recovery-drill evidence.
- Added the six-hour database schedule, daily full backup, weekly maintenance,
  and weekly restore-drill structure with non-deferrable full-backup behavior.
- Preserved AI benchmark scripts, raw results, model comparisons, and rejected
  model evidence instead of treating a benchmark as production qualification.
- Removed rejected local models while retaining their benchmark evidence.

### 2026-08-02 — Product simplification and security hardening

- Removed recipe functionality by product decision. Recipes, yields, costing,
  and consumption were judged too interconnected and error-prone for the
  first inventory release.
- Implemented a read-only, role-aware AI assistant contract with deterministic
  validation, bounded catalogs, authorization checks, throttling, and sanitized
  audit records. No AI writes or unrestricted SQL were approved.
- Completed security hardening for containers, CI, secrets, networks, headers,
  and the edge path.
- External Audit #3 accepted the technical controls for continued supervised
  development, while restaurant data and human acceptance remained required.

### 2026-08-03 — Post-merge compliance review

- External Audit #4 confirmed that recipe removal simplified the product and
  that the manual inventory boundary was clearer.
- AI remained disabled by default. Staff beta, restore-drill acceptance, and
  final operational ownership remained open gates.

### 2026-08-04 — Backup scheduling, branding, and portability

- Finalized the ARCWorks Restaurant Suite public branding while preserving ROMS
  identifiers where changing them could invalidate cookies, migrations,
  storage, or historical evidence.
- Prepared the portable Resto-VM contract with isolated instance identifiers,
  database identity, backup identity, monitoring identity, and tunnel mapping.
- Formalized the backup scheduler registration boundary and made daily/weekly
  full operations non-deferrable.
- Kept VM cutover as preparation only; the live workstation deployment was not
  silently replaced.

### 2026-08-06 — AI moved to a gated future-version hold

- Decided that AI was creating unnecessary complexity and delaying the core
  product, so it was removed from the active release path.
- Added the independent `Ai:Hold=true` fail-closed gate so a stale
  `AI_ENABLED=true` value cannot reactivate the feature accidentally.
- Removed the app-to-gateway connection, removed the app from the `command`
  Docker network, hid the Assistant navigation and route, and stopped the
  Ollama/command-gateway containers.
- Preserved all AI implementation and evidence for future re-evaluation under
  the documented re-enable gate in [`docs/AI_HOLD.md`](docs/AI_HOLD.md).
- Rebuilt and recreated the app, confirmed local and public health HTTP 200,
  and verified that the app runs only on the backend and edge networks.

### 2026-08-08 — Backup recovery milestones completed

- Corrected the installed backup source root from the retired
  `D:\\ARCWorks_Restaurant Suite` path to the canonical
  `D:\\ARCWorks_Restaurant_Suite` path after the normal restore drill exposed
  the mismatch. The prior runtime configuration was preserved for rollback.
- Completed the normal isolated restore drill with two accepted snapshots:
  ideal restore and controlled interrupted-restore recovery.
- Validated 992-file SHA-256 manifests, MariaDB dumps (24 tables), and
  PostgreSQL dumps (203 tables) in disposable containers.
- Completed the overwrite and damaged-data drill using disposable instances.
  Missing and modified files were repaired, unexpected data was quarantined
  with hashes and an inventory, and no data was permanently deleted.
- Confirmed the live application remained HTTP 200 and live Docker volumes,
  databases, tunnel, and monitoring services were not touched.
- Cross-PC recovery remains the final pre-beta portability and runtime gate.

## Decision principles

- **Core before expansion:** complete and accept the deterministic restaurant
  workflows before UI polish, portability expansion, or AI integration.
- **Manual inventory before recipes:** independent-item ledger operations are
  easier to audit, reverse, and train against than recipe-driven consumption.
- **Fail closed:** ambiguous permissions, stale data, unsupported actions,
  unavailable services, and disabled features must not produce guessed writes.
- **Evidence over assumption:** unit tests, real MariaDB, browser tests,
  container checks, external audits, and recovery drills are separate forms of
  evidence and are not substituted for one another.
- **Reversible operations:** preserve backups, rollback paths, benchmark
  evidence, and compatibility identifiers before cleanup or migration.
- **Human authorization:** the project owner remains the authority for beta,
  production, inventory, tunnel, and future AI enablement decisions.

## Canonical records

| Record | Purpose |
| --- | --- |
| [`docs/WORK_LOG.md`](docs/WORK_LOG.md) | Detailed chronological implementation and verification diary |
| [`docs/ROADMAP_2026-08-06.md`](docs/ROADMAP_2026-08-06.md) | Current phase plan, blockers, release gates, and deferred work |
| [`docs/WORKFLOW_CONTRACT_2026-08-06.md`](docs/WORKFLOW_CONTRACT_2026-08-06.md) | Frozen role, state, inventory, schedule, reporting, and acceptance contract |
| [`docs/ROADMAP_2026-08-02.md`](docs/ROADMAP_2026-08-02.md) | Historical AI-first roadmap retained for context |
| [`docs/audits/README.md`](docs/audits/README.md) | Independent audit sequence and dispositions |
| [`docs/AI_HOLD.md`](docs/AI_HOLD.md) | Current AI hold and future re-enable gate |
| [`docs/INVENTORY_OPERATIONS.md`](docs/INVENTORY_OPERATIONS.md) | Manual inventory boundaries and reversal rules |
| [`docs/FAILOVER_RUNBOOK.md`](docs/FAILOVER_RUNBOOK.md) | Recovery and promotion safeguards |
| [`docs/evidence/`](docs/evidence/) | Dated backup, portability, and operational evidence |

## Major commit anchors

The Git history provides the implementation-level timeline. The following
anchors are especially useful when reviewing the project on GitHub:

| Commit | Meaning |
| --- | --- |
| `671564d` | Remove recipe functionality |
| `00662fe` | Add guarded read-only assistant |
| `e9d1788` | Security hardening for AI, CI, containers, and edge |
| `da4467e` | Add certified backup and recovery system |
| `1fd7bb6` | Add prompted six-hour backup schedule |
| `bffb460` | Adopt ARCWorks Restaurant Suite branding |
| `4465daf` | Establish portable ROMS instance contract |
| `f8c3a1e` | Prepare Resto-VM portable instance |
| `0d0db2b` | Place AI integration behind the future-version hold |

## 2026-08-12 - Landing page accepted and promoted

The project owner accepted the premium Chef Doy's Gourmet Restaurant staff
login in desktop, landscape, portrait/mobile, and zoomed views. Pull request
#11 passed CI and secret scanning, then merged into protected `main` as
`daedd2c`. The approved image was promoted to the production-facing ROMS app;
local and public endpoints returned HTTP 200, the public visual assets matched
the accepted files by SHA-256, and the app returned healthy without recreating
MariaDB or the Cloudflare tunnel.

The Landing Page section is complete. The next surgical UI work area is the
Waiter section, beginning with Tables Overview and the order-entry workflow.

## Open gates before UI completion

- Complete and accept the deterministic waiter, kitchen, management,
  inventory, schedule, and reporting workflows.
- Run the final real-user or supervised beta acceptance with restaurant-approved
  data and roles.
- Perform the selected backup restore drill and retain the evidence.
- Resolve remaining staging-versus-production configuration and ownership
  decisions.
- Revisit the UI only after the workflow contract is stable.

The AI hold can be revisited later, but it is not on the critical path for
finishing the core restaurant system.
# 2026-08-12 - Waiter shell and account-security gate

- Completed the pre-Tables waiter shell: Dashboard now owns attendance,
  schedule, password, clock-out, and logout actions.
- Added first-login password replacement and safe reuse of login names from
  inactive staff without deleting historical records.
- Reduced Waiter navigation to Dashboard and Tables & Orders and added an
  explicit landscape sidebar control.
- Tables & Orders visual/function inspection remains the next phase.

# 2026-08-12 - Tables and menu preview populated

- Corrected the navigation-heading focus presentation without weakening
  keyboard focus visibility for interactive controls.
- Populated only the isolated waiter-shell preview with 12 tables and 12 menu
  items across four categories, including prices and preparation times.
- Added matching contained menu photographs to create realistic visual load
  before the Tables & Orders UI inspection.
- The live database and production-facing application remain unchanged.

# 2026-08-12 - Gate 0 Waiter visual checkpoint

- Froze the accepted landing page and the current pre-personal-dashboard
  Waiter shell as formal desktop, landscape, portrait, and phone-landscape
  screenshots.
- Captured authenticated Waiter evidence through a disposable clone of the
  isolated preview so neither the accepted preview database nor the live
  instance needed a password reset or evidence-only account.
- Recorded the exact source commit, preview image, data counts, viewport
  dimensions, limitations, security boundaries, and SHA-256 hashes.
- Made no feature-code, schema, live database, tunnel, or production changes.
- Gate 0 evidence is in
  `docs/UI/evidence/checkpoints/2026-08-12-waiter-before-personal-dashboard/`.
- Backend work remains blocked until the project owner reviews this checkpoint.

# 2026-08-12 - External Audit #6 requested

- Restored the already-completed External Audit #5 to the authoritative `main`
  audit timeline through PR #14, preserving the original independent report.
- Closed the obsolete stacked Audit #5 PR #6 with a traceable reference to its
  replacement rather than leaving two competing audit pull requests open.
- Prepared an independent-review handoff for Audit #6 against PR #13 and the
  Gate 0 Waiter checkpoint.
- Audit #6 remains pending: the handoff is not an audit verdict, and Gate 1
  implementation remains blocked until the independent report is received and
  assessed by the project owner.

## 2026-08-14 - Gate 2 accepted; restaurant identity and clock foundation

The project owner accepted the final Waiter Dashboard implementation plan
after independent review. Gate 2A then established one modular Chef Doy's
restaurant profile, local-only replacement assets with controlled fallbacks,
and a server-authoritative Asia/Manila clock while preserving UTC persistence.
The accepted landing page now consumes the shared profile without a visual
redesign. Gate 1 session security, tenant identity, workflow behavior, and the
live instance were not changed. Gate 2B — the restricted Waiter Dashboard read
model — is the next implementation gate after Gate 2A CI acceptance.
# 2026-08-14 — Gate 2B: restricted Waiter Dashboard contract

Gate 2B established the server-authorized read boundary that the accepted Waiter Dashboard will later render. The signed-in Waiter's immutable Identity ID selects the data; active Waiter role membership is mandatory. Restaurant-local date/shift calculations and Monday weekly hours use the Gate 2A Manila clock. The checkpoint intentionally contains no final UI or new database tables.

# 2026-08-14 — Gate 2C: attendance and floor eligibility

Gate 2C made an open attendance record the server-side requirement for Waiter floor commands. Logout and the 15-minute application-session timeout remain separate from attendance. Forgotten scheduled and unscheduled records now close at their exact 12-hour boundaries, require Manager review, and produce an audit trail. Restart, duplicate execution, a real MariaDB two-worker race, existing order concurrency, and the 60-order stress workflow were verified. Gate 2D — staff portraits and the date-based Today's Team carousel — is next.

# 2026-08-20 — Gate 2D: staff profiles and privacy-safe Today's Team

Gate 2D added staff-profile portrait metadata and lifecycle state through an
EF Core migration, then established a server-authoritative, Asia/Manila
calendar-date Team read model. It returns only a local portrait reference and a
fallback flag; employee names, roles, identifiers, schedules, and contacts are
not exposed. Only active, approved profiles scheduled on the current date are
eligible. Missing, remote, and unsupported paths use a neutral local fallback.

Development-only fixtures provide ten synthetic non-login profiles with local
SVG placeholders, schedule coverage, attendance history, and audited seed
events. Focused Gate 2D tests passed 8/8; domain and command-gateway suites
also passed. A workstation-local unfiltered Integration/E2E runner hang was
recorded separately for CI follow-up rather than treated as a Gate 2D feature
failure. Full evidence and asset-replacement rules are retained under
`docs/testing/GATE_2D_PROFILES_TODAYS_TEAM_EVIDENCE_2026-08-20.md` and
`docs/UI/Gate 2/MODULAR_RESTAURANT_ASSET_REGISTRY_2026-08-14.md`.
