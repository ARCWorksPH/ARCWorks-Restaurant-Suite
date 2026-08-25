# Gate 2H — Final Waiter UI evidence

**Date:** 2026-08-24

**Branch:** `agent/gate2h-final-waiter-ui`

**Status:** corrected final candidate is merged and deployed. The first preview
was rejected after owner inspection exposed runtime defects; the corrective
build, focused regressions, independent GitHub checks, and live health checks
now pass.

## Owner-review correction, 2026-08-25

The first Gate 2H preview was not accepted as final. Owner inspection found
four material issues and one presentation defect:

1. the Waiter dashboard failed while loading Staff Hub data because
   MySQL.EntityFrameworkCore could not type-map a captured `List<Guid>`;
2. a new encrypted journal received an empty successful response and the
   browser attempted to parse it as JSON;
3. the personal `/journal` route restored the legacy application sidebar and
   top chrome;
4. `/journal` was authenticated but not explicitly Waiter-role restricted;
5. the dashboard used an old anniversary graphic instead of the configured
   production restaurant wordmark.

The correction keeps the database predicate scalar and performs the bounded
receipt/version match in memory, emits literal JSON `null` for a missing key
envelope, tolerates empty successful bodies in the browser client, treats the
Waiter dashboard and journal as one chrome-free personal experience, restricts
the route to Waiters, and points the compact restaurant identity to the
approved production wordmark. No production database or live container was
changed during correction.

## Implemented surface

The authenticated root route now resolves to the personal Waiter dashboard
only for the Waiter role. Non-Waiter users retain the established operations
home. The Waiter dashboard removes the old office navigation and top chrome
without changing those shared components for Kitchen, Manager, or Admin.

The integrated surface contains:

- replaceable restaurant identity from the central restaurant profile;
- time-of-day greeting and authenticated employee display name;
- server-issued Asia/Manila date, time, attendance state, and one Clock In /
  Clock Out action;
- current-day shift and its existing schedule Manager note;
- Monday-based weekly hours, recent attendance, and a self-only full timesheet;
- current-restaurant-date Today's Team portraits with no employee names, roles,
  or identifiers;
- the authenticated waiter's approved local profile portrait in the dock and
  profile overlay, with the neutral local avatar as a safe fallback;
- a dominant Enter the Floor transition that remains unavailable until the
  server-authoritative attendance gate permits it;
- compact profile and Staff Hub launchers;
- Staff Hub destinations for announcements, Manager note, leave requests, and
  the separately encrypted My Journal route.

No attendance, authorization, announcement, leave, team-membership, journal,
or floor-entry decision was duplicated in CSS or JavaScript. The UI consumes
the existing Gate 2A-through-2G application contracts.

## Responsive and interaction contract

The component-scoped visual system uses reusable surface, border, status,
spacing, and action styles rather than page-specific inline copies. It retains
the accepted dark premium identity with restrained gold, cool, purple, and
teal accents while keeping text and status labels non-color-dependent.

- Landscape uses a readable 1500-pixel content maximum, balanced shift/hours
  cards, a full-width portrait wheel, and a compact bottom action dock.
- Portrait changes to a single-column flow; the floor action remains prominent
  and Staff Hub becomes a near-full-width sheet.
- Interactive controls maintain at least 44 CSS pixels where the compact
  responsive layouts apply.
- Staff Hub and profile are modal overlays. The dashboard becomes inert,
  focus stays inside the overlay, Escape and the visible close control dismiss
  it, and focus returns to its launcher.
- Reduced-motion preference disables component animation and transitions.
- Urgent unacknowledged announcements open the announcement destination and
  cannot be dismissed until the server records acknowledgment.

## Automated verification

The focused real-application Playwright scenario uses a disposable MariaDB
container and a synthetic Waiter account. It verifies:

1. Waiter-only dashboard resolution and role-specific removal of legacy chrome;
2. server-time presentation and clocked-out floor denial;
3. Staff Hub keyboard open/Escape-close behavior;
4. clock-in enabling the floor transition;
5. no document-level horizontal overflow at 412 x 915 Android portrait,
   915 x 412 Android landscape, and 1920 x 1080 desktop;
6. continued visibility of the shift and floor controls at every viewport.
7. Staff Hub loads without the former MariaDB type-mapping exception;
8. first-use journal setup renders the `Create private journal` action without
   the former JSON parse exception;
9. the journal retains the Waiter experience and does not expose legacy
   sidebar or top chrome.

Local verification completed:

- solution build after the correction — **passed, 0 warnings, 0 errors**;
- full suite before the test-tool package pin — **passed, 118/118**;
- focused real-MariaDB communication and journal regressions after the pin —
  **passed, 12/12**;
- corrected Gate 2H real-application Playwright scenario — **passed, 1/1**;
- focused Gate 2A-through-2G service regressions — **passed, 32/32**
  (attendance, dashboard read model, staff communications, leave requests,
  demo staff fixtures, and private journal);
- `git diff --check` — **passed**.

The test projects now pin SSH.NET 2026.0.0 over the transitive Testcontainers
dependency. `dotnet list package --vulnerable` reports no vulnerable packages,
and the post-pin solution build reports zero warnings.

## Isolated visual-review runtime

After the audit PR and automated checks were green, Gate 2H was built as
`roms:gate2h-preview` and deployed only to `127.0.0.1:7171` for owner visual
inspection. The preview's existing MariaDB and data-protection volumes were
preserved, so its established test accounts and protected sessions were not
reseeded or replaced.

- Preview health endpoint: **HTTP 200**.
- Preview login endpoint: **HTTP 200**.
- Preview app container: **healthy**.
- Preview database container: **healthy** and not recreated.
- Previous preview image retained as
  `roms:gate2h-preview-rollback-20260824`.
- Corrected preview image: `roms:gate2h-final-correction-20260825`.
- Rejected preview container retained for immediate rollback as
  `arcworks-landing-preview-app-rollback-20260825-2015`.
- Production loopback health endpoint: **HTTP 200** and unchanged.
- Public ROMS health endpoint: **HTTP 200** and unchanged.

The personal Gate 2H surface is intentionally Waiter-only. Admin, Manager, and
Kitchen accounts retain their established role surfaces; an Admin login is not
a valid visual test for the Waiter dashboard.

## Preserved preview-stage boundaries

- Only the isolated `127.0.0.1:7171` preview app container was recreated. Its
  retained database, keys, accounts, and credentials were not changed.
- During preview correction, the production app, public tunnel, Cloudflare
  settings, and retained production data were not changed.
- No restaurant/customer asset was copied from an external URL.
- No plaintext journal data is presented outside the journal's own browser
  memory boundary.
- This document does not claim owner visual acceptance. The audit and owner's
  inspection of the review branch are required before merge.
- Gate 2I subsequently performed the rollback-preserving production promotion
  and runtime checks recorded below.

## Gate 2I promotion closeout, 2026-08-25

- Pull request #25 merged to `main` as `db12ebc` after both CI verify runs,
  GitGuardian, and Snyk passed.
- The former production image was preserved as
  `roms:rollback-before-gate2h-final-20260825`.
- Only `arcworks-resto-app-1` was recreated from the corrected image. MariaDB,
  data-protection keys, Cloudflare Tunnel, and the other project containers
  were not recreated.
- Production app health: **healthy**.
- `http://127.0.0.1:7070/health`: **HTTP 200**.
- `http://127.0.0.1:7070/Account/Login`: **HTTP 200**.
- `https://roms.arkworksph.online/health`: **HTTP 200**.
- `https://roms.arkworksph.online/Account/Login`: **HTTP 200**.
- Public approved restaurant wordmark asset: **HTTP 200**, 1,794,181 bytes.
- Production error- and critical-level logs after replacement: **0**.

This is automated and operational acceptance of the corrected UI. Owner
inspection remains the final visual acceptance of the live rendered surface.
