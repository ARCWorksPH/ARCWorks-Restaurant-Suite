# Staff portrait assets

`neutral-avatar.svg` is the local fallback used whenever a scheduled,
approved staff profile has no valid local portrait.

`demo/` contains ten synthetic development-only placeholders. They are not
photographs, do not identify people, and are only seeded when both
`Seed:DemoData=true` and the application environment is `Development`.

## Replacement contract

- Keep staff portrait references under `/images/staff/`.
- SVG sources should use a square `viewBox` (the supplied assets use 160 × 160).
- Production portraits may be stored as a separately approved, local asset
  package; remote URLs are not accepted by the Waiter read model.
- Preserve aspect ratio. The dashboard can crop a supplied portrait to a
  circular frame, but must never stretch it.
- Do not put names, job titles, contact details, or encoded identity metadata
  into the carousel asset or its read model.

The future Admin profile-management surface—not a direct file copy—will be the
authorized and audited replacement path for real staff portraits.
