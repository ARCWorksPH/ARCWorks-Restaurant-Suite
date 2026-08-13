# Waiter Personal Dashboard — Backend-First Implementation Plan

**Prepared:** 2026-08-12

**Status:** Planning and final review only — no feature implementation authorized yet

**Implementation baseline:** `D:\ARCWorks_Restaurant_Suite_Codex_Waiter_Shell`

**Baseline branch:** `ui/waiter-shell-account-security` at `a259dbe`

**Disposable preview:** `http://127.0.0.1:7171`
**Protected systems:** live database, live application container, Cloudflare tunnel, and `roms.arkworksph.online`

## 1. Goal

Build the waiter experience as two deliberately separated spaces:

1. **Personal dashboard — the calm space**
   - Restaurant-branded, no operational side panel.
   - Clock in/out, today's team portraits, personal attendance, today's shift,
     dismissible staff announcements, leave requests, profile/settings, and a
     genuinely private journal.
2. **Tables & Orders — the operational floor**
   - Entered through **Enter the Floor**.
   - Accessible to a Waiter only while clocked in.
   - Existing order and table behavior remains the operational authority.

The backend and security rules must be complete and tested before the final UI
is attached to them.

## 2. Non-negotiable decisions

- One active authenticated device session per employee account.
- Fifteen minutes of genuine inactivity ends authentication but does **not**
  clock the employee out.
- Normal activity keeps the authenticated session alive; a fixed short cookie
  must not log out an active employee.
- Clock in and clock out are separate from login and logout.
- **Enter the Floor** is enabled only while the employee has an open attendance
  record, and the server enforces the same rule on direct URLs.
- A forgotten attendance session is automatically closed:
  - scheduled shift: `scheduled end + 12 hours`;
  - no associated schedule: `clock in + 12 hours`.
- Automatically closed attendance is provisional and flagged for Manager
  review; it is never silently treated as approved payroll time.
- Today's Team contains portrait images only: no displayed name, role, schedule
  time, profile link, or identity tooltip.
- Today's Team membership uses the restaurant's local calendar date and includes
  active employees whose schedule **starts on that date**. It does not depend on
  whether they are clocked in.
- Staff announcements can be dismissed per employee without deleting the
  original announcement for other employees.
- Leave requests are structured one-way requests, not a chat system.
- The journal is personal. Manager, Administrator, database operator, reports,
  audits, and support functions must not be able to read its plaintext.
- No journal plaintext, persistent authentication credential, application data,
  telemetry, or offline database is stored on an employee device.
- No AI dependency or AI connection is introduced.

## 3. Current-state findings

### Reusable foundations

- ASP.NET Core Identity and role policies already exist.
- `ApplicationUser` already supports active/inactive staff, archived usernames,
  first-login password replacement, and one server-backed active session.
- `StaffSchedule`, `AttendanceRecord`, `AuditEntry`, and their services exist.
- Attendance clock-in, clock-out, scheduling, correction, and audit logging exist.
- The waiter branch already consolidates attendance and account actions on the
  Dashboard and limits Waiter navigation to Dashboard and Tables & Orders.
- A disposable MariaDB preview and populated table/menu catalog are available.

### Blocking defect to resolve first

`Program.cs` currently issues a cookie with a fixed 20-minute lifetime and
`SlidingExpiration = false`. Consequently, a genuinely active employee may be
logged out when the cookie expires. Browser activity detection is also limited
to `pointerdown`, `keydown`, and `touchstart`.

The documented 15-minute inactivity rule therefore is not yet reliably true.
This must be repaired before dashboard expansion.

### Missing backend concepts

- Restaurant-local configuration is hard-coded in UI code instead of owned by
  a validated restaurant configuration service.
- Attendance has no automatic-closure or Manager-review metadata.
- The operational floor does not yet require an open attendance record.
- Staff profile portrait data and approval state do not exist.
- There is no dedicated Today's Team query.
- Announcements and per-employee dismissal records do not exist.
- Leave requests do not exist.
- Encrypted journal records, encryption metadata, and journal endpoints do not
  exist.
- Browser cleanup is not yet a formal, tested logout contract.

## 4. Delivery strategy

Each gate is a separate, reversible commit or short commit series. A gate may
not start until the previous gate passes its automated checks and the evidence
record is written. Live deployment is prohibited until the complete waiter
milestone receives visual and functional acceptance.

### Gate 0 — Freeze and evidence baseline

1. Preserve the accepted landing-page baseline and current waiter runtime.
2. Capture formal desktop, landscape, and mobile waiter-dashboard screenshots.
3. Store them under:
   `docs/UI/evidence/checkpoints/2026-08-12-waiter-before-personal-dashboard/`.
4. Include a `CHECKPOINT.md` with commit, URL, viewport, account role, populated
   data state, limitations, and SHA-256 manifest.
5. Confirm the source worktree is clean before any implementation commit.

**Exit gate:** reproducible before-state evidence exists and the disposable
preview can be rebuilt without touching live services.

### Gate 1 — Repair authentication-session truth

Backend changes:

1. Make the database activity timestamp the authoritative idle clock.
2. Replace the fixed 20-minute active-session cutoff with a longer bounded
   session cookie and sliding renewal, while retaining the 15-minute server-side
   inactivity rule.
3. Expand throttled activity/heartbeat coverage to include pointer, keyboard,
   touch, input/change, scroll, visibility/focus restoration, and the active
   Blazor circuit heartbeat without writing per-event telemetry.
4. Make session expiration atomically clear `ActiveSessionId` and
   `SessionLastActivityUtc`.
5. Keep logout independent from attendance.
6. Keep password reset, account disablement, security-stamp changes, and session
   revocation authoritative.
7. Use session-only, `HttpOnly`, appropriately `Secure`, `SameSite=Strict`
   authentication cookies in the production HTTPS environment.
8. Add logout cleanup headers and removal of ARCWorks-owned browser caches. Do
   not add `localStorage`, IndexedDB, an offline data cache, or a service worker.

Tests:

- Active use beyond 20 minutes remains authenticated.
- Fifteen minutes without server-recognized activity logs out.
- Second-device login is rejected while the first session is current.
- Second-device login succeeds after logout or inactivity expiration.
- Logout does not clock out; clock out does not automatically log out.
- Password change and account disabling revoke the prior authenticated state.
- No stale session claim remains after inactivity cleanup.

**Exit gate:** session policy documentation matches actual automated and browser
behavior.

### Gate 2 — Restaurant time and dashboard read contract

1. Introduce validated restaurant settings, initially:
   - time zone ID: `Asia/Manila`;
   - restaurant display name and logo reference;
   - attendance auto-close grace: 12 hours.
2. Centralize UTC/local conversion in a restaurant-clock service. Remove new
   page-level hard-coded conversions.
3. Add a single `WaiterDashboardView` read contract containing only data the
   authenticated employee may receive:
   - personal display/profile state;
   - current local date/time metadata;
   - open attendance state;
   - recent personal attendance summaries;
   - today's assigned shift and notes;
   - today's portrait carousel entries without names or identity metadata;
   - visible announcements and this employee's dismissal state;
   - current leave-request summaries.
4. Return no Manager/Admin-only fields through this read model.

**Exit gate:** contract and authorization tests prove a Waiter can retrieve only
their own personal data plus the intentionally anonymous portrait carousel.

### Gate 3 — Attendance gate and automatic closure

Domain/database changes:

1. Extend `AttendanceRecord` with:
   - scheduled-end snapshot;
   - automatic-close deadline;
   - closure source (`Employee`, `Administrator`, `Automatic`);
   - automatic-closure timestamp;
   - review status (`NotRequired`, `Pending`, `Approved`, `Corrected`);
   - reviewer, review timestamp, and review note.
2. Snapshot the relevant schedule end at clock-in so later schedule edits cannot
   silently move an existing attendance deadline.
3. Permit clock-in without a schedule but flag it for Manager visibility. This
   avoids blocking real work when scheduling data is incomplete.
4. Add an idempotent server background processor that closes overdue records.
5. Prevent two workers/containers from closing the same record twice using an
   atomic conditional update or equivalent concurrency guard.
6. Audit the closure event and subsequent Manager review without changing the
   original facts.
7. Add an `ActiveAttendance` authorization requirement for Waiter access to
   Tables & Orders. Admin access remains governed by Admin authority rather than
   pretending to be a clocked-in Waiter.

Tests:

- Scheduled and unscheduled deadlines calculate correctly.
- Logout leaves attendance open.
- Scheduled shifts edited after clock-in do not rewrite the snapshot.
- Automatic close occurs once and is flagged Pending.
- Restarting the application catches overdue records safely.
- Direct navigation to Tables & Orders is denied when a Waiter is not clocked in.
- Clocking in enables access; clocking out removes access.
- Manager review is audited and cannot erase the original closure event.

**Exit gate:** the attendance lifecycle survives restart, concurrency, direct
URL attempts, and missing schedule data.

### Gate 4 — Profile portraits and Today's Team

1. Store employee portraits on the server/database, not on the employee device.
2. Restrict type, decoded dimensions, and byte size; normalize accepted images
   to a safe display format and remove metadata.
3. Use an approval lifecycle so employee uploads do not become team-visible
   until an Administrator approves them.
4. Retain the previous approved image until replacement approval.
5. Today's Team query rules:
   - restaurant-local current date;
   - active staff only;
   - schedule local start date equals current date;
   - order by scheduled start, then stable staff identifier;
   - de-duplicate employees with multiple entries;
   - no clock-in dependency;
   - return portrait bytes/reference and an opaque carousel key only.
6. Missing or unapproved photo renders a neutral silhouette, never initials.
7. Portraits are non-clickable and non-focusable; generic accessible text must
   not reveal identity.

**Exit gate:** network payload and rendered markup do not expose names, roles,
schedule times, usernames, employee IDs, or profile links through the carousel.

### Gate 5 — Dismissible staff announcements

Entities:

- `StaffAnnouncement`: content, audience, publish/expiry window, author,
  priority, created/updated timestamps, active state.
- `StaffAnnouncementDismissal`: announcement ID, employee ID, dismissed UTC.

Rules:

- Manager/Admin may publish within their final role authority; exact authorship
  permission is enforced server-side.
- The waiter dashboard shows at most the small configured number of current
  announcements.
- Dismissal is per employee and does not delete the announcement.
- Expired/inactive announcements are not delivered.
- Announcement text is work data and is audited; dismissal is a lightweight
  event, not a message conversation.

**Exit gate:** audience, expiry, dismissal isolation, inactive-user handling,
and authorization tests pass.

### Gate 6 — Leave requests

Entity: `LeaveRequest` with employee, date/range, whole/partial day, optional
leave type, optional message, state, submitted/updated/cancelled timestamps,
Manager decision metadata, and concurrency token.

Rules:

- Future dates only at initial submission.
- Reason is optional; `Personal leave` is sufficient.
- Employee sees and edits only their own pending future requests.
- Employee may cancel a pending request.
- Approved requests are not silently rewritten; later change starts a
  cancellation/change request.
- Historical entries are read-only.
- Manager processing will be exposed in the later Manager UI, but backend role
  authorization and audit behavior are implemented and tested here.
- Approved leave does not silently rewrite schedules in this milestone.

**Exit gate:** ownership, state transitions, invalid dates, overlapping request
rules, concurrent decisions, and audit tests pass.

### Gate 7 — Private, portable My Journal

This gate is an isolated privacy subsystem and must not share ordinary Blazor
form binding with the journal plaintext.

#### Cryptographic design

1. Generate a random per-employee journal data-encryption key in the browser.
2. Derive a wrapping key from a journal-only passphrase using a reviewed,
   versioned password-based key derivation scheme with per-user salt.
3. Wrap the data-encryption key in the browser.
4. Encrypt entry title, body, and optional tags in the browser with an
   authenticated encryption mode and unique nonce per encrypted value/version.
5. Send only ciphertext, nonces, salts, wrapped-key material, algorithm/version
   identifiers, opaque entry IDs, and timestamps to the server.
6. Generate a one-time recovery key. The server may store only a recovery-wrapped
   data key, never the recovery secret itself.
7. Use a journal passphrase separate from the ARCWorks login password.

#### Blazor Server boundary

- The plaintext editor runs in an isolated JavaScript module.
- Plaintext must not be assigned to a Razor component field, sent through a
  Blazor circuit event, included in validation state, or logged.
- The JavaScript module encrypts first and sends ciphertext to dedicated,
  antiforgery-protected same-origin endpoints.
- Decrypted plaintext exists only in page memory while the unlocked editor is
  open and is cleared on lock, logout, inactivity, navigation, or page unload.

#### Database entities

- `JournalVault`: employee ID, KDF/encryption versions, salts, wrapped data key,
  recovery-wrapped data key, created/updated timestamps.
- `JournalEntry`: opaque ID, employee ID, encrypted title/body/tags, nonces,
  encryption version, created/updated timestamps, optional personal trash state.

#### Privacy rules

- Journal authorization is owner-only. Admin and Manager roles receive no read
  override.
- Audit logs contain metadata actions only (`CreateJournalEntry`,
  `UpdateJournalEntry`, `DeleteJournalEntry`) and never title/body/tags or
  ciphertext.
- Application logs, exceptions, request logs, reports, search, and telemetry
  must not include journal payloads.
- Database backups preserve ciphertext and key-wrapping metadata.
- Support can verify hashes and restoration integrity without decryption.
- Losing both journal passphrase and recovery key means plaintext cannot be
  recovered by ARCWorks.
- Account removal does not transfer journal readability to management.

#### Device-storage tests

After save, lock, logout, inactivity, browser close, and app-data reset, inspect:

- cookies;
- local/session storage;
- IndexedDB;
- Cache Storage and service workers;
- browser-accessible file downloads;
- request payloads and Blazor SignalR frames;
- application logs and MariaDB plaintext searches.

No journal plaintext may remain or traverse the server.

**Exit gate:** an independent privacy test demonstrates ciphertext-only server
storage and zero plaintext in browser persistence, network server payloads,
logs, audits, and database fields.

### Gate 8 — UI implementation last

Only after Gates 0–7 pass:

1. Build the restaurant-branded personal dashboard with no side panel.
2. Add profile menu at the upper-right on desktop and familiar compact placement
   on mobile.
3. Use one state-aware Clock In / Clock Out button beneath the live clock.
4. Present the portrait-only, user-controlled horizontal Today's Team carousel.
5. Render personal attendance and today's shift.
6. Render dismissible announcements.
7. Add Leave Request and My Journal as personal destinations.
8. Enable **Enter the Floor** only while clocked in and preserve server-side
   enforcement.
9. Give Tables & Orders an explicit return to My Dashboard.
10. Do not add Team, Reports, Reservations, or general messaging tabs to the
    Waiter shell.

Responsive acceptance viewports:

- 1920×1080 landscape desktop;
- 1366×768 laptop;
- 1080×2400 portrait reference;
- 390×844 phone CSS viewport;
- 844×390 phone landscape;
- Android emulator portrait and landscape.

Accessibility acceptance includes keyboard order, visible control focus,
reduced motion, text zoom, touch targets, contrast, and screen-reader landmarks.

**Exit gate:** visual acceptance plus waiter workflow acceptance; screenshots
and video are stored as a new formal checkpoint.

## 5. Migration and rollback rules

1. Never edit an already-applied migration.
2. Every schema change receives a new migration and reviewed SQL script.
3. Migrations must be additive through the preview and beta preparation gates.
4. Do not drop legacy columns in the same release that replaces them.
5. Before applying a migration to any non-disposable database:
   - take a logical MariaDB dump;
   - hash it;
   - validate that it contains table definitions and inserts;
   - record image and migration IDs.
6. Preview rollback is application-image rollback plus disposable database
   recreation.
7. Non-disposable rollback is application-image rollback plus the documented
   database restore procedure—never manual row deletion or migration-table edits.
8. Automatic attendance correction and journal records are never “cleaned up”
   with destructive ad hoc SQL.

## 6. Test and evidence matrix

Each gate updates automated tests at the narrowest useful layer:

- **Domain tests:** state transitions, validation, time/deadline calculations,
  encryption-envelope validation without plaintext fixtures in logs.
- **Integration tests:** real MariaDB persistence, authorization, concurrency,
  ownership isolation, automatic processors, and audit metadata.
- **Browser tests:** single-device login, real inactivity, logout/attendance
  independence, direct URL denial, carousel disclosure boundary, responsive UI.
- **Privacy tests:** storage inspection, request/frame inspection, database and
  log plaintext scans.
- **Recovery test:** encrypted journal rows, attendance review state,
  announcements, dismissals, leave requests, and media survive logical backup
  and disposable restore.

Evidence must distinguish:

- automated pass;
- disposable runtime acceptance;
- visual acceptance;
- live acceptance;
- deferred human/beta acceptance.

No gate may call itself live-accepted based only on InMemory tests or screenshots.

## 7. Documentation and GitHub process

For each completed gate:

1. Update the authoritative work log and root `PROJECT_TIMELINE.md`.
2. Add or update the relevant policy document.
3. Record migration name, commit, commands, test counts, preview URL, limitations,
   and rollback.
4. Store sanitized evidence; never commit credentials, session IDs, recovery
   keys, journal secrets, plaintext journals, or identifiable private notes.
5. Run secret scanning and `git diff --check` before commit.
6. Push only the feature branch and wait for green GitHub checks.
7. Require user acceptance before merging each visual milestone.

The older dirty visual workspace at `D:\ARCWorks_Restaurant_Suite` is reference
material only. Implementation must remain in the isolated Git worktree unless a
new clean worktree is deliberately created.

## 8. Final pre-implementation review

### Feasibility

The plan is technically achievable with the existing .NET, Blazor Server,
Identity, EF Core, and MariaDB architecture. No new desktop program or external
paid service is required.

### Highest-risk areas

1. Journal plaintext accidentally crossing the Blazor Server circuit.
2. Incorrect session renewal reintroducing active-user logout or weakening the
   one-device rule.
3. Automatic attendance closure being applied twice or treated as approved pay.
4. Today's Team payload revealing identity metadata despite a portrait-only UI.
5. Browser cleanup being described more strongly than ordinary browser APIs can
   guarantee.

The gates above directly isolate and test each risk.

### Recommended decisions retained for implementation

- Profile picture publication requires Admin approval.
- Today's Team is ordered by schedule start time and then a stable internal key.
- Clock-in without a schedule is allowed but flagged for Manager review.
- Journal is implemented as a separate privacy gate, not mixed into the initial
  dashboard Razor component.
- A later dedicated WebView client may provide stronger whole-profile wipe
  guarantees. The browser version will guarantee no ARCWorks-designed persistent
  local data and will clear every application-controlled store on logout/reset,
  while not making false claims about browser-history or operating-system caches.

### Readiness verdict

**READY FOR IMPLEMENTATION AFTER USER APPROVAL OF THIS PLAN.**

The first implementation action must be Gate 0 evidence capture, followed by
Gate 1 session repair. UI redesign does not begin until the backend gates pass.
