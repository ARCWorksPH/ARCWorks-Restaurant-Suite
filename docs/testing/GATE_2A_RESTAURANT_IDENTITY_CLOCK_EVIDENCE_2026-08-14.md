# Gate 2A — Restaurant identity and authoritative clock evidence

Date: 2026-08-14

Branch: `agent/gate2a-restaurant-identity-clock`

Scope: configuration and server-time foundation only; no Waiter Dashboard UI,
schema migration, production deployment, or tenant/authentication change.

## Implemented boundary

- Replaced the landing-only branding options with one startup-validated
  `Restaurant` configuration section.
- Configured Chef Doy's Gourmet Restaurant display/short names, descriptor,
  locale (`en-PH`), currency (`PHP`), optional contact fields, local assets,
  and theme tokens.
- Restricted this deployment to server-supported `Asia/Manila`.
- Rejected external/traversal/query/fragment asset paths.
- Added an immutable restaurant presentation profile. A configured optional
  asset that is absent from the local web root resolves to a neutral local
  fallback; it never creates a remote request.
- Preserved the accepted landing-page composition by making it consume the new
  profile without changing its markup hierarchy or CSS.
- Added `IRestaurantClock` beside the existing UTC `IClock`. The restaurant
  clock converts server UTC instants for display/calendar use, converts
  restaurant-local input back to UTC, and defines Monday as week start.

## Security and data invariants

- Database instants remain UTC.
- Browser time is not accepted as attendance, schedule, timer, expiry, or
  audit authority.
- Restaurant presentation configuration cannot change user identity, roles,
  active-session leases, tenant routing, workflow timers, or history.
- Assets remain local and passive. No remote URL, inline script, or executable
  restaurant content is accepted.
- Gate 1 duplicate-session protection was not changed.

## Verification

| Check | Result |
| --- | --- |
| `dotnet build Roms.slnx --no-restore` | PASS — 0 errors; 2 existing `SSH.NET` NU1903 advisory warnings |
| Focused Gate 2A tests | PASS — 5/5 |
| Domain tests | PASS — 16/16 |
| Command Gateway tests | PASS — 11/11 |
| `git diff --check` | PASS |
| Aggregate `dotnet test Roms.slnx --no-restore` | INCONCLUSIVE — exceeded the 120-second local command window |
| Broad IntegrationTests selections | INCONCLUSIVE — existing long-running external/database paths exceeded the 180-second local command window |
| Existing application smoke selection | INCONCLUSIVE — existing browser/application harness exceeded the 180-second local command window |

Focused tests prove:

1. the approved Chef Doy profile validates;
2. a remote asset and incorrect timezone fail closed;
3. an existing replacement asset is selected while a missing optional asset
   receives a local fallback; and
4. hosts that omit an explicit web-root path safely resolve against their
   content root; and
5. UTC remains UTC while Manila local date/time and Monday week-start are
   calculated by the server boundary.

Timed-out aggregate runs were terminated after their command windows to avoid
leaving orphaned test hosts. They are not represented as passed or failed.
GitHub CI remains the authoritative complete-suite gate for the pull request.

## Rollback

Revert the Gate 2A commit or close its pull request. No database rollback is
required because Gate 2A adds no migration and changes no persisted data.

## Exit assessment

The Gate 2A implementation exit criteria are satisfied locally for the new
boundary, subject to clean GitHub CI. Gate 2B must consume these interfaces; it
must not introduce a second restaurant identity source or browser-time
authority.
