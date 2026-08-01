# External Audit #2 — Architecture, Security & Product

**Date:** 2026-07-30  
**Source:** Independent third-party review thread (exported as `Audit 2.docx`)  
**Primary sources:** main branch + `agent/pre-audit-checkpoint` PR #1 (docs, work log, compose, AI lab); live production notes in WORK_LOG.md

Clean-room review of documented system, code structure, deployment model, and operational state. Intended to support roadmap re-assessment with accelerated development.

## 1. Executive summary

ROMS is a mature single-location, touch-first **operational** system (not a customer-facing ordering app). It is past MVP and into usable beta / early production for a Philippine restaurant:

- Core order → kitchen → payment → reporting works end-to-end.
- Real-time SignalR, role isolation, attendance, feature-gated inventory present.
- Production live behind Cloudflare Tunnel (`roms.gbserverph.online`), MariaDB 11.4, health checks, recent recovery/hardening.
- Strong AI lab security (isolated, fail-closed).
- Inventory correctly disabled until real data is loaded.

**Overall maturity:** Solid operational core + thoughtful ops/AI boundaries. Biggest gaps: production hardening depth, inventory pilot readiness, observability/alerting completeness, clear prioritised roadmap beyond the checkpoint.

**Risk level:** Medium-low for continued single-site use; higher if inventory or AI is enabled without documented gates.

## 2. Architecture & tech stack

| Layer | Implementation | Assessment |
| --- | --- | --- |
| UI | Blazor Interactive Server (.NET 10) | Excellent for touch/waiter pads & KDS |
| Auth | ASP.NET Core Identity + Admin/Waiter/Kitchen | Correct role-gated pages |
| Domain | Domain → Application → Infrastructure → Web + CommandGateway | Good separation; explicit contracts |
| Data | EF Core 10 + Pomelo + MariaDB 11.4 (GTID-ready) | Solid for single-location + future replication |
| Realtime | SignalR | Appropriate |
| Deploy | Docker Compose, loopback app port, optional Caddy, Cloudflare Tunnel | Production-minded |
| AI | Isolated Ollama + non-executing CommandGateway | Exemplary isolation |

**Strengths:** Inventory feature flag; price/item snapshots; idempotent submit; revision/versioning; inventory reversal for cancel/amend; data-protection keys; non-root container; Manila business-date reporting.

**Weaknesses / risks:** Blazor Interactive Server process affinity under heavy concurrent load; no multi-instance story (OK for single-location if documented); production host Git state reported unusable in work log — deployment risk.

## 3. Feature completeness

Verified in production acceptance: role experiences; table board; draft → kitchen → served; admin payment confirm; Manila reporting; menu/table/user admin; attendance; inventory setup UI (deduction gated); health, Gatus, backup script, failover runbook.

**Intentionally out of scope (good discipline):** payroll, leave, biometrics, multi-location, customer-facing ordering, full POS hardware.

## 4. Security & trust boundaries

**Strong:** App on `127.0.0.1:7070`; Cloudflare Tunnel edge; internal backend network; AI lab disconnected from DB, no credentials/host ports, read-only root, dropped capabilities, resource limits, fail-closed deterministic validation; protocol “database owns the truth”; TinyLlama rejected; public Adminer removed.

**Gaps:** Rate limiting / account lockout / failed-login alerting detail; data-protection key rotation/backup; Cloudflare Tunnel config outside repo (document least-privilege); quarterly restore documented but not yet evidenced as run.

## 5. Operations & reliability

Production (WORK_LOG 2026-07-29): `arcworks-resto` healthy; demo seed data; two paid demo orders; inventory empty and disabled; pre-recovery backup with SHA-256.

Ops baseline good but thin: hardware minimums; DNS split; image digest pinning recommended; daily encrypted backups; manual failover (never dual-write).

**Missing:** Wired alerting (PagerDuty/Telegram/email); Prometheus/Grafana or log aggregation beyond Gatus; cloud standby replication not confirmed running; no chaos tests.

## 6. AI laboratory

Clean isolated experiment: narrow protocol v1; original-text evidence for writes; gateway cannot mutate data; exact vs safety metrics separated.

**Verdict:** Keep lab isolated. Do not connect to UI/inventory until stronger model + Taglish/voice corpus + human confirmation. TinyLlama correctly rejected.

## 7. Recommended roadmap (4–8 weeks)

**P0 – Stabilize production (1–2 weeks)**  
Restore clean Git / publish checkpoint; pin image digest; first quarterly restore test (record RPO/RTO); wire health + missed-backup alerts; document Tunnel hardening.

**P1 – Inventory pilot (2–3 weeks)**  
Load real items/units/balances/recipes; supervised sample order → amend → cancel watching stock; only then enable flag; reconciliation report + low-stock UI.

**P2 – Observability & resilience**  
Structured logging + correlation IDs; Blazor circuit limits; document/implement cloud standby.

**P3 – Product polish**  
Touch UX under concurrent load; expanded reporting; optional offline/PWA for KDS.

**P4 – AI (later)**  
Only after inventory stable and better model evaluated; keep gateway non-executing; human confirmation + audit before any write path.

**Avoid:** multi-location, customer self-order, full POS hardware, payroll, voice ordering, connecting AI lab.

## 8. Quick health checklist

- [ ] Merge/publish pre-audit-checkpoint cleanly
- [ ] Image digest pinned + documented
- [ ] First restore test completed and logged
- [ ] Inventory master data loaded and verified offline
- [ ] Alerting live for `/health` and backups
- [ ] AI lab remains profile-gated and disconnected

## Outcome

**Strong place for a single-site system.** Careful inventory gating, isolated AI lab, and production recovery show good discipline. Highest-leverage work: close operational and inventory pilot gaps rather than add features.
