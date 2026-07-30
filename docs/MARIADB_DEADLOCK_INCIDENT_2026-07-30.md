# MariaDB Deadlock Diagnostic Record

Date investigated: 2026-07-30  
Classification: synthetic overload finding, not a production incident  
System: ROMS inventory transition workflow  
Database: MariaDB 11.4.12, InnoDB  
Evidence source: disposable Testcontainers database

## Executive summary

ROMS deliberately launched 24 independent order transitions toward
`Preparing`, with a maximum of eight transitions executing concurrently. Every
order needed one unit of the same inventory item, while only 12 units existed.

The first surge consistently produced MariaDB deadlock error **1213**:

```text
Deadlock found when trying to get lock; try restarting transaction
```

This was not corruption, negative stock, or a crash. InnoDB detected the lock
cycle, selected a transaction as the victim, and rolled that transaction back.
All committed inventory remained internally consistent.

Two instrumented reproductions produced:

| Reproduction | First-surge commits | Rejected/rolled back | Captured error |
|---|---:|---:|---|
| Diagnostic run 1 | 4 | 20 | MariaDB 1213, 20 occurrences |
| Detailed run 2 | 5 | 19 | MariaDB 1213, 19 occurrences |

After the high-contention surge stopped, ROMS retried the still-`New` tickets
sequentially. The final state was exactly:

- 12 orders in `Preparing`;
- 12 orders still `New`;
- 12 inventory-consumption movements;
- inventory balance exactly zero;
- no negative stock;
- no partial state from deadlock victims.

## Safety boundary

The active ROMS database was not involved.

The reproduction used:

- a temporary MariaDB 11.4 container;
- a randomly named temporary database;
- synthetic users, tables, orders, recipes, and stock;
- automatic database/container disposal after the test.

The active `arcworks-resto-db-1` container, its volume, and restaurant data were
not read, written, restarted, or load-tested.

## Exact reproduction

Test:

```text
Roms.IntegrationTests.ResilienceStressTests.
Twenty_four_competing_tickets_cannot_spend_more_than_twelve_units
```

Configuration:

```text
Orders:                  24
Available inventory:    12 units
Quantity per order:      1 unit
Parallelism:              8
Inventory feature:       enabled in disposable test only
Transaction isolation:   SERIALIZABLE
Database engine:         InnoDB
MariaDB image version:   11.4.12
```

Detailed instrumented run:

```text
Started:  2026-07-30 21:41:40.143 +08:00
Ended:    2026-07-30 21:42:06.784 +08:00
Duration: 26.655 seconds
Outcome:  Passed
```

The InnoDB event time is shown as `2026-07-30 13:42:03`, which is the same
period expressed in UTC. The test entities use a fixed synthetic business
timestamp of `2026-07-30 14:00:00 UTC`; that value is payload data and is not
the wall-clock time of the deadlock.

Command:

```powershell
dotnet test tests/Roms.IntegrationTests/Roms.IntegrationTests.csproj `
  --filter "FullyQualifiedName~Twenty_four_competing_tickets" `
  --logger "trx;LogFileName=mariadb-deadlock-diagnostic-detailed.trx"
```

## Exact provider-level diagnostic

The detailed reproduction reported:

```text
Initial surge: 5 committed, 19 rejected; captured MariaDB transaction conflicts: 19.
MariaDB error 1213, SQLSTATE , occurrences 19: Deadlock found when trying to get lock; try restarting transaction
```

`SQLSTATE` was blank in the `MySql.Data.MySqlClient.MySqlException` property for
this run. The numeric MariaDB error number, 1213, was populated and is the value
ROMS uses for classification.

The full TRX result, including the captured `SHOW ENGINE INNODB STATUS` output,
is preserved at:

`docs/evidence/mariadb-deadlock-diagnostic-20260730.trx`

Evidence SHA-256:

```text
05A6CD3336F8690C661B8DABF3511E4B8D5754589BBC8AD94DE96EB7E0CB5C70
```

The evidence file was scanned before preservation. It contains no password,
connection string, or production data.

## Exact InnoDB deadlock excerpt

The latest deadlock graph identifies two transactions, 563 and 564. Both were
active for less than one second and were inserting consumption rows into
`StockMovements`.

Transaction 563:

```sql
INSERT INTO `StockMovements`
    (`ActorId`, `IdempotencyKey`, `InventoryItemId`, `OccurredUtc`,
     `OrderId`, `QuantityDelta`, `Reason`, `Type`)
VALUES
    ('stress-kitchen',
     'order:c5e3da5b-170f-486d-97ff-d25dc9125a09:preparing:8d05d050-f3e4-4abe-8d03-58b83fe21dd7',
     '8d05d050-f3e4-4abe-8d03-58b83fe21dd7',
     timestamp('2026-07-30 14:00:00'),
     'c5e3da5b-170f-486d-97ff-d25dc9125a09',
     -1.000,
     'Order c5e3da5b-170f-486d-97ff-d25dc9125a09 entered Preparing',
     'Consumption')
```

Transaction 564:

```sql
INSERT INTO `StockMovements`
    (`ActorId`, `IdempotencyKey`, `InventoryItemId`, `OccurredUtc`,
     `OrderId`, `QuantityDelta`, `Reason`, `Type`)
VALUES
    ('stress-kitchen',
     'order:a2d9082d-2a58-4125-9d4c-f5daf8e38cfc:preparing:8d05d050-f3e4-4abe-8d03-58b83fe21dd7',
     '8d05d050-f3e4-4abe-8d03-58b83fe21dd7',
     timestamp('2026-07-30 14:00:00'),
     'a2d9082d-2a58-4125-9d4c-f5daf8e38cfc',
     -1.000,
     'Order a2d9082d-2a58-4125-9d4c-f5daf8e38cfc entered Preparing',
     'Consumption')
```

For each transaction, InnoDB reported:

```text
ACTIVE 0 sec inserting
mysql tables in use 1, locked 1
LOCK WAIT 30 lock struct(s), heap size 3488, 29 row lock(s), undo log entries 3
```

The requested lock was:

```text
index PRIMARY of table ...`StockMovements`
lock_mode X insert intention waiting
Record: supremum
```

The conflicting held lock was:

```text
index PRIMARY of table ...`StockMovements`
lock mode S
Record: supremum
```

MariaDB resolved the cycle with:

```text
*** WE ROLL BACK TRANSACTION (2)
```

Only the latest full deadlock graph is retained by
`SHOW ENGINE INNODB STATUS`. The application logger independently captured 19
error-1213 exceptions during this particular run, but InnoDB status does not
contain 19 complete historical graphs.

## Relevant ROMS code path

The transition logic is in:

`src/Roms.Infrastructure/Services/OrderService.cs`

Important points:

1. The complete order transition runs inside a serializable transaction at
   approximately line 218.
2. `ReconcileInventoryAsync` is called before the transition commits.
3. It sums the stock ledger for each required inventory item at approximately
   line 345.
4. It rejects projected negative stock.
5. It appends the planned consumption movement at approximately line 385.
6. The order status, status history, audit entry, and stock movement commit as
   one transaction.

The stress scenario is in:

`tests/Roms.IntegrationTests/ResilienceStressTests.cs`

The eight-way surge begins at approximately line 90. The test now also captures
provider exceptions and the latest InnoDB deadlock graph.

## Likely lock-cycle explanation

The following is an evidence-based interpretation, not a MariaDB query-plan
proof:

1. Each transaction reads the current sum of movements for the same inventory
   item under `SERIALIZABLE`.
2. InnoDB protects the qualifying range against phantoms. The deadlock graph
   shows shared (`S`) locking at the `StockMovements` primary-index supremum,
   the virtual record above the highest key on that page/range.
3. Each transaction then tries to insert a new movement and requests an
   exclusive insert-intention lock.
4. Multiple transactions hold compatible shared range locks acquired during
   their reads.
5. Their later insert requests cannot proceed while the other transactions
   retain those shared locks, producing a lock-conversion cycle.
6. InnoDB detects the cycle immediately and rolls back a victim rather than
   waiting indefinitely.

The status output directly proves that the cycle occurred on
`StockMovements`, involved shared supremum locks and insert-intention waits, and
was resolved by rolling back transaction 2. Confirming exactly which earlier
LINQ-generated query acquired each shared lock would require SQL command
tracing or an `EXPLAIN`/optimizer trace during another controlled run.

## Why this test is unusually aggressive

This is intentionally harsher than an ordinary restaurant interaction:

- eight kitchen transitions hit the same ingredient at nearly the same time;
- all tickets use the same one-item recipe;
- all transactions read and append to the same ledger range;
- only half of the requested stock exists;
- `SERIALIZABLE` isolation deliberately favors correctness over concurrency.

A real rush can still create related contention—for example, many burger orders
consuming the same patty stock—so this is not dismissed as merely artificial.
The exact 24/12/eight-way shape is a stress boundary, not an estimate of normal
restaurant traffic.

## Impact assessment

### What happened

- Some first-attempt `Preparing` actions were rejected.
- MariaDB rolled back the victim transactions.
- Staff would need to reload and retry affected tickets.

### What did not happen

- No negative inventory.
- No duplicate stock consumption.
- No half-completed order transition.
- No lost committed movement.
- No database outage.
- No container crash or restart.
- No effect on the active application.

Because the status update and stock movement share one transaction, a victim's
order remains `New`; it does not appear `Preparing` without its corresponding
inventory deduction.

## Remediation already implemented

ROMS walks the complete exception chain and recognizes:

```text
1205 — lock wait timeout
1213 — deadlock found
```

Only error 1213 was observed in these reproductions. Error 1205 is handled
because it represents the related transient lock-timeout class.

Instead of displaying a raw database exception, the application now returns:

```text
Another inventory update happened at the same time. Reload and try this action again.
```

The classifier is in `OrderService.IsTransientTransactionConflict`.

Automatic blind retry was deliberately not added at the UI boundary. Reloading
first ensures the operator sees authoritative order and stock state before
repeating the action. A bounded transaction-level retry may still be safe if it
re-executes the entire transaction with fresh state and jitter; that requires a
separate design and load comparison.

## Research directions

The most useful options to compare are:

### 1. Retry the entire serializable transaction

- Retry only error 1213/1205.
- Use a small randomized backoff.
- Recreate the DbContext and transaction for every attempt.
- Re-read authoritative order and stock state.
- Keep a strict attempt/time limit.
- Preserve idempotency and audit invariants.

Research question: why did the configured EF/MySQL execution strategy not
classify or retry this provider exception automatically?

### 2. Lock one stable balance row per inventory item

Maintain or introduce a single balance row and lock it using
`SELECT ... FOR UPDATE` before checking and changing stock. This intentionally
serializes competing deductions for the same ingredient while allowing
different ingredients to proceed independently.

Trade-off: simpler locking and faster balance checks versus maintaining a
derived balance consistently with the append-only ledger.

### 3. Atomic conditional balance update

Conceptually:

```sql
UPDATE InventoryBalances
SET Quantity = Quantity - @required
WHERE InventoryItemId = @id
  AND Quantity >= @required;
```

The affected-row count decides success. The ledger append must remain in the
same transaction.

### 4. Review isolation plus explicit locks

Compare `READ COMMITTED` with deliberate row locking against the current
`SERIALIZABLE` range-lock approach. Lower isolation must not reintroduce the
race where two tickets both observe the same final unit.

### 5. Query and index analysis

- Capture generated SQL for the balance sum and idempotency checks.
- Run `EXPLAIN` for the balance query.
- Verify use of the `(InventoryItemId, OccurredUtc)` index.
- Determine why the deadlock graph centers on the primary-index supremum.
- Test whether a covering index changes the lock footprint.

### 6. Production observability before inventory activation

Record, without sensitive payloads:

- count of error 1213 and 1205;
- operation name and inventory-item identifier;
- retry/reload success rate;
- transaction duration;
- concurrent Preparing actions;
- deadlocks per 100 or 1,000 transitions.

`innodb_print_all_deadlocks=ON` can preserve every graph in the MariaDB error
log, but it should first be assessed for log volume and business-data exposure.

## Questions for external research or review

1. Is the supremum shared-lock/insert-intention pattern expected for this
   `SERIALIZABLE` aggregate-read-then-insert workflow on MariaDB 11.4?
2. Would an explicit per-inventory balance row be preferable to summing an
   append-only movement ledger inside every transition?
3. Does MySql.Data/EF Core expose a supported transient-error configuration for
   MariaDB 1213 and 1205?
4. Would `READ COMMITTED` plus `SELECT ... FOR UPDATE` preserve the strict
   no-negative-stock invariant with less range contention?
5. Which index/query plan is causing primary-index supremum locking despite the
   existing inventory/time index?
6. What retry count and randomized delay are appropriate for a touch-first KDS
   without making duplicate operator actions confusing?

## Current conclusion

The deadlock is real, reproducible, and well-contained. It is a concurrency
availability issue under an intentionally extreme same-ingredient surge, not a
data-integrity failure. The current transaction design correctly fails closed,
and the application now presents a controlled reload-and-retry response.

Further work should compare transaction-level retry and per-inventory row
locking using the same 24-ticket test, with correctness invariants unchanged.
