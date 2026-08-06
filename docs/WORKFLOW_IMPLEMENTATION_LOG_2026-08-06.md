# Workflow implementation log — 2026-08-06

## Scope

This change completes the deterministic backend preparation for the revised
Waiter → Kitchen → Manager → Admin/Owner workflow. The visual redesign remains
intentionally untouched until supervised browser acceptance passes.

## Implemented

- Added bounded manager-controlled settings for the single waiter order-entry
  timer (default 15 minutes) and kitchen acceptance timer (default 5 minutes).
- Draft creation starts and persists the order-entry timer. Existing drafts
  without a deadline are repaired when reopened.
- Submission starts and persists the kitchen acceptance timer.
- Preparation continues to snapshot the sum of configured
  `PreparationMinutes × quantity` when Kitchen accepts the order.
- Added bounded timer extensions with mandatory reason, actor, timestamp,
  requested minutes, cumulative count, and audit entry.
- Added manager-only workflow settings/live-order service and the
  `ManagerOrAdmin` authorization policy. Managers receive live operational
  facts; historical editing/payment remains outside their authority.
- Added MariaDB migration `20260806143000_AddWorkflowTimers`.

## Verification performed

```text
dotnet build Roms.slnx --no-restore                 PASS (0 warnings, 0 errors)
dotnet test tests/Roms.Domain.Tests/... --no-restore PASS (15/15)
dotnet test tests/Roms.IntegrationTests/... --filter OrderWorkflowTests PASS (5/5)
```

The migration has not been applied to the production-like database in this
change. Apply it only in an isolated/staging database first, then run the
browser and simultaneous-role acceptance scenarios from the contract.

## Rollback

1. Do not edit or reset the protected base branch.
2. Restore the database snapshot taken immediately before applying
   `20260806143000_AddWorkflowTimers`.
3. If the migration has already been applied and a controlled rollback is
   approved, run the migration's `Down` path through the normal EF migration
   tooling; do not drop tables manually.
4. Revert this commit only after preserving the test output and migration
   status in the work log.

## Remaining gate before UI redesign

- Apply and verify the migration in an isolated database.
- Run simultaneous waiter, kitchen, manager, and admin browser acceptance.
- Verify timer expiry/extension behavior and rejected role boundaries.
- Verify the real MariaDB/SignalR/Docker path, not only InMemory tests.
- Record evidence and then unlock the UI redesign instructions.
