# Operations baseline

- On-site primary and cloud standby: 2 vCPU, 4 GB RAM, 40 GB SSD minimum; use 64-bit Linux with Docker Compose.
- Use one HTTPS hostname per isolated restaurant instance. Local DNS resolves it to the on-site server; public DNS resolves to the published tunnel. Both nodes require a valid certificate or Cloudflare edge.
- Pin the application image by digest in production. Apply .NET, MariaDB, Caddy, and monitor patches in staging before the restaurant maintenance window.
- The Windows Restic scheduler under `deploy/backup/` is the authoritative backup system for this workstation. Its six-hour database, daily full, weekly maintenance, and weekly recovery tasks are registered through `Register-ARCWorksBackupTasks.ps1`. The older `scripts/backup.sh` age/cron file is retained only as historical Linux material and must not be used as the current recovery procedure.
- Each portable instance needs its own source root, database/container identity, Restic host/tag, and recovery boundary. A second local repository is not an off-site backup.
- Alert on `/health` failure, MariaDB failure, replication lag over 60 seconds, disk use over 80%, repeated failed logins, unhandled exceptions, and missed backups.
- Review audit entries weekly and before investigating any disputed order.
- Keep production inventory use supervised until the workflow pilot, units, opening stock, and approval policies are signed off. Orders never deduct ingredients.

## Portable instances

Use `deploy/portable/instance.example.psd1` and
`deploy/portable/README.md` for every restaurant VM. Never clone production
identity, Data Protection keys, Cloudflare tokens, database passwords, or backup
repositories without the documented reset procedure. The application remains
internal to the Compose network at `app:8080`; only the configurable loopback
host port is published for local administration.
