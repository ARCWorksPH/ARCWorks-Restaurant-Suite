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
