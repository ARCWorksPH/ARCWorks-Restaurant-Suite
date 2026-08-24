# Gate 2H — Final Waiter UI evidence

**Date:** 2026-08-24

**Branch:** `agent/gate2h-final-waiter-ui`

**Status:** implementation complete and audit-ready; isolated preview is live
for owner visual acceptance, while merge and production deployment remain
pending.

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

Local verification completed:

- solution build — **passed, 0 errors**;
- focused Gate 2H MariaDB + Playwright test — **passed, 1/1**;
- focused Gate 2A-through-2G service regressions — **passed, 32/32**
  (attendance, dashboard read model, staff communications, leave requests,
  demo staff fixtures, and private journal);
- `git diff --check` — **passed**.

The build continues to report the pre-existing NU1903 warning for SSH.NET
2025.1.0 through the test-container dependency path. Gate 2H does not introduce
or modify that package.

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
- Production loopback health endpoint: **HTTP 200** and unchanged.
- Public ROMS health endpoint: **HTTP 200** and unchanged.

The personal Gate 2H surface is intentionally Waiter-only. Admin, Manager, and
Kitchen accounts retain their established role surfaces; an Admin login is not
a valid visual test for the Waiter dashboard.

## Preserved boundaries

- Only the isolated `127.0.0.1:7171` preview app container was recreated. Its
  retained database, keys, accounts, and credentials were not changed.
- The production app, public tunnel, Cloudflare settings, and retained
  production data were not changed.
- No restaurant/customer asset was copied from an external URL.
- No plaintext journal data is presented outside the journal's own browser
  memory boundary.
- This document does not claim owner visual acceptance. The audit and owner's
  inspection of the review branch are required before merge.
- Gate 2I remains responsible for disposable restore, rollback execution,
  production deployment, monitoring, and final runtime evidence.
