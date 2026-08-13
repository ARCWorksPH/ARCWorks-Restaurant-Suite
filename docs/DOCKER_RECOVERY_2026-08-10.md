# Docker recovery after power interruption — 2026-08-10

## Finding

Docker Desktop failed during startup because
`C:\Users\GBServerPH\.docker\daemon.json` contained 124 NUL bytes. The
companion `windows-daemon.json` also contained only NUL bytes (28 bytes). Both
files were invalid JSON and caused the reported parser error.

## Safe repair performed

- Preserved the corrupt files at:
  `C:\Users\GBServerPH\.docker\recovery-backup-20260810-101507\`
- Replaced both files with valid empty JSON objects (`{}`), which restores
  Docker's default daemon settings.
- Docker Desktop now starts and `docker version` reports matching client/server
  version 29.6.2.

## Important storage finding

The project Docker data was not deleted. The original project disk remains at:

`D:\ARCWorks_Restaurant Suite\Docker\storage\DockerDesktopWSL\disk\docker_data.vhdx`

The file is approximately 13.8 GB. Docker Desktop is currently pointed at its
fresh default C: WSL disk, so `docker ps` and `docker volume ls` are empty even
though the original project VHDX is preserved.

## Correct replacement procedure and observed result

Selecting the parent folder alone is not sufficient: Docker creates its own
data subdirectory and a new `docker_data.vhdx` underneath it. The recovery
therefore preserved the original source VHDX and allowed Docker to use the
replacement disk created under the selected storage tree:

- Original source (preserved):
  `D:\ARCWorks_Restaurant Suite\Docker\storage\DockerDesktopWSL\disk\docker_data.vhdx`
- Active replacement selected by Docker:
  `D:\ARCWorks_Restaurant Suite\Docker\storage\DockerDesktopWSL\disk\DockerDesktopWSL\disk\docker_data.vhdx`
- Fresh default disk was not reused as the project disk and is no longer the
  active data source.

The active replacement is approximately 14.6 GB and the original source is
approximately 13.8 GB. The original remains available as a rollback source;
do not delete or overwrite either file until a separate backup has been
verified.

After Docker restart, verify the original containers and volumes with:

```powershell
docker ps -a
docker volume ls
docker compose -f D:\ARCWorks_Restaurant_Suite\compose.yaml ps
```

The repository's logical backups and the preserved Docker VHDX provide separate
recovery paths. No database or project volume was removed during this incident.

## Post-recovery health check

Completed on 2026-08-10:

- ROMS app: running and healthy; `http://127.0.0.1:7070/health` returned HTTP 200.
- MariaDB: running and healthy.
- Cloudflared: running; no image-level healthcheck is configured, so tunnel
  health must also be confirmed in Cloudflare.
- ROMS monitor (Gatus): running; no image-level healthcheck is configured.
- Portfolio and workstation portfolio: running and healthy; local HTTP checks
  returned HTTP 200.
- Zabbix web and PostgreSQL: running and healthy.
- Zabbix agent: running; no image-level healthcheck is configured.
