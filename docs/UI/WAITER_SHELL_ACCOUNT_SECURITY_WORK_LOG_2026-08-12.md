# Waiter Shell and Account Security Work Log — 2026-08-12

## Scope

This change intentionally stops before redesigning or functionally changing
the Tables & Orders page. It prepares the authenticated waiter shell and the
staff account lifecycle required before that visual phase.

## Decisions implemented

- A removed staff account remains in Identity, attendance, schedule, order,
  and audit history, but its former login name is moved to
  `ArchivedUserName`. The active `UserName` becomes a unique archive key so a
  restaurant may reuse a departed employee's username.
- Existing inactive accounts are repaired once at application startup. The
  repair is audited as `ArchiveInactiveUserName` and never deletes history.
- New admin-created accounts are marked `MustChangePassword`.
- First login routes directly to Change Password. Server middleware prevents a
  user from bypassing this requirement by typing another application URL.
- A normal Change Password control remains at the bottom of the Dashboard.
- My Attendance, upcoming schedule, recent attendance, Clock Out + Log Out,
  and Log Out Only now live in the Dashboard.
- Install ARCWorks and My Attendance were removed from navigation.
- A Waiter now sees only Dashboard and Tables & Orders in navigation.
- The landscape navigation panel has an explicit Minimize / Expand control.
- Existing single-active-device enforcement and the 15-minute inactivity
  expiry remain in force.

## Historical-data boundary

Historical order rows still contain the username used at the time of the
transaction. Order display-name resolution now checks both the current login
name and `ArchivedUserName`, so archiving an account does not replace a former
employee's name with an opaque archive key.

## Database migration

- `20260812032502_AddStaffPasswordLifecycle`
- Adds nullable `ArchivedUserName` and non-null `MustChangePassword` with a
  safe default of `false` for existing active accounts.

## Verification evidence

- `dotnet build Roms.slnx --no-restore`: passed with 0 warnings and 0 errors.
- Domain tests: 16/16 passed.
- Command Gateway tests: 11/11 passed.
- GitHub's real-application verification reported all 46/46 Integration tests
  passed. Its two browser failures were traced to stale assertions for the
  intentionally retired `My attendance` heading; those assertions now target
  the consolidated `My dashboard` heading.
- Integration tests reported 29/29 passed before the existing runner process
  failed to exit; the focused OrderWorkflow suite was rerun and passed 6/6,
  including the new archived-waiter display-name regression test.
- Docker image `roms:waiter-shell-preview`: built successfully.
- Migration applied successfully to the isolated MariaDB preview.
- Browser acceptance on `127.0.0.1:7171` confirmed:
  - temporary-password login routed to required Change Password;
  - successful password replacement returned to Dashboard;
  - Waiter navigation contained only Dashboard and Tables & Orders;
  - Dashboard contained attendance, schedule, Change Password, Clock Out + Log
    Out, and Log Out Only;
  - Install ARCWorks and the separate My Attendance navigation item were
    absent;
  - landscape sidebar width changed from 250px to 72px using the explicit
    Minimize control;
  - an inactive `waiter` identity was archived on restart and a new active
    `waiter` identity was created without deleting the archived row.

## Isolation and rollback

- Source worktree: `D:\ARCWorks_Restaurant_Suite_Codex_Waiter_Shell`
- Branch: `ui/waiter-shell-account-security`
- Disposable preview only: `arcworks-landing-preview` on loopback port 7171.
- The live database, live container, tunnel, and public hostname were not
  changed during implementation or acceptance.
- Rollback before deployment: discard this branch/worktree and recreate the
  disposable preview from the accepted main image.
- Rollback after a future deployment requires application-image rollback plus
  the normal database backup/restore process; do not manually delete archived
  Identity rows.

## Preview catalog readiness follow-up

- Removed the blue interactive-control outline from only Blazor's
  programmatically focused page heading. Semantic navigation focus remains;
  controls retain visible keyboard focus.
- Populated the isolated preview with 12 tables, 4 categories, and 12 menu
  items using the guarded, idempotent `scripts/Seed-PreviewCatalog.ps1`.
- Added the 12 supplied menu photographs to the preview order catalog with
  contained, centered rendering so mixed source dimensions are neither
  distorted nor cropped.
- Full evidence and the preview-only boundary are recorded in
  `docs/UI/PREVIEW_CATALOG_POPULATION_2026-08-12.md`.
