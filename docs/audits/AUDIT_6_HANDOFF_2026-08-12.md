# External Audit #6 - Independent Review Handoff

**Prepared:** 2026-08-12 (Asia/Shanghai)

**Status:** Review requested; no Audit #6 verdict has been recorded

**Repository:** `ARCWorksPH/ARCWorks-Restaurant-Suite`

**Review branch:** `ui/waiter-shell-account-security` (PR #13)

**Evidence baseline:** `9021815a80fae5ea736d134b1760481a0feb94af`

**Audit #5 mainline recovery:** PR #14 / merge commit
`c0fcaaca591475c0e8531c3e892e793b7f53c1f8`

## 1. Independence boundary

This document is a handoff packet, not an audit result. The project team has
not written, preselected, or approved the findings of Audit #6. The independent
reviewer must inspect the referenced source and evidence, reach their own
conclusions, and submit a separate report.

The resulting report should be added as:

`docs/audits/2026-08-12-waiter-gate0-preimplementation.md`

The report should identify itself as **External Audit #6**, name the reviewed
branch and exact commit, distinguish source inspection from runtime evidence,
and state any limitations on reviewer access.

## 2. Why this audit is being requested now

Audit #5 accepted continued private-beta preparation but left human acceptance,
restore evidence, restaurant-approved data, monitoring ownership, and the AI
hold as explicit gates. Since that review, the project has completed substantial
recovery evidence and a premium landing-page milestone, then paused before a
new Waiter personal-dashboard backend phase.

Audit #6 is intentionally positioned before that backend implementation. The
reviewer can challenge the security model, product boundary, sequencing, and
data design while changes remain inexpensive and reversible.

## 3. Changes and evidence since Audit #5

### 3.1 Backup and recovery evidence

- Normal isolated restore comparison completed.
- Interrupted-restore recovery completed.
- Populated-instance overwrite recovery completed.
- Damaged/extra-data recovery completed with quarantine instead of deletion.
- The live application was not used as a destructive test target.
- Cross-PC runtime restoration remains a future pre-beta portability gate.

Primary evidence:

- `docs/evidence/BACKUP_RECOVERY_DRILL_2026-08-08.md`
- `docs/evidence/OVERWRITE_RECOVERY_DRILL_2026-08-08.md`
- `PROJECT_TIMELINE.md`

### 3.2 Landing-page milestone

- Premium Chef Doy's staff-login landing page accepted by the project owner on
  desktop, landscape, and portrait/mobile layouts.
- The landing page was promoted through reviewed pull requests and recorded as
  an accepted visual boundary.
- Single-session and inactivity policies were deliberately preserved rather
  than redesigned as part of the visual work.

Primary evidence:

- `docs/UI/LANDING_PAGE_ACCEPTANCE_2026-08-12.md`
- `docs/UI/CODEX_LANDING_PAGE_FINAL_WORK_LOG_2026-08-12.md`
- PR #11 and PR #12

### 3.3 Waiter shell and account lifecycle (PR #13)

The current review branch adds or changes:

- reusable usernames after staff deactivation while retaining historical rows;
- forced replacement of temporary passwords at first login;
- server-side prevention of bypassing the password-replacement gate;
- consolidation of attendance, schedule, password, clock-out, and logout
  controls into the authenticated Dashboard;
- Waiter navigation reduced to Dashboard and Tables & Orders;
- explicit landscape navigation collapse/expand behavior;
- preview-only population of 12 tables, 4 menu categories, and 12 menu items;
- contained menu images that do not distort or crop mixed aspect ratios.

Primary evidence:

- `docs/UI/WAITER_SHELL_ACCOUNT_SECURITY_WORK_LOG_2026-08-12.md`
- `docs/UI/PREVIEW_CATALOG_POPULATION_2026-08-12.md`
- PR #13

### 3.4 Gate 0 visual and rollback checkpoint

Gate 0 freezes the accepted landing page and current Waiter dashboard before
personal-dashboard implementation. It includes desktop, landscape, portrait,
and phone-landscape captures plus SHA-256 hashes.

Gate 0 made no feature-code, migration, live-database, public-tunnel, or
production-container changes.

Primary evidence:

- `docs/UI/evidence/checkpoints/2026-08-12-waiter-before-personal-dashboard/`

### 3.5 Backend-first Waiter plan

The proposed next phase deliberately separates backend correctness from UI
work. Its gates cover session reliability, attendance lifecycle, read models,
announcements, leave requests, private server-stored notes, UI implementation,
and final role/browser acceptance.

No feature implementation under that plan has started beyond Gate 0 evidence.

Primary evidence:

- `docs/WAITER_PERSONAL_DASHBOARD_BACKEND_FIRST_PLAN_2026-08-12.md`

## 4. Required independent review questions

The reviewer should answer each question explicitly.

### 4.1 Authentication and session safety

1. Does PR #13 enforce username reuse, temporary-password replacement, and
   role restrictions server-side rather than relying on hidden UI controls?
2. Does the current single-active-session implementation fail closed without
   creating an account-lockout or abandoned-session problem?
3. The new backend plan identifies that the current authentication cookie uses
   a fixed expiration while the product policy promises 15 minutes of genuine
   inactivity. Is the proposed Gate 1 repair technically correct and complete?
4. Are logout, clock-out, password change, user deactivation, and security-stamp
   invalidation handled consistently across open sessions?
5. Is any persistent application data unnecessarily left on a staff device?

### 4.2 Attendance boundary

1. Is keeping attendance clock-in/out independent from application login/logout
   a sound operational decision?
2. Is the proposed 12-hour fail-safe automatic clock-out plus manager-review
   flag sufficiently auditable and resistant to duplicate processing?
3. Does the plan avoid silently correcting payroll-relevant records?

### 4.3 Waiter personal data and privacy

1. Is a server-stored personal notebook with no Manager/Admin read surface a
   defensible promise given database-operator and backup access?
2. Are encryption, key custody, export/deletion, audit-metadata, and recovery
   expectations clear enough before implementation?
3. Should the feature be deferred if the product cannot honestly describe its
   privacy boundary to staff?

### 4.4 Role and workflow scope

1. Does the proposed personal Waiter dashboard preserve the frozen
   Waiter-Kitchen-Manager-Admin workflow contract?
2. Are announcements, leave requests, today's date-based staff portraits, and
   profile editing bounded appropriately for a private beta?
3. Does `Enter the Floor` correctly remain gated by an active attendance record
   without coupling application authentication to payroll time?
4. Are any proposed features premature or likely to delay the core beta?

### 4.5 Recovery, operations, and release posture

1. Do the completed same-PC restore drills close Audit #5's restore-evidence
   blocker at the data-validation level?
2. Is the remaining cross-PC runtime restore correctly retained as a separate
   pre-beta portability gate?
3. Are staging, tunnel, monitoring, and incident-ownership responsibilities
   sufficiently explicit?
4. Is restaurant-approved operational data still correctly treated as open?
5. Confirm that AI remains held and disconnected from the active product.

### 4.6 Evidence integrity and CI

1. Verify that the Gate 0 hashes match the recorded screenshots.
2. Separate EF Core/InMemory coverage from MariaDB, browser, multi-device, and
   supervised human acceptance.
3. Review CI and PR #13 checks; identify flaky acceptance selectors separately
   from product defects.
4. Identify any security-sensitive or misleading readiness claim in the
   current documentation.

## 5. Requested severity and disposition format

The Audit #6 report should contain:

1. reviewed branch, exact commit, access method, and evidence limitations;
2. overall disposition: accepted, conditionally accepted, or rejected;
3. findings grouped as Critical, High, Medium, Low, and Informational;
4. mandatory remediation before Gate 1 implementation;
5. mandatory remediation before private beta;
6. advisory improvements that must not block the core build;
7. closure/reassessment of each applicable Audit #5 mandatory item;
8. an explicit statement that the report does not itself authorize production;
9. an update to `docs/audits/README.md` adding Audit #6 only after the report is
   complete.

## 6. Deliberate exclusions

Audit #6 must not treat the following as accidental omissions:

- AI is on a gated hold and is not part of the active product.
- Recipe-based inventory and automatic recipe deduction are out of scope.
- Restaurant-approved production data has not yet been supplied.
- Cross-PC runtime restoration is planned but not claimed complete.
- Supervised four-role restaurant acceptance remains a human/private-beta gate.
- The new personal Waiter dashboard UI has not been implemented; Gate 0 is the
  accepted `before` checkpoint.

## 7. Submission boundary

The independent reviewer should create a dedicated branch such as
`docs/audit-6-waiter-gate0`, based on the current PR #13 review branch, and open
a pull request targeting `ui/waiter-shell-account-security`.

The audit pull request should change only:

- the new Audit #6 report;
- `docs/audits/README.md` to add the completed report;
- narrowly necessary audit cross-references.

It must not implement remediation, alter application code, rewrite prior audit
findings, merge PR #13, or release Gate 1. The project owner and implementation
team will assess the findings separately.
