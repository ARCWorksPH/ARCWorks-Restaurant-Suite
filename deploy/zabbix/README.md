# ARCWorks Monitoring

This folder contains the portable Zabbix 7.0 LTS monitoring control plane. It is isolated from the ROMS and portfolio Compose projects.

## Components

- PostgreSQL 16 database with bind-mounted data under `data/postgres`
- Zabbix server 7.0.29 LTS
- Zabbix web dashboard on TCP 8085
- Zabbix Agent 2 container for Docker/container discovery
- Built-in Zabbix trigger severities, Problems view, and browser sound notifications

The stock Zabbix 7.0 Docker template contains two legacy kernel-memory fields removed by Docker Engine 29. Initialization disables only those two incompatible items; container discovery, state, CPU, memory, network, and restart monitoring remain enabled.

## Addresses

- Local dashboard: http://127.0.0.1:8085
- LAN dashboard: http://192.168.1.2:8085
- Active agent receiver: 192.168.1.2:10051

The Zabbix login user is `Admin`. The generated password is stored locally in `.secrets/zabbix_admin_password` and is intentionally not printed or committed.

On a fresh Zabbix deployment, `Configure-Zabbix.ps1` securely prompts for the vendor's initial Admin password. It is used only to replace the bootstrap credential with the locally generated password. For non-interactive automation, pass a `SecureString` through `-InitialAdminPassword`; do not place the password directly in a command line or tracked file.

## Initial deployment

```powershell
Set-Location "<repository>\deploy\zabbix"
& ".\Initialize-Monitoring.ps1" -MonitoringServer "192.168.1.2"
```

Run `Set-Lan-Firewall.ps1` once from an Administrator PowerShell window. Run `Install-Agent2-This-PC.ps1` as Administrator to add Windows host metrics to the main workstation.

If Agent 2 was installed by the initial August 3 script and stopped because its persistent-buffer file was missing, run `Repair-Agent2-This-PC.ps1` once as Administrator. It repairs the configuration and verifies that the service remains running.

## Normal operations

```powershell
docker compose ps
docker compose logs --tail 100
docker compose restart
docker compose pull
docker compose up -d
```

## Windows hosts

Install the official 64-bit Agent 2 MSI. Use the PC's exact Windows computer name as **Host name**, `192.168.1.2` as **Zabbix server IP/DNS**, and `192.168.1.2:10051` as **Server or Proxy for active checks**.

The main workstation uses the `Windows by Zabbix agent active` template. The known LAN mini PCs `KNUCKLES`, `NURSEJOY`, `TADASHI`, and `HANARI` use the passive Windows template through computer-name interfaces on port 10050; each agent restricts accepted polling to the main PC. The installer host name must match the Windows computer name exactly.

The service-discovery filter excludes only known non-actionable stopped registrations: Brave updater, WSL installer/service, Intel Rapid Storage manager, and the stale local MariaDB service. Containerized ROMS/MariaDB and all other automatic Windows services remain monitored.

## Alerts

Template triggers and service checks create Problems with severity levels. In each wallboard browser, enable global notifications and sound in the Zabbix user settings. Email, Telegram, Teams, or SMS notifications require a real destination and credentials and are deliberately not preconfigured.

## Security and excluded state

The nested `.gitignore` prevents passwords, PostgreSQL contents, Agent 2 buffer databases, MSI downloads, and logs from entering Git. The Docker socket mount is required for Docker discovery and should be treated as privileged infrastructure access.

## Portability and backup

Back up the operational monitoring folder while the stack is stopped, or take a PostgreSQL logical dump for an application-consistent live backup. Docker images can be re-pulled using `compose.yaml`; `image-lock.txt` records the exact downloaded image digests.
