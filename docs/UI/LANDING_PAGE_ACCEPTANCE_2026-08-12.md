# Landing Page Visual Acceptance — 2026-08-12

## Decision

The project owner formally accepted the Chef Doy's Gourmet Restaurant landing
page after visual inspection in desktop, landscape, portrait/mobile, and
zoomed views. This is the approved visual baseline for the public staff-login
surface and the rollback anchor before work begins on the Waiter interface.

## Accepted result

- Restaurant identity remains the dominant element on first view.
- The final wordmark preserves the supplied Chef Doy's lettering while using
  the richer, natural gold treatment preferred over the paler mockup color.
- The star and gold/cyan atmospheric background remain visible without the
  procedural grain that previously appeared pixelated.
- The glass login card, fields, and controls retain clear contrast and usable
  touch targets.
- The composition reflows intentionally at desktop, landscape, portrait, and
  zoomed views instead of merely scaling the desktop arrangement.
- The ARCWorks endorsement remains visually secondary to the restaurant.

## Final production logo asset

`src/Roms.Web/wwwroot/images/branding/chef-doys-wordmark-production-v2-crisp.png`

- Dimensions: 3200 x 1406 pixels
- SHA-256: `18BE00E4721B405C2D241D4FBCB3AB8C294AF2AC31D0FAB81E912E15914C439E`
- Source method: deterministic extraction of the 8192 x 5492 embedded PNG
  from the supplied SVG wrapper, alpha-bound crop with a glow safety margin,
  Lanczos resize, and mild edge sharpening.
- No generative redraw was used, so the approved spelling, ornament, and
  composition were not reinterpreted.

The editable clean and sharpened masters are retained under
`RESTO_APP_UI_CONCEPT/Assets/Processed/` outside the public web asset set.

## Verification evidence

- Docker Release publish completed successfully.
- Isolated preview container `arcworks-landing-preview-app-1` became healthy.
- Desktop browser rendering passed visual inspection.
- Mobile portrait rendering at 393 x 852 passed visual inspection.
- The final browser inspection reported no console warnings or errors.
- `git diff --check` passed.
- The complete .NET test command exceeded the local verification window and is
  recorded as pending rather than reported as passed.

## Live promotion evidence

The accepted page was promoted to the production-facing ROMS app on
2026-08-12 after pull request #11 passed both CI verification runs and the
GitGuardian security check. The protected `main` merge commit is `daedd2c`.

- Versioned runtime image: `roms:landing-accepted-daedd2c`
- Running app image ID:
  `sha256:8771075857e870bc8e5606b5c3da3f3c1219d388045ba4224884fb4435c691cd`
- Local endpoint `http://127.0.0.1:7070/Account/Login`: HTTP 200 and references
  the accepted composition.
- Public endpoint `https://roms.arkworksph.online/Account/Login`: HTTP 200 and
  references the accepted composition.
- The public Chef Doy's wordmark, desktop background, and mobile background
  matched the accepted assets byte-for-byte by SHA-256.
- The app health check passed after an app-only recreation.
- MariaDB and Cloudflare tunnel container identities were unchanged.
- No application errors were found in the post-promotion log window.

Rollback was preserved as Docker image
`roms:rollback-pre-landing-20260812` and as a timestamped landing-file snapshot
outside the live repository. No database migration or data mutation was part
of the promotion.

## Acceptance boundary

This acceptance covers the landing-page presentation and its existing login
behavior only. It does not accept the Waiter, Kitchen, Manager, Admin, or
cross-PC recovery gates. Further screen work must preserve this page unless a
new, explicitly approved landing-page revision is requested.
