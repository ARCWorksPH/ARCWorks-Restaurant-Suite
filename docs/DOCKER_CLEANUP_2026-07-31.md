# Docker Cleanup and Tunnel Readiness — 2026-07-31

## Outcome

Docker now contains only the active ROMS Compose workloads and the
`portfolio-v30-hosting` workload. No unrelated or stopped containers remain.

## Backup created before cleanup

- Location:
  `D:\ARCWorks_Restaurant Suite Backups\pre-docker-cleanup-20260731-021827`
- File: `roms-pre-docker-cleanup.sql`
- Size: 29,077 bytes
- SHA-256:
  `6CC9BBE4ED01E7E0832FFD2DFBDF205726F64B762680DC1C79E7AF7520FADF1D`
- Verification: contains both `CREATE TABLE` and `INSERT INTO` statements.
- Source database: active ROMS MariaDB, 21 tables.
- Access: inherited permissions removed; full control retained only for the
  current Windows account and SYSTEM.

## Permanently removed

### Containers

- `friendly_neumann` — exited temporary Cloudflared tunnel container.
- `confident_dubinsky` — anonymous default nginx container with no mounts,
  Compose ownership, or restart policy.
- `priceless_grothendieck` — failed disposable MariaDB container.
- `hungry_heyrovsky` — failed disposable MariaDB container.

The two failed MariaDB containers' anonymous volumes were removed with them.

### Orphan volumes

Removed three unreferenced anonymous MariaDB-sized volumes:

- `8528d73f204cc2fca0c6d114d599e41698a84a0ef7e9c3b2bb2ff3ec59f7ff8d`
- `a87dbb2c31a352f18869da63b11b667e398e0a8933f84dfc4bfd98eba042bc30`
- `bba58e94aaff9c56a6fa2a78c7aae9ceae6a1a993b2baa60af8efcd0b795bdcb`

### Images

- `roms:inventory-operations-test`
- `roms:rollback-pre-ui-20260730`
- `roms-build:latest`
- `adminer:standalone`
- `nginx:alpine`
- `jc21/nginx-proxy-manager:latest`

### Other Docker data

- Removed unused network `arcworks-resto_default`.
- Removed all unused Docker build cache: 4.755 GB.

## Protected and retained

### Running containers

- `arcworks-portfolio`
- `arcworks-resto-app-1`
- `arcworks-resto-command-gateway-1`
- `arcworks-resto-ollama-1`
- `arcworks-resto-monitor-1`
- `arcworks-resto-db-1`

### Persistent volumes

- `arcworks-resto_mariadb-data`
- `arcworks-resto_data-protection-keys`
- `arcworks-resto_monitor-data`
- `ollama`

The `ollama` volume contains `tinyllama:1.1b` (637 MB). Removing the native
Windows Ollama installation must not remove this Docker volume.

### Retained project images

- Active ROMS and Command Gateway images.
- Latest externally audited ROMS candidate image.
- MariaDB, Gatus, Ollama, Cloudflared, and portfolio nginx images.
- .NET build/runtime, Testcontainers Ryuk, and curl images used by the ROMS
  build and test workflow.
- Caddy image referenced by the optional project fallback profile.

## Post-cleanup verification

- Exactly six containers remain, and all six are running.
- ROMS loopback health returned HTTP 200.
- Active MariaDB is healthy.
- Containerized Ollama is healthy.
- Portfolio container is healthy and confirmed as Compose project
  `portfolio-v30-hosting`.
- Gatus logs continue to report successful ROMS and MariaDB probes.
- No unused local volume remains.
- Docker build cache is zero.

## Cloudflare tunnel security action

The temporary Cloudflared container stored its tunnel token in its command
arguments. Inspection exposed that token in local terminal output. The
temporary container has been permanently removed, but the token must be
rotated or revoked in Cloudflare before the permanent tunnel is started.

For the replacement remotely managed tunnel:

- Store the rotated token as `TUNNEL_TOKEN`; do not place it in Compose
  `command`, source control, screenshots, or documentation.
- Run Cloudflared as a Compose service with no published host port.
- Connect it only to the ROMS networks required to reach the intended origins.
- Add a readiness endpoint/healthcheck for the tunnel process.

## Correct Docker origin routing

A Cloudflared container must not use `127.0.0.1:7070`: that address refers to
the Cloudflared container itself.

Use Compose service DNS:

- `roms.arkworksph.online` -> `http://app:8080`
- `status.arkworksph.online` -> `http://monitor:8080`

`roms-staging.arkworksph.online` must point to a separate disposable
application and database stack if it is intended to be true staging. Pointing
it to `http://app:8080` makes it only a second hostname for the production
instance.
