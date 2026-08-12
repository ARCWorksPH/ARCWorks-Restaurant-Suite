# ARCWorks Restaurant Suite — UI Redesign Implementation Instructions

**Audience:** UI implementation agent (Gemini, Claude, or another delegated
designer/developer)

**Status:** Revised 2026-08-06. The backend workflow implementation and
database migration are complete and have automated/synthetic evidence. UI
implementation may proceed now. Supervised four-role/browser acceptance is
intentionally deferred until the redesigned UI is available; do not claim it
passed before that gate.

**Product:** ARCWorks Restaurant Suite

**Repository:** `ARCWorksPH/ARCWorks-Restaurant-Suite`

**Authoritative project root:** `D:\ARCWorks_Restaurant_Suite`.
All source, docs, compose files, and test commands in this handoff refer to
that root. Do not use the historical `D:\GBServerPH - Staff-side Restaurant
Ordering App` or `D:\ARCWorks_Restaurant Suite` paths.

## 1. Non-negotiable sequencing

Do not begin visual implementation until the following backend prerequisites
are confirmed:

- Four-role workflow implementation is complete.
- Waiter return/resubmission behavior is implemented.
- Standard waiter and kitchen timers are persisted and tested.
- Admin-configured per-item preparation targets are migrated and tested.
- Manager live-only access boundaries are implemented and tested.
- The workflow migration has been applied successfully to an isolated database.
- The migration has been backed up and a rollback path has been verified.
- Focused domain, integration, and role-authorization tests pass for the
  implemented backend slice.
- The workflow contract has been updated to reflect the final implementation.

These prerequisites are now satisfied for the current workflow slice. The
remaining supervised four-role/browser acceptance is a post-UI gate, not a
reason to hold the redesign.

The UI redesign must consume the stable workflow. It must not be used to hide,
work around, or redefine incomplete backend behavior.

Before touching UI source files, read these documents in full from the
canonical root:

1. `D:\ARCWorks_Restaurant_Suite\docs\PROJECT_TIMELINE.md`
2. `D:\ARCWorks_Restaurant_Suite\docs\ROADMAP_2026-08-06.md`
3. `D:\ARCWorks_Restaurant_Suite\docs\WORKFLOW_CONTRACT_2026-08-06.md`
4. `D:\ARCWorks_Restaurant_Suite\docs\REVISED_WORKFLOW_DRAFT_2026-08-06.md`
5. `D:\ARCWorks_Restaurant_Suite\docs\UI_REVISION_ALIGNMENT_REVIEW_2026-08-06.md`
6. `D:\ARCWorks_Restaurant_Suite\docs\WORK_LOG.md`
7. The latest workflow migration and its migration/recovery evidence under
   `D:\ARCWorks_Restaurant_Suite`

If one of these files is absent or contradicts the contract, stop and report
the missing/contradictory path. Do not reconstruct requirements from a
mockup, an old folder, or chat history.

If those documents disagree with a mockup, the approved contract and tested
runtime behavior take precedence.

## 2. Design direction

Adopt the Phase A visual language:

- dark charcoal/glass surfaces;
- restrained cyan, blue, amber, green, purple, red, and grey status accents;
- reusable stateful components rather than page-specific one-off controls;
- high-contrast readable text;
- touch-friendly controls for restaurant devices;
- clear elapsed and remaining timers;
- visible validation, busy, disabled, success, warning, and error states;
- responsive layouts for desktop monitors, tablets, and narrow screens;
- subtle visual depth and glow without sacrificing legibility or performance.

The component sheet is a visual reference, not a permission or behavior
specification. Every displayed action must map to an approved service operation.

## 3. Role and security boundaries

The application has four intended roles:

### Waiter

Display only waiter/table/order actions:

- view permitted tables;
- create and edit a draft;
- add, remove, replace, and annotate draft items;
- submit an order;
- see the standard order-entry timer;
- request an extension;
- see a kitchen return reason;
- correct and resubmit a returned order;
- serve a ready order;
- view only permitted current order information.

Do not expose kitchen transitions, payment confirmation, Manager settings,
Admin corrections, or unrestricted historical data.

### Kitchen

Display only kitchen operations:

- view submitted kitchen tickets;
- see the standard acceptance timer;
- accept/start preparation;
- return an order with a required reason;
- request an acceptance/preparation extension;
- see item-based preparation target and elapsed/remaining time;
- mark an order ready;
- mark a menu item unavailable (`86`) according to the approved availability
  policy.

Do not expose waiter editing, payment confirmation, Manager configuration,
Admin correction, or historical editing.

### Manager

Provide a separate supervision/configuration interface, not the actionable
waiter or kitchen interface:

- live/current-shift tables and order state;
- kitchen workload and timer state;
- waiter submission and extension indicators;
- current operational performance indicators;
- standard waiter order-entry timer configuration;
- standard kitchen acceptance timer configuration;
- schedules and operational roster settings;
- current inventory balances and low-stock state;
- future availability configuration where authorized.

Manager must not receive buttons that submit, accept, return, prepare, mark
ready, serve, pay, or resubmit an order. Do not provide impersonation controls.
Do not present unrestricted historical records or edit processed records.

Manager schedule and roster data is read-only in this interface. The Manager
may view current schedules, current-shift staffing, and presence indicators,
but must not add, edit, or delete shifts or roster records. Those mutations
remain Admin/Owner-only in the Administrator interface.

Manager live-data boundary: dashboard queries may include active orders and
drafts currently in operational states (`Draft`, `New`, `ReturnedToWaiter`,
`Preparing`, and `Ready`), current table assignments, currently clocked-in
staff, current shifts, active timer/extension state, and current inventory
balances/availability. They must not load unrestricted completed/cancelled
history or historical performance records. If a metric needs historical data,
define and document an explicit bounded window and query before implementing
it; default to the current shift/operational day.

### Admin/Owner

Admin/Owner retains full system authority:

- users and roles;
- security and system configuration;
- menu/catalog and tables;
- per-item preparation minutes;
- payments;
- inventory and protected corrections;
- historical review and audited corrections;
- recovery and operational administration.

Any historical correction or override must require a reason and preserve the
original value, new value, actor, timestamp, and audit evidence.

Visual hiding is never a security boundary. Server-side route, service, and
domain authorization must remain intact and must be tested directly.

## 4. Workflow surfaces to implement

### 4.1 Tables overview

Use the table-card concept, but preserve current status semantics:

- Available
- Occupied / active draft
- New / submitted
- Preparing
- Ready to serve
- Pending payment
- Cancelled where historical/context display is required

Each card may show table number, assigned waiter, item/order count, total where
the role is allowed to see it, elapsed activity, and the permitted next action.

Do not invent a `Reserved` state: it is not in the current `TableStatus` domain
enum. Omit the Reserved card, or render it only as a clearly disabled/static
future-state specimen that has no action and is not presented as live data.
If a table card shows `Locked`, calculate that as a display-only indication of
current waiter ownership; it is not a new persisted status and must never
replace server-side ownership or authorization checks.

The standard waiter order-entry timer begins when a waiter selects a table and
starts a draft. The visual timer must use persisted/authoritative values, not a
client-only approximation. When `OrderEntryDueUtc` has passed, show a clear
`EXPIRED`/`LATE` state and elapsed lateness, but keep `Send to Kitchen`
available because the current domain permits late submission. Do not silently
turn this visual state into a hard block or invent a client-only extension.

### 4.2 Waiter order editor

Adopt the three-pane concept where screen size permits:

- category navigation;
- menu item cards;
- current-order summary.

Required behaviors:

- draft add/remove/replace;
- item quantity controls;
- item-level notes;
- visible availability state;
- visible validation and busy states;
- submit button with clear timer state;
- an `EXPIRED`/`LATE` banner when the order-entry deadline has passed;
- extension request control;
- returned-order reason and resubmission-note field;
- resubmit action only when the order is returned;
- ready-to-serve action only when the order is `Ready`.

Do not add customer-name, reservation, discount, tax, recipe, or payment
features unless they already exist in the tested contract.

Menu photos are optional. If used, use local versioned assets. Every menu card
must reserve the same image box and use a uniform neutral fallback (for
example, the approved plate/fork/beverage SVG inside the glassmorphism box)
when the asset is missing or fails to load. Missing imagery must never prevent
ordering or change menu identity, price, availability, or quantity.

### 4.3 Kitchen display system

Adopt the 1080p KDS concept:

- oldest incoming tickets first;
- large table/order identifiers;
- clear `New`, `Preparing`, `Returned`, and `Ready` distinctions;
- prominent acceptance and preparation timers;
- item quantities and notes;
- high-contrast Start, Return, and Ready actions;
- required return-reason input;
- extension request state and count;
- clear stale/reconnect indicator.

Preparation target rules:

```text
target minutes = Σ(Admin configured item minutes × ordered quantity)
```

The backend must calculate and snapshot this value when preparation begins
(for example, `TargetPreparationMinutes` and its due timestamp on the order).
The frontend must render the persisted target and due time; it must not execute
aggregate calculation queries or recalculate the target in the client circuit.
Changing a menu item’s future preparation time must not rewrite an active
order’s recorded target.

Do not implement client-side timer resets that alter the authoritative target.
An extension must be a persisted request/event with actor, reason, timestamp,
and count.

### 4.4 Manager operations dashboard

Create a separate dashboard rather than cloning the waiter or kitchen page.
It should show live/current-shift information and configuration controls, for
example:

- active table/order counts;
- submitted, preparing, and ready counts;
- late acceptance/preparation indicators;
- extension counts;
- returned-order counts;
- current staff presence/schedule indicators;
- low-stock state;
- configuration panels for the standard timers.

The Manager dashboard route must be `/manager` (or the explicitly equivalent
`/manager/dashboard`) and must use the `ManagerOrAdmin` policy. Direct URL
access must be denied for Waiter and Kitchen roles even if navigation links are
hidden.

Do not expose completed-order history or historical editing in the Manager
dashboard. If a metric requires historical aggregation, obtain explicit
approval for its time window and data boundary first.

### 4.5 Independent-item inventory

Use the bento visual layout only for supported inventory operations:

- add/edit independent item;
- unit and minimum-stock threshold;
- receive stock;
- physical count;
- manual adjustment;
- Admin/Owner negative-stock override;
- current balance and low-stock indication;
- movement/count history.

Do not implement or visually imply:

- Recipe Ingredient Configuration
- recipe/yield/costing
- automatic order-to-stock deduction
- supplier management
- barcode management
- unit-cost accounting
- total inventory valuation
- waste/spoilage approval

The current inventory scope is intentionally simpler than the mockup.
Do not display the mockup banner stating that automatic stock deduction is
paused; automatic deduction is out of scope, not a paused feature.

## 5. Component and state requirements

Create or extend shared styles/components for:

- primary, secondary, quiet, danger, busy, disabled, and focus states;
- accessible status pills and badges;
- table cards;
- menu cards;
- KDS tickets;
- alert callouts;
- inline validation;
- timer/late/extension indicators;
- empty, loading, error, stale, reconnecting, and unauthorized states.

Every action must provide feedback:

1. user activates the control;
2. control enters busy/disabled state;
3. server operation completes or fails;
4. authoritative data reloads;
5. success/error state is visible and understandable.

Do not show a success message before the database operation succeeds.

Accessibility requirements:

- keyboard focus must be visible;
- color must not be the only status signal;
- labels and error messages must be associated with inputs;
- touch targets must be large enough for kitchen/tablet use;
- timers must have readable text, not color-only urgency;
- reduced-motion preferences must be respected;
- contrast must remain readable against glow effects.

## 6. Implementation rules

- Reuse existing CSS tokens and component patterns before introducing new
  frameworks or dependencies.
- Keep changes inside the existing Blazor structure unless a documented reason
  requires a new abstraction.
- Do not change domain rules from a Razor component.
- Do not add client-only state for data that affects authorization, timers,
  order transitions, payments, or audit history.
- Do not add a mock API, fake success path, or hard-coded restaurant data to
  make a screenshot match.
- Do not reintroduce AI, command gateway, recipes, or autonomous actions.
- Do not expose database identifiers, credentials, tokens, or secrets in UI,
  screenshots, logs, or committed test fixtures.
- Preserve the current application branding: **ARCWorks Restaurant Suite**.
- Preserve compatibility namespaces and database identifiers unless a separate
  migration decision is approved.

## 7. Rollback and safety procedure

Before each UI milestone:

1. Confirm the working tree is clean.
2. Record the current commit SHA.
3. Confirm the current workflow/database migration status.
4. Create or verify a logical database backup and its SHA-256 manifest.
5. Preserve the current Docker image/tag or deployment reference.
6. Create a small, reversible branch/commit for the milestone.

UI work must not modify production data or apply an unverified migration.

If a UI change causes a regression:

1. stop the affected deployment;
2. capture the browser error, console output, current commit, and container
   logs;
3. revert the UI commit or redeploy the last known-good image;
4. verify `/health`, login, role routing, and the core order path;
5. preserve the incident evidence;
6. document the cause and corrective action before retrying.

Never use destructive Git operations such as `reset --hard` or broad cleanup
to recover a UI change unless the exact target and recovery point have been
verified and the owner has explicitly approved it.

## 8. Required test gates

### Automated

- Release build with zero warnings and zero errors.
- Domain tests for every visualized transition and validation rule.
- Integration tests for waiter, kitchen, Manager, and Admin authorization.
- Tests for returned-order reason and resubmission-note requirements.
- Tests for standard timer configuration and extension persistence.
- Tests for item-based preparation target calculation.
- Tests confirming Manager cannot perform waiter/kitchen transactions.
- Tests confirming processed records are immutable to non-Admin roles.

### Browser/visual (after UI implementation)

- Desktop monitor layout.
- Tablet/touch layout.
- Narrow viewport layout.
- Waiter flow with draft, submit, return, correction, and resubmit.
- Kitchen flow with accept, return, extension, preparation, and ready.
- Manager live-only dashboard and configuration boundaries.
- Admin catalog preparation-time configuration.
- Inventory without recipe or unsupported fields.
- Error, busy, stale, reconnect, empty, and unauthorized states.

The UI is not accepted because a screenshot looks correct. The redesigned UI
may be implemented and checked with the synthetic backend acceptance while
human-supervised four-role testing is deferred. Final acceptance still
requires the underlying action, authorization, persistence, audit event, and
reload behavior to pass.

## 9. Documentation and Git process

Before implementation:

- create a dated UI work log entry;
- list the exact pages/components in scope;
- record the baseline commit and test results;
- identify any mockup element intentionally rejected.

Because the supervised four-role acceptance is deferred, also record that the
UI milestone is provisional until that post-UI gate is run.

After each coherent milestone:

- record changed files and behavior;
- record build/test/browser results;
- record known limitations and rollback SHA;
- update `docs/WORK_LOG.md`;
- update `docs/ROADMAP_2026-08-06.md` if phase status changed;
- update `PROJECT_TIMELINE.md` for a material product decision;
- commit one coherent change with a descriptive message;
- push the branch and verify the remote SHA;
- keep `main` protected and use the existing pull-request workflow.

Do not claim “complete,” “production-ready,” or “accepted” from a build or
mockup comparison alone. Separate automated, browser, visual, live, and
operator acceptance evidence.

## 10. Completion checklist

- [x] Workflow implementation and migration complete with automated/synthetic
      evidence before UI work begins.
- [ ] Baseline commit, backup, migration, and rollback evidence recorded.
- [ ] Shared components and state styles implemented.
- [ ] Tables overview implemented without inventing unapproved states.
- [ ] Waiter order editor supports the approved workflow.
- [ ] KDS supports accept/return/ready and authoritative timers.
- [ ] Manager dashboard is separate and live-only.
- [ ] Admin preparation-time configuration is functional.
- [ ] Inventory redesign excludes recipes and unsupported fields.
- [ ] Role, accessibility, responsive, error, stale, and reconnect tests pass.
- [ ] Documentation and GitHub branch/PR are updated.
- [ ] Final supervised four-role browser acceptance and rollback rehearsal are
      documented after the UI redesign.
