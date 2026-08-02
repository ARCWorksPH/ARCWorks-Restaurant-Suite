# ROMS Inventory Receiving and Physical Counts

Status: implemented manual independent-item ledger for sandbox and supervised
acceptance. Orders do not deduct inventory ingredients.

## Receiving

An administrator records each delivery with:

- one active inventory item;
- a positive quantity in that item's configured unit;
- a delivery, invoice, or receiving-document reference;
- an optional note;
- the authenticated administrator and server timestamp.

The operation appends a `Receipt` stock movement and an audit entry. Repeating
the same idempotency key returns the original result and does not post another
receipt. Concurrent duplicate submissions are protected by the database unique
constraint and reconcile to one movement.

Receiving does not change units, minimum levels, or previous
movements. A mistaken receipt must be corrected by an append-only adjustment;
the original receipt remains visible.

## Physical count reconciliation

An administrator selects an active item, enters the witnessed non-negative
quantity on hand, and provides a count-sheet reference or reason.

ROMS records:

- ledger quantity immediately before reconciliation;
- physical quantity counted;
- variance (`counted - ledger`);
- reason;
- administrator;
- server timestamp;
- idempotency key.

Every count is retained, including a zero-variance count. If the variance is
nonzero, ROMS appends one `Adjustment` movement in the same serializable
transaction. It never edits or deletes prior receipts, consumption, reversals,
waste, spoilage, or adjustments.

Example:

```text
Ledger before count: 10.000 kg
Physical count:       7.500 kg
Variance:            -2.500 kg
Posted movement:     -2.500 kg Adjustment
Final ledger:         7.500 kg
```

## Authorization and validation

- Only administrators may receive stock or reconcile physical counts.
- Received quantities must be greater than zero.
- Physical counts may be zero but cannot be negative.
- Values must fit the persisted `decimal(14,3)` range.
- Delivery references and count reasons are mandatory and length-limited.
- Inactive or missing inventory items are rejected.
- Activity-history query limits are bounded.
- HTML, SQL, and script-shaped text is stored and rendered as ordinary text.

## Concurrency

Count reconciliation uses a serializable transaction so the ledger snapshot,
count record, variance movement, and audit entry commit atomically.

MariaDB deadlock or lock-timeout errors are translated to:

```text
Another inventory update happened at the same time. Reload and try this action again.
```

The operator must reload authoritative state before retrying. See
`docs/MARIADB_DEADLOCK_INCIDENT_2026-07-30.md` for the lock investigation.

## User interface

The Inventory page now separates:

1. **Receive stock** — normal delivery intake.
2. **Physical count reconciliation** — witnessed on-hand counts.
3. **Stock adjustment** — advanced exceptional correction.
4. **Recent physical counts** — count snapshot and variance evidence.
5. **Recent stock activity** — append-only movement history.

This avoids using an ambiguous positive/negative adjustment box for routine
deliveries and count sheets.

## Activation boundary

These controls are ready for disposable and supervised testing. They do not
authorize production inventory activation.

The administrator Inventory page exposes a database-backed readiness check.
Its six technical checks cover active items, unique names, canonical units,
witnessed counts, non-negative balances, and pending loss reviews. Restaurant
approval, external-audit acceptance, and the supervised pilot remain visibly
manual gates. See `docs/EXTERNAL_AUDIT_HANDOFF_2026-07-30.md`.

Before production inventory use, obtain restaurant approval for:

- item names and units;
- witnessed opening counts;
- minimum levels;
- receiving/count authority;
- waste, spoilage, and discrepancy policies;
- a supervised multi-device workflow.
