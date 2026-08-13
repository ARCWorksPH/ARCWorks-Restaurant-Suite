# Gate 1 session-truth evidence — 2026-08-13

## Scope and boundary

- Worktree: `D:\ARCWorks_Restaurant_Suite_Codex_Waiter_Shell`
- Branch: `fix/waiter-gate1-session-truth`
- The accepted landing-page workspace and live container were not modified or
  redeployed during this gate.

## Incident diagnosis

The waiter account was not Identity-locked: its failed-access count was zero
and `LockoutEnd` was null. It retained an `ActiveSessionId` and recent
`SessionLastActivityUtc`, so the login page correctly interpreted the row as an
existing session lease. Two implementation gaps made the result incorrect in
practice:

1. the non-persistent authentication cookie had a fixed 20-minute ticket with
   sliding renewal disabled, allowing active staff to be expelled independently
   of the database activity clock; and
2. another window in the same browser inherited the same authentication cookie,
   so it did not perform another login and was not stopped by the second-device
   database check.

## Remediation

- The database timestamp remains the authoritative 15-minute idle clock.
- The session-only cookie now has a bounded 16-hour ticket with sliding renewal;
  it remains `HttpOnly` and becomes `SameSite=Strict`.
- Pointer, keyboard, touch, input, change, scroll, visibility, and focus events
  refresh the idle timer with at most one server write per minute.
- A heartbeat only refreshes the database when meaningful browser activity was
  observed recently; it cannot keep an abandoned window alive indefinitely.
- Expired leases atomically clear both database session fields.
- A same-browser, per-account `BroadcastChannel` lease blocks another active
  application window without using local storage, IndexedDB, or persistent
  device data. A different browser/profile/device remains governed by the
  database lease.
- Logout responses clear ARCWorks-owned browser cache/storage and do not alter
  attendance unless the explicit clock-out-and-logout action is used.

## Verification

- `dotnet build Roms.slnx --no-restore`: passed, 0 warnings / 0 errors.
- `Roms.Domain.Tests`: 16/16 passed.
- `Roms.CommandGateway.Tests`: 11/11 passed.
- Focused real-MariaDB/Playwright session test
  `One_staff_account_allows_only_one_active_window_or_device`: 1/1 passed.
  It proves denial of a second same-browser window and a second independent
  browser/device, then proves the ephemeral window lease can transfer only
  after the owning window closes.
- Real-browser investigation reproduced and verified the cookie and concurrent
  session boundaries against an isolated MariaDB application. During expansion
  of the existing monolithic E2E smoke test, later unrelated UI assertions ran
  beyond the 15-minute inactivity boundary. Those runs were not counted as a
  Gate 1 pass and the experimental changes to that legacy test were reverted.
- The full integration invocation also exceeded the bounded run time without a
  test failure report; this is recorded as a timeout, not a pass.

## Remaining time-bound acceptance

The concurrency defect is covered automatically. Before private beta, retain
the following time-bound/manual browser checks on an isolated preview:

1. active use beyond 20 minutes;
2. 15 minutes of genuine inactivity;
3. logout, clock-out-only behavior, password change, disablement, and session
   revocation.
