# ROMS Work Log

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
