# Gate 2 Waiter Dashboard — Final Implementation Plan

**Status:** final owner-approved baseline; Gates 2A through 2C are implemented and verified. Gate 2D is next.

**Branch at plan freeze:** `design/gate2-waiter-dashboard-contract`

**Depends on:** completed Gate 1 single-session and duplicate-session protection.

## 1. Outcome

Deliver a personal Waiter workspace for Chef Doy's Gourmet Restaurant that is visually separate from the operational floor, uses server-authoritative restaurant time, exposes only Waiter-approved information, and transitions to Tables & Orders only while the employee is clocked in.

The finished stage includes:

1. modular restaurant identity and assets;
2. a restricted Waiter Dashboard read model;
3. reliable attendance state and bounded automatic closure;
4. profile portraits and a date-based Today's Team carousel;
5. dismissible staff announcements and shift Manager notes;
6. structured leave requests;
7. an owner-only encrypted personal journal;
8. a responsive borderless dashboard and Staff Hub overlay;
9. automated, browser, privacy, recovery, and Android acceptance evidence.

## 2. Frozen visual composition

### Landscape

- No office navigation panel.
- No full-height left or right utility rail.
- Top identity row: restaurant logo, personal greeting, server date/time, attendance state, and one stateful Clock In / Clock Out control.
- Main grid: Today's Shift and My Hours & Attendance use the available width in a balanced two-column arrangement.
- Today's Team uses an expanded horizontal carousel below the main cards.
- Bottom action dock:
  - compact profile/avatar launcher;
  - compact Staff Hub launcher with unread and priority state;
  - dominant **Enter the Floor — Tables & Orders** action.
- Staff Hub opens as a centered modal over a dimmed and softly blurred, inert dashboard.
- Content has a readable maximum width and does not stretch indefinitely on ultrawide displays.

### Android portrait

- One-column content order with no horizontal page scrolling.
- Restaurant identity, greeting, server clock, and attendance action remain visible and readable.
- Today's Shift, attendance, Today's Team, and Enter the Floor retain the accepted hierarchy.
- Staff Hub opens as a near-full-height sheet.
- Profile opens as a touch-accessible sheet/drop-up.
- Acceptance widths: 360, 390, 412, and 480 CSS pixels; design reference 1080 x 2400.
- Minimum interactive target: 44 CSS pixels.

## 3. Functional boundaries

### Dashboard

The dashboard may display only:

- approved restaurant logo and identity;
- Waiter display name and greeting;
- server-issued Asia/Manila date and time;
- clocked-in/out state and authoritative clock-in time;
- today's shift, assigned section, and schedule Manager note;
- current Monday-based weekly hours and recent attendance;
- a read-only self-service timesheet containing only the signed-in employee's own attendance records;
- portraits of staff scheduled anywhere on the current Asia/Manila date;
- announcement/Manager-note/leave state counts required by Staff Hub;
- profile launcher;
- Enter the Floor state.

It must not expose payroll, Manager/Admin controls, future team schedules, employee names under Today's Team portraits, private journal contents, or operational order data.

### Staff Hub

Staff Hub contains four destinations:

1. **Shift Announcements** — published Manager/Admin notices with Normal, Important, and Urgent/Acknowledgment behavior. Dismissal is per employee.
2. **Manager Notes** — the note attached to the employee's current-day shift. It is read-only for the Waiter.
3. **Leave Requests** — future-date structured requests with an optional private message and visible request status.
4. **My Journal** — a separate encrypted privacy subsystem available only to the author.

Staff Hub is an overlay, not a new permanent navigation system.

The Manager Note has one source of truth: the existing current-day schedule note. Selecting the note indicator inside Today's Shift opens **Staff Hub -> Manager Notes**; it does not create or copy a second note record.

**View full timesheet** is in scope as a restricted self-service surface. It returns only the authenticated employee's attendance records, contains no payroll calculation or other employee data, and derives identity from the authenticated principal rather than a browser-supplied employee ID.

## 4. Implementation gates

Every gate uses a dedicated feature branch, has an explicit rollback point, produces sanitized evidence, and must pass its exit criteria before the next gate begins.

### Gate 2A — Restaurant identity and authoritative clock

Implement one validated server-side restaurant configuration boundary containing:

- display name: `Chef Doy's Gourmet Restaurant`;
- approved restaurant logo and local asset references;
- display timezone: `Asia/Manila`;
- replaceable labels, colors, and optional contact information defined by the modular asset registry.

Rules:

- Persist instants in UTC.
- Convert to Asia/Manila for display and restaurant-calendar calculations.
- Never use the browser clock for attendance, schedules, timers, expiry, or audit decisions.
- Never load restaurant assets from external URLs.

**Exit:** configuration validation, fallback behavior, UTC/timezone boundary tests, and replacement-asset tests pass.

### Gate 2B — Restricted Waiter Dashboard read model

Create a single server-authorized read model for the signed-in Waiter containing only the frozen dashboard fields. Compute the Monday-based weekly boundary and current restaurant date on the server.

The query must not accept an arbitrary employee ID from the browser. Identity comes from the authenticated principal.

**Exit:** authorization, field-boundary, timezone, empty-state, and inactive-user tests pass.

### Gate 2C — Attendance gate and bounded automatic closure

- Enter the Floor is enabled only while the Waiter has an open attendance record.
- Enforce the same rule server-side; a disabled button is not the security boundary.
- Logout and the 15-minute authenticated-session inactivity timeout do not clock the employee out.
- Automatically close an open attendance record 12 hours after scheduled shift end, or 12 hours after clock-in when no shift end exists.
- Flag automatic closure for Manager review and record an audit event.
- If the employee is still using ARCWorks when automatic closure occurs, revoke floor-entry eligibility immediately, show the reviewed-closure state, and require a fresh Clock In before re-entering the floor. A concurrent manual Clock Out and automatic closure must produce exactly one closed record.
- Never treat automatic closure as a silent payroll correction.

**Exit:** scheduled, unscheduled, logout, inactivity, duplicate execution, restart, and concurrent clock-action tests pass.

### Gate 2D — Profiles and Today's Team

- Add approved portrait metadata and lifecycle state to staff profiles.
- Today's Team membership is based only on the current Asia/Manila calendar date.
- Return portraits only to the carousel; do not return names or roles in that read model.
- Include neutral local fallback portraits.
- All profile edits remain authorization-checked and audited.

**Exit:** date-boundary, portrait fallback, inactive-user, authorization, carousel overflow, and responsive tests pass.

### Gate 2E — Announcements and Manager notes

Implement `StaffAnnouncement` and per-employee dismissal/acknowledgment records.

- Normal: quiet unread indicator.
- Important: labeled priority indicator, not color-only.
- Urgent / Requires acknowledgment: interruptive modal with server-recorded acknowledgment.
- Editing an acknowledged urgent announcement creates a new version requiring acknowledgment again.
- Dismissal hides the notice for one employee and never deletes the source record.
- Expired or inactive announcements are not delivered.
- The existing schedule Notes field supplies the current shift's Manager Note in this stage.

**Exit:** audience, publish/expiry, versioning, dismissal, acknowledgment, authorization, and audit tests pass.

### Gate 2F — Leave requests

Implement employee-owned future-date leave requests with:

- one or more requested dates;
- optional leave type;
- optional private request message;
- Pending, Approved, Declined, and Cancelled states;
- submitted, changed, decision, and cancellation timestamps;
- Manager/Admin decision metadata and optimistic concurrency.

Employees may see only their own requests and may edit/cancel only eligible pending future requests. Approval must not silently rewrite staff schedules during this gate.

**Exit:** ownership, invalid/past dates, overlap, concurrency, state transition, Manager/Admin decision, and audit tests pass.

### Gate 2G — Private ARCWorks Journal

Build an original, purpose-limited ARCWorks Markdown journal. Do not integrate SimpleMDE 1.11.2 as a package or copy its implementation during this gate.

Initial feature scope:

- create, edit, search, soft-delete, restore, and permanently discard the employee's own entries;
- title, Markdown body, optional tags, created time, and updated time;
- bold, italic, headings, lists, quotations, horizontal rule, word count, and sanitized preview;
- desktop focused-writing view and simplified portrait editor;
- no external images or raw HTML in the initial version.

Privacy boundary:

- plaintext exists only in active browser memory while the employee is reading or editing;
- encrypt title, body, and tags in the browser before transmission;
- store only ciphertext, nonces, versioned key metadata, and permitted timestamps in MariaDB;
- use a journal-only passphrase and a separately protected recovery design;
- Manager, Admin, reports, support tools, logs, and backups have no plaintext access;
- do not use cookies, `localStorage`, `sessionStorage`, IndexedDB, Cache Storage, or device files for journal content;
- do not use external CDNs, fonts, spell-checking services, analytics, or telemetry;
- sanitize Markdown preview and reject raw HTML and unsafe URLs;
- audit metadata actions only, never plaintext or ciphertext payloads.
- document and test the final key-loss behavior during Gate 2G: if both the journal passphrase and recovery key are lost, ARCWorks cannot recover plaintext and must not offer an administrative bypass.

**Exit:** independent plaintext scans, XSS tests, ownership tests, logout/device-residue tests, encryption-version tests, backup/restore tests, and documented key-loss behavior pass.

### Gate 2H — Final UI implementation

Implement the frozen landscape and portrait composition only after Gates 2A-2G expose stable tested contracts.

- Use the accepted premium visual system without changing authorization or workflow logic.
- Keep Staff Hub and profile as overlays.
- Preserve keyboard navigation, focus trapping, visible focus, Escape/close behavior, screen-reader labels, reduced-motion behavior, and non-color status cues.
- Do not duplicate business calculations in JavaScript or CSS.

**Exit:** component tests, Playwright role tests, 1920 x 1080 inspection, Android portrait/landscape inspection, no-horizontal-overflow checks, and owner visual acceptance pass.

### Gate 2I — Recovery and deployment acceptance

- Run database migration against a disposable restored instance first.
- Verify rollback from the pre-gate database backup.
- Verify announcements, dismissals, leave requests, portraits, attendance review state, and encrypted journal records through logical backup and disposable restore.
- Deploy locally before the public tunnel instance.
- Verify duplicate-session and inactivity protections remain intact.
- Capture sanitized screenshots for the project visual timeline.

**Exit:** recovery, local runtime, public runtime, monitoring, and final owner acceptance evidence are complete.

## 5. Journal research, attribution, and publication

The folder `C:\Users\GBServerPH\Desktop\simplemde-markdown-editor-1.11.2` was reviewed as UX and architecture research.

- Project: **SimpleMDE Markdown Editor 1.11.2**
- Original author: Wes Cossick / Next Step Webs, Inc.
- Original repository: `https://github.com/NextStepWebs/simplemde-markdown-editor`
- License: MIT
- Useful inspiration: approachable Markdown controls, preview, word count, focused writing, and reduced mobile toolbar.
- Rejected behavior: plaintext local-storage autosave, optional external Font Awesome download, unsanitized preview boundary, external link/image behavior, and the old unpinned dependency tree.

At this planning stage, no SimpleMDE source or binary is incorporated, so ARCWorks is not a SimpleMDE fork and its MIT code is not yet a third-party dependency. This acknowledgment records the design research honestly without claiming derived source.

If any SimpleMDE source is later copied or adapted:

1. stop implementation;
2. identify the exact copied files and modifications;
3. preserve the MIT copyright and license text;
4. add it to the repository's third-party notices and distributable package;
5. run a new dependency/security review.

### Possible public component

After the journal privacy gate passes inside ARCWorks, consider extracting the generic client-side encrypted Markdown editor into a separate repository. Do not create that repository during the restaurant-suite critical path.

Recommended future repository characteristics:

- original implementation and project history;
- framework-neutral privacy contract where practical;
- no ARCWorks, restaurant, employee, database, credentials, or customer assets;
- threat model, integration example, tests, accessibility notes, and device-residue verification;
- deliberate license choice and contribution policy;
- acknowledgment of SimpleMDE as design inspiration.

A SimpleMDE fork is appropriate only if the implementation actually derives from its source. An independently written component should be a new repository, not a misleading fork.

## 6. Rollback and data rules

1. Tag or record the accepted pre-gate commit before each implementation gate.
2. Take and validate a logical MariaDB backup before each schema migration.
3. Test migrations and rollback against disposable instances before live use.
4. Never manually edit the EF migration history or delete production rows to force a rollback.
5. Use forward-compatible migrations and application feature flags where practical.
6. Quarantine unexpected restored data instead of deleting it.
7. A failed gate rolls back only its branch/deployment and cannot weaken Gate 1 security.
8. Journal data is never repaired, inspected, or cleaned using plaintext administrative tools.

## 7. Documentation and GitHub procedure

For every gate:

1. create a focused feature branch from the currently accepted `main`;
2. update the authoritative plan/work log and decision record;
3. implement the smallest coherent gate;
4. add automated tests and sanitized evidence;
5. run build, test, formatting, secret scan, and `git diff --check`;
6. exclude credentials, session identifiers, personal journal data, recovery keys, identifiable private messages, and customer-sensitive information;
7. push the feature branch and open a reviewable pull request;
8. wait for green checks and explicit acceptance before merge;
9. merge, deploy locally, verify, then deploy publicly when authorized;
10. capture milestone screenshots at approved visual checkpoints.

## 8. Final readiness decision

The stage is feasible and its information architecture is approved. Gate 1 is complete. The Gate 2 runtime remains intentionally unimplemented until this plan is accepted.

Recommended first implementation action: **Gate 2A — Restaurant identity and authoritative clock**, followed by the restricted dashboard read model. Journal work remains isolated until Gate 2G so its privacy risks cannot delay or weaken the core attendance dashboard.
