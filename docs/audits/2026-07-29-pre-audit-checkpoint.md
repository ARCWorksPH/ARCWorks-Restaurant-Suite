# Concurrent note — PR #1 Pre-audit Checkpoint Review

**Date:** 2026-07-29  
**Auditor:** Independent third-party review (Grok conversational audit)  
**Branch / checkpoint:** `agent/pre-audit-checkpoint` (PR #1), later merged to `main`  
**Relation to formal series:** Same era as **Audit #2** (2026-07-30). Not a separate numbered audit in the corrected timeline; retained for traceability and because `docs/3rd Party Audit 07-28-20206.docx` preserves related content.

## Purpose

Independent architecture, security, and product review before further inventory expansion or LLM integration.

## Executive summary

Solid foundation for a single-location, touch-first internal ops system, with mature thinking on inventory safety and AI isolation. **Not** production-ready for full inventory enablement or AI features without further hardening and testing.

### Strengths

- Clear Admin / Waiter / Kitchen role separation
- Order lifecycle with price/name snapshots, revisions, idempotent submit, audit trail
- Inventory feature-gated and default-disabled; reversal handling underway
- Operational docs (failover, backups, health)
- AI lab isolated; TinyLlama rejected on accuracy despite fail-closed safety layer

### Critical gaps identified

1. Limited automated coverage for concurrency, real MariaDB, SignalR, browser, failure recovery
2. Inventory required supervised pilot before enablement
3. Payment remained manual confirmation only
4. Resilience procedures needed live drills
5. AI path correctly disconnected but not ready for UI integration

## Prioritized recommendations

1. Expand tests: concurrency, real MariaDB, SignalR, full order lifecycle
2. Supervised inventory pilot with real recipes and opening stock
3. Live-drill failover and backup restore
4. Document and enforce negative-stock policy and unit consistency
5. Keep AI lab disconnected until stronger model + confirmation UX

**Do not** merge and immediately enable inventory or connect the command gateway; do not treat TinyLlama as user-integration ready.

## Outcome

**Conditionally positive.** Findings drove inventory-readiness work in PR #2 and are re-evaluated in **Audit #3** (2026-08-02).
