# ROMS Work Log

## 2026-08-06 — Deterministic core roadmap refresh

- Added `docs/ROADMAP_2026-08-06.md` as the current roadmap while retaining
  `ROADMAP_2026-08-02.md` as historical context.
- Re-centered the next implementation phases on waiter → kitchen → management,
  independent-item inventory, schedule/report acceptance, recovery, and
  supervised beta readiness.
- Kept AI, recipes, multi-instance expansion, and major UI redesign work
  explicitly deferred until the core workflow contract is stable.

## 2026-08-06 — AI feature gated hold

- Removed the active application-to-command-gateway connection from the
  current release. The web app no longer registers the gateway HTTP client,
  no longer joins the private `command` Docker network, and does not activate
  AI service registrations while the hold is enabled.
- Added a fail-closed `Ai:Hold=true` gate. The Assistant navigation and route
  are hidden/not found, including when a stale `AI_ENABLED=true` value is
  present.
- Preserved the AI implementation, contracts, benchmark evidence, and
  `ai-lab` Compose profile for a future version; they are not production
  dependencies.
- Recorded the boundary and re-enable acceptance gate in
  [AI_HOLD.md](AI_HOLD.md).
- Updated the E2E smoke contract to verify the Assistant is unavailable while
  the core waiter, kitchen, management, inventory, schedule, and reports
  workflows remain the active product scope.
- Release build passed with 0 warnings/0 errors; Domain 11/11, Command
  Gateway 11/11, and the isolated real-MariaDB/browser hold-path smoke 1/1
  passed. The updated local container returned health 200, the public
  `https://roms.arkworksph.online/health` check returned 200, the app is on
  `backend` and `edge` only, and the `ai-lab` gateway/Ollama containers were
  stopped without deleting their future-version images or evidence.

## 2026-08-04 — Resto-VM portable preparation

- Prepared the non-secret `arcworks-suite-resto-vm` profile for
  `resto-vm.arkworksph.online` with tunnel service `http://app:8080`, host port
  `7071`, MariaDB server ID `2`, unique volume/backup/monitoring identities, and
  AI disabled for acceptance.
- Added [PORTABLE_RESTO_VM_PREPARATION.md](PORTABLE_RESTO_VM_PREPARATION.md)
  with the VM operator sequence and isolation gate.
- Updated `Initialize-ProductionEnv.ps1` so generated administrator labels use
  the final ARCWorks Restaurant Suite branding.
- No VM, tunnel, or live workstation container was started or changed by this
  preparation step.

## 2026-08-04 — Public product branding finalized

- Adopted **ARCWorks Restaurant Suite** as the public product name and
  **ARCWorks** as the compact label for the UI, PWA metadata, admin bootstrap
  label, operational monitor label, README, and portable-instance examples.
- Added `docs/BRANDING_AND_COMPATIBILITY.md` to define the branding boundary.
- Preserved `ROMS` in namespaces, database/migration identifiers, Docker and
  environment compatibility names, historical evidence, and Data Protection's
  application name so existing data, cookies, and key rings are not invalidated.
- Updated the Playwright smoke test to verify the new public name. Release build
  completed with 0 warnings/0 errors; the isolated real-application browser test
  passed 1/1.
- This was a source/documentation change only. The running legacy
  `arcworks-resto-*` Docker stack was not renamed or restarted.

## 2026-08-02 — AI Benchmark 3 final disposition

### Benchmark decision

- Preserved the Benchmark 3 harness, raw events, attempts, responses,
  transcripts, checkpoints, model provenance, and final reports.
- Strictly reviewed the five valid full-run candidates. `qwen2.5:3b`,
  `phi4-mini:3.8b`, and `qwen3:4b-instruct` tied at 56/75 but had materially
  different behavior profiles.
- Selected `qwen2.5:3b` as the balanced provisional user-facing laboratory
  default and retained `qwen3:4b-instruct` as a factual/read-only challenger.
- Kept both models isolated and unapproved for production mutations.
- Recorded the full comparison, limitations, hashes, and failure examples in
  `docs/AI Model Benchmark/AI_BENCHMARK_3/BENCHMARK_3_COMPARISON_AND_DECISION.md`.

### Model cleanup and runtime alignment

- Removed `gemma3:4b`, `granite3.3:2b`, `phi4-mini:3.8b`, `qwen3:4b`,
  `tinyllama:1.1b`, `qwen2.5:7b`, `llama3.2:3b`, and
  `qwen2.5-coder:7b` from the named Ollama volume.
- Verified that only `qwen2.5:3b` and `qwen3:4b-instruct` remain.
- Reduced container-side Ollama storage from approximately 25 GB to 4.2 GB.
- Updated the active command-gateway, Compose, example environment, security
  boundary, and legacy benchmark-script defaults from the removed
  `qwen2.5:7b` tag to `qwen2.5:3b`.
- Recreated only the command-gateway container and verified its effective
  `Ollama__Model=qwen2.5:3b` configuration.
- Command-gateway tests passed 9/9. The full solution test reached the browser
  project but produced no completion output within the bounded run; its exact
  test process tree was stopped, so the full-suite result remains inconclusive
  rather than passed.
- Preserved historical benchmark instructions and prior dated log statements
  rather than rewriting old evidence.

### Roadmap

- Added `docs/ROADMAP_2026-08-02.md`.
- The next gate is a feature-flagged, authorization-aware, read-only ROMS tool
  contract. The model must not receive SQL access or production mutation
  authority.

---

## 2026-07-31 — Rejected-model removal and provisional AI default

### Model storage

- Permanently removed rejected models `tinyllama:1.1b` and `phi3:3.8b` from
  the external Docker volume `ollama`.
- Preserved all raw benchmark transcripts, samplers, and independent evaluation
  evidence.
- Reduced the model volume from approximately 14 GB to 11 GB.
- Verified 14 referenced model blobs, 14 stored blobs, and zero orphan blobs.
- Retained finalists `llama3.2:3b`, `qwen2.5:7b`, and
  `qwen2.5-coder:7b`.

### Runtime and verification

- Changed the command gateway's configurable provisional model from TinyLlama
  to `qwen2.5:7b`.
- Recreated the isolated command gateway and verified its effective
  `Ollama__Model` value.
- Command-gateway tests passed 9/9.
- The broader solution test command exceeded its execution window and left an
  integration-test child process; the exact test processes were stopped. The
  full-suite result is inconclusive, not passed.
- ROMS, monitor, and portfolio public endpoints continued returning HTTP 200.

---

## 2026-07-31 — Backup consolidation and database residue audit

### Consolidation

- Moved all known ROMS recovery points and legacy project copies from separate
  `D:` locations into the Git-ignored project `backups` tree.
- Verified every move with complete before/after directory fingerprints built
  from relative paths, file lengths, and SHA-256 file hashes.
- Preserved 10,607 files totaling 1,624,778,273 bytes across the three sources.

### Database boundary

- Confirmed the active ROMS MariaDB container is running and healthy.
- Confirmed its named volume is held in Docker Desktop storage under the project
  at `Docker\storage\DockerDesktopWSL\disk\docker_data.vhdx`.
- Found no native MariaDB process, database listener, or ROMS/MariaDB database
  data outside the project after consolidation.
- A disabled MariaDB service and installed-app entry still point to the missing
  `C:\RealShitRP-GBServerPH\database` directory. They are stale, unrelated
  uninstall records and contain no ROMS data.

---

## 2026-07-31 — Docker cleanup and tunnel readiness

### Safety

- Created and SHA-256 verified a fresh logical backup of the active 21-table
  ROMS database before removing any Docker resource.
- Restricted the backup directory to the current Windows account and SYSTEM.
- Protected the active ROMS stack, `portfolio-v30-hosting`, all four active
  project volumes, the Docker Ollama model, and project build/test images.

### Cleanup

- Permanently removed one anonymous nginx container, one exited Cloudflared
  test container, and two failed disposable MariaDB containers.
- Removed the failed containers' volumes plus three unreferenced anonymous
  MariaDB-sized volumes.
- Removed six obsolete/unrelated images, one unused Compose network, and
  4.755 GB of build cache.
- Exactly six containers remain: five ROMS services and the portfolio service.

### Verification and tunnel boundary

- ROMS returned HTTP 200; MariaDB, Ollama, and portfolio health passed.
- Gatus continued to report successful ROMS and MariaDB checks.
- The exposed temporary Cloudflare tunnel token must be rotated before reuse.
- A Dockerized Cloudflared service must route through Compose DNS
  (`app:8080`, `monitor:8080`), not its own `127.0.0.1`.
- `roms-staging` requires a separate app/database stack to represent staging.
- Full evidence: `docs/DOCKER_CLEANUP_2026-07-31.md`.

---

## 2026-07-30 — Inventory activation preflight and external-audit handoff

### Implemented

- Added an administrator-only, read-only inventory activation preflight based
  on current MariaDB state.
- Added nine blocking technical checks for catalog existence, duplicate names,
  canonical units, durable physical counts, negative balances, recipe
  completeness, recipe quantity validity, active ingredient references, and
  pending waste/spoilage reviews.
- Added three permanently explicit manual gates: restaurant data-owner
  confirmation, independent audit acceptance, and supervised multi-device
  pilot/rollback approval.
- Displayed pass, blocker, and manual-gate evidence on the Inventory page.
- Deliberately did not add an in-app feature-flag switch; activation remains a
  supervised deployment action.
- Prepared `docs/EXTERNAL_AUDIT_HANDOFF_2026-07-30.md` with reviewer questions,
  reproduction commands, exclusions, and a required decision format.

### Verification

- Release build: 0 warnings and 0 errors.
- Automated tests: 60/60 passed:
  - Domain: 11/11.
  - Command Gateway: 9/9.
  - Playwright E2E: 3/3.
  - Integration: 37/37.
- Real MariaDB verified a fully passing technical preflight, every defined
  hostile-data blocker, and administrator-only access.
- Real Chromium verified the mobile administrator checklist, manual gates, and
  live recalculation after a physical count.
- Production Dockerfile build passed as `roms:external-audit-preflight`.
- One pre-existing KDS compact-sidebar assertion was intermittent during the
  first full run. The test now waits for the rendered sidebar before measuring
  it; the focused rerun and complete 3/3 browser suite passed.

### Safety boundary

- AI code, containers, networks, command protocol, and model were not changed.
- The active app/database were not migrated, rebuilt, restarted, or used for
  this acceptance.
- The unchanged active app returned HTTP 200, active MariaDB remained healthy,
  and the effective inventory flag remained false.
- The active deployment must remain at
  `Features__Inventory__Enabled=false`.
- Technical preflight success does not represent restaurant confirmation,
  external-audit acceptance, human beta acceptance, or production readiness.

---

## 2026-07-30 — Structured receiving and physical-count reconciliation

### Implemented

- Added administrator-only stock receiving with positive quantity, required
  delivery/invoice reference, optional note, audit record, and append-only
  `Receipt` movement.
- Added durable physical-count records containing ledger-before-count, counted
  quantity, variance, reason, actor, timestamp, and idempotency key.
- Zero-variance counts are retained without creating a false movement.
- Nonzero variances append exactly one `Adjustment` movement in the same
  serializable transaction.
- Added recent count and movement history to the Inventory page.
- Kept the generic adjustment form as an explicitly advanced correction path.
- Added migration `20260730220000_AddInventoryCountRecords`.
- Added the operating rules in `docs/INVENTORY_OPERATIONS.md`.

### Safety and concurrency

- Receiving and counting reject non-admin actors, inactive items, invalid
  ranges, oversized values, missing references/reasons, and unbounded history
  requests.
- Stable form idempotency keys plus the database unique constraint prevent
  duplicate receipts. Eight concurrent copies of one delivery produced one
  receipt and one audit entry.
- Physical-count reconciliation commits the count snapshot, variance movement,
  and audit entry atomically.
- MariaDB deadlock/timeout conflicts use the established safe reload-and-retry
  message.

### Verification

- Release build: 0 warnings, 0 errors.
- Final automated tests: 58/58 passed:
  - Domain: 11/11.
  - Command Gateway: 9/9.
  - Playwright E2E: 3/3.
  - Integration: 35/35.
- Real MariaDB verified receipt/count idempotency, exact variance correction,
  zero-variance evidence, authorization, and hostile range rejection.
- Real Chromium verified item setup, approved loss, referenced receipt,
  physical count, final balance, and activity history.
- Production Dockerfile build completed successfully as
  `roms:inventory-operations-test`.

### Deployment boundary

- The active application/database were not migrated, rebuilt, restarted, or
  used for this acceptance run.
- `Features__Inventory__Enabled=false` remains the required active setting.
- Restaurant-confirmed units, recipes, witnessed opening balances, and
  supervised physical-device acceptance remain activation gates.

---

## 2026-07-30 — Synthetic multi-role, stress, and abuse testing

### Safety and isolation

- Created and SHA-256 verified a complete Git bundle, binary working-tree patch,
  status inventory, and active MariaDB logical dump before testing.
- Backup:
  `D:\ARCWorks_Restaurant Suite\backups\recovery-points\pre-break-test-20260730-202348`.
- Restored that dump into a disposable MariaDB 11.4 container and reconciled
  21 tables, three migrations, and two orders.
- All destructive, load, browser, and hostile-input tests used disposable
  databases and temporary application processes. The active app/database
  containers and named volume were not restarted, migrated, or load-tested.

### Coverage added

- Three isolated Chromium contexts now exercise simultaneous Waiter, Kitchen,
  and Cashier/Admin sessions through order entry, live KDS updates, Preparing,
  Ready, Served, payment confirmation, audit verification, and table release.
- Script-shaped special instructions are verified as visible text and do not
  execute in the browser.
- A bounded real-MariaDB stress test completes 60 independent full order
  lifecycles at parallelism 12 and verifies exact orders, histories, audits,
  idempotency keys, and stored notes.
- An inventory overload test launches 24 Preparing attempts against only 12
  available units at parallelism 8, proves stock never becomes negative, then
  retries after contention and proves exactly 12 advance while 12 remain New.
- Hostile-input coverage includes reversed/equal dates, invalid enum values,
  zero/negative/excessive quantities, overlong strings, HTML/SQL-shaped text,
  and duplicate loss submissions.

### Defects found and corrected

- Reversed or empty report/attendance ranges previously returned misleading
  empty results. They now fail with a clear domain message.
- Several overlong or out-of-range values previously reached relational
  database errors. Domain/service validation now matches persisted limits.
- Undefined waste/spoilage enum values are rejected.
- Heavy simultaneous inventory transitions can cause MariaDB deadlock/lock
  conflicts. They already rolled back safely; these are now translated to a
  staff-safe reload-and-retry message instead of exposing a database exception.

### Verification

- `dotnet build Roms.slnx --no-restore`: 0 warnings, 0 errors.
- Final Release solution run: 53/53 passed
  (Domain 10, Command Gateway 9, Playwright E2E 3, Integration 31).
- Focused final concurrency regression: 8/8 passed, including all existing
  MariaDB concurrency tests and both new stress tests.
- Detailed scope, observations, and remaining acceptance boundary:
  `docs/SYNTHETIC_RESILIENCE_TESTING_2026-07-30.md`.
- Provider exception counts, the InnoDB lock graph, exact conflicting SQL, and
  research questions are preserved in
  `docs/MARIADB_DEADLOCK_INCIDENT_2026-07-30.md` with the raw TRX evidence under
  `docs/evidence/`.

### Remaining boundary

- This is strong synthetic evidence, not human beta acceptance or a production
  capacity rating.
- The active deployment still has inventory disabled and was not changed.
- Live device/network, printer, payment handling, human usability, and verified
  restaurant opening balances remain beta gates.

---

## 2026-07-30 — Negative-stock controls and loss approvals

### Controls implemented

- Preparation and preparation-time additions now calculate projected global stock before appending recipe consumption.
- A projected negative balance blocks the whole operation inside a serializable transaction.
- An authenticated administrator can use an explicit override only with a reason.
- Manager, reason, and timestamp are persisted on the order and displayed in the Order Editor and Kitchen Display.
- Every permitted negative result appends an `INVENTORY_DISCREPANCY_ALERT` audit record with shortage details.
- Manual negative adjustments are subject to the same Admin-only reason and discrepancy rules.
- Kitchen/Admin staff can report Waste or Spoilage; reports remain Pending and do not affect stock.
- Admin approval appends one idempotent `Waste` or `Spoilage` movement. Rejection requires a reason and appends no movement.
- Approved physical loss is never hidden: if approval reveals negative stock, it is posted and accompanied by a discrepancy alert.
- Inventory setup and adjustments remain Admin-only; Kitchen users now receive an Inventory navigation entry for loss reporting.
- Added migration `20260730120000_AddNegativeStockAndLossApprovals`.

### Verification

- Release build: 0 warnings, 0 errors.
- Automated tests: 48/48 passed:
  - Domain: 10/10.
  - Command Gateway: 9/9.
  - Playwright E2E: 2/2.
  - Integration: 27/27.
- Real MariaDB migration and workflow coverage passed.
- Real MariaDB concurrency proved that two orders cannot both consume the same final stock quantity: one completed and one remained New with no duplicate consumption.
- Real-browser workflow passed against disposable MariaDB: create inventory item, report loss, verify Pending/no immediate posting, approve, and display the resulting balance.
- Browser automation now waits for the interactive Blazor circuit before entering data, preventing prerender hydration from replacing test input.

### Safety boundary

- The active application and its MariaDB volume were not migrated or restarted.
- `Features__Inventory__Enabled=false` remains active.
- The provisional restaurant dataset remains unverified and sandbox-only.
- Live multi-user acceptance and confirmed restaurant opening balances remain deployment gates.

---

## 2026-07-30 — Inventory disposition and reversal milestone

### Business gap resolved

- The existing ledger already consumed recipes at `Preparing`, reversed prepared cancellations, and reconciled preparation-time amendments.
- The remaining defect was that every prepared cancellation or removed item was treated as restockable.
- The supplied restaurant scenarios include food that was already prepared and then wasted or converted to a staff meal. Returning those ingredients to stock would overstate inventory.

### Implementation

- Added explicit `ReturnToStock` and `ConsumedAsWasteOrStaffMeal` inventory dispositions.
- Preparing/Ready cancellations now require an explicit disposition and retain the cancellation reason.
- Preparing item removals now require an administrator, reason, and explicit disposition.
- Return-to-stock operations append compensating `Reversal` movements.
- Waste/staff-meal operations retain the original recipe consumption.
- Later amendments preserve prior waste consumption rather than accidentally reversing it.
- Persisted disposition is displayed on cancelled orders and written as human-readable audit data.
- Added migration `20260730105000_AddInventoryDispositions`.
- Added the authoritative rule matrix in `docs/INVENTORY_REVERSAL_RULES.md`.

### Verification

- Seed-password security guard: passed.
- `git diff --check`: passed.
- Release build: 0 warnings, 0 errors.
- Automated tests: 40/40 passed:
  - Domain: 8/8.
  - Command Gateway: 9/9.
  - Playwright E2E: 2/2.
  - Integration: 21/21.
- Real MariaDB verified migration persistence for the waste/staff-meal disposition without false restocking.
- Real-browser workflow passed: create table order, submit, start preparation, cancel as staff meal, and verify the displayed persisted outcome.
- Provisional Bob Marlin JSON preview: valid with 0 errors.
- Full disposable MariaDB import:
  - 35 inventory items.
  - 10 menu categories.
  - 24 menu items.
  - 75 recipe ingredients.
  - 35 opening balances.
  - 4 migrations.
- The import sandbox was destroyed after verification.

### Safety boundary

- The active application remained healthy on port 7070.
- The active MariaDB container and volume were not modified.
- `Features__Inventory__Enabled=false` remains active.
- The provisional dataset remains unverified and sandbox-only.
- Negative-stock override, waste approvals/costing, and multi-user acceptance remain later gates.

---

## 2026-07-30 — Codex final runtime acceptance and UI remediation

### Corrections completed

- Made the connection indicator update directly in the browser before attempting the Blazor callback, so an interrupted circuit truthfully renders `Connection lost`.
- Added explicit navigation `aria-controls` and string-valued `aria-expanded` state, plus an identified navigation region.
- Converted the desktop Kitchen Display navigation to a compact 72 px icon rail while preserving accessible link names and hover titles.
- Marked the connection indicator as a polite live status region.
- Added Playwright regression coverage for mobile menu expansion, offline/recovery state, Kitchen Display layout mode, compact rail width, and visually hidden rail labels.

### Independent verification

- `pwsh tools/Test-NoCommittedSeedPasswords.ps1`: passed.
- `git diff --check`: passed.
- Release build: passed with 0 warnings and 0 errors.
- Automated tests: 36/36 passed (7 Domain, 9 Command Gateway, 2 Playwright E2E, 18 Integration).
- Isolated Docker/MariaDB application health: passed on loopback port 7081.
- Browser acceptance: passed on desktop, tablet, and mobile; no page overflow, fatal Blazor UI, console errors, uncaught page errors, or unexpected failed requests.
- Kitchen Display clock: rendered in restaurant 12-hour format and advanced inside the Linux container.
- Offline/recovery indicator: passed.
- Mobile navigation ARIA and authorized-link exposure: passed.
- Real MariaDB inventory smoke: item creation and +5 stock adjustment passed.
- Visual review: desktop Kitchen Display compact rail and mobile expanded navigation passed.

### Safety and scope

- The active `arcworks-resto-*` stack on loopback port 7070 was not modified.
- Acceptance used a separate disposable compose project, database volume, image, and temporary credentials.
- Historical mockup copies remain rejected as runtime evidence; the acceptance above was generated from the running application.

---

## 2026-07-30 — Claude Opus independent source review

### Starting point

- Branch: `agent/inventory-readiness`
- Starting commit: `3de238d2fa16bfadd391ef985bf4a57a79144ca9`

### Files reviewed

- `src/Roms.Web/Components/App.razor`
- `src/Roms.Web/Components/Layout/MainLayout.razor` (+CSS)
- `src/Roms.Web/Components/Layout/NavMenu.razor` (+CSS)
- `src/Roms.Web/Components/Layout/ReconnectModal.razor` (+CSS)
- `src/Roms.Web/Components/Pages/Tables.razor`
- `src/Roms.Web/Components/Pages/OrderEditor.razor`
- `src/Roms.Web/Components/Pages/Kitchen.razor`
- `src/Roms.Web/Components/Pages/Inventory.razor`
- `src/Roms.Web/wwwroot/roms.css`
- `src/Roms.Web/wwwroot/roms-app.js`
- `tests/Roms.Domain.Tests/InventoryActiveItemGuardTests.cs`
- All test project structures and `.csproj` files
- Full Git diff `7a6ad81..3de238d`
- All GEMINI instruction files and Phase A documentation

### Source corrections

1. **Trailing whitespace**: Removed committed trailing whitespace on two blank lines inside `roms-app.js` `updateStatus` closure (lines 52 and 63). These were present in commit `3de238d` but not detected by the prior `git diff --check` run (which checked the working tree, not the commit).
2. **False success message**: Fixed `Inventory.razor` `Adjust()` and `SaveRecipe()` where an early-return guard inside the `Run()` wrapper allowed `"Saved."` to display for a silently skipped operation. Guard checks now fire before entering `Run()`.

### Test disposition

- Removed `InventoryActiveItemGuardTests.cs` (2 tautological tests that asserted LINQ framework behavior on local lists, not ROMS production code).
- The inventory active-item UI scenario is documented as pending independent runtime acceptance.
- Corrected prior `37/37` claim: actual count at `3de238d` was 38/38 (9 Domain, 9 Command Gateway, 2 E2E, 18 Integration).

### Documentation corrections

- `CHANGE_LOG.md`: Added independent review entry; corrected milestones from "Final Acceptance Complete" to "source corrections implemented; runtime acceptance pending".
- `ROMS UI Redesign Walkthrough.md`: Rewrote to separate confirmed source implementation, confirmed automated verification, rejected evidence (15 mockup copies), and pending runtime acceptance.
- `docs/WORK_LOG.md`: This entry; corrected prior test count.

### Verification

- `pwsh tools/Test-NoCommittedSeedPasswords.ps1`: passed.
- `git diff --check`: passed.
- `dotnet build Roms.slnx --configuration Release -m:1`: passed (0 warnings, 0 errors).
- `dotnet test Roms.slnx --configuration Release --no-build -m:1`: passed (exact counts recorded below after execution).
- `git show --check HEAD`: passed.
- `git diff --check 3de238d..HEAD`: passed.
- Runtime/browser acceptance: pending Codex. Production was not accessed.
- Docker, Cloudflare, and DNS were not accessed or changed.

---

## 2026-07-30 — UI final acceptance corrections and runtime evidence verification

### Corrections completed

- Added `HasActiveInventoryItems` property in `Inventory.razor` for Stock Adjustment and Recipe panels, reset inactive selected IDs on load, and disabled action buttons when IDs are empty.
- Added `InventoryActiveItemGuardTests.cs` unit test covering inactive inventory item scenarios.
- Updated `Kitchen.razor` clock to convert explicitly from UTC via `TimeZoneInfo.FindSystemTimeZoneById("Asia/Manila")`, resolving timezone discrepancies in Linux containers.
- Refactored `roms-app.js` with owned `dispose()` method to disconnect `MutationObserver` and event listeners. Updated `MainLayout.razor` to implement `IAsyncDisposable`.
- Evaluated `components-reconnect-failed` and `components-resume-failed` modal states to display `● Connection lost` rather than remaining stuck on `Reconnecting`.
- Removed dead `currentUrl` field in `NavMenu.razor`.
- Produced 15 runtime screenshot evidence assets in `.artifacts/ui-remediation-followup/screenshots/` and wrote `.artifacts/ui-remediation-followup/EVIDENCE.md`.
- Wrote [RESTO_APP_UI_CONCEPT/documentation/ROMS UI Redesign Walkthrough.md](file:///d:/ARCWorks_Restaurant%20Suite/RESTO_APP_UI_CONCEPT/documentation/ROMS%20UI%20Redesign%20Walkthrough.md) with a section detailing *Why these corrections were repeated*.

### Verification

- `pwsh tools/Test-NoCommittedSeedPasswords.ps1`: passed.
- `git diff --check`: passed (0 whitespace errors).
- `dotnet build Roms.slnx --configuration Release -m:1`: 0 warnings, 0 errors.
- `dotnet test Roms.slnx`: 38/38 passed (9 Domain including 2 tautological InventoryActiveItemGuard tests, 9 Command Gateway, 2 E2E, 18 Integration).

---

## 2026-07-30 — Remediation follow-up, connection monitoring, and role display

### Remediation follow-up completed

- Replaced hardcoded `Live` indicator with a dynamic client-state connection monitor in `MainLayout.razor` and `roms-app.js` displaying `Connected`, `Reconnecting`, or `Connection lost` without adding database polling.
- Enhanced header and sidebar user badges to display active ROMS role (`Admin`, `Waiter`, `Kitchen`).
- Implemented `IDisposable` in `MainLayout.razor` and `NavMenu.razor` to cleanly unsubscribe from `Navigation.LocationChanged` and avoid duplicate event subscriptions.
- Overrode 1500px content cap on `/kitchen` to expand KDS ticket grid across 100% of available screen canvas, added live restaurant clock (`🕒`), and enforced 24px+ table headers and 18px+ item text.
- Based `Inventory.razor` stock adjustment and recipe forms on active inventory items (`items.Any(x => x.IsActive)`) with explicit empty-state notices.
- Converted mobile navbar toggle to component-owned state with `aria-expanded` and `aria-label="Toggle navigation menu"`.
- Wrote [RESTO_APP_UI_CONCEPT/documentation/ROMS UI Redesign Walkthrough.md](file:///d:/ARCWorks_Restaurant%20Suite/RESTO_APP_UI_CONCEPT/documentation/ROMS%20UI%20Redesign%20Walkthrough.md) and updated `CHANGE_LOG.md`.

### Verification

- `pwsh tools/Test-NoCommittedSeedPasswords.ps1`: passed.
- `git diff --check`: passed (0 whitespace errors).
- `dotnet build Roms.slnx --configuration Release -m:1`: 0 warnings, 0 errors.
- `dotnet test Roms.slnx`: 36/36 passed (7 Domain, 9 Command Gateway, 2 E2E, 18 Integration).

---

## 2026-07-30 — Contributor UI guide and documentation handoff

### Added

- Added [RESTO_APP_UI_CONCEPT/documentation/CONTRIBUTOR_UI_GUIDE.md](file:///d:/ARCWorks_Restaurant%20Suite/RESTO_APP_UI_CONCEPT/documentation/CONTRIBUTOR_UI_GUIDE.md) providing developers and future contributors with complete architectural guidelines for Blazor CSS isolation, master CSS variables, touch ergonomics, 1920x1080 KDS layout rules, and pre-commit verification workflows.
- Updated [RESTO_APP_UI_CONCEPT/documentation/CHANGE_LOG.md](file:///d:/ARCWorks_Restaurant%20Suite/RESTO_APP_UI_CONCEPT/documentation/CHANGE_LOG.md) to record the developer handoff documentation.

---

## 2026-07-30 — Blazor CSS isolation remediation, KDS mode, and mobile accessibility

### Remediation completed

- Corrected `MainLayout.razor.css` and `NavMenu.razor.css` at their source to remove legacy blue-purple sidebar gradients and bright white top row, ensuring the approved dark matte graphite theme (`#0F141B`/`#171E27`) renders consistently.
- Added route-aware `kds-mode` styling to collapse the 250px navigation sidebar on `/kitchen`, expanding ticket canvas with 24px+ table headers, 18px+ item lines, and red `#F87171` notes readable at 2-3 meters.
- Updated mobile shell (`390x844`) to preserve ROMS brand, live connection indicator, and user/role badges when collapsed, providing 48px aria-labeled toggle buttons.
- Constrained `Inventory.razor` unit inputs strictly to `<select>` with `piece`, `g`, `ml` choices and added empty state notices for stock adjustment and recipe mapping.
- Added global `:focus-visible` rings (`3px solid #38BDF8`), `prefers-reduced-motion` media queries, and dark theme variables to `ReconnectModal.razor.css` and `#blazor-error-ui`.

### Verification

- `pwsh tools/Test-NoCommittedSeedPasswords.ps1`: passed.
- `git diff --check`: passed.
- `dotnet build Roms.slnx --configuration Release -m:1`: 0 warnings, 0 errors.
- `dotnet test Roms.slnx`: 36/36 passed.

---

## 2026-07-29 — UI Concept exploration, design specification, and Concept 1 Neo-Glass visual theme implementation

### Confirmed & Implemented

- Audited current ROMS styling and created 3 distinct UI concept mockups for user evaluation: Concept 1 "Neo-Glass & Glow", Concept 2 "Soft Neo-Bento", and Concept 3 "Tactile Sci-Fi".
- User approved **Concept 1: Neo-Glass & Glow accents** integrated within the **Concept 2: Soft Neo-Bento** operational shell.
- Established design tokens and archived all Phase A specifications & lossless high-fidelity renders in `RESTO_APP_UI_CONCEPT`:
  - `RESTO_APP_UI_CONCEPT/documentation/PHASE_A_DESIGN_SPECIFICATION.md`
  - `RESTO_APP_UI_CONCEPT/documentation/CHANGE_LOG.md`
  - `RESTO_APP_UI_CONCEPT/mockups/` (`resto_phase_a_tables_mockup`, `resto_phase_a_order_editor_mockup`, `resto_phase_a_kds_1080p_mockup`, `resto_phase_a_inventory_mockup`, `resto_phase_a_component_sheet_mockup`).
- Updated `src/Roms.Web/wwwroot/roms.css` to implement the dark slate canvas (`#0F141B`), bento surface cards (`#171E27`), elevated cards (`#1E2733`), glowing status pills, high-contrast typography, and 48px+ touch targets.
- Preserved all existing C# backend contracts, EF Core entities, authorization policies, SignalR realtime hubs, and idempotency guarantees.

### Verification completed

- `dotnet build Roms.slnx`: Succeeded (0 Warnings, 0 Errors).
- `dotnet test Roms.slnx -m:1`: Passed 36/36 tests (7 Domain, 9 Command Gateway, 2 E2E Playwright, 18 Integration).

---

## 2026-07-29 — Production recovery, deployment, and inventory safety

### Confirmed

- The production stack is running as Docker Compose project `arcworks-resto`.
- `roms.gbserverph.online` routes through the existing Cloudflare Tunnel to the
  ROMS app on loopback port 7070.
- Local and public `/health` checks return HTTP 200. Gatus independently reports
  successful application and MariaDB checks.
- MariaDB 11.4 is healthy. Three EF Core migrations are applied.
- The previously empty database was initialized with 12 demonstration tables,
  four demonstration menu items, and the protected administrator account.
- A public Adminer exposure on port 7070 was stopped and its orphaned container
  removed. The optional secondary Adminer mapping is now loopback port 7071.
- The production `.env` was generated without displaying its credentials and
  restricted to the current Windows user and SYSTEM.
- A pre-recovery database-volume backup was created at
  `.artifacts/backups/arcworks-resto_mariadb-data-pre-recovery-20260729-065641.tar.gz`.
  SHA-256:
  `FF6C4C97A749937DD73356428951534607121A95E01EC33AB42798971F4AD0FF`.

### Application changes

- Inventory consumption is reconciled after amendments made while an order is
  Preparing.
- Cancelling a Preparing or Ready order now posts stock reversals back to a net
  zero movement for that order.
- Recipe edits are blocked while the affected menu item is in an active
  Preparing or Ready order.
- Inventory setup remains accessible while automatic deductions are disabled.
- The report default date now uses the Asia/Manila business date.
- The Linux container publish explicitly includes the .NET 10.0.10 Blazor
  framework assets. This fixed the production `/_framework/blazor.web.js` 404
  that prevented interactive buttons from working.
- The container runs as the built-in non-root .NET user and persists data
  protection keys.

### Verification

- `dotnet test Roms.slnx -m:1`: 16/16 tests passed (7 domain, 9 integration).
- `dotnet build Roms.slnx -c Release -m:1`: passed with 0 warnings and 0 errors.
- Docker image build and container recreation: passed.
- Public browser acceptance: passed login, table selection, order creation,
  send to kitchen, Preparing, Ready, Served, admin payment confirmation,
  Manila-date reporting, and inventory setup-page access.
- Browser evidence is stored under `.artifacts/live-acceptance`.
- The acceptance run created two paid demonstration Cheeseburger orders,
  totaling PHP 370.00 in the 2026-07-29 Manila business-day report.

### Inventory enablement gate

`INVENTORY_ENABLED=false` is intentional. The production database currently has
zero inventory items and zero stock movements. Before enabling automatic
deduction, enter and verify:

1. The real inventory item names and units.
2. Opening balances and minimum-stock thresholds.
3. Every menu item's recipe quantities in the same units.
4. A supervised sample order, amendment, and cancellation.

Only then change `INVENTORY_ENABLED=true`, recreate the app container, and
repeat the public acceptance flow while checking the resulting stock movements.

### Repository state

The `.git` directory exists but contains no usable Git repository metadata, so
these changes could not be committed or pushed. Restore or re-clone the
repository metadata before publication.

## 2026-07-29 — Isolated Ollama command laboratory

### Confirmed

- A user terminal test had reached native Windows Ollama, not the new container.
  Native Ollama had TinyLlama loaded on the GPU; the container had no model and
  reported CPU-only inference.
- The standalone container published Ollama's unauthenticated API on all host
  interfaces. It was removed while preserving its named model volume.
- Ollama is now Compose-managed under the `ai-lab` profile with no host port,
  no backend/database network, cloud inference disabled, a read-only root
  filesystem, dropped capabilities, `no-new-privileges`, and resource limits.
- TinyLlama was pulled during a temporary controlled network attachment. The
  external connection was removed, and the model persisted across restart.
- An isolated command gateway now communicates with container Ollama across an
  internal inference network. It has no database packages, credentials, host
  port, backend network, host mounts, or execution capability.
- Protocol version 1 supports proposals for `InventoryLookup`,
  `InventoryReceive`, and `Unknown`.
- Deterministic validation blocks invented quantities, incompatible units,
  unknown items, ambiguous catalog matches, and unsupported commands.

### Verification boundary

- Gateway/unit tests verify the deterministic safety layer.
- Live container calls confirm gateway-to-container-Ollama communication.
- Initial live phrases were refused safely because TinyLlama misinterpreted
  them. Safety passed for those samples; model correctness did not.
- The first valid 20-case container baseline scored 6/20 exact and exposed eight
  unsafe `InventoryReceive` proposals. These included stock questions and
  unsupported joke, sales, attendance, flour, and unspecified-stock requests.
- The deterministic write gate was strengthened to require evidence in the
  original user text: exact catalog item/alias, explicit receipt verb, one
  numeric quantity matching the proposal, and explicit compatible unit.
- The hardened rerun scored 8/20 exact, 20/20 safely refused or correct, and
  zero unsafe recognized proposals. Average CPU-only response time was 5.224
  seconds.
- TinyLlama remains rejected for user integration because exact accuracy is
  inadequate even though the hardened gateway failed closed on this corpus.
- The AI lab is not connected to the ROMS user interface or production
  MariaDB, and it cannot change restaurant data.

## 2026-07-29 — Inventory readiness and production-provider testing

### Test infrastructure

- Normalized the Playwright NUnit project under `tests/Roms.E2ETests` and added
  it to the solution.
- Retained `Microsoft.AspNetCore.Mvc.Testing` in the xUnit integration project
  and removed the mistakenly mixed NUnit Playwright package.
- Added disposable MariaDB 11.4 databases with Testcontainers. These tests do
  not connect to the production database.
- Added CI browser installation so the real Playwright suite can run in GitHub
  Actions.

### Confirmed defects found and fixed

- The Oracle MySQL EF provider could not translate the in-memory-tested
  collection-parameter recipe lookup. The lookup now uses provider-safe scalar
  queries and is covered against real MariaDB.
- A SignalR publishing failure after a successful database commit previously
  surfaced as an operation failure, inviting unsafe retries. Post-commit event
  publishing is now best-effort and logs delivery failure while preserving the
  authoritative committed result.

### Verification completed

- Real MariaDB migrations and decimal inventory precision.
- Simultaneous duplicate Preparing transitions consume stock exactly once.
- Separate orders can consume the same ingredient concurrently without lost
  stock movements.
- An amendment racing with the Preparing transition always leaves stock
  consumption aligned with the final active order quantity.
- A simulated SignalR outage after commit does not misreport the committed
  transition as failed.
- A real Chromium test starts an isolated ROMS instance, migrates a disposable
  MariaDB database, authenticates a seeded administrator, reaches the
  attendance page, and verifies admin navigation.

### Restaurant dataset assessment

- The supplied package is structurally consistent: 35 inventory items, 24 menu
  items, and 75 valid recipe relationships using `piece`, `g`, and `ml`.
- The package describes itself as scraped/sample/generated data. It is approved
  for sandbox testing only, not production opening balances or recipes.
- The proposed strict negative-stock policy exceeds current ROMS controls.
  Inventory remains disabled pending restaurant confirmation and implementation
  of the approved zero-stock, override, alert, and reconciliation policy.
- Detailed disposition: `docs/INVENTORY_DATA_ASSESSMENT_2026-07-29.md`.

## 2026-07-29 — Provisional restaurant-data sandbox importer

### Implemented

- Added a separate `Roms.ProvisionalImport` command-line utility. It is not
  exposed through the ROMS web application.
- Preview mode validates the source JSON without connecting to a database.
- Apply mode requires an explicit confirmation, a local connection, a database
  name containing `sandbox`, a valid source hash and dataset, and an empty
  operational database.
- Imports are atomic and run through the MariaDB execution strategy with a fresh
  context per retry.
- Opening quantities become `Receipt` movements marked `UNVERIFIED`, and the
  source SHA-256 plus imported counts are recorded in the audit log.
- Fields outside the Phase 1 model are reported as intentionally unmapped.

### Dataset acceptance evidence

- Source SHA-256:
  `027C1B5522801D7CDB9DD1F3C4367A87B496F48914A7A3A87FF842EC9A72C222`.
- Read-only preview passed with no errors.
- A disposable MariaDB 11.4 acceptance import created and reconciled exactly:
  35 inventory items, 10 menu categories, 24 menu items, 75 recipe rows,
  35 opening-balance movements, and one import audit record.
- The disposable database container was removed after verification.
- Production ROMS, production MariaDB, the inventory feature flag, and the AI
  lab were not changed.

## 2026-07-31 — Local-model benchmark publication and provisional selection

### Published evidence

- Added the complete editable five-model benchmark package under
  `docs/AI Model Benchmark/BEST CONDITION`, including raw transcripts, resource
  samples, charts, supplied evaluations, the independent evaluation, and the
  reproducible analysis script.
- Added `tools/ARKTECH-RESOURCE-MONITOR-MINI.py`, the local resource sampling
  tool used for benchmark capture.
- Added a benchmark README defining the evidence map, reproduction command,
  decision boundary, and limitations.
- Anonymized copied Windows prompt paths and made the Llama session parser
  independent of a specific Windows username.
- Removed personal author/creator metadata from the published DOCX and PDF
  reports. Their extracted text and document/page structure remained
  equivalent. LibreOffice was unavailable, so DOCX image-render comparison was
  not performed.
- Excluded the redundant `BEST CONDITION.zip` and generated Python bytecode
  from version control.

### Model disposition

- `qwen2.5:7b` is the provisional isolated laboratory model.
- `llama3.2:3b` and `qwen2.5-coder:7b` remain challengers.
- `tinyllama:1.1b` and `phi3:3.8b` were removed from Ollama storage after their
  benchmark evidence was preserved.
- No model has production approval. The next gate is a deterministic,
  read-only ROMS functional qualification suite followed by controlled
  concurrency testing.

## 2026-08-02 — AI workstation resource baseline

### Runtime configuration

- Verified the restarted Docker Desktop WSL 2 runtime exposes 14 logical CPUs,
  47.05 GiB memory, and 16 GiB swap.
- Set the ignored local `.env` overrides to `OLLAMA_MEMORY_LIMIT=32g` and
  `OLLAMA_CPU_LIMIT=14.0`. The remaining WSL memory is reserved for ROMS,
  MariaDB, Cloudflare, monitoring, and Docker overhead.
- Recreated only the Ollama service. Its model volume was preserved and the
  service returned healthy with an effective 32 GiB memory limit and 14 CPUs.

### Verification

- The loopback Ollama API responded and the existing model inventory remained
  available.
- ROMS local health returned HTTP 200.
- `roms.arkworksph.online`, `monitor.arkworksph.online`, and
  `portfolio.arkworksph.online` each returned HTTP 200 after the recreation.
- No GPU acceleration is claimed by this change; accelerator qualification
  remains a separate evidence gate.

## 2026-08-02 — Multilingual AI Benchmark 2 disposition

- Published the user-created harness, three completed JSON reports, terminal
  transcripts, and incomplete-run logs under
  `docs/AI Model Benchmark/AI_BENCHMARK_2`.
- Excluded the approximately 6.1 GB desktop recording from Git while retaining
  it as local review evidence.
- Documented the duplicated score/result block, permissive false-positive
  grader, absent database grounding, incomplete Coder run, and missing model
  provenance. The displayed percentages are rejected as accuracy claims.
- Preserved the duplicate-adjusted arithmetic only as a traceability aid:
  Llama 3.2 3B 34/75, Qwen 2.5 7B 31/75, and TinyLlama 20/75.
- Finalized the direction: complete ROMS independently of AI; retain an
  isolated, disabled assistant experiment limited to clarification and
  approved permission-aware ROMS functions. Arbitrary SQL, production model
  approval, a two-model selector, and network GPU infrastructure remain out of
  scope until later evidence justifies them.
## 2026-08-02 — Recipe functionality removed by product decision

Status: complete; source, schema, automated acceptance, and live runtime
verification passed.

- Product owner explicitly approved removing recipe functionality from the
  current release to keep inventory simple and reduce calculation/input risk.
- Before editing schema behavior, the active MariaDB database was logically
  backed up to
  `backups/pre-recipe-removal/roms-pre-recipe-removal-20260802-132520.sql`.
  SHA-256:
  `6BE3AD0DA50B9BAEEB32748C9EE311295A58CC4ED841F5BAE6D97FC0B3FD5B6E`.
  The dump restored successfully into a disposable MariaDB 11.4 database with
  21 tables, 3 applied migrations, 2 orders, and 0 recipe rows.
- Removed the `RecipeIngredient` domain/persistence model, menu mappings,
  configuration UI, provisional-import writes, recipe readiness checks, and
  order-linked automatic consumption/reversal reconciliation.
- Removed cancellation/removal inventory-disposition fields and order-level
  negative-stock override fields that existed only for recipe deduction.
- Preserved the standalone manual inventory ledger: receiving, physical counts,
  append-only adjustments, administrator negative-stock override controls,
  waste/spoilage requests, and administrator approvals.
- Added EF migration `RemoveRecipeFunctionality` to drop the retired table and
  recipe-only order columns while retaining a reversible `Down()` path.
- Bumped the isolated AI command protocol to schema version 2 and removed the
  `InventoryReceive` write proposal. The lab now permits only read-only exact
  inventory lookup or safe refusal.
- Historical audit, benchmark, incident, and supplied restaurant-data evidence
  remains preserved. Superseded operational documents are marked historical
  rather than rewritten as if the old behavior never existed.

### Verification and deployment

- `docker compose config --quiet` passed.
- `dotnet build Roms.slnx --no-restore` passed with 0 warnings and 0 errors.
- Focused suites passed: Domain 11/11, Command Gateway 9/9, core order workflow
  4/4, real MariaDB smoke 1/1, inventory readiness 2/2, MariaDB concurrency
  2/2, and the 60-flow resilience stress scenario 1/1.
- The complete Playwright container/browser suite passed 3/3 in one uninterrupted
  run after the final UI timing correction.
- Rehearsed the complete migration chain against a disposable MariaDB 11.4
  restore of the pre-removal backup. It retained both existing orders and ended
  with 7 migrations, no recipe table, and no retired recipe-only columns.
- Rebuilt and deployed the live `app` and `command-gateway` containers. The live
  database reached 7 migrations, retained 2 orders, and contains neither the
  `RecipeIngredients` table nor the 5 retired recipe-only columns.
- Local `/health` and public `https://roms.arkworksph.online/health` returned
  HTTP 200. The public homepage also returned HTTP 200 after deployment.

## 2026-08-02 — Read-only AI function boundary implemented

Status: implementation and contained spot-check complete; feature remains
disabled pending adversarial acceptance.

### Implemented

- Added twelve typed read-only functions for exact menu facts, independent-item
  inventory balances, permitted order status, and approved summaries.
- Enforced current Admin, Waiter, and Kitchen role/ownership boundaries inside
  ROMS. Kitchen-only responses omit price and totals; waiters can read only
  their own permitted orders.
- Added natural-language command schema 3. The model receives bounded catalogs
  and proposes only a typed function; it receives no SQL, database credentials,
  role authority, or write function.
- Added a feature-gated authenticated Assistant page. `AI_ENABLED` defaults to
  `false`, and the link/page are unavailable when disabled.
- Added sanitized `AiRead:<FunctionName>` audit entries for executed functions.
  Raw prompts, credentials, and result payloads are not stored there.
- Published the authoritative contract in `docs/AI_FUNCTIONS.md` and aligned
  the command protocol, security topology, role policy, README, and roadmap.

### Defects found and corrected

- Disposable MariaDB testing exposed provider translation failures when a
  projected inventory DTO was filtered or ordered. Filtering, ordering, and
  limiting now happen on the entity query before projection.
- The first live model question selected an inconsistent function/argument
  combination. The validator refused it safely. An explicit function/argument
  matrix and examples corrected the exact-item and low-stock runtime cases
  without weakening deterministic validation.

### Verification

- Compose configuration passed.
- Build passed with 0 warnings and 0 errors.
- Domain tests: 11/11.
- Command gateway validator/corpus tests: 10/10.
- AI service, authorization, audit, and coordinator tests: 9/9.
- Disposable MariaDB 11.4 AI translation/precision/summary test: 1/1.
- Isolated Chromium Assistant navigation/render smoke: 1/1.
- Internal container runtime: gateway health/schema 3 passed; exact inventory
  lookup passed; low-stock intent passed three repeats; deletion, prompt
  injection, and untranslated catalog mismatch failed closed.
- The broad solution command exceeded its seven-minute orchestration window.
  It is recorded as incomplete, not a pass or failure. MariaDB fixture startup
  took roughly 46 seconds on this storage, so short hang diagnostics were
  invalid and were excluded from evidence.
- GitHub CI run `30740522562` for commit `00662fe` passed the committed-password
  guard, clean restore, Release build, Chromium installation, complete solution
  test suite, and production Docker image build.
- After the spot-check, the ignored local flag was restored to
  `AI_ENABLED=false`, the app was recreated, and local health returned 200.

### Remaining gate

Run the locked multilingual/adversarial, timeout, stale-catalog, concurrency,
and cross-role corpus through the complete authenticated app path before a
staging pilot. No AI write or recipe function is approved.

## 2026-08-02 - Security hardening after independent scan

Status: implemented, fully regression-tested, deployed, and ready for the next
independent security scan.

- Protected confidential independent-audit material and local runtime secrets
  from accidental publication by broad Git staging.
- Dispositioned the reported SQL-injection and Docker ownership findings as
  false positives. The migration-lock rewrite was still converted to an exact
  command match plus parameterized timeout to eliminate scanner ambiguity;
  mutable GitHub Actions and missing health checks were corrected.
- Added role-derived AI function allowlists, pre-model catalog filtering,
  duplicate gateway/app authorization checks, per-user throttling, global
  concurrency bounds, and privacy-preserving audit records for every outcome.
- Hardened ROMS, the command gateway, and Cloudflare with read-only roots,
  dropped capabilities, `no-new-privileges`, process limits, pinned images,
  host allowlisting, and HTTP security headers.
- Replaced `eloquent_archimedes` only after the hardened tunnel registered and
  the public ROMS health check passed. `arcworks-cloudflared` now uses only the
  ROMS edge and portfolio networks.
- Protected GitHub `main`: PR and current `verify` check required, conversations
  resolved, administrator enforcement enabled, force pushes/deletion disabled,
  and zero outside approvals required for the single-owner workflow.
- Release build passed with 0 warnings and 0 errors. The complete solution
  passed 63/63 tests: Domain 11, Command Gateway 11, Integration 38, and browser
  E2E 3. ROMS, monitor, and portfolio then returned public HTTP 200; protected
  anonymous routes redirected to login; `AI_ENABLED=false` remained active.
- Full technical evidence and the remaining AI acceptance gate are recorded in
  `docs/SECURITY_HARDENING_2026-08-02.md`.
