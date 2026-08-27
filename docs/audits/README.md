# ARCWorks Restaurant Suite — External Audit Timeline

Independent third-party audits, ordered chronologically. Audits are advisory.
They do not authorize public production rollout, multi-tenant hosting, inventory
pilot enablement beyond documented human gates, or AI release from hold.

**Repository:** [ARCWorksPH/ARCWorks-Restaurant-Suite](https://github.com/ARCWorksPH/ARCWorks-Restaurant-Suite)  
**Prior name:** `xXGunborgXx/GBServerPH-Restaurant-Ordering-System` (history preserved)  
**Internal code name:** ROMS (namespaces, migrations, compatibility keys)

| # | Date | Document | Scope | Outcome |
| --- | --- | --- | --- | --- |
| 1 | ~2026-07-26 | [2026-07-26 Baseline maturity](2026-07-26-baseline-maturity.md) | Early main baseline; phased productionize plan | Strong beta baseline |
| 2 | 2026-07-30 | [2026-07-30 Architecture security product](2026-07-30-architecture-security-product.md) | main + PR #1; live prod; AI lab; ops | Solid core; P0 stabilize; AI isolated |
| — | 2026-07-29 | [2026-07-29 Pre-audit checkpoint note](2026-07-29-pre-audit-checkpoint.md) | Concurrent PR #1 checkpoint | Conditionally positive |
| — | 2026-07-30 | [EXTERNAL_AUDIT_HANDOFF](../EXTERNAL_AUDIT_HANDOFF_2026-07-30.md) | Inventory preflight handoff | Decision requested |
| 3 | 2026-08-02 | [2026-08-02 Inventory readiness](2026-08-02-inventory-readiness.md) | Pre-merge inventory-readiness (recipe-era) | Technical controls accepted; pilot gated |
| 4 | 2026-08-03 | [2026-08-03 QA compliance post-merge](2026-08-03-qa-compliance-post-merge.md) | Recipe removal; manual ledger; security harden | Conditionally accepted; AI off |
| 5 | 2026-08-08 | [2026-08-08 ARCWorks Suite rebrand and workflow freeze](2026-08-08-arcworks-suite-rebrand-workflow.md) | Org/repo rename; AI hold; four-role contract; backup; portable | Conditionally accepted for private-beta prep |
| 6 | 2026-08-12 | [2026-08-12 Landing page and ops progress](2026-08-12-landing-ops-progress.md) | Chef Doy's landing live; restore drill evidence; PR #13 waiter shell | Landing accepted; local restore closed; staff/workflow acceptance still open |

## Next independent review

Audit #6 has been requested against the Waiter account-security and Gate 0
checkpoint branch. The request is documented in
[`AUDIT_6_HANDOFF_2026-08-12.md`](AUDIT_6_HANDOFF_2026-08-12.md). It is not a
completed audit and must not be added to the numbered table until the
independent report is submitted and reviewed.

## How to use

1. Read the newest audit first (Audit 6).
2. Treat **Mandatory remediation** items as private-beta / production blockers unless waived later.
3. Keep `AI_HOLD=true` (cannot be bypassed by a stale `AI_ENABLED=true`).
4. Inventory remains a **manual independent-item ledger**; recipes stay out of scope.
5. Human CEO/Product Manager remains the only production enablement authority.

## Related evidence

- `docs/WORK_LOG.md`
- `docs/PROJECT_TIMELINE.md`
- `docs/ROADMAP_2026-08-06.md`
- `docs/WORKFLOW_CONTRACT_2026-08-06.md`
- `docs/AI_HOLD.md`
- `docs/UI/LANDING_PAGE_ACCEPTANCE_2026-08-12.md`
- `docs/evidence/BACKUP_RECOVERY_DRILL_2026-08-08.md`
- `docs/SECURITY_HARDENING_2026-08-02.md`
