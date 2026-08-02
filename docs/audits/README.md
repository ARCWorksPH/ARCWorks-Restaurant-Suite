# ROMS External Audit Timeline

Independent third-party audits of ROMS, ordered chronologically. Audits are advisory. They do not authorize production inventory enablement, restaurant data acceptance, or AI integration.

| # | Date | Document | Scope | Outcome |
| --- | --- | --- | --- | --- |
| 1 | ~2026-07-26 | [2026-07-26 Baseline maturity](2026-07-26-baseline-maturity.md) | Early main baseline (~2 commits); feature completeness; phased productionize plan | Strong beta baseline; validate before inventory; phased roadmap |
| 2 | 2026-07-30 | [2026-07-30 Architecture security product](2026-07-30-architecture-security-product.md) | main + PR #1 pre-audit checkpoint; live production notes; AI lab; ops | Solid operational core; P0 stabilize production; inventory pilot gated; AI isolated |
| — | 2026-07-29 | [2026-07-29 Pre-audit checkpoint note](2026-07-29-pre-audit-checkpoint.md) | Concurrent checkpoint review of PR #1 (same era as Audit 2) | Conditionally positive; concurrency/real-MariaDB/browser tests required |
| — | 2026-07-30 | [EXTERNAL_AUDIT_HANDOFF](../EXTERNAL_AUDIT_HANDOFF_2026-07-30.md) | Project-prepared inventory preflight handoff | Decision requested from external reviewer |
| 3 | 2026-08-02 | [2026-08-02 Inventory readiness](2026-08-02-inventory-readiness.md) | Full re-audit of `agent/inventory-readiness` (PR #2) vs Audits 1–2 | Material technical progress accepted; supervised pilot still blocked by restaurant data + human gates |

## Numbering note

Audits **1** and **2** come from prior independent review threads (exported into the project). The 2026-07-29 checkpoint note and the 2026-08-02 inventory-readiness review were produced in a later thread; **2026-08-02 is Audit #3**, not #2.

The legacy file `docs/3rd Party Audit 07-28-20206.docx` is a saved copy of the PR #1 checkpoint-style review (content aligns with the 2026-07-29 note / Audit 2 era). Prefer this Markdown timeline going forward.

## How to use

1. Read the newest audit first (Audit 3).
2. Treat every **Mandatory remediation** item as a merge/pilot blocker unless waived in a later audit.
3. Keep `Features__Inventory__Enabled=false` in the active deployment until restaurant confirmation, external-audit acceptance of the pilot plan, and supervised multi-device acceptance are complete.
4. Keep the AI lab disconnected from the ROMS UI and operational database.

## Related operational evidence

- `docs/WORK_LOG.md`
- `docs/INVENTORY_REVERSAL_RULES.md`
- `docs/INVENTORY_OPERATIONS.md`
- `docs/SYNTHETIC_RESILIENCE_TESTING_2026-07-30.md`
- `docs/MARIADB_DEADLOCK_INCIDENT_2026-07-30.md`
- `docs/AI_SECURITY_BOUNDARY.md`
- `docs/AI_COMMAND_PROTOCOL.md`
