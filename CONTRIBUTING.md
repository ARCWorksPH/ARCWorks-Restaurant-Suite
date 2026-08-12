# Contributing to ARCWorks Restaurant Suite

Thank you for taking an interest in ARCWorks Restaurant Suite. This project
values small, reviewable, evidence-backed changes over broad rewrites.

## Before opening an issue

Search existing issues first. For a defect, include:

- the role and workflow being tested (Waiter, Kitchen, Manager, or Admin);
- exact reproduction steps;
- expected and actual behavior;
- browser, display orientation, Docker/runtime, and database context;
- sanitized screenshots or logs.

Never attach passwords, tunnel tokens, connection strings, `.env` files,
identity cookies, database dumps, or private customer/staff data.

## Before opening a pull request

Keep one concern per pull request. Update the relevant documentation and
`docs/PROJECT_TIMELINE.md`, preserve the audit/history rules, and include tests
for changed business behavior. The normal local checks are:

```powershell
dotnet build Roms.slnx --no-restore -m:1 -v:minimal
dotnet test Roms.slnx -m:1
git diff --check
```

Runtime/browser acceptance is separate from unit and integration tests. Do not
claim live acceptance without recording the environment and observed result.

## Scope boundaries

AI is currently held behind a fail-closed feature gate. Inventory remains an
independent ledger, and destructive operations must preserve historical audit
references. Please discuss any change that affects these boundaries before
implementing it.
