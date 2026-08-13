# ARCWorks Restaurant Suite — Project Timeline

This is the concise decision timeline for the active repository. Detailed
implementation evidence remains in `docs/WORK_LOG.md` and the dated logs
linked below.

## 2026-07-26 — Phase 1 baseline

- Established the role-based waiter-to-kitchen-to-payment application baseline.
- Confirmed that automated EF/InMemory tests are not equivalent to MariaDB,
  Docker, HTTPS, SignalR, or browser acceptance.
- Kept inventory acceptance behind reversal and correction safeguards.

## 2026-07-30 to 2026-08-02 — Resilience, security, and AI evaluation

- Completed negative-stock, manager/Admin override, synthetic resilience, and
  external security/audit work.
- Preserved benchmark evidence while placing AI behind a fail-closed hold;
  recipes, autonomous actions, and natural-language SQL remain deferred.
- Selected `qwen2.5:3b` only as a future isolated laboratory default; it is not
  a production dependency.

## 2026-08-04 — Branding and portable preparation

- Adopted **ARCWorks Restaurant Suite** as the public product name while
  preserving ROMS compatibility identifiers, migrations, and data keys.
- Prepared the optional Resto-VM profile without starting or changing the live
  VM instance.

## 2026-08-06 — Deterministic workflow freeze

- Froze the four-role contract: Waiter, Kitchen, Manager, and Admin/Owner.
- Defined waiter draft editing and timed submission, kitchen return/resubmit,
  persisted timer extensions, item-based preparation targets, Manager
  live-only supervision, and immutable processed records for non-Admin roles.
- Removed recipe/yield/costing, waste/spoilage approval, and automatic
  order-to-stock deduction from the active release.
- Deferred operational alert codes until the core workflow is accepted.

## 2026-08-06 — Backend workflow implementation

- Implemented Manager timer configuration, persisted order/kitchen deadlines,
  bounded extension events, preparation-target snapshots, and live-order
  service support.
- Applied and verified the workflow migration against an isolated/live
  database; recorded rollback and migration evidence.
- Added a deterministic synthetic four-role acceptance test. Supervised
  browser acceptance is not claimed yet.

## 2026-08-06 — UI redesign gate revision

- Approved the dark glass/component-sheet visual direction for tables, KDS,
  waiter order editor, and simplified independent-item inventory.
- UI implementation may proceed against the stable backend and synthetic
  checks. Human-supervised four-role/browser acceptance is intentionally
  deferred until the redesigned UI is available.
- Added the clarification rules to the UI implementation handoff: canonical
  document paths, Manager query boundaries, uniform missing-image fallbacks,
  and server-snapshotted preparation targets.

## 2026-08-07 — UI corrective pass: availability and Manager read model

- Enforced the availability role matrix in the application service: Kitchen
  may apply `86` (mark unavailable) only; Manager and Admin may apply `68`
  (restore) as well as `86`. The server remains authoritative even if a UI
  control is bypassed.
- Replaced the Manager attendance payload that reused the Admin three-day
  view with a dedicated, read-only `ManagerOperationalView` containing only
  currently clocked-in active staff. Historical attendance, schedules, and
  administrative corrections remain Admin-only.
- Added targeted authorization and role-matrix coverage. Build and targeted
  Manager tests pass; full browser/visual acceptance remains a user test gate.

## Current decision

The next gate is supervised visual and role-based acceptance of the redesigned
UI. It must not expand the domain scope or reintroduce AI/recipes. Only after
that acceptance should workflow completion be claimed.

## 2026-08-07 — UI data population and Phase 1 shell correction

- Created a reversible pre-change MariaDB dump under `backups/` before adding
  demo catalog data.
- Populated the active test instance with 12 tables, 4 menu categories, 12
  menu items, and 10 independent inventory items. No orders, attendance
  history, stock movements, or recipe data were changed.
- Began Phase 1 UI shell work: removed the desktop horizontal navigation,
  restored the standard left-side navigation, and made the brand render once
  per responsive viewport.
- Responsive phone drawer behavior and the intentional landscape-only KDS
  gate remain the next UI phases; they are not being claimed complete by this
  change.

## 2026-08-07 — Landscape shell regression correction

- Corrected the standard shell's flex rule so landscape desktop/tablet views
  place the side navigation beside the content instead of pushing the content
  below a full-width sidebar.
- Verified one visible desktop brand, no unintended horizontal overflow, and
  the responsive shell through the Playwright smoke suite.

## 2026-08-07 — Kitchen display and staff-access corrective pass

- Fixed the desktop KDS shell so the Kitchen Display renders beside the
  navigation rail rather than below it. The rail now stays expanded by
  default; the top-bar **Minimize panel / Expand panel** control makes
  compaction explicit and reversible.
- Corrected pending-payment ticket contrast by using dark text on the light
  invoice surface, including headings, amounts, and timestamps.
- Added the Admin **Remove access** action. Removal is a soft deactivation
  (`IsActive = false`), so the Identity row and all order, attendance, and
  audit references remain available for historical records; inactive users are
  rejected by the existing login gate. A restore control was intentionally
  removed in the follow-up policy pass: reactivation is an Admin-only
  administrative operation, not an employee-facing workflow.
- Rebuilt the app container and ran the Playwright smoke suite: **3/3 passed**
  (including expanded KDS navigation, explicit collapse/restore, and the
  existing workflow smoke coverage). The solution build completed with **0
  warnings, 0 errors**.

## 2026-08-07 — Catalog, access, payments, and audit follow-up

- Added the Kitchen-facing availability panel: Kitchen can mark an active
  menu item unavailable (`86`) as soon as the shortage is known, while
  Manager/Admin retain restore authority (`68`). The server remains
  authoritative for the role restriction.
- Repeated additions now merge only when the menu item and its notes match.
  Identical notes increase one line quantity; different notes remain separate
  lines, preserving kitchen instructions and the combined total.
- Pending Payments is now explicitly Manager-or-Admin only, including payment
  confirmation. Waiters and Kitchen staff remain excluded.
- Staff removal remains Admin-only and is now hidden from the normal user list
  after deactivation. The Identity row and historical references are retained;
  inactive staff are excluded from new scheduling choices and current schedule
  rows while historical attendance remains preserved.
- Admin catalog controls now provide Edit and Delete actions for menu items and
  Delete actions for tables/categories. These are reversible soft removals
  (`IsActive = false`), not destructive database deletes, so historical orders
  remain valid. Menu edits include name, description, price, category, and
  preparation time, with old/new values captured in the audit record.
- Added the Admin-only historical action log to Reports with date filtering,
  server-recorded actor/action/entity data, and expandable raw JSON values.
  This is an operational audit log rather than noisy client-side click
  telemetry.

## 2026-08-08 — Waiter summary, schedule safety, and confirmation safeguards

- Added a read-only customer order summary to the waiter order editor. It is
  refreshed with the order event stream and after each local edit, so the
  displayed quantities, notes, and total remain current while the editable
  controls stay separate.
- Moved the Kitchen `86` availability panel to the bottom of the KDS so active
  tickets remain the primary workspace.
- Added Admin schedule editing for today and future schedules only. Past
  schedules are not editable in the UI or service; historical schedule records
  remain preserved.
- Added confirmation prompts before Admin staff removal, catalog/schedule
  deletion, clock-out, and both logout paths.
- Added Admin schedule downloads: a seven-day schedule CSV export and a
  download-ready CSV template using one schedule per row (`Staff`, `Username`,
  `Start`, `End`, `Notes`). The existing attendance export remains available
  separately.

## 2026-08-13 — Authenticated browser-copy fail-safe gate

- Hardened the staff login boundary from one active credential session to one
  exact authenticated application instance.
- A second runtime presenting a copied authentication cookie now revokes the
  entire staff session; both the suspected copy and the original are forced to
  login rather than allowing ARCWorks to guess which one is legitimate.
- Added immutable, fingerprint-only replay evidence and retained Manager/Admin
  dashboard alerts. Repeated use of the revoked copy remains denied without
  creating an alert flood.
- Verified the distinction between an ordinary page reload and a genuinely
  fresh copied-browser runtime with a real MariaDB + Playwright regression.
- Work remained isolated in `D:\ARCWorks_Restaurant_Suite_Codex_Waiter_Shell`;
  the live container, database, tunnel, and public hostname were not changed.

## 2026-08-14 — Gate 1 promoted to the live instance

- Accepted Gate 1 was merged with the approved landing page and current live
  UI checkpoint, backed up, and deployed to the ROMS application container.
- A copied authenticated runtime now triggers server-side fail-safe revocation
  for the whole staff session. The current migration, local service, and public
  tunnel endpoint were verified after deployment.
- The previous web image remains available under the explicit Gate 1 rollback
  tag. Gate 2 begins with central restaurant/time contracts and final Waiter
  dashboard mockups; it does not begin by changing the operational UI.
