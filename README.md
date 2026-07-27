# ROMS — Restaurant Order Management System

[![Framework: .NET 10](https://img.shields.io/badge/Framework-.NET%2010-purple.svg)](https://dotnet.microsoft.com/)
[![UI: Blazor Interactive](https://img.shields.io/badge/UI-Blazor%20Interactive-512BD4.svg)](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor)
[![Database: MariaDB 11.4](https://img.shields.io/badge/Database-MariaDB%2011.4-003545.svg)](https://mariadb.org/)
[![Container: Docker](https://img.shields.io/badge/Container-Docker%20Compose-2496ED.svg)](https://www.docker.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-amber.svg)](LICENSE)

**ROMS** (Restaurant Order Management System) is a single-location, touch-first restaurant operational workflow system designed for waiters, kitchen staff, and administrators. Built with .NET 10 Blazor Interactive Server, ASP.NET Core Identity, EF Core 10, SignalR real-time updates, and MariaDB 11.4.

---

## System Feature Matrix

| Module | Features & Capabilities |
| :--- | :--- |
| **Role-Protected Workflows** | Role-gated experiences for `Admin`, `Waiter`, and `Kitchen` staff accounts. |
| **Interactive Order Flow** | Dynamic table status, draft order building, price/item snapshots, idempotent submission, and KDS (Kitchen Display System) status flow. |
| **Real-Time Synchronization** | SignalR web sockets for instant order updates across waiter pads, kitchen displays, and admin terminals. |
| **Inventory Ledger** | Feature-gated inventory ledger, recipe mapping, low-stock alerts, and transactional stock deduction on order preparation. |
| **Staff Attendance** | Touch-friendly clock-in / clock-out interface, shift scheduling, weekly hours tracking, audited manual corrections, and 7-day CSV export. |
| **DevOps & Security** | MariaDB migration support, Docker Compose multi-container setup, HTTPS reverse proxy, health checks, automated database backup scripts, and failover runbooks. |

---

## Tech Stack & Requirements

- **Backend / Web**: .NET 10 SDK (ASP.NET Core Blazor Interactive Server, EF Core 10).
- **Database**: MariaDB 11.4 (with Pomelo MySQL provider).
- **Real-Time Messaging**: ASP.NET Core SignalR.
- **Containerization**: Docker & Docker Compose.
- **Reverse Proxy**: NGINX / Caddy with HTTPS TLS termination.

---

## Quick Start with Docker Compose

1. Clone the repository:
   ```powershell
   git clone https://github.com/xXGunborgXx/GBServerPH-Restaurant-Ordering-System.git
   Set-Location GBServerPH-Restaurant-Ordering-System
   ```
2. Copy the environment configuration template:
   ```powershell
   Copy-Item .env.example .env
   ```
3. Edit `.env` to configure your database passwords and hostname. For local testing, set:
   ```env
   ROMS_HOST=localhost
   ```
4. Build and start the container stack:
   ```powershell
   docker compose up --build -d
   ```
5. Access the web interface at `https://localhost` and log in with your configured administrator credentials.

> [!NOTE]
> On first startup, ROMS automatically applies EF Core database migrations and seeds default roles (`Admin`, `Waiter`, `Kitchen`) and initial administrator credentials.

---

## Local Development (Without Docker)

To run the application directly via the .NET 10 SDK:

1. Ensure a MariaDB 11.4 instance is running and set your connection string in `src/Roms.Web/appsettings.Development.json` or via environment variables (`ConnectionStrings__DefaultConnection`).
2. Configure local development secrets:
   ```powershell
   dotnet user-secrets set "Seed:AdminPassword" "<strong-local-password>" --project src/Roms.Web
   ```
3. Restore dependencies, execute tests, and launch:
   ```powershell
   dotnet restore Roms.slnx
   dotnet test Roms.slnx -m:1
   dotnet run --project src/Roms.Web
   ```

---

## Operational Documentation & Runbooks

For production deployment, security hardening, backup maintenance, and disaster recovery, refer to:
- [docs/OPERATIONS.md](docs/OPERATIONS.md) — Production deployment guidelines, backup scripts, and health check monitoring.
- [docs/FAILOVER_RUNBOOK.md](docs/FAILOVER_RUNBOOK.md) — Manual database failover and recovery procedures.

---

## License

This project is licensed under the [MIT License](LICENSE).
