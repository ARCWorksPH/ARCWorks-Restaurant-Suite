# Portability baseline — 2026-08-04

## Decision

The canonical release source for the portable ROMS work is:

```text
D:\GBServerPH - Staff-side Restaurant Ordering App
```

The live workstation containers are intentionally unchanged during this
baseline. They still point to the legacy checkout:

```text
D:\ARCWorks_Restaurant Suite
branch: agent/inventory-readiness
commit: e9d1788
```

The audited repository is on `agent/backup-recovery` at `4465daf`, the portable
instance contract commit. The pre-portability scan baseline was `070218a`.
This drift is the release-control blocker for the later controlled migration.

## Controls added in this pass

- Compose project name, instance ID, host port, allowed hosts, Cloudflare token
  file, and Ollama volume are now configurable.
- ROMS services carry `com.arcworks.instance` and `com.arcworks.service` labels.
- ROMS database backup can resolve a database container by those labels instead
  of requiring a fixed generated container name.
- A portfolio-network override keeps restaurant-only VMs isolated from the
  workstation portfolio stack.
- Production environment initialization now generates fresh database and admin
  secrets instead of depending on the removed `Docker\MariaDB\.env` path.
- Portable-instance and clone-reset requirements are documented in
  `deploy/portable/README.md`.

## Not performed

- No live container restart or migration.
- No Cloudflare DNS/tunnel change.
- No backup repository reconfiguration.
- No VM clone.
- No public hostname cutover.

## Required next acceptance

Run a clean Compose bootstrap from the canonical checkout, then provision one
private VM using a fresh manifest and secrets. Confirm database, key-ring,
tunnel, monitoring, and backup isolation before touching the main instance.

## Validation performed in this pass

- Compose base, edge-tunnel, AI-lab, and portfolio-override configurations
  rendered successfully with `.env.example`.
- PowerShell parser accepted the changed bootstrap, gateway, backup, and
  initializer scripts.
- Release build: 0 warnings, 0 errors.
- Domain tests: 11/11 passed.
- Command-gateway tests: 11/11 passed.
- Real MariaDB smoke test: 1/1 passed.
- Playwright installation smoke test: 1/1 passed.
- Committed seed-password guard passed.
- The complete solution suite was not claimed as passed; its earlier broad run
  exceeded the four-minute diagnostic bound and remains a separate acceptance
  task.
