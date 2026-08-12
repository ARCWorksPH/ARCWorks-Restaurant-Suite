# ROMS External Audit Timeline

Independent third-party audits of ROMS, ordered chronologically. Audits are advisory. They do not authorize production inventory enablement, restaurant data acceptance, or AI production enablement.

| # | Date | Document | Scope | Outcome |
| --- | --- | --- | --- | --- |
| 1 | ~2026-07-26 | [2026-07-26 Baseline maturity](2026-07-26-baseline-maturity.md) | Early main baseline; feature completeness; phased productionize plan | Strong beta baseline; validate before inventory |
| 2 | 2026-07-30 | [2026-07-30 Architecture security product](2026-07-30-architecture-security-product.md) | main + PR #1; live prod notes; AI lab; ops | Solid core; P0 stabilize; inventory pilot gated; AI isolated |
| — | 2026-07-29 | [2026-07-29 Pre-audit checkpoint note](2026-07-29-pre-audit-checkpoint.md) | Concurrent PR #1 checkpoint (same era as Audit 2) | Conditionally positive |
| — | 2026-07-30 | [EXTERNAL_AUDIT_HANDOFF](../EXTERNAL_AUDIT_HANDOFF_2026-07-30.md) | Project inventory preflight handoff | Decision requested |
| 3 | 2026-08-02 | [2026-08-02 Inventory readiness](2026-08-02-inventory-readiness.md) | PR #2 pre-merge inventory-readiness (recipe-era) | Technical controls accepted; pilot blocked on data + human gates |
| 4 | 2026-08-03 | [2026-08-03 QA compliance post-merge](2026-08-03-qa-compliance-post-merge.md) | Post-merge main: recipe removal, manual inventory, read-only AI, security harden | Core ops path clearer; AI still disabled; staff beta + restore drill remain |

## How to use

1. Read the newest audit first (Audit 4).
2. Treat **Mandatory remediation** items as pilot/production blockers unless waived in a later audit.
3. Keep `Features__Inventory` / automatic deduction assumptions aligned with current product: **manual independent-item ledger only**; recipes are out of scope.
4. Keep `AI_ENABLED=false` until adversarial/multilingual acceptance and explicit product sign-off.
5. Human CEO/Product Manager (GUNBORG) remains the only authority for production enablement decisions.

## Related evidence

- `docs/WORK_LOG.md`
- `docs/SECURITY_HARDENING_2026-08-02.md`
- `docs/ROADMAP_2026-08-06.md`
- `docs/WORKFLOW_CONTRACT_2026-08-06.md`
- `docs/ROADMAP_2026-08-02.md`
- `docs/AI_ROLE_AND_SCOPE_POLICY.md`
- `docs/AI_FUNCTIONS.md`
- `docs/AI_SECURITY_BOUNDARY.md`
- `docs/INVENTORY_OPERATIONS.md`
