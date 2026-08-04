@{
    SchemaVersion = 1

    # Resto-VM staging profile. Replace only local paths and generated secrets;
    # never commit the operator-controlled copy of this file.
    InstanceId = 'arcworks-suite-resto-vm'
    DisplayName = 'ARCWorks Restaurant Suite - Resto VM'
    ComposeProjectName = 'arcworks-suite-resto-vm'

    # Cloudflare route is already prepared, but remains a staging route until
    # the private acceptance checklist passes.
    Hostname = 'resto-vm.arkworksph.online'
    AllowedHosts = 'resto-vm.arkworksph.online;app;localhost;127.0.0.1'
    TunnelService = 'http://app:8080'
    HostPort = 7071
    DatabaseServerId = 2

    # Set these paths to the actual VM checkout and operational roots.
    RomsRoot = 'C:\ARCWorks\Restaurant Suite'
    MonitoringRoot = 'C:\ARCWorks\Monitoring'
    PortfolioRoot = ''

    # Fresh per-instance volume names. Do not reuse the main instance volumes.
    DataProtectionVolume = 'arcworks-suite-resto-vm_data-protection-keys'
    MariaDbVolume = 'arcworks-suite-resto-vm_mariadb-data'
    MonitorVolume = 'arcworks-suite-resto-vm_monitor-data'
    OllamaVolume = 'arcworks-suite-resto-vm_ollama'

    CloudflareTokenFile = '.\.secrets\cloudflare-tunnel-token'
    BackupHost = 'arcworks-suite-resto-vm'
    LocalBackupRepository = ''
    ReplicationBackupRepository = ''
    CloudBackupRepository = ''

    ZabbixHostName = 'ARCWORKS-SUITE-RESTO-VM'
    ZabbixDatabaseContainer = ''

    # Keep the AI lab disabled during the portable acceptance phase.
    AiEnabled = $false
}
