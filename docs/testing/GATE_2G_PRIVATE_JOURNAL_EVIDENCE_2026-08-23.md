# Gate 2G — Private ARCWorks Journal evidence

**Date:** 2026-08-23
**Branch:** `agent/gate2g-private-journal`
**Status:** implementation and local verification complete; PR #24 verification
is green and the merge is the remaining repository closeout step.

## Scope completed

Gate 2G implements the original ARCWorks encrypted Markdown journal contract.
It does not copy or package SimpleMDE, does not add a third-party editor, and
does not implement the final Staff Hub visual composition reserved for Gate 2H.

- Each active authenticated employee owns one independent journal vault.
- Employees can create, edit, search, soft-delete, restore, and permanently
  discard only their own entries.
- Title, Markdown body, and tags are serialized and encrypted in the browser.
  MariaDB receives only ciphertext, a 12-byte nonce, crypto version, ownership,
  lifecycle timestamps, and optimistic concurrency version.
- Search operates only over decrypted entries held in active browser memory.
- The restricted renderer supports headings, lists, quotations, horizontal
  rules, bold, italic, and word count. Raw HTML, images, links, event handlers,
  and unsafe URLs remain inert text.
- The browser module does not use cookies, `localStorage`, `sessionStorage`,
  IndexedDB, Cache Storage, files, analytics, telemetry, external fonts, or
  spell-check services for journal data.
- Soft-delete and restore preserve encrypted history. Permanent discard is
  limited to an already deleted entry and requires its current version.
- Audit records contain action, actor, entry identity, record version, and
  crypto version only. They contain neither plaintext nor ciphertext.

The direct `/journal` route is functional for contract testing. Its final Staff
Hub overlay, navigation, focus behavior, and accepted landscape/portrait visual
composition remain deliberately deferred to Gate 2H.

## Cryptographic and recovery design

The browser generates a random 256-bit AES-GCM data-encryption key. A separate
journal passphrase derives an AES-GCM wrapping key using PBKDF2-HMAC-SHA-256,
a random 16-byte salt, and 600,000 iterations. A separately generated random
256-bit recovery key wraps the same data-encryption key with a different random
nonce. The recovery key is displayed once and is never sent to or retained by
ARCWorks in plaintext.

The server accepts only crypto version 1, bounded PBKDF2 iterations, exact nonce
lengths, bounded wrapped-key sizes, and bounded ciphertext. Envelope rotation
and entry updates require the last observed version.

### Key-loss behavior

If the employee loses both the journal passphrase and the recovery key, the
journal plaintext is permanently unrecoverable. Manager, Administrator,
support, reports, database operators, backups, and ARCWorks itself have no
plaintext key and no administrative bypass. The encrypted rows may be retained
or discarded according to lifecycle policy, but they cannot be decrypted.

Recovery with the employee-held recovery key rotates both the passphrase wrap
and the recovery key. This prevents an old recovery key from remaining valid
after a successful recovery.

## Persistence and recovery

Migration `20260823023044_AddPrivateJournal` creates owner-bound
`JournalKeyEnvelopes` and `JournalEntries` tables, binary key-envelope and
ciphertext columns, lifecycle/concurrency metadata, ownership foreign keys,
and the active/deleted lookup index.

The generated idempotent migration script contains the expected tables,
`longblob` ciphertext column, ownership foreign keys, and lookup index. EF Core
reports no pending model changes. A logical backup/restore test copies the
opaque journal rows into an independent database and verifies the key wraps,
ciphertext, nonce, versions, and timestamps byte-for-byte. It intentionally
does not invent a server-side plaintext check.

## Verification

Focused local results:

- private-journal service and disposable-MariaDB tests — **6 passed, 0 failed**;
- browser Markdown/XSS test — **1 passed, 0 failed**;
- Gate 2D-through-2G focused integration regression — **31 passed, 0 failed**;
- domain tests observed during the aggregate run — **16 passed, 0 failed**;
- command-gateway tests observed during the aggregate run — **11 passed, 0 failed**;
- release build — **passed, 0 errors**;
- Gate 2G scoped formatter verification — **passed**;
- EF pending-model check — **passed**;
- idempotent migration generation and structural inspection — **passed**;
- PR #24 GitHub verification jobs — **2 passed**;
- PR #24 GitGuardian and Snyk checks — **passed**.

The focused tests prove encrypted lifecycle, ownership and no role bypass,
malformed/stale crypto rejection, metadata-only auditing, lack of durable
browser storage/telemetry APIs, inert hostile Markdown input, browser-memory
cleanup, byte-preserving restore, and real-MariaDB optimistic concurrency.

The repository continues to report the pre-existing NU1903 warning for
SSH.NET 2025.1.0 through the test-container dependency path. Gate 2G does not
introduce or modify that package.

An additional unfiltered local solution run completed the Domain and Command
Gateway projects but the aggregate Integration/E2E runner produced no terminal
result for several minutes and was stopped after its disposable containers
were cleaned up. No failure was reported, but this is not counted as a passed
full-solution run. Pull-request CI remains the authoritative unfiltered suite.
The repository-wide formatter also reports pre-existing whitespace findings in
older files outside Gate 2G; those unrelated files were deliberately left
unchanged, while every Gate 2G C# file passes scoped formatter verification.

## Preserved boundaries

- No live or preview container, retained database, tunnel, credential,
  restaurant record, staff account, or journal content was changed.
- No journal plaintext, passphrase, recovery key, or encrypted payload is
  written to audit records or server logs.
- No final dashboard/Staff Hub UI is claimed complete.
- No external editor dependency or SimpleMDE source was incorporated. The
  accepted plan retains its honest design-research acknowledgment.
- Gate 2H remains the final responsive UI implementation gate; Gate 2I remains
  recovery and deployment acceptance.
