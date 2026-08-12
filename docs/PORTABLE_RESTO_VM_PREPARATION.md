# Portable Resto-VM preparation

Status: preparation only — no live workstation cutover has been performed.

## Target instance

| Value | Staging target |
|---|---|
| Public hostname | `resto-vm.arkworksph.online` |
| Cloudflare service | `http://app:8080` |
| Instance ID | `arcworks-suite-resto-vm` |
| Compose project | `arcworks-suite-resto-vm` |
| Suggested host port | `7071` |
| MariaDB server ID | `2` |
| AI | Disabled until portable acceptance passes |

The tunnel route may exist in Cloudflare, but it must not be treated as
production-ready until the VM has booted privately and passed the acceptance
checklist below.

## Files prepared

- `deploy/portable/instance.resto-vm.example.psd1` — non-secret profile for the
  Resto-VM values.
- `scripts/Initialize-ProductionEnv.ps1` — generates fresh per-instance secrets
  and now defaults the administrator label to ARCWorks Restaurant Suite.
- `deploy/portable/README.md` — clone/reset, tunnel, backup, and isolation rules.

## VM operator sequence

1. Install Docker Desktop/WSL, Git, and the required .NET/runtime tools in the
   VM. Do not copy the main `.env`, database volume, Data Protection keys,
   Cloudflare token, or backup repository.
2. Check out the selected canonical commit and copy the example profile to an
   operator-controlled manifest outside Git, adjusting only local paths.
3. Create a new `.secrets\cloudflare-tunnel-token` file in the VM and protect it
   with the VM account permissions.
4. Generate a fresh environment file from the VM checkout:

   ```powershell
   .\scripts\Initialize-ProductionEnv.ps1 `
     -RomsHost 'resto-vm.arkworksph.online' `
     -AllowedHosts 'resto-vm.arkworksph.online;app;localhost;127.0.0.1' `
     -ComposeProjectName 'arcworks-suite-resto-vm' `
     -InstanceId 'arcworks-suite-resto-vm' `
     -DbServerId 2 `
     -RomsHostPort 7071 `
     -AdminDisplayName 'ARCWorks Restaurant Suite Administrator'
   ```

5. Start only the base restaurant stack and the `edge-tunnel` profile. Do not
   use `compose.portfolio.yaml` for this VM.
6. Verify the app privately through the VM host port before relying on the
   Cloudflare route.
7. Confirm database name, Compose labels, volume names, health endpoint, login,
   and initial administrator bootstrap.
8. Run the two-instance isolation checklist: database writes, Data Protection
   keys, Cloudflare route, monitoring identity, backup identity, and host port
   must all be unique from the main instance.

## Acceptance gate before public use

- [ ] VM stack starts with a clean database volume.
- [ ] `http://127.0.0.1:7071/health` returns healthy.
- [ ] Staff login and role authorization work.
- [ ] Waiter → kitchen → payment workflow passes.
- [ ] Inventory controls and approval paths pass.
- [ ] Main instance data is not visible in the VM.
- [ ] Resto-VM hostname reaches only the VM app.
- [ ] Backup and restore identity is unique and tested.
- [ ] Monitoring identity is unique and reporting.
- [ ] No production credentials, tokens, key rings, or volumes were copied.

The current workstation stack remains untouched until this gate is complete.
