# ROMS UI Change Log

This is the canonical source-controlled UI change log. Agent-specific chat logs and large reference assets are supporting material, not proof of implementation.

## 2026-07-30 — Final runtime acceptance remediation

### Implemented

- The connection indicator now updates locally in JavaScript when the browser goes offline, even when the disconnected Blazor circuit cannot receive a callback.
- The mobile navigation button has explicit `aria-controls` and truthful string-valued `aria-expanded` state.
- The connection indicator is a polite live status region.
- Kitchen Display mode uses a 72 px desktop icon rail. Link text remains available to assistive technology and each icon has a title.
- Playwright regression coverage now exercises mobile navigation, offline/recovery state, Kitchen Display mode, compact rail width, and hidden visual labels.

### Verified

- Security seed-password guard: passed.
- Release build: 0 warnings, 0 errors.
- Automated test suite: 36/36 passed.
- Isolated Docker/MariaDB health check: passed.
- Running-browser acceptance across desktop, tablet, and mobile: passed.
- Browser console errors, uncaught page errors, and unexpected request failures: 0.
- Linux-container Kitchen Display clock format and advancement: passed.
- Real MariaDB inventory item creation and stock adjustment: passed.

### Evidence policy

Earlier concept-mockup copies are not accepted as runtime evidence. Final acceptance was performed against the built container on an isolated loopback port. Detailed commands, scope, and results are recorded in `docs/WORK_LOG.md`.

## 2026-07-30 — Claude Opus independent source review

- Removed trailing whitespace from `roms-app.js`.
- Prevented false `Saved.` messages when inventory adjustment or recipe guards reject an operation.
- Removed two tautological LINQ-only tests.
- Corrected the meaningful automated test baseline to 36 tests.
- Marked runtime/browser acceptance as pending until the Codex acceptance recorded above completed it.
