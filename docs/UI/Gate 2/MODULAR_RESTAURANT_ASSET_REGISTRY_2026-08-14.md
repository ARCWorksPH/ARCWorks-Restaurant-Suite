# Modular restaurant asset and presentation registry

This registry defines every restaurant-replaceable presentation element known for the current application. Dimensions are source recommendations, not fixed CSS sizes. Preserve aspect ratio; never stretch. Raster menu imagery uses `object-fit: contain` so different source sizes are not distorted or cropped.

## Shared configuration keys

| Key | Chef Doy value | Validation / fallback |
| --- | --- | --- |
| `Restaurant.DisplayName` | `Chef Doy's Gourmet Restaurant` | 2–100 Unicode characters; fallback `Restaurant` |
| `Restaurant.ShortName` | `Chef Doy's` | 2–40 characters; fallback display name |
| `Restaurant.Descriptor` | `Gourmet Restaurant` | 0–80 characters |
| `Restaurant.TimeZone` | `Asia/Manila` | server allow-list; fixed for this deployment |
| `Restaurant.Locale` | `en-PH` | allow-list; fallback `en-PH` |
| `Restaurant.Currency` | `PHP` | ISO 4217; fallback `PHP` |
| `Restaurant.PrimaryLogo` | local transparent asset | SVG preferred, or transparent PNG ≥ 1600 px wide |
| `Restaurant.CompactLogo` | local transparent asset | square SVG preferred, or PNG ≥ 512 × 512 |
| `Restaurant.Favicon` | local asset | SVG plus 32/192/512 px PNG fallbacks |
| `Restaurant.AccentGold` | approved Chef Doy gold | CSS color token, contrast-tested |
| `Restaurant.AccentCool` | approved cyan/blue | CSS color token, contrast-tested |

All paths resolve inside the local static-asset root. No remote URL, inline script, or restaurant-supplied executable content is accepted.

## Gate 2D staff portrait rules

The neutral fallback is `/images/staff/neutral-avatar.svg`. Development fixtures
use ten synthetic, local SVG portraits under `/images/staff/demo/`; they do not
represent real people and are not sign-in accounts. A portrait is eligible for
the Waiter Dashboard's **Today's Team** read model only when the staff account
is active, the profile lifecycle is `Approved`, the asset is a local
`/images/staff/*.svg` path, and that employee has a schedule overlapping the
current Asia/Manila calendar date. Invalid, remote, or missing paths resolve
to the neutral fallback.

For a real restaurant, replace the synthetic files through an Admin-authorized
profile-management workflow to be delivered with the later editable-profile
surface. That workflow must audit the old/new local asset reference, actor,
and reason. Until then, there is deliberately no browser/API endpoint that can
alter profile portraits or lifecycle state.

## Page/tab replacement inventory

| Surface | Replaceable assets/data | Recommended source | Display behavior |
| --- | --- | --- | --- |
| Landing / Login | primary logo, compact product endorsement, desktop/portrait backgrounds, name, descriptor, support text | logo SVG/PNG ≥1600 px; desktop WebP 3840×2160; portrait WebP 1440×2560 | art-directed sources; logo `contain`; safe-center focal point |
| Waiter Dashboard | compact restaurant logo, greeting name, staff portraits, announcement accents | compact logo SVG; portraits 1024×1024 WebP/AVIF, or 160×160 SVG placeholder | portraits centered `cover`; original retained server-side; carousel returns no names or roles |
| Tables Overview | compact logo, table/section labels, status colors | SVG; labels from database | cards use theme tokens |
| Order Editor | logo, category icons, menu photos, currency/locale | SVG icons; images ≥1600×1200, 4:3 preferred | images `contain`; neutral letterbox allowed; never crop/stretch |
| Kitchen Display | horizontal logo, station label, status colors, alert tones | horizontal SVG ≥1200 px; tokens | landscape-only; no decorative photo behind tickets |
| Manager Dashboard | logo, restaurant name, metric/status palette | SVG + tokens | no private-journal access |
| Inventory | logo, category/unit labels, low-stock palette | SVG + database labels | unit vocabulary is config/data, not artwork |
| Staff Schedule / Attendance | logo, staff portraits, section names, note labels | portraits 1024×1024; database labels | original portrait retained; thumbnails are derivatives |
| Pending Payments | logo, currency/locale, receipt identity | SVG; print mark ≥1200 px | Manager and Admin only |
| Admin — Users | logo, default portrait silhouette | SVG silhouette | neutral local fallback |
| Admin — Menu & Tables | logo, category icons, menu images, table terminology | SVG icons; images ≥1600×1200 | same media pipeline as Order Editor |
| Reports / Audit | report logo, legal/display name, currency/locale | print SVG or 2400 px PNG | audit data is never theme-controlled |
| Leave Requests | logo, leave labels, calendar accents | SVG + server data | branding cannot change authorization |
| Personal Journal | logo, optional neutral cover texture | local SVG/WebP | no Manager/Admin reading surface |

## Swap package manifest

Every restaurant package contains a versioned `restaurant-branding.json`, desktop and portrait backgrounds, primary/compact/favicon/print logos, default portrait, optional category icons, menu mapping by immutable item ID, SHA-256 manifest, asset license declaration, and accepted landscape/portrait screenshots. Activation is versioned and retains the prior package for rollback. It never changes database identity, permissions, timers, or history.
