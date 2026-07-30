# ROMS External Audit Handoff — Inventory Activation Preflight

Prepared: 2026-07-30

Branch: `agent/inventory-readiness`
Decision requested: Is ROMS still on a safe and technically sound path toward a
supervised inventory pilot?

## Executive position

ROMS has completed the application controls planned before inventory activation:

- preparation-time deduction with negative-stock blocking;
- explicit administrator override with reason and discrepancy audit;
- cancellation and amendment reversal rules;
- pending waste/spoilage reports with administrator approval;
- structured receiving;
- durable physical-count reconciliation;
- an administrator-only activation preflight backed by current database state.

Automatic inventory deduction remains disabled in the active deployment. This
handoff does not request approval for public launch, live restaurant inventory,
or AI integration.

## New activation preflight

The Inventory page now evaluates nine technical checks every time an
administrator loads or refreshes it.

| Code | Blocking technical check | Passing evidence |
| --- | --- | --- |
| `INV-001` | Active inventory catalog exists | At least one active inventory item |
| `INV-002` | Active inventory names are unique | No case-insensitive duplicate active names |
| `INV-003` | Canonical units only | Every active item uses `piece`, `g`, or `ml` |
| `INV-004` | Witnessed opening counts | Every active item has a durable physical-count record |
| `INV-005` | Non-negative opening state | No active item has a negative ledger balance |
| `REC-001` | Complete recipes | Every active and available menu item has at least one ingredient |
| `REC-002` | Valid recipe quantities | Every quantity is positive and fits `decimal(14,3)` |
| `REC-003` | Active recipe references | Every active menu recipe points to an active inventory item |
| `LOSS-001` | Loss review queue clear | No waste/spoilage request remains pending |

The page deliberately shows these three items as **Manual gate**, never as an
automated pass:

1. restaurant data-owner confirmation;
2. independent external-audit acceptance;
3. supervised multi-device pilot, backup verification, and rollback approval.

There is no UI control that changes `Features:Inventory:Enabled`. Passing the
technical preflight is evidence, not activation authority.

## Evidence generated

- Release solution build: 0 warnings and 0 errors.
- Domain tests: 11/11 passed.
- Command Gateway tests: 9/9 passed.
- Real MariaDB integration tests: 37/37 passed.
- Real Chromium tests: 3/3 passed.
- Total current automated tests: 60/60 passed after the browser timing
  assertion was rerun with an explicit rendered-sidebar wait.
- Production Dockerfile build: passed as
  `roms:external-audit-preflight`.
- Clean-data MariaDB preflight: all nine technical checks passed while the
  three human gates remained manual.
- Hostile-data MariaDB preflight: duplicate name, unsupported unit, missing
  physical count, negative balance, missing recipe, invalid recipe quantity,
  inactive ingredient reference, and pending loss review were all exposed as
  blockers.
- Authorization test: a non-admin caller was rejected.
- Mobile Chromium: the preflight rendered on the administrator Inventory page,
  updated after a physical count, and retained the three manual gates.

All database and browser acceptance used disposable MariaDB containers and
temporary application processes.

The unchanged active deployment was checked separately: loopback `/health`
returned HTTP 200, MariaDB reported healthy, and
`Features__Inventory__Enabled=false` remained effective.

## Reviewer reproduction

From a clean checkout of the branch:

```powershell
git rev-parse HEAD
pwsh tools/Test-NoCommittedSeedPasswords.ps1
git show --check HEAD
dotnet build Roms.slnx -c Release -m:1 --nologo
dotnet test Roms.slnx -c Release --no-build -m:1 --nologo
docker build -f Dockerfile -t roms:external-audit-preflight .
```

Docker Desktop must be running. The integration and browser projects start
disposable MariaDB 11.4 containers through Testcontainers.

Relevant implementation:

- `src/Roms.Application/Contracts.cs`
- `src/Roms.Infrastructure/Services/ReportAndInventoryServices.cs`
- `src/Roms.Web/Components/Pages/Inventory.razor`
- `tests/Roms.IntegrationTests/InventoryOperationsTests.cs`
- `tests/Roms.E2ETests/RomsApplicationSmokeTests.cs`

Relevant prior evidence:

- `docs/INVENTORY_REVERSAL_RULES.md`
- `docs/INVENTORY_OPERATIONS.md`
- `docs/SYNTHETIC_RESILIENCE_TESTING_2026-07-30.md`
- `docs/MARIADB_DEADLOCK_INCIDENT_2026-07-30.md`
- `docs/WORK_LOG.md`

## Questions for the external reviewer

1. Are the nine technical blockers sufficient for a supervised inventory
   activation decision? Identify any missing database-verifiable prerequisite.
2. Is “at least one durable physical count per active item” adequate for the
   opening gate, or should a count freshness window be required?
3. Should pending loss requests block initial activation, and is the current
   all-pending policy appropriately conservative?
4. Are `piece`, `g`, and `ml` an acceptable canonical baseline, or should a
   controlled conversion model be implemented before pilot?
5. Are the manager override and discrepancy audit controls sufficient, or is a
   separate discrepancy-resolution/acknowledgement entity required?
6. Does the MariaDB deadlock reload-and-retry behavior remain acceptable for
   supervised beta, or should bounded application retries be added first?
7. Is the evidence boundary clear enough that automated tests cannot be
   mistaken for restaurant confirmation or production acceptance?
8. Do any findings require remediation before work begins on the isolated AI
   command layer?

## Explicit exclusions and remaining risk

- The supplied restaurant dataset is provisional and sandbox-only.
- Real restaurant item names, quantities, minimum levels, and recipes are not
  approved.
- No live printer, payment device, unstable network, or human usability
  acceptance is claimed.
- Synthetic concurrency tests are not a production capacity rating.
- The active app/database were not migrated, rebuilt, restarted, or used for
  this acceptance.
- The active deployment must retain `Features__Inventory__Enabled=false`.
- The isolated Ollama/command-gateway lab is unchanged and must remain
  disconnected from the ROMS UI and operational database during this audit.

## Required decision record

The reviewer should return:

- checkout commit SHA;
- environment and commands used;
- findings with severity and file/line evidence;
- accepted, conditionally accepted, or rejected status;
- mandatory remediation before supervised pilot;
- advisory follow-up;
- explicit opinion on whether AI-layer work may begin without weakening the
  current safety boundary.
