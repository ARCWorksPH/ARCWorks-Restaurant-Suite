# External Audit #1 — Baseline Maturity & Productionize Plan

**Date:** ~2026-07-26 (project established 26 Jul 2026; ~2 commits on main at time of review)  
**Source:** Independent third-party review thread (exported as `Audit 1.docx`)  
**Scope:** Early main-branch baseline — feature completeness, architecture maturity, recommended path to production

## Current stage summary

ROMS is a well-structured, production-oriented single-location restaurant workflow system in a **mature beta / early production-ready baseline** stage.

| Area | Status | Notes |
| --- | --- | --- |
| Core ordering & KDS | Complete | Draft → New → Preparing → Ready → Completed; idempotent submit; price/name snapshots; amendments; SignalR; audit history |
| Roles & UI | Complete | Admin / Waiter / Kitchen; touch-first Blazor Interactive Server |
| Tables, Menu, Users | Complete | Admin catalog & user management |
| Reporting | Basic complete | Completed order value, counts, averages, best-sellers |
| Attendance | Complete (lightweight) | Clock-in/out, schedules, present list, weekly hours, audited corrections, 7-day CSV. Excludes payroll/leave/OT/biometrics |
| Inventory | Feature-gated (off by default) | Ledger, recipes, low-stock, transactional deduction on Preparing. Ready for pilot once units/recipes/opening stock signed off |
| Payment | Basic | Pending-payments page + ConfirmPayment (no gateway) |
| Ops / Deploy | Strong | Docker Compose (MariaDB 11.4 + app + Caddy + Gatus), EF migrations + seed, encrypted backup, health checks, manual failover runbook |
| Tests | Present | Domain unit + integration tests |
| CI | Present | `.github/workflows/ci.yml` |
| Docs | Minimal but solid | README, OPERATIONS.md, FAILOVER_RUNBOOK.md |
| Issues / Roadmap | None | Empty issues; no open bugs or feature trackers visible |

**Tech stack:** .NET 10, Blazor Interactive Server, ASP.NET Core Identity, EF Core 10, SignalR, MariaDB 11.4. Clean layered architecture (Domain / Application / Infrastructure / Web). Domain model enforces invariants on status transitions, amendments, inventory movements, and attendance corrections.

This is **not** a green-field or early-prototype project. The core restaurant loop (waiter → kitchen → complete → payment confirm) plus attendance and ops scaffolding is implemented and intended for real single-location use.

## Best way to continue

1. Validate the baseline immediately (highest priority).
2. Pilot in a real (or realistic) environment with inventory still off.
3. Harden and close gaps that matter for daily operations.
4. Only then enable and refine inventory; add payment gateways / receipts / multi-device polish.
5. Treat everything after the current baseline as incremental, measured improvements — not large rewrites.

## Recommended build plan

### Phase 0 – Baseline validation (1–3 days)

- Clone, configure `.env`, `docker compose up --build -d`.
- Full happy-path: tables/menu/users → draft → submit → kitchen → complete → confirm payment → reports & audit.
- Exercise attendance; run `dotnet test`; verify health, backup dry-run, inventory stays disabled.
- Document UI friction.

### Phase 1 – Operational hardening (1–2 weeks)

- Expand integration/E2E tests (order lifecycle, amendments, concurrent kitchen, attendance).
- Structured logging; tablet UX polish; receipt/print support.
- Security: rate-limit login, session timeout, Data Protection keys.
- Day-1 restaurant checklist.

### Phase 2 – Inventory pilot (1–2 weeks, after Phase 1)

- Enable inventory only in staging/pilot.
- Define units, recipes, opening stock with owner.
- Validate deduction, low-stock, adjustments, reversals.
- Keep feature flag for instant off.

### Phase 3 – Payment & commercial (2–4 weeks)

- Payment provider or manual confirm + external terminal.
- Optional split bills / tips; optional QR order; localization if needed; richer exports.

### Phase 4 – Production readiness (ongoing)

- Pin images by digest; staging → production; automate backup + quarterly restore; practice failover; monitoring/on-call; changelog process.

## Explicit non-goals (for now)

- Multi-location / franchise
- Full HR/payroll
- Complex loyalty or delivery platforms
- Heavy analytics / BI

## Suggested next concrete actions

1. Run Docker stack and walk every page with realistic data.
2. Create a project board with the phases as milestones.
3. Prioritize UX friction from Phase 0.
4. Decide inventory enablement criteria with restaurant operators.
5. Extend the current domain model — do not rewrite.

## Outcome

**Strong beta baseline.** Project is further along than most open-source restaurant systems of this type. Treat main as the beta baseline, validate thoroughly, then close operational and commercial gaps in the order listed.
