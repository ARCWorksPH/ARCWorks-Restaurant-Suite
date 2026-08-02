# External Audit #4 — QA, Compliance & Security (Post-Merge)

**Date:** 2026-08-03 (review window 2026-08-02 evening PST)  
**Auditor role:** Lead QA & Compliance Auditor (Grok), with QA Automation and Security & Regulatory Compliance focus  
**Repository HEAD:** `dbc91f9ed371156440e48ac1d24569055dc7eff1` (`main` after merge of PR #2)  
**Prior audits:** #1 baseline, #2 architecture/security/product, #3 inventory-readiness (pre-recipe-removal)

## 0. Team context (non-binding org chart)

ARCWorks Development Team chart assigns AI systems to functional lanes under human CEO/Product Manager **GUNBORG**:

| Lane | Assigned system |
| --- | --- |
| CEO / Product Manager | GUNBORG (human) |
| DevOps & Infrastructure (+ SE, Cloud, CI/CD) | Codex |
| R&D / Prototype (+ Rapid Prototyping) | ChatGPT |
| UI/UX & Frontend (+ Visual, Web/Mobile) | Gemini |
| Lead QA & Compliance (+ QA Automation, Security & Regulatory) | Grok |
| Office Experience & Hospitality | ClaudeAI |

This audit is written from the **QA / compliance / security** lane. It does not replace human product authority. AI assignees are tools under GUNBORG; production risk acceptance remains human-only.

---

## 1. Decision record

| Field | Value |
| --- | --- |
| Overall status | **Conditionally accepted** for continued single-site beta preparation |
| Recipe-based inventory / auto-deduction | **Correctly removed from scope** — treat as deferred product, not a defect |
| Manual inventory ledger (receive, count, waste/spoilage, adjustments) | **Accepted** as current inventory product surface |
| Staff / restaurant beta | **Not complete** — still required |
| Production restore drill | **Still required** (Audit 2 P0 / Audit 3 residual) |
| `AI_ENABLED=true` in production | **Rejected** until roadmap gates 4–6 complete |
| AI write / SQL / recipe functions | **Rejected** (policy-aligned) |

### Mandatory before claiming “production ready”

1. Simultaneous waiter / kitchen / admin acceptance on real (or realistic) devices and network.
2. Restaurant-confirmed menu, pricing, roles, hours, and (if using inventory) opening balances + units.
3. Documented backup **restore** drill with RPO/RTO notes — not only backup creation.
4. Wired alerting for health failure and missed backup (or explicit accepted risk).
5. Keep AI disabled in production until adversarial corpus + browser acceptance pass.

### Advisory

- Expand Playwright beyond 3/3 smoke toward multi-role regression on disposable stacks.
- Align leftover docs that still describe recipe-era reversal as “current” (file is marked retired — good; ensure no UI copy or tests reintroduce coupling).
- Track AI compute/budget explicitly so R&D does not outrun core ops (policy already warns of this).

---

## 2. Executive summary

Since Audit #3, the project made two **high-quality product/security moves**:

1. **Removed recipe functionality and order-linked stock deduction** (PR #2 merged). Inventory is now a **manual independent-item ledger**. This eliminates the highest-complexity failure mode Audits 1–3 were gating (wrong recipes → wrong stock mid-service).
2. **Security hardening pass** (2026-08-02): CI action SHA pins, health checks, container least privilege, tunnel isolation, AI rate limits, role-filtered catalogs, assistant outcome auditing, branch protection on `main`, NuGet advisory clean, 63/63 tests reported.

**Risk posture:** Medium-low for **core order → kitchen → payment** single-site use with inventory optional/manual and AI off. Residual risk concentrates on **human beta acceptance**, **ops drills**, and **not turning AI on early**.

The recipe removal is a **strength**, not a regression: it matches the “simpler, auditable inventory” decision and reduces pilot surface area.

---

## 3. Change log vs Audit #3

| Topic | Audit #3 expectation | Current main |
| --- | --- | --- |
| Recipe consumption / reversal | Central to inventory pilot | **Removed**; reversal rules doc marked retired historical |
| Inventory model | Recipe + deduction on Preparing | Manual ledger: receive, count, adjust, waste/spoilage |
| AI protocol | Isolated lab; writes carefully gated | Feature-gated **read-only** assistant; 12 approved read functions; writes excluded |
| Security | Isolation good; rate-limit/alert gaps | Hardening doc: limits, audit hashes, CSP/HSTS, tunnel net isolation, branch protection |
| Tests | ~60 reported on branch | **63/63** claimed post-harden (domain 11, gateway 11, integration 38, E2E 3) |
| PR #2 | Draft inventory-readiness | **Merged** after scope pivot to simplify inventory |

---

## 4. QA assessment

### Strengths

- Real MariaDB, concurrency, stress, adversarial, and Playwright smoke remain in the suite.
- Recipe removal rehearsed with backup + restore of MariaDB data (PR validation notes).
- AI authorization tests included in the 38 integration slice (important for non-bypass of roles).
- Fail-closed interpretation and ROMS-side fact formatting reduce “model as source of truth” risk.

### Gaps

| ID | Severity | Finding |
| --- | --- | --- |
| Q1 | Medium | Browser suite still thin (3 E2E) relative to integration depth |
| Q2 | Medium | Staff beta / multi-device acceptance still not evidenced as complete |
| Q3 | Low | Ensure CI `verify` is the only merge path in practice (branch protection configured — good) |
| Q4 | Low | After recipe removal, confirm no orphaned UI strings or admin flows still promise “deduct on prepare” |

### Compliance-oriented test expectations (next sprint)

1. Regression: full order lifecycle **without** any stock movement side effects from order status changes.
2. Inventory: receive / count / waste approve-reject / negative adjustment controls only via admin paths.
3. AI: cross-role denial (waiter must not receive inventory catalog or admin-only summaries).
4. Assistant disabled path: `AI_ENABLED=false` hides or blocks assistant end-to-end.

---

## 5. Security & regulatory compliance assessment

### Accepted controls (from SECURITY_HARDENING_2026-08-02)

- False-positive SQLi on migration lock rewritten to parameterized exact command — good disposition.
- GitHub Actions pinned to commit SHAs; workflow permissions read-only.
- Container health checks; non-root UIDs; capability drop; read-only roots.
- Cloudflare tunnel not on default bridge; secret file pattern for token.
- AI: role-derived function list; catalog filtering; concurrency + per-user rate limits; audited outcomes without raw prompt storage.
- `main` protected: PR required, successful `verify`, no force-push.

### Residual compliance notes

| ID | Severity | Finding |
| --- | --- | --- |
| S1 | High (if AI enabled) | AI production enablement still gated — **keep disabled** |
| S2 | Medium | Quarterly **restore** drill still not closed from Audit 2 checklist |
| S3 | Medium | Alerting channel for health/backup still a documented gap unless implemented outside repo |
| S4 | Low | PH BIR / official receipt / payment gateway still out of scope — disclose to any paying pilot restaurant |
| S5 | Low | Confidential independent scanner report kept out of Git — correct; retain offline |

No evidence of AI write authority or DB credentials in the model/gateway path. Policy language (“code and database determine facts”) is the right compliance posture.

---

## 6. Product / roadmap alignment

`ROADMAP_2026-08-02.md` correctly states:

- Non-AI core is substantially implemented.
- AI Phase 1 is read-only; writes excluded.
- Production blockers AI cannot solve: restaurant validation, simultaneous staff acceptance, restore drill, staging vs prod config, external-audit sign-off.
- Recipes deferred until business case / funding.

**QA endorsement:** Prioritize **staff beta of the core loop** over further AI corpus expansion if resources conflict. AI has already consumed disproportionate effort relative to restaurant-facing validation (policy §6 acknowledges this).

---

## 7. Feedback for the ARCWorks “team” lanes

**To GUNBORG (CEO/PM)**  
Recipe removal was the right call for beta risk. Own the go/no-go for staff beta and any AI flag flip. Do not let tool velocity substitute for restaurant sign-off.

**To Codex (DevOps/CI)**  
Hardening pass is strong. Close restore-drill evidence and alerting next. Keep action/image digests current.

**To ChatGPT (R&D/prototype)**  
Prototype energy should not reintroduce recipes or AI writes without a written product RFC and QA threat model.

**To Gemini (UI/UX)**  
After recipe removal, audit all inventory and order screens for accurate copy (manual ledger, no implied auto-deduct). Touch targets and offline/reconnect messaging remain beta-critical.

**To Grok (this lane)**  
Next audit should re-check after staff beta or after AI adversarial corpus completion — whichever comes first.

**To ClaudeAI (ops hospitality)**  
When staff trials run, capture friction notes (login, table flow, KDS glanceability, payment confirm) as first-class evidence beside automated tests.

---

## 8. Outcome

**Conditionally accepted.**

ROMS on `main` is in a **clearer, safer product shape** than at Audit #3: simpler inventory, stronger security controls, read-only AI behind a default-off flag, and a merged, tested tree. The path to trustworthy daily use is no longer blocked primarily by missing inventory machinery — it is blocked by **human acceptance, restore/alerting ops closure, and disciplined AI non-enablement**.

**Do not** market or operate with AI enabled or with recipe/auto-deduct expectations until the mandatory items in §1 are closed.
