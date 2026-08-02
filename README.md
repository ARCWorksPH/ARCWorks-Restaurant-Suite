# ROMS — Restaurant Order Management System

ROMS is a single-location, touch-first restaurant workflow for waiters, kitchen staff, and administrators. It uses .NET 10 Blazor Interactive Server, ASP.NET Core Identity, EF Core 10, SignalR, and MariaDB 11.4.

## Implemented

- Role-protected Admin, Waiter, and Kitchen experiences.
- Table status, draft ordering, price/name snapshots, idempotent submission, KDS status flow, waiter completion, audit history, and real-time updates.
- Menu/table/user administration and completed order-value reporting.
- Independent-item inventory ledger, low-stock balances, structured receiving, witnessed physical-count reconciliation, protected adjustments, and waste/spoilage approvals.
- MariaDB migration, Docker Compose deployment, HTTPS reverse proxy, health monitoring, encrypted-backup script, and manual failover runbook.

## Run locally with Docker

1. Install Docker Engine with Compose.
2. Copy `.env.example` to `.env` and replace every password and hostname.
3. For localhost-only testing, set `ROMS_HOST=localhost`.
4. Run `scripts\Initialize-ProductionEnv.ps1` to create a protected production
   `.env`, or configure `.env` manually from `.env.example`.
5. Run `docker compose up --build -d`.
6. Open the configured Cloudflare Tunnel hostname and sign in with
   `ADMIN_USERNAME` and `ADMIN_PASSWORD`. The optional direct-Caddy edge can be
   started with the `direct-https` Compose profile.

The first start applies EF migrations and creates the three roles plus the initial administrator. Inventory is a manual, independent-item ledger; orders do not deduct ingredients.

## Lightweight attendance

Every active staff account has a **My Attendance** page for explicit clock-in and clock-out plus upcoming schedules and recent hours. Administrators use **Staff Schedule** to add non-overlapping shifts, see who is currently present, review weekly hours, correct records with a mandatory audited reason, and export a seven-day CSV. This module intentionally excludes payroll, leave, overtime approval, biometrics, and other employee-management functions.

## Build and test without Docker

Install the .NET 10 SDK and point `ConnectionStrings__DefaultConnection` to a MariaDB 11.4 instance, then run:

```powershell
dotnet restore Roms.slnx
dotnet test Roms.slnx -m:1
dotnet run --project src/Roms.Web
```

See `docs/OPERATIONS.md`, `docs/FAILOVER_RUNBOOK.md`, and `docs/WORK_LOG.md`
before a production rollout or inventory enablement.

## Feature-gated read-only assistant lab

The optional `ai-lab` profile runs a private Ollama service and command
interpreter. The model and gateway have no database network or credentials.
The authenticated ROMS app can submit a bounded catalog for interpretation,
then execute only an approved permission-checked read and format the database
facts itself. AI writes and arbitrary SQL do not exist.

```powershell
docker compose --profile ai-lab up -d ollama command-gateway
scripts\Evaluate-CommandGateway.ps1
```

`AI_ENABLED` defaults to `false`. See `docs/AI_FUNCTIONS.md`,
`docs/AI_COMMAND_PROTOCOL.md`, and `docs/AI_SECURITY_BOUNDARY.md` before
enabling or extending the lab.
