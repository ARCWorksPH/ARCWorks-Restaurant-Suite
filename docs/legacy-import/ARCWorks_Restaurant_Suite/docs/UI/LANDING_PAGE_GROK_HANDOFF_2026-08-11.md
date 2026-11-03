# Grok Handoff — Landing Page / Staff Login, Design 2

## Assignment

Implement only the public **landing page and staff login** using the separately
supplied Design 2 reference image as visual direction. It should have a compact
dark-glass card, warm-gold restaurant identity, a restrained cyan/violet
ARCWorks accent, and a premium but operationally readable layout.

Do not redesign any authenticated application page, workflow, role, data model,
or authorization behavior.

## Repository and branch rules

- Repository: `ARCWorksPH/ARCWorks-Restaurant-Suite`.
- Create `ui/landing-page-design-2` from the designated handoff branch; never
  push directly to `main`.
- Submit one pull request containing a concise summary, before/after desktop
  and phone screenshots, and verification results.
- Never commit passwords, tokens, tunnel details, database strings, user
  secrets, Data Protection keys, personal data, or production screenshots.
- Do not overwrite unrelated in-progress work. Keep the diff inside the
  approved scope.

## Approved file scope

Primary implementation files:

```text
src/Roms.Web/Components/Account/Pages/Login.razor
src/Roms.Web/wwwroot/roms.css
src/Roms.Web/wwwroot/roms-app.js                  # only if needed for password visibility
src/Roms.Web/wwwroot/images/branding/*            # reviewed local assets only
```

Only if required to prevent global Bootstrap/Identity defaults from interfering:

```text
src/Roms.Web/wwwroot/app.css
```

Do not modify application/domain/infrastructure projects, authenticated pages,
layout/navigation components, session/authentication services, `Program.cs`,
deployment files, backups, or Docker configuration for this assignment.

The following preparation is read-only unless the project owner explicitly
requests a change:

```text
src/Roms.Web/Configuration/LandingPageBrandingOptions.cs
src/Roms.Web/appsettings.json                      # LandingPageBranding section
```

## Functional invariants — must remain unchanged

The current `Login.razor` already implements protected Identity login. Preserve
model binding, antiforgery protection, validation, lockout behavior, normal
username/password login, two-factor redirect, recovery-code path, logger calls,
and return URL behavior.

The application has server-backed shared-device protection:

- one account may hold only one active session;
- a second device/browser is refused while the prior session remains active;
- explicit logout and clock-out/logout release the session claim;
- 15 minutes of inactivity automatically logs the account out;
- a database-backed session claim—not hidden UI—enforces this behavior.

Do not replace the form with JavaScript fetch/AJAX authentication, a mock form,
a client-only timer, or external identity. Do not alter the `EditForm` submit
handler, form name, `Input.Username`, `Input.Password`, or POST behavior.

## Branding configuration prepared for this page

Inject `IOptions<LandingPageBrandingOptions>` into `Login.razor`. The
`LandingPageBranding` configuration provides:

| Property | Use |
| --- | --- |
| `RestaurantName` | Centered restaurant name |
| `RestaurantDescriptor` | Small restaurant tagline/descriptor |
| `RestaurantLogoPath` | Approved restaurant logo from local `/images/` assets |
| `BackgroundImagePath` | Local decorative background only |
| `SupportMessage` | Account/access guidance below the form |

Use the existing placeholders when no approved restaurant logo or background is
available. Do not hard-code `Chef Doy's`, other sample names, or remote image
URLs. Future restaurant instances must change configuration, not Razor markup.

Suggested injection:

```razor
@using Microsoft.Extensions.Options
@using Roms.Web.Configuration
@inject IOptions<LandingPageBrandingOptions> LandingBranding
```

## Design requirements

1. Full-height dark background with subtle gold and cyan depth.
2. Restaurant logo/name centered in the upper content area.
3. Small ARCWorks Restaurant Suite identity in the top-right.
4. A centered smoked-glass login card, approximately 440–560px on desktop.
5. `Staff login` heading, short instruction, labelled username/password
   controls, and high-contrast Log in button.
6. Optional accessible password show/hide control; preserve password-manager
   and autocomplete behavior.
7. Real validation, inactive-session, clock-out, and second-device refusal
   messages must remain visible and readable in the card.
8. Show the configured support message below the form.
9. Keep the card readable; transparency and glow must not reduce contrast.

## Responsive and accessibility requirements

- No required scrolling at 1366×768 and 1920×1080 normal zoom.
- At 360px portrait, no horizontal scroll; controls remain at least 44px high.
- In landscape phone/tablet view, scale identity content before forcing the
  controls below the fold.
- Visible keyboard focus, sensible tab order, and Enter submits the form.
- Use real labels/validation; placeholders do not replace labels.
- Respect `prefers-reduced-motion`.

## Static asset rules

- Store assets only in `src/Roms.Web/wwwroot/images/branding/`.
- Use local files only: no external image URL, tracking pixel, CDN font,
  third-party script, or network-loaded animation.
- Prefer SVG/WEBP with known source/license. Include the asset source/license
  note in the pull request.
- Do not reuse a restaurant's supplied logo for another restaurant instance.

## Explicit exclusions

- No installation tab or post-login navigation changes.
- No registration, password reset, social login, or external-login buttons.
- No changes to roles, authorization, attendance, timeout values, workflow,
  tables, kitchen display, manager, inventory, or admin pages.

## Required verification

Run and report:

```powershell
dotnet build Roms.slnx --no-restore
git diff --check
```

Manually confirm without revealing credentials:

1. Invalid-login, inactive-session, clocked-out, and second-device refusal
   messages are readable inside the card.
2. Normal form submission still posts and redirects normally.
3. Desktop 1366×768 and phone 360px portrait have no clipping/horizontal scroll.
4. Keyboard focus appears on each interactive element.

Do not claim the pull request independently proves multi-device or 15-minute
idle-timeout behavior. Those remain supervised browser acceptance checks.

## Pull request checklist

- [ ] Only approved landing-page files changed.
- [ ] No secret, token, database string, or personal data added.
- [ ] No external assets, scripts, or fonts introduced.
- [ ] Server-side session/authentication code unchanged.
- [ ] Restaurant identity comes from `LandingPageBrandingOptions`.
- [ ] Build and whitespace checks pass.
- [ ] Desktop and mobile screenshots attached.
- [ ] Pull request remains unmerged until owner review.
