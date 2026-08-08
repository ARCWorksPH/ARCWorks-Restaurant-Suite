# External Audit #5 — ARCWorks Restaurant Suite Rebrand & Workflow Freeze

**Date:** 2026-08-08  
**Auditor role:** Lead QA & Compliance Auditor (Grok)  
**Primary review target:** `agent/backup-recovery` @ `66eb9b04e46eb224f37fa4624d580362f99fffc5` (PR #5 draft)  
**Also noted:** `main` @ `be5f05f252316e3d44ae2b643e688901ccb3feb2` (includes Audits 1–4); open PR #4 Zabbix  
**Repository:** [ARCWorksPH/ARCWorks-Restaurant-Suite](https://github.com/ARCWorksPH/ARCWorks-Restaurant-Suite)

## 1. Decision record

| Field | Value |
| --- | --- |
| Overall status | **Conditionally accepted** for continued **private-beta preparation** |
| Public product name | **ARCWorks Restaurant Suite** — accepted |
| Org / repo move | **ARCWorksPH / ARCWorks-Restaurant-Suite** — accepted; ROMS retained as internal code name |
| AI in active product | **Held** (`AI_HOLD=true`); lab-only; **not** authorized for staff use |
| Four-role workflow contract | **Accepted as freeze** for acceptance testing; browser/staff acceptance still pending |
| Manual independent-item inventory | **Accepted** as current inventory product |
| Certified backup system + schedule | **Material progress**; full restore-drill **evidence still required** |
| Private / supervised staff beta | **Not complete** |
| Public production rollout | **Rejected** until release gates in roadmap §Release gates are closed |
| Merge of PR #5 to `main` | **Reasonable after** CI green + smoke on workflow/UI changes; keep draft until acceptance notes attached |
| PR #4 Zabbix | **Advisory accept** as optional ops stack; keep secrets out of Git; wire notifications only after ownership assigned |

### Mandatory before claiming private-beta “ready”

1. Supervised multi-role browser/staff acceptance against the frozen workflow contract (waiter, kitchen, manager, admin) on realistic devices.
2. One **documented restore drill** from the certified backup path (isolated destination, app restart, history/audit verification).
3. Restaurant-approved menu, staff roles, and (if used) inventory opening data — not only synthetic seed.
4. Staging/tunnel/monitoring ownership and incident contact assigned in writing.
5. Keep **AI hold** active; do not treat lab profile as product.

### Advisory

- Finish phone drawer / remaining UI phases before claiming visual acceptance complete.
- Expand Playwright beyond smoke toward contract scenarios marked unchecked in `WORKFLOW_CONTRACT_2026-08-06.md`.
- Close or explicitly schedule PR #4 (Zabbix) relative to Gatus; avoid dual-monitoring confusion without ownership.
- Ensure README on `main` is updated when PR #5 merges so public branding matches the org repo.

---

## 2. Executive summary

Since Audit #4 (2026-08-03), the project completed a **strategic product maturation pass**:

1. **Permanent branding** under ARCWorksPH with public name **ARCWorks Restaurant Suite**, while preserving ROMS as the internal compatibility identity.
2. **Hard AI hold** — stronger than “feature flag off”: Assistant hidden, `/assistant` not-found, no gateway client, app off the `command` network, hold cannot be bypassed by stale `AI_ENABLED=true`.
3. **Deterministic four-role workflow freeze** (Waiter / Kitchen / Manager / Admin-Owner) with timers, return-to-waiter, preparation targets, and a written contract.
4. **Ops depth** — certified backup/recovery work, prompted six-hour backup schedule, portable instance contract, tunnel-in-compose correction.
5. **Product polish for beta** — side navigation shell, KDS layout fixes, 86/68 availability matrix, soft staff/catalog removal, order summaries, schedule CSV/template, confirmation gates, Admin audit log on Reports.
6. **Collaboration posture** — draft PR #5 invites external workflow/ops feedback with clear deliberate boundaries.

**Risk posture:** Medium-low for **private, supervised beta** of the deterministic core **if** AI stays held and inventory stays manual. Residual risk is still **human acceptance and recovery evidence**, not missing core domain machinery.

This is the right direction relative to Audits 1–4: less AI surface, clearer roles, more ops and UX discipline.

---

## 3. Progress vs Audit #4 mandatory / residual items

| Audit #4 item | Status at Audit #5 |
| --- | --- |
| Staff multi-device beta | **Still open** — synthetic workflow tests and Playwright smoke improved; supervised acceptance not claimed |
| Restaurant-confirmed data | **Still open** — demo catalog populated for UI; owner-approved production data not signed off |
| Backup **restore** drill | **In progress** — certified backup system + schedule exist; roadmap Phase 5 still requires complete drill record |
| Health / missed-backup alerting | **Partial** — backup prompts non-deferrable; Zabbix PR open; external notification destinations intentionally unconfigured |
| `AI_ENABLED=false` | **Strengthened** to full **AI hold** |
| Recipe / auto-deduct expectations | **Still correctly out of scope** |

---

## 4. Findings by severity

### High (private-beta blockers)

| ID | Finding |
| --- | --- |
| H1 | Supervised four-role acceptance incomplete (contract scenarios still unchecked for human/browser paths) |
| H2 | Restore-drill evidence not yet closed as accepted recovery |
| H3 | Restaurant-approved operational data not yet the source of truth for beta |

### Medium

| ID | Finding |
| --- | --- |
| M1 | UI redesign mid-flight (shell/KDS improvements landed; full visual acceptance deferred by design) |
| M2 | Manager role is a major domain expansion since Audit #4 — needs explicit matrix tests in CI beyond targeted unit coverage |
| M3 | `main` lags `agent/backup-recovery` substantially (37 commits on PR #5); prolonged draft is fine only if production deploy source is explicit |
| M4 | Dual monitoring paths (Gatus + proposed Zabbix) need a single ownership model |

### Low / informational

| ID | Finding |
| --- | --- |
| L1 | Public README on `main` may still lean on older ROMS-only framing until PR #5 merges |
| L2 | Internal name ROMS vs product name ARCWorks Restaurant Suite is correctly documented; keep both consistent in support docs |
| L3 | Collaboration/issue templates and security-contact guidance are a positive maturity signal |
| L4 | Payment remains manual confirm; PH BIR/OR still out of scope — disclose to pilot restaurants |

---

## 5. Architecture & product assessment

### Branding and repository governance

Moving under **ARCWorksPH** and adopting **ARCWorks Restaurant Suite** as the permanent product name is appropriate for external collaboration. Retaining ROMS for namespaces/migrations avoids breaking EF history and deploy contracts. `docs/BRANDING_AND_COMPATIBILITY.md` (on the reviewed branch) supports that split.

### Workflow contract (strength)

`WORKFLOW_CONTRACT_2026-08-06.md` is the most important quality artifact since Audit #4. It freezes:

- four roles and ownership boundaries;
- order state machine including **ReturnedToWaiter**;
- timer model (order-entry, kitchen acceptance, item-based preparation snapshot);
- inventory as manual ledger only;
- route policies including Manager live-only supervision.

**QA view:** Treat any PR that changes those rules without updating the contract + tests as a process failure.

### AI hold (strength)

`AI_HOLD.md` implements the compliance posture Audits 2–4 recommended more strictly than a simple boolean. Fail-closed defaults, network detachment, and explicit re-enable gates are correct for a restaurant ops product.

### Inventory

Manual independent-item ledger with receive/count/adjust and 86 availability remains the right beta surface. Recipe removal stays validated as a product decision, not unfinished work.

### Backup, portable, monitoring

- Certified backup + non-deferrable prompts address Audit 2/4 ops gaps in **design**; acceptance still needs a full restore record.
- Portable instance manifests reduce “one workstation” lock-in — good for demo/VM strategy.
- Zabbix stack is reasonable optional infrastructure; do not commit secrets; assign who pages whom before enabling external notifications.

### UI

Side rail, KDS beside nav, contrast fixes, confirmation dialogs, and order summary are concrete beta-quality improvements. Claiming “UI complete” remains premature until phone drawer and supervised visual pass finish.

---

## 6. Security & compliance re-check

- Prior hardening (digest pins, least privilege, tunnel isolation, branch protection) remains the baseline.
- Soft deactivation of staff/catalog preserves audit history — correct for dispute and compliance trails.
- Pending payments restricted to Manager/Admin — good separation from waiter/kitchen.
- Kitchen `86` vs Manager/Admin `68` enforced server-side — correct; do not rely on hidden buttons alone.
- AI path cannot affect production while hold is on.

No new critical security regression identified relative to Audit #4. Highest risk remains **operational enablement without human gates**.

---

## 7. Open pull requests

| PR | Title | Audit note |
| --- | --- | --- |
| [#5](https://github.com/ARCWorksPH/ARCWorks-Restaurant-Suite/pull/5) | Workflow and collaboration baseline | Primary delivery vehicle for post–Audit-4 work; merge when CI + smoke green and scope matches contract |
| [#4](https://github.com/ARCWorksPH/ARCWorks-Restaurant-Suite/pull/4) | Zabbix monitoring stack | Optional; secrets ignored; notifications unconfigured — acceptable for draft |

---

## 8. Recommended near-term sequence (QA priority)

1. Attach supervised acceptance evidence to the unchecked scenarios in the workflow contract.  
2. Execute and file one restore drill under `docs/evidence/` or work log.  
3. Merge PR #5 to `main` only with green verify and no AI-hold regression.  
4. Decide Zabbix vs Gatus ownership; then merge or park PR #4.  
5. Only then schedule limited private beta with restaurant-approved data.  
6. AI remains future-version until a new threat model and audit explicitly reopen it.

---

## 9. Outcome

**Conditionally accepted** for private-beta preparation under the ARCWorks Restaurant Suite identity.

The project is **clearer, safer, and more operable** than at Audit #4: permanent branding, hard AI hold, frozen four-role contract, backup system progress, and tangible UI/workflow fixes. The path to trustworthy daily use is now almost entirely about **proving the contract with people and recovery drills**, not inventing more domain features.

**Do not** market public production readiness, enable AI, or reintroduce recipes until the mandatory items in §1 are closed and dispositioned in the audit timeline.
