# ROMS Synthetic Resilience Test Record

Date: 2026-07-30

Scope: disposable MariaDB 11.4 databases and temporary ROMS processes only

Active deployment: excluded from destructive and load testing

## Recovery point

Before testing, the repository and active database were captured at:

`D:\ARCWorks_Restaurant Suite Backups\pre-break-test-20260730-202348`

The package contains:

- a complete Git bundle for all refs;
- a binary-capable working-tree patch and status inventory;
- an active MariaDB logical dump;
- SHA-256 checksums and restore notes.

The Git bundle verified successfully. The database dump was restored into a
separate MariaDB 11.4 container and reconciled to 21 tables, three migrations,
and two orders. The verification container was then removed.

## Test matrix

| Layer | Scenario | Required invariant | Result |
|---|---|---|---|
| Browser | Independent Waiter, Kitchen, and Cashier/Admin contexts | Live state crosses sessions and one order reaches paid/completed | Passed |
| Browser | `<script>`-shaped order note | Text is displayed; JavaScript does not execute | Passed |
| MariaDB load | 60 full lifecycles, parallelism 12 | Exact 60 paid orders; no lost history/audits/idempotency | Passed |
| Inventory overload | 24 Preparing attempts, 12 available units, parallelism 8 | Stock never negative; no more than 12 advance | Passed |
| Recovery | Retry still-New tickets after overload | Exactly 12 Preparing and 12 New; balance exactly zero | Passed |
| Input abuse | Reversed/equal date ranges | Clear rejection instead of misleading empty output | Passed |
| Input abuse | Invalid enum, huge decimal, quantity 0/100, 501-character fields | Clear rejection before persistence | Passed |
| Injection-shaped data | SQL and HTML/script-like text | Stored as ordinary data; schema remains intact | Passed |
| Idempotency | Duplicate loss-report key with different second payload | One request only; original result returned | Passed |

## Observed overload behavior

The first eight-way inventory surge intentionally exceeded comfortable
transaction contention. In one observed run, four of the twelve possible
Preparing transitions committed on the first surge and the other attempts
rolled back. No stock was lost or overspent. Once contention subsided, normal
retries consumed the remaining available units and stopped exactly at zero.

This exposed an availability/usability problem rather than a data-integrity
problem: raw MariaDB deadlock/lock messages could reach staff. ROMS now detects
MariaDB error 1205/1213 in an order transition and returns:

> Another inventory update happened at the same time. Reload and try this
> action again.

Automatic retry was deliberately not added. Retrying an operator command
without reloading authoritative order state can be unsafe; the current message
asks the operator to reload and consciously retry.

## Validation changes

Clean application validation now rejects:

- report or attendance date ranges whose end is not after the start;
- undefined waste/spoilage types;
- waste/spoilage quantities outside the MariaDB decimal range;
- special instructions, schedule/correction/amendment/cancellation/override,
  loss, and review text above their persisted limits;
- table/category/menu/inventory names or units above their persisted limits;
- menu prices above their persisted decimal range.

## Reproduction

Run the complete solution serially:

```powershell
dotnet test Roms.slnx -m:1 --logger "console;verbosity=minimal"
```

Run only the multi-role browser scenario:

```powershell
dotnet test tests/Roms.E2ETests/Roms.E2ETests.csproj `
  --filter "FullyQualifiedName~Independent_waiter_kitchen_and_cashier"
```

Run only stress and abuse coverage:

```powershell
dotnet test tests/Roms.IntegrationTests/Roms.IntegrationTests.csproj `
  --filter "FullyQualifiedName~ResilienceStressTests|FullyQualifiedName~AdversarialInputTests"
```

These commands require Docker because each suite creates disposable MariaDB
containers. They must not be pointed at the active database.

## Evidence boundary and next tests

The synthetic suite substantially raises confidence in concurrency, transaction
rollback, browser encoding, input validation, and recovery. It is not evidence
of:

- real waiter/kitchen/cashier usability under service pressure;
- production network latency or temporary disconnect behavior on each device;
- printer, cash drawer, or payment-device integration;
- a rated maximum concurrent-user or orders-per-minute capacity;
- correctness of unverified restaurant inventory units, recipes, or balances.

Recommended next gate: repeat the three-role scenario on separate physical
devices over the intended restaurant network, then conduct a supervised pilot
with inventory still disabled. Enable inventory only after actual units,
recipes, opening balances, and reversal decisions are signed off.
