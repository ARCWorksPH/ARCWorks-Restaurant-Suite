# External Audit #7 — Gate 1 Session Security & Gate 2 Waiter Dashboard Progress

**Date:** 2026-08-24  
**Auditor role:** Lead QA & Compliance Auditor (Grok)  
**Primary review target:** `main` @ `6ad04b4e7a471e5e46c50eb7c0c88b1b197be93f` (Gate 2G merged)  
**Also reviewed:** open PR #25 (`agent/gate2h-final-waiter-ui`); Gate 2 plan/contracts under `docs/UI/Gate 2/`; timeline and restore evidence  
**Repository:** [ARCWorksPH/ARCWorks-Restaurant-Suite](https://github.com/ARCWorksPH/ARCWorks-Restaurant-Suite)

## 1. Decision record

| Field | Value |
| --- | --- |
| Overall status | **Conditionally accepted** for continued private-beta preparation |
| Gate 1 authenticated-session security (incl. cookie-copy fail-safe) | **Accepted** as live baseline (merged + promoted per project timeline) |
| Gate 2A–2G backend contracts on `main` | **Conditionally accepted** as implemented slices with automated evidence claims |
| Gate 2H final Waiter dashboard UI (PR #25) | **Conditionally positive** — merge only after owner visual acceptance + green CI; not a full workflow-beta close |
| Gate 2I deployment / recovery boundary | **Not started** as product claim — required before waiter milestone is “live complete” |
| AI hold | **Must remain active** |
| Supervised four-role operational workflow acceptance | **Still open** (order floor path beyond waiter personal dashboard) |
| Cross-PC application runtime restore | **Still open** |
| Restaurant-approved operational data | **Still open** |
| Public production rollout | **Rejected** until release gates close |

### Mandatory before claiming private-beta ready

1. Complete Gate 2H with **owner visual + functional acceptance**, then Gate 2I deploy/rollback evidence without unsupervised live risk.
2. Supervised **four-role** acceptance of the operational contract (waiter floor → kitchen → manager → admin), not only personal dashboard features.
3. **Cross-PC / second-host** application runtime restore drill with filed evidence.
4. Restaurant-approved catalog, staff, roles, and (if used) inventory opening data.
5. Staging/tunnel/monitoring ownership and incident contact in writing.
6. Keep **AI hold** on; no journal or dashboard path may introduce AI dependency.

### Advisory

- Merge or close stale open PRs (#4 Zabbix, #9/#10 landing design branches, #15 audit-6) to reduce branch noise; landing is already accepted on `main`.
- Update root `README.md`: remove obsolete “Active isolated UI design handoff” for landing; document Gate 2 waiter dashboard status accurately.
- Align `docs/ROADMAP_2026-08-06.md` status rows with Gate 1–2 reality (local restore exists; UI is no longer “deferred until workflow only”).
- Keep leave-request and announcement scope employee-owned / one-way; do not silently expand into payroll or chat.
- Treat browser-encrypted journal as high-sensitivity: any regression that stores plaintext server-side or in logs is a **High** finding.

---

## 2. Executive summary

Since Audit #6 (2026-08-12), the project executed a disciplined **backend-first waiter personal dashboard program**:

| Gate | Theme | Status on review date |
| --- | --- | --- |
| 1 | Session truth: inactivity vs cookie lifetime; second-runtime cookie-copy fail-safe | **Merged and live-promoted** |
| 2A | Restaurant identity + authoritative server clock (Asia/Manila display) | **Merged** |
| 2B | Restricted Waiter Dashboard read model | **Merged** |
| 2C | Attendance-backed floor eligibility (server-enforced) | **Merged** |
| 2D | Staff profiles + anonymous Today’s Team portraits | **Merged** |
| 2E | Staff announcements + manager notes | **Merged** |
| 2F | Employee-owned leave requests | **Merged** |
| 2G | Private browser-encrypted journal | **Merged** |
| 2H | Final Waiter dashboard UI integration | **Open PR #25** |
| 2I | Deployment / recovery boundary | **Explicitly deferred** by PR #25 |

This is the right sequencing relative to Audits 5–6: **security and contracts before chrome**. Gate 1’s cookie-copy revocation (both suspected copy and original forced to re-auth) is a strong fail-safe for shared devices.

**Risk posture:** Medium for private supervised beta of the **deterministic order workflow**; Medium-low for the **waiter personal dashboard backend** once 2H/2I and owner acceptance complete. Residual beta blockers remain **people proving the floor workflow**, **second-host recovery**, and **approved restaurant data** — not missing Gate 2A–2G concepts.

---

## 3. Progress vs Audit #6 mandatory items

| Audit #6 item | Status at Audit #7 |
| --- | --- |
| Supervised four-role acceptance | **Still open** — Gate 2 advances waiter personal/attendance surface; operational multi-role acceptance not claimed |
| Cross-PC runtime restore | **Still open** — local isolated drill remains the latest evidence |
| Restaurant-approved data | **Still open** |
| Staging/monitoring ownership | **Still open** |
| AI hold | **Still active** (correct) |
| Landing page | **Still accepted** (live baseline) |
| Waiter shell / account security (PR #13) | **Absorbed** into Gate 1 / Gate 2 line (session + dashboard program) |

---

## 4. Findings by severity

### High (private-beta blockers)

| ID | Finding |
| --- | --- |
| H1 | Four-role operational workflow acceptance still incomplete |
| H2 | Cross-PC / second-host application restore not evidenced |
| H3 | Gate 2H/2I not closed — waiter UI not owner-accepted on live; deploy/rollback for full Gate 2 not filed as complete |

### Medium

| ID | Finding |
| --- | --- |
| M1 | Roadmap / README lag implementation (stale landing handoff; roadmap phase statuses understate Gate 1–2) |
| M2 | Scope expansion into leave requests and encrypted journal is valuable but increases surface area before floor acceptance — keep Manager processing and payroll out of scope as planned |
| M3 | Journal privacy depends on correct client-side crypto and zero plaintext in server logs/DB; treat as continuous regression risk |
| M4 | Open PR backlog (Zabbix, old landing design PRs, Audit #6 PR) creates review noise |

### Low / informational

| ID | Finding |
| --- | --- |
| L1 | Attendance module README still describes “excludes leave” in places; leave requests now exist as structured requests — update product wording |
| L2 | Pre-existing NU1903 SSH.NET warnings noted in PR #25 verification — track dependency hygiene |
| L3 | Gate naming (2A–2I) is clear; keep PR titles and evidence logs using the same IDs |

---

## 5. Architecture & security assessment

### Gate 1 (strength)

- Separates **login session** from **attendance** (correct).
- Server-side inactivity vs fixed short cookie repaired per plan.
- Cookie-copy fail-safe that revokes the whole staff session avoids guessing which browser is “real” — appropriate for shared POS tablets.
- Live promotion with rollback tag recorded in project timeline.

### Gate 2 contracts (strength)

- **Server time authority** (UTC + Asia/Manila) prevents device-clock gaming of attendance.
- **Floor eligibility** tied to open attendance and enforced on direct URLs matches the backend-first plan.
- **Today’s Team** anonymity (portraits only, no names/roles/tooltips) is a deliberate privacy design for floor social presence.
- **Leave requests** as one-way structured requests (not chat) matches ops reality.
- **Journal** design goal — manager/admin/DB operator cannot read plaintext — is the correct privacy bar if implementation holds.

### Scope discipline

- AI remains held; Gate 2 plan explicitly forbids AI dependency.
- Recipes / auto-deduct remain out of scope.
- Tables & Orders content intentionally staged after personal dashboard contracts.

### Residual concerns

- Expanding HR-adjacent features (leave, journal, announcements) before **kitchen/manager/admin** visual parity and floor acceptance can create a “waiter-rich, kitchen-thin” product perception for beta restaurants.
- Auto-closed attendance flagged for Manager review is correct; ensure Managers have a usable review UI before relying on auto-close in real shifts.

---

## 6. Open pull request #25

| Topic | Assessment |
| --- | --- |
| Intent | Integrate final role-restricted Waiter dashboard; wire 2A–2G contracts; accessible modals; Android portrait/landscape |
| Verification claimed | Build 0 errors; Gate 2A–2G MariaDB 32/32; focused Playwright 1/1; format/diff check |
| Boundaries | No local/public deploy; no credential/tunnel changes; Gate 2I owns deploy/recovery |
| Audit view | Appropriate isolation. Require owner visual acceptance and CI green before merge. Do not treat merge as four-role beta close. |

---

## 7. Recommended near-term sequence

1. Owner visual/functional acceptance of Gate 2H → merge PR #25.  
2. Gate 2I: controlled deploy + rollback evidence; update live image tags.  
3. Supervised multi-role floor acceptance (contract scenarios).  
4. Cross-PC restore drill → `docs/evidence/`.  
5. Restaurant-approved data under change control.  
6. Refresh README + roadmap status language.  
7. Only then limited private beta. AI remains future-version.

---

## 8. Outcome

**Conditionally accepted.**

Gate 1 live session hardening and Gate 2A–2G backend contracts represent **material, well-sequenced progress** since Audit #6. The project is closer to a trustworthy waiter personal experience. Private-beta readiness still depends on **closing 2H/2I**, **proving the operational four-role floor with people**, and **second-host recovery** — not on inventing more personal-dashboard features.

**Do not** claim public production readiness, enable AI, or expand recipes/auto-deduct until mandatory items in §1 are closed and dispositioned on this timeline.
