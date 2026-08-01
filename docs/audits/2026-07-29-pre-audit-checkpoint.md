# External Audit #1 — Pre-audit Checkpoint

**Date:** 2026-07-29  
**Auditor:** Independent third-party review (Grok / xAI conversational audit)  
**Branch / checkpoint:** `agent/pre-audit-checkpoint` (PR #1), subsequently merged to `main`  
**Commit context:** PR #1 merge `11a719a3755d4090b4770843809b584a110c88ca`

## Purpose

Independent architecture, security, and product review before further inventory expansion or LLM integration, requested because production mistakes could cause real sales loss or customer disruption.

## Scope reviewed

- README and feature matrix
- Domain model and order lifecycle
- Inventory feature flag, reversal handling, and reconciliation approach
- Docker Compose isolation for the AI lab
- Command protocol and fail-closed deterministic validation
- Operations and failover documentation
- Reported test results (25/25 at checkpoint)

## Executive summary

ROMS was assessed as a solid foundation for a single-location, touch-first internal operations system, with unusually careful inventory safety thinking and AI isolation for its size. It was **not** judged production-ready for full inventory enablement or AI features without further hardening.

### Strengths

- Clear Admin / Waiter / Kitchen role separation
- Order lifecycle with price/name snapshots, revisions, idempotent submit, audit trail
- Inventory feature-gated and default-disabled
- Reversal handling for cancellations and post-preparation amendments underway
- Operational docs (failover runbook, backup expectations, health monitoring)
- AI lab isolated; TinyLlama rejected on accuracy despite fail-closed safety layer

### Critical gaps identified

1. Limited automated coverage for concurrency, real MariaDB, SignalR, browser, and failure recovery
2. Inventory still required a supervised pilot before enablement
3. Payment remained manual confirmation only
4. Resilience procedures needed live drills
5. AI path correctly disconnected but not ready for UI integration

## Prioritized recommendations (Audit #1)

**Before inventory enablement or AI expansion**

1. Expand tests: concurrency, real MariaDB, SignalR, full order lifecycle including amendments/cancellations
2. Supervised inventory pilot with real recipes and opening stock on non-production data
3. Live-drill failover runbook and backup restore
4. Document and enforce negative-stock policy and unit consistency
5. Keep AI lab fully disconnected until a much stronger model and confirmation UX exist

**Do not**

- Merge and immediately enable inventory or connect the command gateway to the main app
- Treat TinyLlama results as user-integration readiness

## Outcome

**Conditionally positive.** Proceed with inventory hardening and test expansion; keep all activation gates closed. PR #1 was appropriate as an audit checkpoint and was later merged.

## Follow-up

Findings from this audit drove the inventory-readiness work tracked in PR #2 (`agent/inventory-readiness`) and are re-evaluated in Audit #2 (2026-08-02).
