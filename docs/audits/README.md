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
| 5 | 2026-08-08 | [2026-08-08 ARCWorks Suite rebrand and workflow freeze](2026-08-08-arcworks-suite-rebrand-workflow.md) | Org/repo rename; AI hold; four-role contract; backup; portable; UI; open PRs | Conditionally accepted for private-beta prep; staff acceptance + restore drill still open |

## How to use

1. Read the newest audit first (Audit 5).
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
- `docs/SECURITY_HARDENING_2026-08-02.md`
- `docs/OPERATIONS.md`
- `deploy/portable/` (when present on the reviewed branch)
