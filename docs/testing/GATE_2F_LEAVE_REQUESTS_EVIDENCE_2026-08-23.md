# Gate 2F — Leave requests evidence

**Date:** 2026-08-23
**Branch:** `agent/gate2f-leave-requests`
**Status:** implementation and verification complete; pull-request merge is the final repository closeout step.

## Scope completed

Gate 2F adds the server-side leave-request lifecycle required by the later
Waiter Staff Hub and Manager decision surface. It does not implement the final
modal, Manager inbox, responsive visuals, or any live-instance deployment.

- An active authenticated employee may submit one request containing 1–31
  distinct future Asia/Manila calendar dates, an optional bounded leave type,
  and an optional private request message.
- Pending, Approved, Declined, and Cancelled are explicit states. Submission,
  update, decision, and cancellation times are retained in UTC.
- Employees can read only their own requests. They may update or cancel only a
  pending request whose dates remain in the future.
- A Pending or Approved request blocks another request for the same employee
  and date. Declined and Cancelled history is retained without blocking a new
  request.
- Manager and Admin may read the decision queue and approve or decline a
  request. Self-decision is prohibited, and a decline requires a reason.
- Decisions require the expected request version. The relational concurrency
  token ensures two concurrent decisions cannot both commit.
- Approval records the decision but deliberately does not create, remove, or
  rewrite a staff schedule.
- Audit snapshots record dates, state, version, actor metadata, and whether a
  private message exists; the private message itself is excluded from audit
  payloads.

## Persistence

Migration `20260822200234_AddLeaveRequests` creates the request and requested-
date tables, their ownership and cascade relationships, decision metadata,
state/version fields, and lookup indexes. `DateOnly` remains the domain type;
an explicit provider conversion maps it to MySQL `date` because this project's
MySql.Data provider materializes SQL dates as `DateTime`.

The generated idempotent migration script completed successfully, and EF Core
reported no pending model changes after generation.

## Focused verification

The five in-memory `LeaveRequestTests` prove:

1. employee ownership, active-role enforcement, and Manager/Admin queue access;
2. future-date, duplicate-date, request-size, leave-type, and private-message validation;
3. overlap rejection without treating Declined/Cancelled history as active;
4. pending-only employee update/cancellation and immutable decided records; and
5. Manager/Admin decision authorization, no self-decision, required decline
   reason, optimistic version checks, schedule isolation, and private-message-
   safe auditing.

`MariaDbLeaveRequestConcurrencyTests` uses a disposable MariaDB container to
prove that two Manager decisions racing the same request yield exactly one
committed transition and one decision audit event. This test also exposed and
then verified the provider-specific DateOnly conversion and a bounded overlap
query compatible with MySQL translation.

Local results:

- solution Release build — **passed**, 0 errors;
- Gate 2D–2F focused integration regression — **18 passed, 0 failed**;
- real MariaDB concurrency test — **1 passed, 0 failed**;
- domain tests — **16 passed, 0 failed**;
- command-gateway tests — **11 passed, 0 failed**;
- EF migration model check — **passed**, no pending changes;
- idempotent migration script generation — **passed**.

The Release build continues to report the repository's pre-existing NU1903
warning for SSH.NET 2025.1.0. Gate 2F does not introduce or modify that package.

### Independent pull-request CI

Pull request #23 completed both CI executions triggered by the initial branch
push and pull request. GitHub Actions runs `32595822141` and `32595825559`
passed on 2026-08-23. Each completed the committed-seed-password guard, Release
restore and build, Playwright Chromium installation, full solution test suite,
and Docker image build. GitGuardian and Snyk pull-request checks also passed.

## Preserved boundaries

- No live or preview container, database, tunnel, credential, restaurant
  record, or schedule was changed.
- No Leave Request browser UI or Manager decision interface is claimed complete.
- No journal, later-gate feature, or automatic schedule mutation was added.
- The private request message is available only through the authorization-
  checked leave-request service and is not copied into audit records.
- Final responsive and visual acceptance remains in the later Waiter Dashboard
  UI gate.
