# ARCWorks Restaurant Suite — Chef Doy's Landing Page V2 Handoff

**Prepared:** 2026-08-12  
**Repository:** `ARCWorksPH/ARCWorks-Restaurant-Suite`  
**Handoff branch:** `ui/landing-page-design-2-handoff-v2`  
**Required submission branch:** create a new branch from this handoff branch  
**Required PR base:** `ui/landing-page-design-2-handoff-v2` — **never `main`**

## Objective

Produce a second, substantially more faithful implementation of the approved
Design 2 landing page for **Chef Doy's Gourmet Restaurant**, ARCWorks' first
potential demo and private-beta restaurant.

This is an implementation task, not another concept-only mockup. The output
must be real responsive HTML/CSS/assets connected to the existing login page.

Approved visual reference:

- `docs/UI/reference/landing-page-design-2.jpg`

Approved source assets:

- `docs/UI/assets/chef-doys-original-source.png`
- `docs/UI/assets/arcworks-transparent-source.png`

The Chef Doy's identity is not placeholder content. Do not replace it with
`Your Restaurant`, a generic SVG, invented lettering, or a different business.

## Why a second pass is required

The first implementation reproduced the general arrangement, but its layers
remain too flat compared with the approved reference:

| Layer | Current implementation | Required V2 direction |
|---|---|---|
| Background | Simple CSS gradients plus a flat SVG | Composed desktop and mobile atmosphere with gold and cyan light, depth, texture, and controlled contrast |
| Restaurant identity | Cropped flat source image | A clean, dominant Chef Doy's identity treatment derived from the supplied source without changing the name |
| Glass card | `backdrop-filter` plus translucent fill | Layered smoked glass with edge lighting, reflection, depth, and readable real controls |
| ARCWorks mark | Small CSS badge | Clear secondary product endorsement using the supplied ARCWorks source |
| Mobile | Responsive but visually generic | Purpose-composed portrait experience, centered and immediately readable on a phone |

The Blazor/C# stack is not the limitation. The missing fidelity comes from the
asset composition and visual layers. Use image/SVG assets where appropriate and
CSS for layout, interaction, responsiveness, and control states.

## Layered implementation requirement

Implement the page as independent layers so each can be reviewed or replaced:

1. **Background art layer**
   - Produce separate optimized desktop and mobile assets when one composition
     cannot crop cleanly for both orientations.
   - Preferred runtime formats: WebP, optimized PNG, or SVG as appropriate.
   - Add a dark readability wash as a separate CSS layer.
   - The reference screenshot itself must not be shipped as the page background.

2. **Atmosphere layer**
   - Gold overhead glow and restrained particles on the left/top.
   - Cyan/blue glow on the right/lower area.
   - Optional subtle grain/noise and reflection overlays.
   - Decorative layers must use `pointer-events: none`.

3. **Restaurant identity layer**
   - Chef Doy's must be the dominant brand.
   - Derive a clean wordmark treatment from the supplied source if practical.
   - Preserve the original aspect ratio and exact spelling.
   - Do not substitute generated restaurant branding.
   - If an irreversible asset cleanup is required, add a new derivative asset;
     preserve the original source file unchanged.

4. **ARCWorks endorsement layer**
   - Use the supplied transparent ARCWorks source or a clean derivative.
   - Keep it visually secondary to Chef Doy's but clearly readable.

5. **Glass login layer**
   - Real HTML inputs and button must remain interactive.
   - Add separate glass body, inner highlight, edge reflection, ambient shadow,
     and restrained cyan accent layers rather than relying on blur alone.
   - Preserve contrast and legibility when `backdrop-filter` is unsupported.

6. **Responsive composition layer**
   - Desktop and portrait mobile are both first-class approved compositions.
   - Mobile is not merely a scaled desktop canvas.
   - Avoid horizontal scrolling, clipped form controls, off-center cards, and
     fixed pixel positioning that fails on different phone heights.

## Protected behavior — do not change

The following already works and must remain intact:

- ASP.NET Identity username/password authentication.
- Lockout behavior and existing validation/error messages.
- Single-active-session guard.
- Fifteen-minute inactivity logout.
- Return URL handling.
- The dedicated public `LandingLayout` with no staff sidebar or operational
  navigation.
- Authorization on `/`; anonymous requests must continue to reach the login.
- Password visibility control and browser autofill support.

Do not edit authentication services, database entities, migrations, session
logic, role policies, Docker configuration, inventory, orders, kitchen flow,
manager flow, or admin behavior.

## Permitted implementation surface

Prefer limiting the submission to:

- `src/Roms.Web/Components/Account/Pages/Login.razor`
- `src/Roms.Web/wwwroot/landing-design2.css`
- New files under `src/Roms.Web/wwwroot/images/branding/`
- Supporting documentation/evidence under `docs/UI/`

`src/Roms.Web/Components/App.razor`, `Home.razor`, `LandingLayout.razor`,
`appsettings.json`, and the current Chef Doy's asset are handoff infrastructure.
Do not rewrite them unless a demonstrated V2 requirement cannot be met within
the permitted surface. Explain every exception in the work log.

Do not add JavaScript frameworks, CSS frameworks, npm dependencies, remote font
calls, remote image hosts, analytics, trackers, or external runtime services.

## Mandatory responsive acceptance sizes

Provide evidence at minimum for:

| View | CSS viewport | Acceptance expectation |
|---|---:|---|
| Desktop | 1440 × 900 | Reference-level composition; no sidebar; form and brands balanced |
| Small laptop | 1280 × 720 | All controls usable without hidden submit button |
| Mobile portrait | 390 × 844 | Centered, no horizontal scroll, restaurant identity prominent |
| Narrow mobile | 360 × 800 | Controls fit and remain at least 44px high |
| Mobile landscape | 844 × 390 | Page scrolls safely; no clipped controls |

Also test 200% browser zoom and `prefers-reduced-motion: reduce`.

## Functional acceptance

The visual submission is not accepted unless all of these remain true:

- Anonymous `/` resolves to `/Account/Login`.
- No sidebar, operational header, connected badge, or staff identity is visible.
- Username and password can be entered with keyboard and touch.
- Password show/hide remains accessible.
- Autofill does not turn fields into unreadable white rectangles.
- Invalid credentials display the existing error behavior.
- A valid login continues to follow the existing role/session flow.
- All page assets are local and load without console errors.
- No credential, token, connection string, or staff data is added to Git.

## Required work log and evidence

Add:

- `docs/UI/GROK_LANDING_V2_WORK_LOG.md`
- Desktop and mobile screenshots under `docs/UI/evidence/grok-landing-v2/`
- A short asset manifest in the work log containing file names, purpose,
  original/derivative status, dimensions, format, and SHA-256.

The work log must record:

1. Files inspected before editing.
2. Files changed and why.
3. Any derivative assets created from the supplied sources.
4. Commands used for build/test.
5. Screenshot viewport dimensions.
6. Known visual differences from the approved reference.
7. Rollback instructions.

Do not claim pixel parity unless the screenshots objectively support it.

## Verification commands

At minimum, run from the repository root:

```powershell
dotnet restore Roms.slnx
dotnet build Roms.slnx --no-restore
dotnet test Roms.slnx --no-build
```

If Docker is available, build the app service in the submission checkout without
changing or replacing the live production-like container.

## GitHub submission process

1. Fetch the repository.
2. Check out `ui/landing-page-design-2-handoff-v2`.
3. Create a new branch, suggested name:
   `ui/landing-page-design-2-grok-v2`.
4. Implement and verify the work.
5. Commit only the scoped implementation, assets, work log, and evidence.
6. Push the new branch.
7. Open a pull request with base branch
   `ui/landing-page-design-2-handoff-v2`.
8. Mark the PR as requiring visual review. Do not merge it.

The repository's protected `main` branch is not the target of this design round.
Codex will independently review source, assets, responsive evidence, and runtime
behavior before any integration decision.

## Stop conditions

Stop and report instead of guessing if:

- the supplied Chef Doy's source cannot produce a readable identity treatment;
- the ARCWorks source cannot be used without distortion;
- a requested visual effect requires changing authentication behavior;
- build/test failures appear outside the permitted implementation surface;
- the PR base is `main` or any branch other than the named handoff branch.

