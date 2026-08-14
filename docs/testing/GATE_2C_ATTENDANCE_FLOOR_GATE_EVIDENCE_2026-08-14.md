# Gate 2C Attendance and Floor Gate Evidence — 2026-08-14

## Scope

Gate 2C makes an open attendance record the server-authoritative requirement for Waiter floor work. It also bounds forgotten attendance records without coupling duty time to the authenticated browser session.

This checkpoint does not redesign the Waiter Dashboard and does not implement portraits, Today's Team, announcements, leave requests, the journal, or the final Staff Hub UI.

## Accepted behavior

- A Waiter may create, read, amend, submit, resubmit, extend, serve, or cancel their own eligible order only while an attendance record is open.
- An Administrator retains the existing explicit operational bypass. A Manager does not inherit Waiter floor authority.
- Unknown, inactive, or non-Waiter actors are rejected by the service even if a browser button is visible or a command is forged.
- Explicit logout and the 15-minute authenticated-session inactivity cleanup clear only the application session. They do not close attendance.
- A scheduled attendance record closes exactly 12 hours after its scheduled end.
- An unscheduled attendance record closes exactly 12 hours after clock-in.
- The closure is marked automatic, requires Manager review, and creates an audit entry. Review is a separate audited action and does not silently alter recorded hours.
- A closed attendance record immediately revokes Waiter floor eligibility. Re-entry requires a fresh Clock In.

## Concurrency and restart safety

- The worker runs once at application start and then every minute, so overdue records are recovered after restart.
- Relational closure uses one conditional database claim inside a retriable transaction. The attendance update and audit record commit together.
- Two simultaneous MariaDB workers produced one closure and one audit entry; the losing worker reported a concurrency skip.
- A manual Clock Out racing the automatic worker also produced exactly one closure and one audit entry.
- Re-running the worker after closure produced no second closure or audit.
- A manual Clock Out completed first is never rewritten by the automatic worker.

## Schema change

Migration `20260814013539_AddAttendanceAutoClosureReview` adds closure kind, Manager-review metadata, an optimistic version token, and an index supporting open/review queries.

The upgrade SQL was generated successfully from `20260813090000_AddApplicationInstanceLease` to the Gate 2C migration. Existing records receive safe defaults and are not retroactively labeled automatic.

## Automated evidence

| Check | Result |
|---|---|
| Scheduled and unscheduled exact closure boundaries | Pass |
| Worker restart/idempotent rerun | Pass |
| Manual Clock Out is preserved | Pass |
| Manager review is explicit and audited | Pass |
| Logout/session cleanup leaves attendance open | Pass |
| Floor commands denied before Clock In and after auto-close | Pass |
| Waiter read-model regression and synthetic four-role workflow | Pass |
| Real-browser waiter clocks in before completing the waiter/kitchen/cashier workflow | Pass |
| Two simultaneous MariaDB workers create exactly one closure/audit | Pass |
| Manual and automatic MariaDB clock-out race creates exactly one closure/audit | Pass |
| Existing MariaDB order-concurrency scenarios | Pass |
| Existing 60-order waiter/kitchen/cashier stress scenario | Pass |
| Domain tests | 16/16 passed |
| Command Gateway tests | 11/11 passed |

Focused in-memory Gate 2C/read-model/workflow suite: **13/13 passed**.

MariaDB attendance races: **2/2 passed**.

MariaDB order-concurrency regression: **2/2 passed**.

MariaDB 60-flow stress regression: **1/1 passed**.

Real-browser multi-role workflow: **1/1 passed**.

The browser workflow now confirms the persisted open attendance record before
entering the floor. This keeps the acceptance assertion tied to server state and
removes dependence on a single UI event being processed during a loaded CI run.

The full solution build has zero errors. NuGet reports the already-known high-severity advisory `GHSA-q939-rpr3-3284` for test-only package `SSH.NET` 2025.1.0; this checkpoint does not conceal or broaden that separate dependency-remediation item. GitHub CI remains the authoritative full-solution build, browser, test, and Docker gate for merge.

## Rollback

Rollback requires reverting the Gate 2C application commit and applying the migration's `Down` operation only after confirming no automatic-closure review data must be retained. A safer production rollback is to stop the worker and revert application behavior while preserving the additive columns until retained review/audit data is exported.
