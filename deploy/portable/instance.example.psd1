@{
    SchemaVersion = 1

    # Identity must be unique for every restaurant/VM.
    InstanceId = 'arcworks-resto-main'
    DisplayName = 'ARCWorks Restaurant Suite Main'
    ComposeProjectName = 'arcworks-resto-main'

    # Public and local deployment values.
    Hostname = 'roms.example.com'
    AllowedHosts = 'roms.example.com;app;localhost;127.0.0.1'
    HostPort = 7070
    DatabaseServerId = 1

    # Source and operational boundaries.
    RomsRoot = 'D:\GBServerPH - Staff-side Restaurant Ordering App'
    MonitoringRoot = 'D:\ARCWorks_Monitoring'
    PortfolioRoot = 'E:\ARCANUM VAULT\PROJECTS\ARCWorks-Portfolio'

    # Runtime resources. These are names, not credentials.
    DataProtectionVolume = 'arcworks-resto-main_data-protection-keys'
    MariaDbVolume = 'arcworks-resto-main_mariadb-data'
    MonitorVolume = 'arcworks-resto-main_monitor-data'
    OllamaVolume = 'arcworks-resto-main_ollama'

    # Cloudflare and backup are provisioned separately per instance.
    CloudflareTokenFile = '.\.secrets\cloudflare-tunnel-token'
    BackupHost = 'arcworks-resto-main'
    LocalBackupRepository = 'H:\ARCWorks_Restic_Local'
    ReplicationBackupRepository = 'G:\ARCWorks_Restic_Replication'
    CloudBackupRepository = ''

    # Zabbix is a separate monitoring stack on this workstation.
    ZabbixHostName = 'ARCWORKS-SUITE-MAIN'
    ZabbixDatabaseContainer = 'arcworks-monitoring-postgres'

    # Keep false until the AI lab is separately provisioned for this instance.
    AiEnabled = $false
}
