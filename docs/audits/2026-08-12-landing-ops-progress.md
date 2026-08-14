# External Audit #6 — Landing Page Live Acceptance & Ops Progress

**Date:** 2026-08-12  
**Auditor role:** Lead QA & Compliance Auditor (Grok)  
**Primary review target:** `main` @ `c0fcaaca591475c0e8531c3e892e793b7f53c1f8`  
**Also reviewed:** open PR #13 (`ui/waiter-shell-account-security`); live `https://roms.arkworksph.online/Account/Login`  
**Repository:** [ARCWorksPH/ARCWorks-Restaurant-Suite](https://github.com/ARCWorksPH/ARCWorks-Restaurant-Suite)

## 1. Decision record

| Field | Value |
| --- | --- |
| Overall status | **Conditionally accepted** for continued private-beta preparation |
| Chef Doy's landing / staff login (visual + login behavior) | **Accepted** as production-facing baseline |
| Identity / session behavior on login | **Preserved** (single-session, inactivity, real Identity form) |
| Local isolated backup restore drill (2026-08-08) | **Accepted** for same-PC content integrity |
| Cross-PC / full application runtime restore | **Still required** before beta claim |
| Supervised multi-role workflow acceptance | **Still open** |
| Restaurant-approved operational data | **Still open** |
| AI hold | **Must remain active** |
| PR #13 (waiter shell + account security) | **Conditionally positive** — merge after CI green + assertion fixes; does not close workflow beta |
| Public production rollout | **Rejected** until remaining release gates close |

### Mandatory before claiming private-beta ready

1. Supervised four-role acceptance against `WORKFLOW_CONTRACT_2026-08-06.md` (waiter, kitchen, manager, admin) on realistic devices.
2. **Cross-PC** (or second-host) application runtime restore drill — local isolated restore is no longer the gap; runtime portability is.
3. Restaurant-approved menu, staff, roles, and (if used) inventory opening data.
4. Staging/tunnel/monitoring ownership and incident contact in writing.
5. Keep **AI hold** on.

### Advisory

- Merge PR #13 when verify is green; treat Tables & Orders redesign as a separate phase (as scoped).
- Update README “Active isolated UI design handoff” section — landing is now accepted on `main`, not an isolated handoff.
- Prefer full-suite test runs that complete without outer timeout so E2E is not “pending” by orchestration limits.
- Close or schedule Zabbix PR #4 against Gatus ownership.

---

## 2. Executive summary

Since Audit #5 (2026-08-08):

1. **Landing page shipped.** Premium Chef Doy's Gourmet Restaurant staff-login surface was designed, isolated-previewed, owner-accepted, CI+GitGuardian checked, merged (PR #11), and **promoted live** to `roms.arkworksph.online` with image rollback preserved. Independent live inspection on 2026-08-12 confirmed HTTP 200, restaurant-dominant branding, smoked-glass login card, secondary ARCWorks mark, and usable form controls.
2. **Audit #5 restored to main** (PR #14) after earlier push/path issues — timeline integrity recovered.
3. **Backup restore drill evidence exists** (`docs/evidence/BACKUP_RECOVERY_DRILL_2026-08-08.md`): isolated file/DB restore and interrupted-restore recovery passed after fixing a `RomsRoot` path config error. Cross-PC runtime restore remains explicit next ops gate.
4. **PR #13** advances account security (username archive on soft-remove, forced first-login password change, waiter nav consolidation, landscape rail collapse) without touching Tables & Orders content — correct sequencing.

**Risk posture:** Medium-low for private supervised beta of the deterministic core **with AI held**. The public face of the product is now credible; the remaining blockers are still **human workflow acceptance** and **runtime recovery portability**, not missing login aesthetics or basic backup machinery.

---

## 3. Progress vs Audit #5 mandatory items

| Audit #5 item | Status at Audit #6 |
| --- | --- |
| Supervised multi-role acceptance | **Still open** |
| Documented restore drill | **Local isolated drill accepted**; cross-PC runtime drill **still open** |
| Restaurant-approved data | **Still open** (preview catalog exists; production sign-off not claimed) |
| Staging/tunnel/monitoring ownership | **Still open** |
| AI hold | **Still active** (correct) |
| Landing / first impression | **Closed** — accepted and live |

---

## 4. Landing page assessment (QA / compliance)

### Strengths

- **Owner formal acceptance** with clear boundary: presentation + existing login only; does not accept role UIs or recovery gates.
- **Identity preserved:** real Blazor Identity controls; single-session and 15-minute inactivity unchanged; disposable admin login path verified in preview.
- **Asset integrity:** production wordmark SHA-256 recorded; deterministic extraction (no generative redraw of lettering); live public assets matched byte-for-byte on promotion.
- **Promotion discipline:** versioned image `roms:landing-accepted-daedd2c`, pre-landing rollback image retained, MariaDB/tunnel untouched, app-only recreation, post-promotion health 200.
- **Responsive intent:** desktop / landscape / portrait compositions rather than pure scale; live desktop inspection shows restaurant identity dominant, ARCWorks secondary, contrast on glass card adequate.
- **Accessibility notes in work log:** labeled fields, password visibility control, hidden inactive error UI without false overflow.

### Residual / low findings

| ID | Severity | Finding |
| --- | --- | --- |
| L1 | Low | Full solution test often recorded as “pending” due to runner timeout — prefer bounded project-level suites plus dedicated browser smoke in CI |
| L2 | Low | README still describes an “active isolated UI design handoff” for landing; should be rewritten as historical after merge |
| L3 | Info | Public hostname remains `roms.arkworksph.online` while product name is ARCWorks Restaurant Suite — acceptable if branding doc remains authoritative |

**Independent live check (2026-08-12):** `https://roms.arkworksph.online/Account/Login` returned the accepted composition: Chef Doy's gold wordmark, star/atmosphere background, glass Staff login card, username/password fields, Log in control, ARCWorks mark top-right. No attempt was made to authenticate with production credentials.

---

## 5. Ops / recovery assessment

The 2026-08-08 drill is a **material closure** of Audit #5’s “restore evidence missing” item **for same-PC isolated restore**:

- Config path error found and corrected before acceptance snapshots.
- Two corrected snapshots; ideal restore and interrupted-restore recovery both zero manifest mismatches.
- MariaDB 24 tables / PostgreSQL 203 tables validated in disposable containers.
- Live project, live volumes, tunnel never overwritten.

**Remaining ops gate:** “Application can be restored and run on a different machine” is still explicitly out of scope of that drill. Treat cross-PC recovery as **High** for private-beta claim, not as optional polish.

---

## 6. PR #13 — Waiter shell & account security (pre-merge note)

| Topic | Assessment |
| --- | --- |
| Username archive on soft-remove | Sound; preserves history while allowing name reuse |
| `MustChangePassword` + server middleware | Strong compliance control for admin-provisioned accounts |
| Waiter nav limited to Dashboard + Tables & Orders | Aligns with role focus |
| Tables & Orders content frozen | Correct phase boundary |
| CI / browser assertion drift | Work log notes stale “My attendance” assertions fixed to “My dashboard” — confirm green `verify` before merge |
| Live deploy | Not yet; preview-only — appropriate |

**Recommendation:** Merge when CI is green. Do not treat merge as workflow-beta acceptance.

---

## 7. Security re-check

- AI hold remains the production default.
- Landing promotion did not alter DB or tunnel identity.
- Soft-remove + archive path (PR #13) improves account lifecycle without hard-delete of audit trails.
- Forced password change closes a common staff-onboarding gap.
- No evidence of AI write authority or recipe/auto-deduct reintroduction.

Highest residual risk remains **operating without supervised role acceptance** or **without cross-host recovery proof**.

---

## 8. Recommended near-term sequence

1. Merge PR #13 after green verify; update README landing handoff language.  
2. Schedule **cross-PC restore drill** and file evidence under `docs/evidence/`.  
3. Run supervised four-role acceptance; tick contract scenarios with dated notes.  
4. Load restaurant-approved data under change control.  
5. Only then limited private beta.  
6. AI remains future-version until a new threat model and audit reopen it.

---

## 9. Outcome

**Conditionally accepted.**

The product now has a **credible, owner-accepted, live staff-login face** without sacrificing Identity controls. Local backup restore is evidenced. Audit timeline integrity on `main` is restored. Private-beta readiness still depends on **people proving the workflow contract** and **runtime recovery on a second host** — not on further landing polish.

**Do not** claim public production readiness, enable AI, or expand scope into recipes/auto-deduct until mandatory items in §1 are closed.
