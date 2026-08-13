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
- Each authenticated top-level browsing context now claims one server-owned
  application-instance lease. The instance identifier lives in `window.name`,
  survives an ordinary reload, and is not copied with cookies, local storage,
  session storage, or an exported browser profile.
- If another runtime presents the same authentication cookie with a different
  instance identifier, ARCWorks treats the condition as authenticated-session
  replay. It atomically revokes the database session and application lease,
  forcing both the original and copied runtimes back to login.
- The retained audit record contains only cryptographic fingerprints. Reusing
  the already revoked cookie remains denied and does not flood the audit log.
- Logout responses clear ARCWorks-owned browser cache/storage and do not alter
  attendance unless the explicit clock-out-and-logout action is used.

## Verification

- `dotnet build Roms.slnx --no-restore`: passed, 0 warnings / 0 errors.
- `Roms.Domain.Tests`: 16/16 passed.
- `Roms.CommandGateway.Tests`: 11/11 passed.
- Focused real-MariaDB/Playwright copied-profile regression
  `Copied_authenticated_session_revokes_every_instance_and_records_security_incident`:
  1/1 passed. It proves that an ordinary reload retains authority, an exported
  authenticated browser state triggers full revocation in a new runtime, the
  original runtime also loses authority, and five additional replay attempts
  remain denied while exactly one incident is retained.
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

## Gate 1 final verification

- Solution build: passed with 0 warnings and 0 errors.
- Domain tests: 16/16 passed.
- Command Gateway tests: 11/11 passed.
- Copied-profile real MariaDB + Playwright regression: 1/1 passed.
- The full integration-suite invocation exceeded its bounded execution window
  without emitting a failure report. It remains recorded as a timeout rather
  than being represented as a pass.

## Accepted merge and live deployment — 2026-08-14

- Merged the isolated Gate 1 security branch with the accepted live-UI
  checkpoint in a clean deployment worktree. No untracked design source or
  unrelated workspace material was included.
- Before deployment, completed database-only backup run
  `20260813T164133Z`: MariaDB and PostgreSQL dumps validated, SHA-256 manifest
  created, restic snapshot `8912e212` saved, and repository integrity check
  completed with no errors. One abandoned six-day-old restic lock was removed
  only after confirming its recorded PID was no longer running.
- Preserved the prior application image as
  `roms:rollback-gate1-20260814` (image ID beginning `8771075857e8`).
- Built the integrated release as `roms:gate1-20260814` / `roms:local` and
  recreated only `arcworks-resto-app-1`. MariaDB, Cloudflare tunnel, and the
  monitor were not recreated.
- The application became healthy. Local `/health`, local `/Account/Login`, and
  public `https://roms.arkworksph.online/health` returned HTTP 200; the local
  login response contained the accepted Chef Doy branding.
- MariaDB confirms migration
  `20260813090000_AddApplicationInstanceLease` as the newest applied migration.
- The integrated solution build and focused Domain/Command tests passed. The
  solution build reports two existing NU1903 findings in test projects through
  their SSH.NET dependency; the production web image publish itself completed
  successfully. This dependency warning remains tracked separately and is not
  represented as resolved by Gate 1.
