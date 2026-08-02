# Operations baseline

- On-site primary and cloud standby: 2 vCPU, 4 GB RAM, 40 GB SSD minimum; use 64-bit Linux with Docker Compose.
- Use one HTTPS hostname. Local DNS resolves it to the on-site server; public DNS resolves it to cloud. Both nodes require a valid certificate.
- Pin the application image by digest in production. Apply .NET, MariaDB, Caddy, and monitor patches in staging before the restaurant maintenance window.
- Run `scripts/backup.sh` daily from cron with `BACKUP_RECIPIENT`, `DB_ROOT_PASSWORD`, and off-site `BACKUP_DIR` configured. Protect the age private key outside both servers.
- Alert on `/health` failure, MariaDB failure, replication lag over 60 seconds, disk use over 80%, repeated failed logins, unhandled exceptions, and missed backups.
- Review audit entries weekly and before investigating any disputed order.
- Keep production inventory use supervised until the workflow pilot, units, opening stock, and approval policies are signed off. Orders never deduct ingredients.
