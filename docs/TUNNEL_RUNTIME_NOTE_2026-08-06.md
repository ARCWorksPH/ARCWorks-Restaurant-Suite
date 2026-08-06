# Cloudflare tunnel runtime note — 2026-08-06

## Incident

The Cloudflare tunnel connector registered successfully, but ROMS and the
portfolio initially returned Bad Gateway. The connector was running on
Docker's default `bridge` network while its remotely managed ingress referred
to Compose service names (`app`, `monitor`, and `portfolio-web`). Those names
are resolvable only on the corresponding project networks.

## Correction

The active connector was attached to:

- `arcworks-resto_edge` — ROMS `app:8080` and monitor `monitor:8080`
- `arcworks-portfolio-net` — production and workstation portfolio services

The ROMS container's local allowed-host setting was also updated to include
`roms.arkworksph.online`. No token or credential is recorded here.

## Verification

| Endpoint | Result |
| --- | ---: |
| `https://roms.arkworksph.online/` | HTTP 200 |
| `https://portfolio.arkworksph.online/` | HTTP 200 |
| `https://WBPortfolio.arkworksph.online/` | HTTP 200 |
| `https://monitor.arkworksph.online/` | HTTP 200 |
| `https://cloud.arkworksph.online/` | HTTP 302 (application redirect) |
| `https://Resto-VM.arkworksph.online/` | HTTP 400 — no VM instance is active and the route currently targets `app:8080` |

## Persistence warning

The connector was created manually and is currently named by Docker rather
than managed by Compose. If it is recreated, the network attachments must be
restored or it should be migrated to the project's `edge-tunnel` Compose
profile using the ignored token-file mechanism. Never commit or document the
tunnel token.
