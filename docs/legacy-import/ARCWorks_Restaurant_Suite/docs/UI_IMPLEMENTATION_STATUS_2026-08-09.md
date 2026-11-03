# ARCWorks Restaurant Suite — UI implementation status (2026-08-09)

## Scope

This pass completes the visual implementation directly in the canonical project
folder. It does not change authentication, authorization, order state
transitions, timers, inventory rules, database schema, or the AI hold.

The approved visual direction remains:

- Neo-Glass accents and status glow for the operational shell.
- Soft Neo-Bento grouping for tables, order summary, manager, and inventory.
- Tactile Sci-Fi KDS treatment with a landscape-first wide-screen layout.
- Side navigation on desktop; responsive portrait/landscape behavior on phones.

## Implemented

- Expanded the content canvas so desktop layouts are not trapped in a narrow
  centered column beside the side rail.
- Reworked table cards with larger touch targets, stronger status-coded
  surfaces, status pills, waiter/total metrics, and clearer actions.
- Re-enabled the intended three-pane order-editor geometry (category rail,
  menu catalog, live summary). Removed a conflicting Bootstrap flex utility
  that was preventing the CSS grid from taking effect.
- Increased menu-card visual hierarchy with image/visual placeholder treatment,
  readable pricing, and hover/focus feedback without introducing external
  assets or changing menu data. The supplied 12 menu images are now served from
  `src/Roms.Web/wwwroot/images/menu/` using database-name slugs.
- Menu images use a fixed visual frame with `object-fit: contain` and centered
  positioning, so differing source aspect ratios remain fully visible without
  distortion or cropping.
- Refined Tables to three large status cards per desktop row, with responsive
  two-column and one-column fallbacks.
- Refined KDS to remove the duplicated sidebar brand on desktop, improve muted
  text contrast, and preserve the manual sidebar-minimize control.
- Restyled KDS tickets for wide-screen use: three columns on large landscape
  displays, two columns on medium screens, one column on small screens, larger
  timers, stronger ticket status contrast, and a visually separated 86 panel.
- Restyled inventory as independent-item operational bento panels. Recipe
  configuration was not reintroduced.
- Preserved existing mobile breakpoints and touch-target sizing.

## Verification

- `dotnet build Roms.slnx --no-restore`: **passed**, 0 warnings, 0 errors.
- `git diff --check`: **passed**.
- Focused unit suites passed: Domain **16/16** and Command Gateway **11/11**.
- The app image was rebuilt and only the app container was recreated; local
  `/health` returned **HTTP 200**.
- Existing Docker services remained running; no database or tunnel changes were
  made during this pass.
- `dotnet test Roms.slnx --no-build --no-restore`: did not complete within the
  two-minute verification window. This is not being represented as a passing
  test run; focused/runtime acceptance remains pending.

## Acceptance still required

1. Rebuild/recreate only the app container from this working tree.
2. Check the local instance at `http://127.0.0.1:7070` in desktop landscape,
   phone portrait, and phone landscape.
3. Inspect `/tables`, a real `/orders/{id}` editor, `/kitchen`, and `/inventory`
   with populated data.
4. Confirm workflow actions and role restrictions remain unchanged.
5. Capture screenshots and decide whether to merge/push this UI pass.

The public tunnel is intentionally not treated as visual acceptance until the
local instance passes those checks.
