# Gate 2B Waiter Dashboard Read Model Evidence — 2026-08-14

## Scope

Gate 2B establishes one server-authorized, read-only Waiter Dashboard contract. It does not implement the final dashboard UI, attendance auto-close, portraits, announcements, leave requests, or the journal.

## Security and data boundary

- Employee identity is read from an authenticated principal's immutable `NameIdentifier` claim; an unauthenticated claim bag is rejected.
- No employee identifier is accepted as a query argument from the browser.
- Only an active account assigned the `Waiter` role is accepted.
- Manager, Kitchen, Admin, inactive, and anonymous identities cannot obtain this read model.
- The returned contract is limited to display name, restaurant date/time, attendance state, today's shift and note, Monday-based weekly hours, and three recent self-attendance records.
- The contract contains no payroll, other-employee identity, future-team schedule, order, journal, Manager, or Admin data.

## Time boundary

- `IRestaurantClock` remains the only calendar boundary.
- Restaurant date and today's shift are calculated in `Asia/Manila`.
- Weekly hours begin Monday in restaurant time.
- Persisted attendance and schedule instants remain UTC and are converted only for the read model.

## Automated evidence

| Check | Result |
|---|---|
| Principal-derived identity cannot be redirected by a mismatched browser name | Pass |
| Active Waiter receives only the frozen field set | Pass |
| Manager and inactive Waiter are rejected | Pass |
| Anonymous principal is rejected | Pass |
| Empty schedule/attendance returns a safe empty state | Pass |
| Manila date, local shift conversion, Monday hours, and open attendance state | Pass |

Focused Gate 2B suite: **6/6 passed**.

## Rollback

Gate 2B adds no database migration and changes no existing UI. Rollback is the feature-branch commit/merge reversal. Gate 1 session security and Gate 2A restaurant configuration remain independent.
