@{
    SchemaVersion = 1

    # Runtime paths. Initialize-ARCWorksBackup.ps1 writes the operational copy
    # beneath C:\ProgramData\ARCWorks\Backup; secrets never belong in Git.
    ControlRoot          = 'C:\ProgramData\ARCWorks\Backup'
    StagingRoot          = 'F:\ARCWorks_Backup_Staging'
    LocalRepository      = 'H:\ARCWorks_Restic_Local'
    ReplicationRepository = 'G:\ARCWorks_Restic_Replication'
    EaseUsImageRoot      = 'I:\ARCWorks_EaseUS_Images'
    RestoreTestRoot      = 'I:\ARCWorks_Restore_Tests'

    InstanceId     = 'arcworks-resto-main'
    ComposeProjectName = 'arcworks-resto-main'
    RomsRoot       = 'D:\GBServerPH - Staff-side Restaurant Ordering App'
    MonitoringRoot = 'D:\ARCWorks_Monitoring'
    PortfolioRoot  = 'E:\ARCANUM VAULT\PROJECTS\ARCWorks-Portfolio'
    CodexRoot      = 'C:\Users\GBServerPH\.codex'

    # Leave the ROMS container blank to resolve it by the Compose instance labels.
    # Keep Zabbix explicit when monitoring is hosted in a separate stack.
    RomsDatabaseContainer = ''
    ZabbixDatabaseContainer = 'arcworks-monitoring-postgres'
    ResticHost = 'arcworks-resto-main'

    LocalPasswordFile       = 'C:\ProgramData\ARCWorks\Backup\.secrets\restic-local-password'
    ReplicationPasswordFile = 'C:\ProgramData\ARCWorks\Backup\.secrets\restic-replication-password'
    ResticExe               = 'C:\ProgramData\ARCWorks\Backup\bin\restic.exe'

    # A real remote endpoint is intentionally blank until explicitly supplied.
    # Supported examples include sftp:user@host:/srv/arcworks-restic or an
    # rclone/S3 backend configured outside this tracked file.
    CloudRepository       = ''
    CloudPasswordFile     = ''
    CloudCredentialScript = ''

    Retention = @{
        Hourly  = 48
        Daily   = 14
        Weekly  = 8
        Monthly = 12
        Yearly  = 2
    }
}
