# ROMS — Restaurant Order Management System

ROMS is a single-location, touch-first restaurant workflow for waiters, kitchen staff, and administrators. It uses .NET 10 Blazor Interactive Server, ASP.NET Core Identity, EF Core 10, SignalR, and MariaDB 11.4.

## Implemented

- Role-protected Admin, Waiter, and Kitchen experiences.
- Table status, draft ordering, price/name snapshots, idempotent submission, KDS status flow, waiter completion, audit history, and real-time updates.
- Menu/table/user administration and completed order-value reporting.
- Feature-gated inventory ledger, recipes, low-stock balances, and transactional deduction on Preparing.
- MariaDB migration, Docker Compose deployment, HTTPS reverse proxy, health monitoring, encrypted-backup script, and manual failover runbook.

## Run locally with Docker

1. Install Docker Engine with Compose.
2. Copy `.env.example` to `.env` and replace every password and hostname.
3. For localhost-only testing, set `ROMS_HOST=localhost`.
4. Run `docker compose up --build -d`.
5. Open the configured HTTPS hostname and sign in with `ADMIN_USERNAME` and `ADMIN_PASSWORD`.

The first start applies EF migrations and creates the three roles plus the initial administrator. Inventory defaults to disabled.

## Lightweight attendance

Every active staff account has a **My Attendance** page for explicit clock-in and clock-out plus upcoming schedules and recent hours. Administrators use **Staff Schedule** to add non-overlapping shifts, see who is currently present, review weekly hours, correct records with a mandatory audited reason, and export a seven-day CSV. This module intentionally excludes payroll, leave, overtime approval, biometrics, and other employee-management functions.

## Build and test without Docker

Install the .NET 10 SDK and point `ConnectionStrings__DefaultConnection` to a MariaDB 11.4 instance, then run:

```powershell
dotnet user-secrets set "Seed:AdminPassword" "<strong-local-only-password>" --project src/Roms.Web
dotnet restore Roms.slnx
dotnet test Roms.slnx -m:1
dotnet run --project src/Roms.Web
```

Keep local passwords in .NET user secrets or environment variables. Do not add a real `.env` file or password to source control.

See `docs/OPERATIONS.md` and `docs/FAILOVER_RUNBOOK.md` before a production rollout.
