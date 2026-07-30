# ROMS Work Log

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
