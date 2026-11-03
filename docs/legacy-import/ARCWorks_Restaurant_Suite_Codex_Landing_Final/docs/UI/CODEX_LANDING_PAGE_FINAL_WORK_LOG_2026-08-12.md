# Codex landing-page finalization work log — 2026-08-12

## Boundary

- Worktree: `D:\ARCWorks_Restaurant_Suite_Codex_Landing_Final`
- Branch: `ui/landing-page-design-2-codex-final`
- Starting point: Grok V2 branch `origin/ui/landing-page-design-2-grok-v2`
- Live instance, live database, tunnel, and `main`: untouched.
- Disposable preview only: project `arcworks-landing-preview`, loopback port
  `7171`, isolated database and Data Protection volumes.

## Implemented

1. Replaced missing referenced artwork with local project assets.
2. Removed the baked checkerboard from the supplied Chef Doy's wordmark and
   verified the transparent result over the real desktop background.
3. Added a purpose-composed 9:16 mobile background and WebP alternatives.
4. Kept the username/password form as real Blazor Identity controls and kept
   the existing single-session and 15-minute inactivity rules unchanged.
5. Removed the duplicate page-level stylesheet link.
6. Made restaurant identity, product mark, desktop art, and mobile art paths
   startup-validated configuration values.
7. Added desktop, phone portrait, and short-landscape compositions.
8. Removed the visible global focus rectangle from the programmatically focused
   page heading while retaining its semantic navigation focus.
9. Added landing-layout error UI styling so its hidden inactive state does not
   create false page overflow.
10. Changed the password-toggle accessible name so the `Password` form label
    resolves to one field in browser automation.
11. Replaced the non-functional server-side password visibility click handler
    with a small local script suitable for the statically rendered login page.

## Evidence captured

- `dotnet build Roms.slnx --no-restore`: passed, 0 warnings, 0 errors.
- `Roms.Domain.Tests`: 16/16 passed.
- `Roms.CommandGateway.Tests`: 11/11 passed.
- Isolated Docker image `roms:landing-preview`: built successfully.
- Preview application and MariaDB: healthy.
- Browser render checks completed at 1440 x 900, 390 x 844, and 844 x 390.
- DOM checks confirmed one labeled Username field, one labeled Password field,
  one password visibility control, and one Log in button.
- Final 390 x 844 and 844 x 390 checks reported zero horizontal or vertical
  overflow, hidden inactive error UI, and no heading focus outline.
- Password visibility changed `password -> text -> password` in the static
  login page; browser console errors: 0.
- Disposable admin login navigated successfully from `/Account/Login` to
  `/attendance`; browser console errors remained 0.

## Known test-runner note

An all-solution test invocation was stopped by the outer command timeout while
real-MariaDB/E2E child processes remained active. Only processes and disposable
containers belonging to this isolated worktree were terminated. This was a
test-orchestration timeout, not a failed assertion. The dedicated build and
unit suites above passed; browser acceptance used the isolated healthy Docker
preview.

## Rollback

No rollback of the live application is necessary because nothing was deployed
or merged. Remove the isolated worktree/branch and the
`arcworks-landing-preview` Compose project to discard the work completely.
