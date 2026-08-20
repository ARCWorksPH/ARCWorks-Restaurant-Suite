# Gate 2D — Profiles and Today's Team evidence

**Date:** 2026-08-20
**Branch:** `agent/gate2d-profiles-todays-team`
**Status:** implementation verified locally and by pull-request CI; merge is
the final repository closeout step.

## Scope completed

Gate 2D adds only the data and read-model foundation for the later Waiter
Dashboard carousel. It does not implement the final dashboard visual design,
profile-management UI, a browser upload endpoint, or any production employee
records.

### Database and lifecycle

- Migration `20260819010137_AddStaffProfilePortraits` adds the profile portrait
  path, lifecycle (`Draft`, `Approved`, `Archived`), update timestamp, and a
  development-fixture marker to `AspNetUsers`.
- Existing users receive the migration's `Approved` default, so the new
  lifecycle filter does not hide them solely because the schema was upgraded.
- Today's Team eligibility requires an active, `Approved` profile and a
  schedule overlapping the current server-authoritative Asia/Manila calendar
  date.
- The read model returns only `PortraitPath` and `UsesFallback`; it deliberately
  omits employee IDs, names, roles, schedule times, and contact information.
- Missing, remote, or unsupported portrait paths are replaced by the local
  `/images/staff/neutral-avatar.svg` asset.

### Development fixtures

In Development only, and only when `Seed:DemoData=true`, the application seeds
ten synthetic non-login employee profiles. They cover Waiter, Kitchen, and
Manager roles; each has a local SVG portrait, a seven-day date-relative
schedule window, and three completed attendance records. They are explicitly
marked `IsDemoProfile=true`, never receive a password, and are not restaurant
personnel records. Profile and schedule seed actions are written to the audit
table.

## Test coverage

Focused integration tests prove:

1. only the authenticated active Waiter can read the Waiter Dashboard data;
2. the server's Asia/Manila date is used, including rejection of tomorrow-only
   schedules;
3. inactive and `Draft` profiles are excluded;
4. the carousel output has no name field and only local safe portrait paths;
5. unsupported external portrait references receive the neutral fallback;
6. the demo fixture creates exactly ten non-sign-in profiles, 70 schedules,
   30 history records, ten role assignments, and 80 audit entries without
   duplication on a second run.

## Operational and replacement boundary

The current application intentionally exposes no browser/API endpoint for
editing profile portraits or lifecycle state. Consequently, there is no
unprotected edit route. The later Admin-only profile-management UI must:

- require Admin authorization;
- validate local asset references and allowed formats;
- update `ProfileUpdatedUtc` from the restaurant server clock; and
- audit actor, old/new values, and a reason.

Replace or remove the development fixtures before a restaurant beta by using a
fresh production configuration with `Seed:DemoData=false`. Never copy real
staff images or credentials into source control. The local source asset
contract is documented in `src/Roms.Web/wwwroot/images/staff/README.md` and
the overarching replacement inventory in
`docs/UI/Gate 2/MODULAR_RESTAURANT_ASSET_REGISTRY_2026-08-14.md`.

## Gate result

### Local verification on 2026-08-20

- `dotnet build Roms.slnx --nologo --no-restore` — **passed**, 0 errors.
- Gate 2D focused integration tests — **8 passed, 0 failed**.
- Domain tests — **16 passed, 0 failed**.
- Command Gateway tests — **11 passed, 0 failed**.
- `dotnet ef migrations script` — generated successfully for the new migration.

The full unfiltered Integration/E2E runner was attempted twice on this
workstation. Each attempt became non-responsive after the suite started its
long database/browser test process; the affected test-process chain was stopped
without touching the application, Docker, or data. This is an existing broad
test-runner/environment issue rather than a Gate 2D assertion failure.

### Independent pull-request CI

GitHub Actions run `32338610125` for pull request #21 passed on 2026-08-20. It
completed the committed-seed-password guard, Release restore/build, Playwright
Chromium installation, the full solution test suite, and the Docker image
build. GitGuardian and Snyk pull-request checks also passed. This independent
result closes the full-suite gap left by the workstation runner.

This gate is ready for repository review and CI. It is not a claim of
browser/mobile visual acceptance; that belongs to the later Waiter Dashboard UI
gate.
