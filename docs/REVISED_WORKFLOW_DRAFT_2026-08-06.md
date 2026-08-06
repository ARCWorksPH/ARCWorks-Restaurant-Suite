# ARCWorks Restaurant Suite — Revised Workflow Draft

**Status:** Draft for product-owner review; not yet the implementation contract.

This draft updates the frozen workflow to use four operational roles and keeps
the alert system deferred until the core waiter → kitchen → manager workflow is
implemented and accepted.

## Role model

| Role | Primary responsibility | Boundary |
| --- | --- | --- |
| Waiter | Tables, customer orders, notes, resubmission, serving | Cannot perform kitchen, manager, payment, or historical-correction actions |
| Kitchen | Review, accept, return, prepare, and finish orders; report operational availability | Cannot perform waiter, manager, payment, or historical-correction actions |
| Manager | Supervise live operations; configure future operating rules; manage schedules and operational settings | Cannot create/submit/serve orders, perform kitchen transitions, edit processed records, or impersonate staff |
| Admin/Owner | Full system ownership, user/security administration, payments, corrections, overrides, and historical access | All protected actions require identity, reason where applicable, and audit evidence |

`Manager` is a distinct role. It is not an alias for `Admin`.

## Order lifecycle

```text
Draft → Submitted → Accepted → Preparing → Ready → Served → Payment confirmed
             │          │
             │          └── Returned to waiter → Resubmitted
             └───────────── Returned to waiter → Resubmitted
```

The implementation may retain the current persisted names (`New`,
`Preparing`, `Ready`, `Completed`) where compatibility requires them, but the
user-facing workflow must distinguish submission, kitchen acceptance, return,
resubmission, serving, and payment.

### Waiter flow

1. Waiter selects a table and creates a draft.
2. Waiter adds, removes, replaces, and annotates items while the draft is not
   submitted.
3. One standard order-entry timer, configured by the Manager for all waiters,
   begins when the waiter selects the table and starts the draft.
4. The waiter submits the order before the timer expires.
5. If more time is needed, the waiter requests an extension. The request and
   extension count are retained in the order history.
6. The order is sent to the kitchen.
7. If the kitchen returns it, the waiter sees the kitchen reason, edits the
   order, adds a resubmission note, and submits it again.
8. A resubmission increments the resubmission count and creates a new
   submission event; it does not erase the prior rejected version.
9. When the kitchen marks the order ready, the waiter serves it.
10. The waiter cannot confirm payment or edit processed/historical records.

### Kitchen flow

1. Kitchen staff see submitted orders in the kitchen display.
2. One standard kitchen acceptance timer, configured by the Manager, begins
   when the order is submitted. It is the same acceptance limit for all
   submitted orders.
3. Kitchen staff may accept the order or return it to the waiter.
4. A return requires a clear reason and sends the order back for correction.
5. Kitchen staff may request an acceptance-time extension; the request and
   extension count are audited.
6. Once accepted, the order enters preparation and a preparation timer starts.
7. Preparation duration is calculated from Admin-configured preparation minutes
   for each menu item and the ordered quantities. For example, two five-minute
   burgers plus one ten-minute fried chicken produces a twenty-minute target.
8. Kitchen staff mark the order ready when preparation is complete.
9. Kitchen staff may mark a menu item unavailable (`86`) when it cannot be
   served. This changes menu availability, not historical inventory movements.
10. Kitchen staff cannot edit completed orders, confirm payment, or change
    historical records.

### Manager flow

The Manager receives live operational displays and current-shift indicators,
not the actionable waiter or kitchen interfaces.

The Manager may:

- View live tables, submitted orders, kitchen queue, and timer state.
- Configure future waiter order-entry limits.
- Configure the standard future kitchen acceptance timer.
- Manage staff schedules and active roster membership.
- View live/current-shift performance indicators.
- View current inventory balances and low-stock conditions.
- Mark an item unavailable (`86`) or restore availability (`68`) according to
  the availability policy.
- Supervise operational alerts once the alert phase is implemented.

The Manager may not:

- Create, submit, serve, accept, return, prepare, or mark ready an order.
- Resubmit an order for a waiter.
- Confirm payment.
- Edit completed orders, historical attendance, processed inventory movements,
  or historical performance records.
- Impersonate another employee.

Changing a future configuration is not permission to rewrite past results. For
example, a Manager may change a burger's future preparation target from ten to
twelve minutes, but cannot alter yesterday's recorded preparation time.

### Admin/Owner flow

Admin/Owner is the only role with unrestricted authority. Admin may manage
users and roles, security, menu/catalog, payments, inventory, historical
corrections, protected overrides, audit review, and recovery operations.

Admin actions that alter an existing record require an explicit reason and are
recorded with the original value, new value, actor, and timestamp.

Admin also configures the preparation minutes for each menu item. These values
are used to calculate each accepted order's preparation target from its item
quantities.

## Independent inventory boundary

Inventory remains a standalone manual item ledger. Recipe, yield, costing,
automatic order deduction, and waste/spoilage approval workflows are not part
of this release.

The current item-availability controls are separate from the inventory ledger:

- `85` — item running low; warning only, normally generated from the minimum
  stock threshold.
- `86` — item unavailable; Kitchen or Manager may mark the item unavailable.
- `68` — item available again; Manager or Admin restores availability.

These codes belong to the later alert/availability phase and are not required
to complete the first workflow implementation.

## Historical-data rule

Once an order, payment, attendance record, inventory movement, or performance
event is processed, it is immutable to Waiter, Kitchen, and Manager. Admin may
perform a controlled correction with a reason and audit record.

## Deferred alert phase

The alert system is recorded in the roadmap but intentionally follows core
workflow acceptance. The initial approved codes are `85`, `86`, `68`, `100`,
and `200`; codes `50` and `95` are removed from scope. Admin/Owner alone may
define additional future codes.

The alert system will use one generic model with role routing, optional named
assignment, acknowledgement, resolution, expiry, and audit history. It must
not be implemented as five unrelated features.

## Acceptance gate before alerts

The core workflow must first prove:

- Waiter draft editing and timed submission.
- Kitchen acceptance, timed preparation, return reason, and resubmission.
- Manager live-only visibility and configuration boundaries.
- Admin protected corrections and payment authority.
- Immutable processed records for non-Admin roles.
- Concurrent/stale-update safety and complete audit history.

Only after these scenarios pass should the alert phase be implemented.
