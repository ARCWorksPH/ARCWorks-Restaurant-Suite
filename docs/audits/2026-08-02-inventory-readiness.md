# External Audit #2 — Inventory Readiness

**Date:** 2026-08-02  
**Auditor:** Independent third-party review (Grok / xAI)  
**Branch:** `agent/inventory-readiness` (PR #2, draft)  
**HEAD reviewed:** `1a2f42ed95ed506869fdeca0f0bb1a4c7247bc9a`  
**Baseline comparison:** Audit #1 (2026-07-29) + `docs/EXTERNAL_AUDIT_HANDOFF_2026-07-30.md`

## 1. Decision record

| Field | Value |
| --- | --- |
| Checkout SHA | `1a2f42ed95ed506869fdeca0f0bb1a4c7247bc9a` |
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
5. Resolve or formally accept residual MariaDB deadlock/reload-and-retry behavior under expected concurrent device load (see §5).

### Advisory follow-up (not pilot blockers if documented)

- Expand Playwright coverage beyond login/smoke toward full multi-role lifecycle on disposable stacks.
- Add bounded application-level retries only if operator UX of pure reload-and-retry proves insufficient in pilot.
- Consider count freshness window (e.g., opening counts older than N days re-require verification).
- Payment gateway / BIR official-receipt extension points remain future work.
- Grow AI evaluation corpus and tool-grounding tests before any ROMS function binding.

### AI-layer work

AI-layer **laboratory** work may continue without weakening the current safety boundary. Connecting the gateway to ROMS UI, auth, or the operational database is **not** approved.

---

## 2. Executive summary

Since Audit #1, the project made **substantial, correctly prioritized progress** on the exact risks called out previously:

| Audit #1 gap | Status in PR #2 branch |
| --- | --- |
| Real MariaDB tests | **Addressed** — disposable MariaDB 11.4 via Testcontainers; concurrency and smoke coverage |
| Concurrent stock / order races | **Materially addressed** — dedicated concurrency tests; negative-stock race proved one winner |
| Browser coverage | **Partially addressed** — Playwright E2E project + Chromium in CI; multi-role synthetic sessions documented |
| Negative-stock policy | **Addressed** — block by default; admin override + discrepancy audit |
| Inventory ops (receive / count) | **Addressed** — structured receiving, physical counts, append-only ledger |
| Activation discipline | **Strengthened** — nine technical preflight checks + three explicit manual gates; no in-app flag flip |
| AI isolation | **Maintained** — lab disconnected; weak models rejected; second benchmark percentages correctly discarded |
| SignalR / post-commit failure | **Improved** — post-commit event delivery made best-effort so DB commit is not unsafe-retried |

**Verdict:** The codebase is on a **safe technical path toward a supervised inventory pilot**. It is **not** ready to enable automatic deduction in the live restaurant deployment. The remaining blockers are primarily **data ownership, human process, and live multi-device acceptance**, not missing core controls.

---

## 3. Progress against Audit #1 recommendations

### 3.1 Testing (major improvement)

Observed on branch:

- Domain tests
- Command-gateway tests
- Integration tests including `MariaDbOrderConcurrencyTests`, `InventoryOperationsTests`, `InventoryControlTests`, `ResilienceStressTests`, `AdversarialInputTests`, `ProvisionalSeedImportTests`, `RealMariaDbSmokeTests`
- Playwright E2E project (`Roms.E2ETests`)

PR #2 claims **36/36** at one checkpoint; work log later records **60/60** after preflight work. Exact count will drift; the important change is **real MariaDB + concurrency + browser smoke**, which Audit #1 required.

Stress evidence (from work log / synthetic resilience doc):

- 60 full order lifecycles at parallelism 12
- Inventory overload: 24 Preparing attempts vs 12 units → stock never negative; exactly 12 advance
- Hostile inputs (ranges, enums, quantities, overlong/HTML/SQL-shaped text) rejected at domain layer

**Residual:** Synthetic load is not a production capacity rating. Live Wi-Fi, tablets, and human timing remain unproven.

### 3.2 Inventory controls (major improvement)

Documented and implemented rules now cover:

- Prepare-time consumption with projected negative-stock block
- Admin override with reason + `INVENTORY_DISCREPANCY_ALERT`
- Cancel/amend dispositions: return-to-stock vs consume-as-waste/staff-meal
- Pending waste/spoilage with admin approve/reject
- Receiving with delivery reference + idempotency
- Physical counts with zero-variance retention and nonzero variance adjustments
- Append-only movements; reconciliation via compensating reversals

Canonical units baseline: `piece`, `g`, `ml` — acceptable for pilot if the restaurant accepts it; a conversion model can wait.

### 3.3 Activation preflight (excellent discipline)

Nine blocking technical checks (`INV-001`…`LOSS-001`) plus three **manual** gates that never auto-pass:

1. Restaurant data-owner confirmation  
2. Independent external-audit acceptance  
3. Supervised multi-device pilot / backup / rollback approval  

No UI control toggles `Features:Inventory:Enabled`. This correctly prevents “green checklist = go live.”

### 3.4 AI (correctly constrained)

- TinyLlama / Phi-3 rejected and removed from model volume
- Second multilingual/offline benchmark harness results **correctly rejected** as accuracy claims due to duplicate scoring and permissive keyword grader false positives
- Duplicate-adjusted traceability scores recorded only for honesty (e.g. Llama 3.2 3B 34/75, Qwen 2.5 7B 31/75, TinyLlama 20/75)
- AI retained as disabled, non-critical-path experiment
- Lab still without DB credentials / execution path

**Note:** Loopback publish of Ollama on `127.0.0.1:11434` for local benchmarks is acceptable **only** if it never binds `0.0.0.0` and never routes through Cloudflare. Documented controls match that requirement; continue to treat any exposure beyond loopback as a severity-high finding.

### 3.5 Operations hygiene

Work log shows backup consolidation, Docker cleanup with pre-cleanup logical backup, tunnel token rotation awareness, and incident documentation for MariaDB deadlocks. This is mature operational behavior for a small team.

---

## 4. Answers to EXTERNAL_AUDIT_HANDOFF questions

1. **Are the nine technical blockers sufficient?**  
   **Yes for a supervised pilot decision framework**, with two advisories: (a) optional count freshness window; (b) ensure “active + available menu items without recipes” cannot be sold when inventory is later enabled (preflight already covers active/available recipe completeness — keep that invariant enforced at order time too).

2. **Is one durable physical count per item enough?**  
   **Adequate to start pilot** if counts are witnessed and recent. Prefer requiring counts within a defined window before activation and after major receiving events.

3. **Should pending loss requests block activation?**  
   **Yes — keep the conservative all-pending policy** for initial activation. During steady-state ops, pending losses should still surface loudly but need not freeze the whole system.

4. **Are piece / g / ml acceptable?**  
   **Yes as a pilot baseline** if the restaurant signs off. Defer unit conversion matrices until after pilot pain is measured.

5. **Manager override + discrepancy audit enough?**  
   **Sufficient for supervised beta.** A separate discrepancy-acknowledgement workflow is advisory for later (who cleared the alert, when).

6. **Deadlock reload-and-retry acceptable?**  
   **Acceptable for supervised beta** if the UI message is clear and operators are trained. Prefer not to hide contention with silent unbounded retries. Revisit if pilot devices hit frequent conflicts.

7. **Is the evidence boundary clear?**  
   **Yes.** Docs repeatedly state disposable DBs, inventory flag false on active deploy, and synthetic ≠ production. Maintain that language in PR descriptions and README.

8. **Must anything block isolated AI lab work?**  
   **No**, provided the lab stays disconnected. Do not spend primary engineering time on AI until inventory pilot gates are closed or explicitly deferred by the restaurant.

---

## 5. Findings by severity

### High (pilot blockers)

| ID | Finding | Evidence / notes |
| --- | --- | --- |
| H1 | Restaurant master data not confirmed | Provisional import path exists; dataset marked sandbox/UNVERIFIED |
| H2 | Active deployment must not enable inventory | Repeated correctly in work log and handoff; enforce at deploy time |
| H3 | Human multi-device pilot not yet run | Synthetic multi-role browser sessions ≠ waiter/kitchen tablets on restaurant network |

### Medium (should resolve or explicitly accept before pilot)

| ID | Finding | Evidence / notes |
| --- | --- | --- |
| M1 | MariaDB deadlock under heavy parallel inventory transitions | Documented in `MARIADB_DEADLOCK_INCIDENT_2026-07-30.md`; safe rollback + operator message; may need UX training |
| M2 | Playwright coverage still relatively thin vs integration depth | Smoke/login and some workflows present; full path matrix still expanding |
| M3 | Blazor Interactive Server sticky circuits under flaky Wi-Fi | Architectural residual from Audit #1; monitor reconnect UX in pilot |

### Low / informational

| ID | Finding |
| --- | --- |
| L1 | Payment remains manual confirmation; PH BIR/OR path not in scope |
| L2 | Attendance correctly scoped (no payroll) |
| L3 | AI benchmark harness quality issues were detected and results demoted — good scientific hygiene |
| L4 | Existing DOCX audit artifact filename has a date typo (`07-28-20206`); prefer this Markdown timeline going forward |

---

## 6. Security and isolation re-check

- Role checks and audit entries remain central to mutating paths.
- Inventory write paths are admin-centric for receive/count/override; kitchen limited to appropriate loss reporting.
- AI containers: no DB network, no execution, capability drop, read-only roots — consistent with Audit #1 intent.
- Provisional data import requires empty sandbox DB, explicit confirmation, and UNVERIFIED opening balances — appropriate.
- Secret scanning / no vulnerable packages claimed in PR #2 — continue in CI.

No new critical security regression identified relative to Audit #1. The main risk remains **premature enablement**, not missing isolation primitives.

---

## 7. Product / real-world consequence view

ROMS is still correctly positioned as an **internal** waiter / kitchen / admin tool. Enabling inventory too early is the highest path to “real consequences” (wrong stock → 86’d items, disputed waste, prep blocked mid-service). The current preflight + manual gates are the right cultural and technical response to that risk.

Recommended pilot shape (when H1–H3 clear):

1. Sandbox or quiet-period stack with restaurant-confirmed data  
2. Inventory flag on **only** for that stack  
3. Scripted scenarios: prepare, amend, cancel with both dispositions, receive, count, waste approve/reject, concurrent two-device prep on low stock  
4. Backup + restore drill the same day  
5. Written go/no-go before any production flag change  

---

## 8. Outcome

**Conditionally accepted** for continued work on `agent/inventory-readiness`.

- Merge to `main` is reasonable **after** PR review of tests and docs, provided production deploy config keeps inventory disabled.
- Supervised inventory pilot is **not** authorized by this audit.
- AI remains laboratory-only.

This audit closes the open decision request in `EXTERNAL_AUDIT_HANDOFF_2026-07-30.md` with the decision record in §1.
