# Gate 0 checkpoint - Waiter before personal dashboard

Date: 2026-08-12 (Asia/Shanghai)

Status: **captured; awaiting project-owner inspection**

## Purpose

This checkpoint freezes the accepted landing page and the current Waiter
dashboard before the proposed personal-dashboard backend and UI work begins.
It is the rollback and visual-comparison boundary for the next Waiter phase.

Gate 0 made no feature-code, migration, live-database, public-tunnel, or
production-container changes.

## Source and runtime boundary

| Field | Recorded value |
| --- | --- |
| Source worktree | `D:\ARCWorks_Restaurant_Suite_Codex_Waiter_Shell` |
| Branch | `ui/waiter-shell-account-security` |
| Source commit | `a259dbe32b0d10247c66eba3417b5309faa1c8ea` (`Populate waiter preview catalog`) |
| Accepted preview URL | `http://127.0.0.1:7171/` |
| Preview app | `arcworks-landing-preview-app-1` |
| Preview app image | `roms:waiter-shell-preview` / `sha256:89687c42c7bb111bfe2f5ac03fb1db5f2fd118e7273959924a76f691c4b8693e` |
| Preview database | `arcworks-landing-preview-db-1`, MariaDB 11.4 |
| Captured role | Waiter |
| AI state | Held / disabled by policy |

The landing images were captured directly from the isolated preview. The
Waiter browser had no reusable authenticated session after the PC restart, so
the Waiter images were captured from a disposable local clone at
`http://127.0.0.1:7272/`. The clone was built from a single-transaction logical
dump of the isolated preview and used a disposable Waiter identity. It did not
modify the accepted preview database or the live instance, and it was removed
after capture.

## Recorded data state

The isolated preview contained:

- 12 restaurant tables;
- 4 menu categories;
- 12 menu items with realistic prices, preparation times, and contained menu
  photographs;
- 4 active preview staff identities before the disposable evidence clone was
  created.

The Waiter checkpoint account was intentionally not clocked in and had no
assigned schedule or attendance record. That state exposes the current empty
dashboard behavior without inventing operational records.

## Screenshot inventory

| File | Requested viewport | Captured raster | What it preserves |
| --- | ---: | ---: | --- |
| `landing-desktop-1920x1080.png` | 1920 x 1080 | 1920 x 1080 | Accepted desktop landing page |
| `landing-mobile-390x844.png` | 390 x 844 | 390 x 844 | Accepted portrait/mobile landing page |
| `waiter-dashboard-desktop-1920x1080.png` | 1920 x 1080 | 1905 x 1072 | Current desktop Waiter dashboard |
| `waiter-dashboard-landscape-1366x768.png` | 1366 x 768 | 1351 x 760 | Current laptop/landscape Waiter dashboard |
| `waiter-dashboard-mobile-portrait-390x844.png` | 390 x 844 | 375 x 812 | Current portrait Waiter dashboard |
| `waiter-dashboard-mobile-landscape-844x390.png` | 844 x 390 | 829 x 383 | Current phone-landscape Waiter dashboard |

The small raster difference on authenticated Waiter captures is the browser's
visible scrollbar/chrome allocation. No image was resized, recompressed, or
cropped after capture.

## Baseline limitations intentionally preserved

The screenshots are evidence of the current state, not a claim that the
current Waiter dashboard is complete. They intentionally preserve these
known limitations for before/after comparison:

- office/back-office presentation rather than a personal pre-shift space;
- large unused desktop areas and vertically stacked generic panels;
- redundant `Tables & Orders` dashboard card;
- no restaurant-first Waiter identity or profile area;
- no `Today's Team`, shift summary, announcement, leave-request, or private
  notebook experience;
- mobile header crowding and horizontal overflow;
- the side navigation/header remain visually dominant on a phone;
- the current page is not the approved no-sidebar personal-dashboard concept;
- known fixed-cookie/session-expiry behavior remains a separate backend item
  and was not changed during Gate 0.

## Security and privacy notes

- No credential, token, password hash, database dump, cookie, or Data
  Protection key is stored in this checkpoint.
- The disposable evidence credentials were generated locally, never printed,
  never committed, and deleted with the disposable environment.
- No public URL, Cloudflare route, live container, or live database was
  changed.

## Acceptance gate

Gate 0 is complete when the project owner confirms that these images are an
accurate and useful `before` checkpoint. Backend implementation must not begin
until that review is complete.

The screenshot hashes are recorded in `SHA256SUMS.txt`.
