# ARCWorks Restaurant Suite — UI Redesign Follow-up Fix Instructions

**Date:** 2026-08-07
**Audience:** Gemini or the delegated UI implementation agent
**Status:** Required corrective pass before the Phase A redesign can be
considered compliant
**Canonical root:** `D:\ARCWorks_Restaurant_Suite`

## 1. Important deployment clarification

`https://roms.arkworksph.online/` currently returns HTTP 200 and is reachable,
but it is serving the running `roms:local` Docker image. The current UI changes
are still uncommitted local working-tree changes and have not been built into
that image or deployed to the public endpoint.

Do not claim that the public URL reflects the redesign until all of the
following are complete:

1. The corrective source changes are committed.
2. The application image is rebuilt from that commit.
3. Only the intended application container is recreated or redeployed.
4. Health, login, role routing, and the public UI are checked after deployment.
5. The deployed commit/image reference is recorded in the work log.

Do not expose credentials, tokens, database passwords, or tunnel values in the
walkthrough, screenshots, logs, or commit.

## 2. Read before editing

Read these files in full:

- `D:\ARCWorks_Restaurant_Suite\docs\UI_REDESIGN_IMPLEMENTATION_INSTRUCTIONS_2026-08-06.md`
- `D:\ARCWorks_Restaurant_Suite\docs\UI_REVISION_ALIGNMENT_REVIEW_2026-08-06.md`
- `D:\ARCWorks_Restaurant_Suite\docs\WORKFLOW_CONTRACT_2026-08-06.md`
- `D:\ARCWorks_Restaurant_Suite\docs\WORK_LOG.md`
- `D:\ARCWorks_Restaurant_Suite\docs\PROJECT_TIMELINE.md`

The approved scope remains deterministic and AI-free. Do not add recipes,
automatic stock deduction, autonomous actions, or new inventory features.

## 3. Required corrective changes

### 3.1 Remove retired waste/spoilage UI

Remove the entire Inventory UI for:

- Report waste or spoilage;
- Waste/Spoilage type selection;
- Loss quantity/reason submission;
- Loss approval/rejection controls;
- Pending loss request panels.

Do not create a replacement workflow. Do not delete database tables, old audit
records, or migrations as part of this UI pass; this is a scope correction,
not a destructive data migration.

### 3.2 Remove the recipe placeholder and misleading stock-deduction text

Remove the visible `Recipe Ingredient Configuration` panel, even if it is
disabled or labelled “out of scope.” A disabled recipe panel still implies a
current product feature.

Remove wording that automatic stock deduction is “paused,” “disabled,” or
“coming soon” from the active inventory surface. The correct product meaning
is: automatic order-to-stock deduction is out of scope for this release.

The Inventory page may contain only the approved independent-item operations:

- item name, unit, and minimum-stock threshold;
- receive stock;
- physical count;
- manual adjustment;
- Admin/Owner negative-stock override;
- current balances and low-stock state;
- supported movement/count history.

### 3.3 Correct Manager inventory access and read-only behavior

The Manager role must be able to open the inventory route in read-only mode.
The route policy must allow Kitchen, Manager, and Admin according to the
workflow contract, while mutations remain Admin-only except approved item
availability actions.

Manager inventory view must not render buttons or forms for:

- add/edit item;
- receive stock;
- physical count;
- manual adjustment;
- negative-stock override;
- loss reporting or loss approval.

Kitchen and Manager availability actions must remain limited to the approved
`86`/`68` availability behavior and must not alter historical inventory
movements.

### 3.4 Add service-level authorization for Manager data

The `/manager` route is correctly protected by `ManagerOrAdmin`, but the
application services must repeat authorization. Do not rely on navigation
visibility or the Razor route attribute alone.

Correct the live-order and attendance data paths so that:

- `GetLiveOrdersAsync` is callable only for Manager/Admin;
- Manager schedule/presence reads are explicitly bounded and authorized;
- Admin-only attendance history methods are not reused as an unbounded Manager
  query;
- Waiter and Kitchen direct service calls are rejected;
- unauthorized direct URL and service-level tests both pass.

Prefer a dedicated bounded Manager read model/service over passing an Admin
view into the Manager page. The Manager default window is the current shift or
current operational day. A historical metric requires an explicit documented
window and approval.

### 3.5 Remove fabricated table capacity

The table card must not display a hard-coded `Seats: 4`. Use an approved,
persisted capacity field only if one exists in the current domain. Otherwise,
omit the capacity row until capacity is formally added to the contract and
schema.

Keep the following behavior:

- `Reserved` is omitted or shown only as a disabled/static future specimen;
- `Locked` is a display-only waiter-ownership indication;
- server-side ownership and authorization remain authoritative.

### 3.6 Preserve late submission behavior

When `OrderEntryDueUtc` has passed:

- show `EXPIRED` or `LATE` and elapsed lateness;
- keep `Send to Kitchen` enabled because the current domain permits late
  submission;
- do not silently hard-block submission;
- extension requests remain explicit, persisted, and audited.

## 4. Required verification

### Automated

- Build with 0 warnings and 0 errors.
- Existing E2E suite passes.
- Add or update tests proving:
  - no waste/spoilage or recipe controls are rendered;
  - Manager can read inventory but cannot mutate it;
  - Waiter and Kitchen cannot access `/manager`;
  - Manager/Admin can access `/manager`;
  - direct Manager service reads are authorized;
  - Admin-only attendance history is not exposed through the Manager read path;
  - late order submission remains enabled;
  - table capacity is never fabricated.

### Browser/visual

Check at desktop, tablet, and narrow widths:

- navigation has no duplicate connection indicator or user badge;
- Inventory contains only independent-item operations;
- Manager sees read-only schedules/presence and current balances;
- Waiter sees the late banner with an enabled submission action;
- KDS notes and timers remain readable;
- Reserved is not shown as a live actionable state;
- locked ownership is visually clear without becoming a new domain status.

## 5. Rollback and Git procedure

Before editing:

1. Record the current commit SHA (`c453ae7` is the last documented baseline).
2. Preserve the current dirty working tree; do not use `reset --hard` or broad
   cleanup.
3. Record the changed-file list and keep the corrective pass separate from
   unrelated UI work.

After editing:

1. Run `git diff --check`.
2. Run the build and E2E tests.
3. Update `docs/WORK_LOG.md` with changed files, results, limitations, and the
   rollback SHA.
4. Commit one coherent corrective change.
5. Push the branch and verify the remote SHA.
6. Rebuild/redeploy the application only after the commit is verified.
7. Recheck `/health`, login, role routing, and the public URL.

Do not call the redesign complete until the corrective items, automated tests,
browser checks, deployment evidence, and post-UI supervised four-role
acceptance are separately recorded.

## 6. Completion checklist

- [ ] Waste/spoilage UI removed.
- [ ] Recipe placeholder removed.
- [ ] Automatic-deduction-paused wording removed.
- [ ] Manager inventory access is read-only and policy-correct.
- [ ] Manager live-order and schedule/presence service authorization added.
- [ ] Manager data is bounded to current operations by default.
- [ ] Hard-coded table capacity removed or replaced by approved persisted data.
- [ ] Late submission remains enabled with visible lateness.
- [ ] Build and E2E results recorded.
- [ ] Corrective commit pushed and deployment verified.
- [ ] Supervised four-role acceptance remains a separate post-UI gate.
