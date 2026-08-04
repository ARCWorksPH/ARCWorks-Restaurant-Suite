# Portable ROMS instance contract

This directory defines the values that make one ROMS installation distinct
from another. Copy `instance.example.psd1` to an operator-controlled manifest
for each VM; never put passwords or Cloudflare tokens in the manifest.

The current canonical source is:

```text
D:\GBServerPH - Staff-side Restaurant Ordering App
```

The live workstation stack still runs from the legacy `D:\ARCWorks_Restaurant
Suite` checkout. This document does not migrate or stop that stack. A migration
is allowed only after the chosen commit, image digests, database migration
level, and tunnel routes are recorded for the target instance.

## Required uniqueness

Every VM/restaurant must have a different:

- `InstanceId` and Compose project name.
- Hostname and allowed-host list.
- MariaDB `DB_SERVER_ID`.
- MariaDB, monitoring, Data Protection, and optional Ollama volume names.
- Cloudflare tunnel token and tunnel route.
- Restic host/tag and backup repository boundary.
- Zabbix host identity.

The application remains reachable inside the Compose network as `app:8080`.
The host-side port is only a local convenience and is configurable with
`ROMS_HOST_PORT`.

## Provisioning sequence

1. Start from the canonical commit and record its SHA-256/image digests.
2. Create a private VM with a fresh database volume.
3. Copy the instance manifest and generate a new ignored `.env` with
   `scripts/Initialize-ProductionEnv.ps1`. For example:

   ```powershell
   .\scripts\Initialize-ProductionEnv.ps1 `
     -RomsHost 'resto2.example.com' `
     -AllowedHosts 'resto2.example.com;app;localhost;127.0.0.1' `
     -ComposeProjectName 'arcworks-resto-vm01' `
     -InstanceId 'arcworks-resto-vm01' `
     -DbServerId 2 `
     -RomsHostPort 7071
   ```

   The generated file is local-only and must never be committed.

   Do not use `-Force` against an existing database volume unless the database
   credentials are being rotated deliberately. A newly generated password does
   not change an already-initialized MariaDB account by itself.
4. Create a new Cloudflare token file at the manifest path. Do not reuse the
   main restaurant token.
5. Start ROMS and MariaDB without public DNS. Confirm migrations and the first
   administrator bootstrap complete successfully.
6. Verify the app health endpoint, login, database name, and instance label.
7. Register the instance in monitoring and backup using its unique identity.
8. Run the two-instance isolation checklist before publishing the hostname.

## Clone reset checklist

Do not treat a disk clone as a ready restaurant instance. Before enabling the
clone, rotate/regenerate the database passwords, administrator password, Data
Protection key ring, Cloudflare token, backup identity, monitoring identity, and
database server ID. Confirm that no production users, sessions, keys, or backup
repositories are shared unintentionally.

## Tunnel model

For two restaurants, use one independent Cloudflare tunnel per VM. Route the
restaurant hostname to `http://app:8080` and the local status hostname to
`http://monitor:8080`. Never route a tunnel container to `127.0.0.1:7070`.

The base Compose file is restaurant-only. The optional
`compose.portfolio.yaml` override attaches the workstation tunnel to the
external portfolio network. Restaurant VMs should not use that override.

## Backup model

The Windows Restic scheduler is authoritative. Configure each instance with
its own source root, database/container identity, Restic host/tag, and recovery
repository. A second local repository is not an off-site backup; configure and
test a genuinely separate remote endpoint before calling cloud replication
production-ready.
