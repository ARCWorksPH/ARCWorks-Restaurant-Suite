# External Audit #3 — Inventory Readiness

**Date:** 2026-08-02  
**Auditor:** Independent third-party review (Grok / xAI)  
**Branch:** `agent/inventory-readiness` (PR #2, draft)  
**HEAD reviewed:** `1a2f42ed95ed506869fdeca0f0bb1a4c7247bc9a` (docs commit may be newer)  
**Baseline comparison:** Audit #1 (~2026-07-26), Audit #2 (2026-07-30), plus `docs/EXTERNAL_AUDIT_HANDOFF_2026-07-30.md`

> **Numbering correction:** This is **Audit #3**, not Audit #2. Audits 1 and 2 were completed in prior review threads and are now recorded under `docs/audits/`.

## 1. Decision record

| Field | Value |
| --- | --- |
| Checkout SHA (code review baseline) | `1a2f42ed95ed506869fdeca0f0bb1a4c7247bc9a` |
| Environment | GitHub API + raw branch content; project work log and evidence docs; not a live production host login |
| Overall status | **Conditionally accepted for continued supervised development** |
| Supervised inventory pilot | **Not authorized yet** |
| Production `Features__Inventory__Enabled=true` | **Rejected until restaurant data + human gates complete** |
| AI UI / execution integration | **Rejected; lab may continue offline benchmarking only** |

### Mandatory remediation before supervised pilot

1. Restaurant-confirmed item names, units, minimum levels, recipes, and witnessed opening balances (provisional dataset remains sandbox-only).
2. Explicit written acceptance of cancellation/amendment disposition policy (return-to-stock vs waste/staff-meal) by the restaurant operator.
3. Supervised multi-device pilot plan with backup verification and documented rollback steps.
4. Keep active deployment on `Features__Inventory__Enabled=false` until the above are signed off.
5. Resolve or formally accept residual MariaDB deadlock/reload-and-retry behavior under expected concurrent device load.

### Advisory follow-up

- Expand Playwright coverage toward full multi-role lifecycle on disposable stacks.
- Add bounded application-level retries only if pure reload-and-retry UX proves insufficient in pilot.
- Consider count freshness window before activation.
- Payment gateway / BIR official-receipt extension points remain future work (Audits 1–2 Phase 3 / P3).
- Grow AI evaluation corpus before any ROMS function binding.

### AI-layer work

Laboratory work may continue without weakening the safety boundary. Connecting the gateway to ROMS UI, auth, or the operational database is **not** approved.

---

## 2. Executive summary

Since Audits #1 and #2, the project made **substantial, correctly prioritized progress** on the gaps those audits called out:

| Prior audit gap | Status on PR #2 branch |
| --- | --- |
| Real MariaDB tests (Audit 1 Phase 1, Audit 2) | **Addressed** — disposable MariaDB 11.4 via Testcontainers |
| Concurrent stock / order races | **Materially addressed** — concurrency tests; negative-stock race proved one winner |
| Browser coverage | **Partially addressed** — Playwright E2E + Chromium; multi-role synthetic sessions documented |
| Negative-stock policy (Audit 1/2) | **Addressed** — block by default; admin override + discrepancy audit |
| Inventory ops receive/count | **Addressed** — structured receiving, physical counts, append-only ledger |
| Activation discipline | **Strengthened** — nine technical preflight checks + three explicit manual gates; no in-app flag flip |
| AI isolation (Audit 2 §6) | **Maintained** — lab disconnected; weak models rejected; second benchmark % correctly discarded |
| Post-commit / SignalR failure | **Improved** — post-commit event delivery best-effort |
| Production stabilize P0 (Audit 2) | **Partially** — recovery, cleanup, backups documented; restore drill / alerting still track |

**Verdict:** Safe technical path toward a **supervised inventory pilot**. **Not** ready to enable automatic deduction in the live restaurant deployment. Remaining blockers are primarily **data ownership, human process, and live multi-device acceptance**.

This closes the decision request in `EXTERNAL_AUDIT_HANDOFF_2026-07-30.md`.

---

## 3. Progress vs Audit #1 phases and Audit #2 roadmap

| Prior plan item | Assessment |
| --- | --- |
| Audit 1 Phase 0 baseline validation | Largely done (production acceptance, demo orders, health) |
| Audit 1 Phase 1 operational hardening / tests | Major progress (real MariaDB, stress, adversarial, Playwright smoke) |
| Audit 1 Phase 2 inventory pilot | Controls ready; **data + human pilot not done** — do not enable |
| Audit 2 P0 production stabilize | Git/publish improved via PRs; image pin, restore test, alerting still checklist items |
| Audit 2 P1 inventory pilot | Technical preflight + ops rules in place; restaurant master data still provisional |
| Audit 2 P4 AI later | Correctly still later; lab-only |

---

## 4. Answers to EXTERNAL_AUDIT_HANDOFF questions

1. **Nine technical blockers sufficient?** Yes for supervised pilot framework; advisory: count freshness window; enforce recipe completeness at order time when inventory is on.
2. **One durable physical count per item?** Adequate to start if witnessed and recent; prefer a freshness window.
3. **Pending loss block activation?** Yes — keep conservative all-pending policy for initial activation.
4. **piece / g / ml?** Yes as pilot baseline if restaurant signs off.
5. **Override + discrepancy audit enough?** Sufficient for supervised beta; acknowledgement workflow later.
6. **Deadlock reload-and-retry?** Acceptable for supervised beta with clear UI + training; revisit if frequent in pilot.
7. **Evidence boundary clear?** Yes — maintain disposable-DB / flag-false language.
8. **Block isolated AI lab work?** No, if disconnected. Prefer inventory pilot gates over AI spend.

---

## 5. Findings by severity

### High (pilot blockers)

| ID | Finding |
| --- | --- |
| H1 | Restaurant master data not confirmed (sandbox/UNVERIFIED provisional data only) |
| H2 | Active deployment must not enable inventory |
| H3 | Human multi-device pilot not yet run |

### Medium

| ID | Finding |
| --- | --- |
| M1 | MariaDB deadlock under heavy parallel inventory transitions (safe rollback + message; train operators) |
| M2 | Playwright thinner than integration depth |
| M3 | Blazor Interactive Server sticky circuits under flaky Wi-Fi (residual from Audits 1–2) |
| M4 | Audit 2 P0 items still open: digest pin evidence, quarterly restore run, wired alerting |

### Low

| ID | Finding |
| --- | --- |
| L1 | Payment still manual; PH BIR/OR not in scope |
| L2 | Attendance correctly scoped |
| L3 | AI benchmark harness issues detected and demoted — good hygiene |

---

## 6. Security and isolation re-check

Role checks and audit entries remain central. Inventory writes admin-centric for receive/count/override. AI containers still without DB network/execution. Provisional import requires empty sandbox DB + explicit confirmation. No new critical security regression vs Audit #2. Main risk remains **premature enablement**.

---

## 7. Recommended pilot shape (when H1–H3 clear)

1. Sandbox or quiet-period stack with restaurant-confirmed data  
2. Inventory flag on **only** for that stack  
3. Scripted scenarios: prepare, amend, cancel (both dispositions), receive, count, waste approve/reject, concurrent two-device prep on low stock  
4. Backup + restore drill the same day  
5. Written go/no-go before any production flag change  

---

## 8. Outcome

**Conditionally accepted** for continued work on `agent/inventory-readiness`.

- Merge to `main` is reasonable after PR review **if** production deploy keeps inventory disabled.
- Supervised inventory pilot is **not** authorized by this audit.
- AI remains laboratory-only.

**Progress narrative:** Audit #1 established the beta baseline and phased plan. Audit #2 stressed production stabilize + inventory pilot gates + AI isolation. Audit #3 finds the **technical inventory controls and test depth largely delivered**; the baton is now with **restaurant data confirmation and supervised multi-device acceptance**, plus finishing Audit #2 P0 ops checklist items.
