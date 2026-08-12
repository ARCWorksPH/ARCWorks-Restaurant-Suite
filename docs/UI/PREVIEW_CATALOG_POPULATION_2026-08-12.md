# Isolated Preview Catalog Population — 2026-08-12

## Scope

This is test data for the isolated waiter-shell preview only. It is not
restaurant-approved production data, and no live ROMS database was changed.

## Applied preview data

- 12 active restaurant tables (`1` through `12`)
- 4 active menu categories: Mains, Drinks, Sides, Desserts
- 12 available menu items
- varied PHP prices and per-item preparation times
- matching menu photographs for all 12 items

The photographs use `object-fit: contain` so mixed source aspect ratios remain
undistorted and uncropped.

## Reproducible command

From the project root:

```powershell
pwsh -NoLogo -NoProfile -File .\scripts\Seed-PreviewCatalog.ps1
```

The script is idempotent and refuses to run unless the target database
container belongs to the `arcworks-landing-preview` Compose project. It does
not contain or print a database password.

## Focus-ring correction

Blazor's `FocusOnNavigate` still moves screen-reader focus to the page heading,
but the programmatically focused `h1[tabindex="-1"]` no longer receives the
blue interactive-control outline. Buttons, links, and form controls retain the
global visible keyboard focus ring.

## Acceptance boundary

- Source build: passed with 0 warnings and 0 errors.
- Domain workflow tests: 16/16 passed.
- Preview containers: application and MariaDB healthy.
- Database verification: 12 tables, 4 categories, 12 available menu items.
- Automated full integration runs were not used as a completion claim because
  the existing integration runner stalled in this workstation environment.
- Final authenticated Tables & Orders visual acceptance remains with the user.
