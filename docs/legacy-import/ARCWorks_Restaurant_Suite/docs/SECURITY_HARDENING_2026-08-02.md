# ROMS Security Hardening - 2026-08-02

Status: implemented, deployed, and independently re-auditable

## Scope

This pass reviewed current source, authentication and role boundaries, the AI
command path, live Docker networks, Cloudflare exposure, dependency advisories,
secret hygiene, CI configuration, and GitHub repository controls. The supplied
independent report remains local and confidential; `/docs/Independent-Audit/`
is deliberately excluded from Git.

## Independent scanner finding disposition

- The reported migration-lock SQL injection was a false positive: the original
  code replaced one exact compile-time connector string with another and
  contained no user-controlled value. The interceptor was nevertheless
  rewritten to accept only the exact EF lock command and supply the finite
  timeout through a database parameter, eliminating scanner ambiguity.
- Mutable `actions/checkout` and `actions/setup-dotnet` references were valid
  supply-chain findings and are now pinned to full commit SHAs. Workflow token
  permissions are explicitly read-only.
- The warnings against Docker `COPY --chown` are rejected. Ownership is
  intentionally assigned to the non-root runtime user; removing it would
  weaken rather than improve least privilege.
- Missing application and command-gateway health checks were valid findings.
  Both production images now contain HTTP health checks.

## Additional controls implemented

- AI command protocol schema 4 carries a role-derived permitted-function list.
  The gateway and ROMS independently reject a proposal absent from that list.
- Catalogs are filtered before model access. Waiters send no inventory catalog
  to the gateway or model.
- The local model is limited to two concurrent requests and six requests per
  user per minute by default.
- Every assistant outcome is audited, including unsupported, denied, throttled,
  unavailable, and successful requests. Records contain request ID, outcome,
  prompt length, and SHA-256; raw prompts and result data are not stored.
- ROMS, the command gateway, Ollama, and Cloudflare run with read-only root
  filesystems, all capabilities dropped, `no-new-privileges`, and process
  limits. ROMS and the gateway run as UID 1654; Cloudflare runs as 65532.
- The tunnel was replaced by `arcworks-cloudflared`. It is connected only to
  `arcworks-resto_edge` and `arcworks-portfolio-net`; the default Docker bridge
  is no longer attached.
- Active base/application images and GitHub Actions are digest/SHA pinned.
- Production host allowlisting, 180-day HSTS, CSP frame/base/object controls,
  `nosniff`, referrer policy, permissions policy, and clickjacking protection
  are active.
- The bootstrap administrator password is required only when the administrator
  does not yet exist. It may be removed from `.env` after first initialization.
- `main` requires a pull request, a current successful `verify` check, and
  resolved conversations. Administrator bypass, force pushes, and branch
  deletion are disabled. Zero outside approvals are required for the current
  single-owner workflow.

## Tunnel portability

The optional `edge-tunnel` Compose profile uses an ignored local secret file:

```text
.secrets/cloudflare-tunnel-token
```

Place only the Cloudflare tunnel token in that file, restrict local access, and
start the portable hardened service with:

```powershell
docker compose --profile edge-tunnel up -d cloudflared
```

The base Compose file is restaurant-only. Add `-f compose.portfolio.yaml` only
on the workstation that intentionally publishes the separate portfolio network;
restaurant VMs must not attach their tunnel to that external network.

Never commit the token file or paste its value into documentation or logs.

## Verification evidence

- Secret guard passed; `.env`, private-key candidates, local secrets, and the
  confidential audit directory are not tracked.
- Current NuGet advisory audit found zero vulnerable direct or transitive
  packages across all solution projects.
- Compose validation passed for the `ai-lab` and `edge-tunnel` profiles.
- Release build passed with zero warnings and zero errors.
- Full solution regression passed 63/63 tests:
  - Domain: 11/11
  - Command gateway: 11/11
  - Integration, real MariaDB, concurrency, stress, adversarial, inventory,
    attendance, and AI authorization: 38/38
  - Chromium/browser E2E: 3/3
- Hardened app and command-gateway container health checks report healthy.
- Public ROMS, monitor, and portfolio endpoints returned HTTP 200 after the
  tunnel replacement.
- Anonymous registration, Assistant, attendance, and administration requests
  redirected to login.
- `AI_ENABLED=false` remained effective after deployment.

## Remaining operational gate

This pass does not approve AI production use. Keep the feature disabled until
the locked multilingual, prompt-injection, timeout, stale-catalog, cross-role,
and sustained-concurrency acceptance run succeeds through authenticated browser
sessions.
