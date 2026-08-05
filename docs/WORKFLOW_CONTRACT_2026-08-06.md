# Deterministic Workflow Contract — 2026-08-06

This contract freezes the business workflow before acceptance testing. It is
derived from the current domain entities, application services, authorization
policies, and pages. Any change to these rules must update this document and
the corresponding tests before UI work proceeds.

## Active role model

| Role | Primary responsibility | Explicit boundary |
| --- | --- | --- |
| Waiter | Create and submit table orders; follow served orders through completion; clock in/out | Owns assigned orders only; cannot advance kitchen preparation or confirm payment |
| Kitchen | View the active kitchen queue; start and finish preparation; report waste/spoilage | Cannot edit menu/orders, confirm payments, or approve loss reports |
| Management (`Admin`) | Manage catalog, payments, inventory, schedules, corrections, reports, and protected order overrides | All protected actions remain audited and require the administrator identity |

The application has three persisted roles: `Admin`, `Waiter`, and `Kitchen`.
Management in this contract means the `Admin` role; it is not a fourth role.

## Order lifecycle

### Persisted order states

`Draft → New → Preparing → Ready → Completed`

`Cancelled` is a terminal branch from `Draft`, `New`, `Preparing`, or `Ready`.
`Completed` and `Cancelled` are terminal for status changes.

| From | Allowed next state | Who may perform it | Required evidence |
| --- | --- | --- | --- |
| Draft | New | Owning waiter or Admin | At least one item; idempotent submission key |
| Draft | Cancelled | Owning waiter or Admin | Non-empty cancellation reason |
| New | Preparing | Kitchen or Admin | Audited status transition |
| New | Cancelled | Owning waiter or Admin | Non-empty cancellation reason |
| New | Amend items | Owning waiter or Admin | Non-empty amendment reason |
| Preparing | Ready | Kitchen or Admin | Audited status transition |
| Preparing | Cancelled | Admin only | Non-empty cancellation reason |
| Preparing | Amend add | Owning waiter or Admin | Non-empty amendment reason |
| Preparing | Amend remove | Admin only | Non-empty amendment reason |
| Ready | Completed | Owning waiter or Admin | Audited served/completed transition |
| Ready | Cancelled | Admin only | Non-empty cancellation reason |
| Completed | Confirm payment | Admin only | Payment actor and timestamp; cannot repeat |

The domain rejects skipped, reversed, duplicate, or terminal-state transitions.
The client must reload authoritative state after a stale or concurrent update;
SignalR notifications are advisory and never replace the database state.

### Order editing rules

- Draft items may be added and removed directly by the owning waiter or Admin.
- Submitted orders may only be amended while `New` or `Preparing`.
- Every amendment requires a reason and increments the order revision.
- Menu items must be active and available at the time of the operation.
- Quantities are whole numbers from 1 through 99; special instructions are
  limited to 500 characters.
- Completed or cancelled orders cannot be amended.
- Cancellation and amendment records are retained in audit/status history.

## Inventory contract

Inventory is a manual independent-item ledger. Recipes, yields, ingredient
consumption, costing, and automatic order-to-stock deduction are out of scope.

| Operation | Who may perform it | Rule |
| --- | --- | --- |
| View balances, items, movements, counts, and loss requests | Authenticated Kitchen/Admin through the inventory route | Read current persisted facts only |
| Add or edit an item | Admin | Name, unit, and minimum stock are validated and audited |
| Receive stock | Admin | Positive quantity, delivery/invoice reference, idempotency key |
| Reconcile physical count | Admin | Witnessed non-negative count, reason, idempotency key, serializable correction |
| Post adjustment | Admin | Reason and idempotency key; negative result requires explicit override and override reason |
| Report waste/spoilage | Kitchen or Admin | Positive quantity, type, reason, idempotency key; remains pending |
| Approve/reject waste/spoilage | Admin | Approval changes stock; rejection requires review reason; action is audited |

No loss report changes stock until an Admin approves it. Duplicate idempotency
keys must not create duplicate movements or approvals.

## Schedule, attendance, and reports

| Area | Allowed action | Who |
| --- | --- | --- |
| Attendance | View own records/schedules; clock in; clock out | Any authenticated staff member |
| Staff schedule | Add, edit, delete non-overlapping shifts | Admin |
| Attendance correction | Correct a record with a mandatory reason | Admin |
| Attendance export | Export the authorized seven-day CSV | Admin |
| Reports | View completed-order summaries for a valid date range | Admin |
| Catalog and tables | Manage active categories, menu items, prices, availability, and tables | Admin |
| Pending payments | View completed unpaid orders and confirm payment | Admin |

Report ranges must have an end strictly after the start. Date filters use the
application's explicit UTC/local conversion rules; empty periods are valid and
must return an empty result rather than an error.

## Route and authorization contract

| Route | Policy |
| --- | --- |
| `/tables`, `/orders/{id}` | Waiter or Admin |
| `/kitchen` | Kitchen or Admin |
| `/inventory` | Kitchen or Admin route; mutation methods enforce Admin, loss reporting allows Kitchen/Admin |
| `/admin/catalog`, `/admin/users`, `/admin/attendance`, `/admin/payments`, `/reports` | Admin |
| `/attendance` | Authenticated staff |
| `/assistant` | Future-version hold; hidden/not found while `AI_HOLD=true` |

Route visibility is not the only boundary. Application services repeat role and
ownership checks, and the domain enforces state invariants even if a client is
modified or a request bypasses the normal page.

## Acceptance scenarios for the next phase

The following scenarios are the minimum contract-based test set. They are not
claimed as passed by this document; they are the checklist for the next
waiter/kitchen/management acceptance run.

- [ ] Waiter creates a draft, adds/removes items, and submits exactly once.
- [ ] Another waiter cannot open or mutate the assigned order.
- [ ] Kitchen moves `New → Preparing → Ready` and cannot skip or reverse states.
- [ ] Waiter completes a `Ready` order; Admin confirms payment once.
- [ ] Cancellation and amendment reasons are required and audited.
- [ ] Preparing/Ready cancellation is denied to a waiter and allowed to Admin.
- [ ] Inventory receipt, count, adjustment, loss report, approval, rejection,
      and duplicate-idempotency cases behave as documented.
- [ ] Schedule overlap, attendance ownership, correction reason, report date
      range, and empty-period cases behave as documented.
- [ ] Unauthorized direct route access is denied even when a page link is
      manually constructed.
- [ ] A stale/concurrent update produces a safe reload/error path and does not
      create duplicate state history.

## Change control

This contract is frozen for the current acceptance phase. A requested change
must identify the affected role, state transition, data invariant, audit event,
tests, and UI surface. It must be recorded in `docs/WORK_LOG.md` and reviewed
before implementation.
