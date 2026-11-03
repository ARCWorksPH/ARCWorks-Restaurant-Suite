# ARCWorks Restaurant Suite

ARCWorks Restaurant Suite is a single-location, touch-first restaurant workflow for waiters, kitchen staff, and administrators. Its internal code name remains ROMS for namespaces, database migrations, and compatibility-sensitive tooling. It uses .NET 10 Blazor Interactive Server, ASP.NET Core Identity, EF Core 10, SignalR, and MariaDB 11.4.

## Implemented

- Role-protected Admin, Waiter, and Kitchen experiences.
- Table status, draft ordering, price/name snapshots, idempotent submission, KDS status flow, waiter completion, audit history, and real-time updates.
- Menu/table/user administration and completed order-value reporting.
- Independent-item inventory ledger, low-stock balances, structured receiving, witnessed physical-count reconciliation, protected adjustments, and waste/spoilage approvals.
- MariaDB migration, Docker Compose deployment, HTTPS reverse proxy, health monitoring, encrypted-backup script, and manual failover runbook.

## Run locally with Docker

1. Install Docker Engine with Compose.
2. Copy `deploy/portable/instance.example.psd1` to an instance manifest and
   choose unique instance, project, hostname, port, and database-server values.
3. Run `scripts\Initialize-ProductionEnv.ps1` with the manifest values to create
   a protected, ignored `.env`; it generates fresh database and administrator
   secrets without reading a legacy database environment file.
4. Create `.secrets\cloudflare-tunnel-token` only when the edge-tunnel profile
   is being enabled. Keep one token per instance.
5. Run `docker compose up --build -d`.
6. Open the configured Cloudflare Tunnel hostname and sign in with
   `ADMIN_USERNAME` and `ADMIN_PASSWORD`. The optional direct-Caddy edge can be
   started with the `direct-https` Compose profile.

For the workstation portfolio route, add `-f compose.portfolio.yaml`. Do not
use that override for a restaurant-only VM. See `deploy/portable/README.md`
before cloning an instance.

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

See `docs/OPERATIONS.md`, `docs/FAILOVER_RUNBOOK.md`,
`deploy/portable/README.md`, and `docs/WORK_LOG.md`
before a production rollout or inventory enablement.

## Future-version AI lab (currently held)

The optional `ai-lab` profile preserves a private Ollama service, command
interpreter, contracts, and benchmark harness for a future release. The
current application is deliberately disconnected: it does not join the
`command` network, register a gateway HTTP client, or expose the Assistant
route. `AI_HOLD=true` is the fail-closed default and cannot be bypassed by a
stale `AI_ENABLED=true` value. See [docs/AI_HOLD.md](docs/AI_HOLD.md).

```powershell
docker compose --profile ai-lab up -d ollama command-gateway
scripts\Evaluate-CommandGateway.ps1
```

`AI_ENABLED` defaults to `false` and `AI_HOLD` defaults to `true`. See `docs/AI_FUNCTIONS.md`,
`docs/AI_COMMAND_PROTOCOL.md`, and `docs/AI_SECURITY_BOUNDARY.md` before
changing the hold or extending the lab.
