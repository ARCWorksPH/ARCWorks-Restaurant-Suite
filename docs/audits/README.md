# ROMS External Audit Timeline

This directory records independent third-party audits of ROMS so progress, residual risk, and activation gates remain visible over time.

Audits are advisory. They do not authorize production inventory enablement, restaurant data acceptance, or AI integration.

| Date (UTC context) | Document | Scope | Outcome |
| --- | --- | --- | --- |
| 2026-07-29 | [2026-07-29 Pre-audit checkpoint](2026-07-29-pre-audit-checkpoint.md) | Architecture, security, inventory reversal baseline, isolated AI lab after PR #1 | Conditionally positive; inventory and AI remain gated; concurrency / real-MariaDB / browser tests required |
| 2026-07-30 | [EXTERNAL_AUDIT_HANDOFF_2026-07-30.md](../EXTERNAL_AUDIT_HANDOFF_2026-07-30.md) | Inventory activation preflight handoff prepared by project | Decision requested; technical preflight implemented |
| 2026-08-02 | [2026-08-02 Inventory readiness](2026-08-02-inventory-readiness.md) | Full re-audit of `agent/inventory-readiness` (PR #2) against prior findings | Material progress accepted; supervised pilot still blocked by restaurant data + human gates |

## How to use

1. Read the newest audit first.
2. Treat every **Mandatory remediation** item as a merge/pilot blocker unless explicitly waived in a later audit.
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
