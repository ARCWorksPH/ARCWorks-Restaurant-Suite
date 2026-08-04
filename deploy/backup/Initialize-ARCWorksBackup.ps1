[CmdletBinding(SupportsShouldProcess)]
param(
    [string]$RuntimeRoot = 'C:\ProgramData\ARCWorks\Backup',
    [string]$InstanceId = 'arcworks-resto-main',
    [string]$ComposeProjectName = 'arcworks-resto-main',
    [string]$RomsRoot = (Join-Path $PSScriptRoot '..\..'),
    [string]$MonitoringRoot = 'D:\ARCWorks_Monitoring',
    [string]$PortfolioRoot = 'E:\ARCANUM VAULT\PROJECTS\ARCWorks-Portfolio'
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function Write-Step([string]$Message) {
    Write-Host "[ARCWorks Backup] $Message"
}

function Assert-Administrator {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = [Security.Principal.WindowsPrincipal]$identity
    if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
        throw 'Run this initializer from an elevated PowerShell session.'
    }
}

function Get-ResticSource {
    $command = Get-Command restic -ErrorAction SilentlyContinue
    if ($command) { return $command.Source }

    $packageRoot = Join-Path $env:LOCALAPPDATA 'Microsoft\WinGet\Packages\restic.restic_Microsoft.Winget.Source_8wekyb3d8bbwe'
    $candidate = Get-ChildItem -LiteralPath $packageRoot -Recurse -File -Filter 'restic*.exe' -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1
    if (-not $candidate) {
        throw 'Restic is not installed. Install the exact winget package restic.restic first.'
    }
    return $candidate.FullName
}

function New-RandomSecret([string]$Path) {
    if (Test-Path -LiteralPath $Path) { return }
    $bytes = [byte[]]::new(48)
    [Security.Cryptography.RandomNumberGenerator]::Fill($bytes)
    [IO.File]::WriteAllText($Path, [Convert]::ToBase64String($bytes), [Text.UTF8Encoding]::new($false))
}

function Protect-Directory([string]$Path) {
    $currentSid = [Security.Principal.WindowsIdentity]::GetCurrent().User.Value
    & icacls.exe $Path '/inheritance:r' '/grant:r' "*$currentSid`:(OI)(CI)F" '*S-1-5-18:(OI)(CI)F' '*S-1-5-32-544:(OI)(CI)F' | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "Could not protect $Path with NTFS ACLs." }
}

function Assert-DriveLabel([char]$Letter, [string]$Expected) {
    $volume = Get-Volume -DriveLetter $Letter -ErrorAction Stop
    if ($volume.HealthStatus -ne 'Healthy') { throw "Drive $Letter`: is not healthy." }
    if ($volume.FileSystemLabel -ne $Expected) {
        throw "Drive $Letter`: has label '$($volume.FileSystemLabel)', expected '$Expected'."
    }
}

function Invoke-Restic([string[]]$Arguments) {
    & $script:ResticExe @Arguments
    if ($LASTEXITCODE -ne 0) { throw "Restic failed with exit code $LASTEXITCODE." }
}

Assert-Administrator

$expectedLabels = [ordered]@{
    F = '=BACKUP-STAGING='
    G = '=ENCRYPTED-CLOUD-REPO='
    H = '=RESTIC LOCAL REPOSITORY='
    I = '=FINAL DESTINATION='
}
foreach ($item in $expectedLabels.GetEnumerator()) {
    Assert-DriveLabel -Letter ([char]$item.Key) -Expected $item.Value
}

$diskByDrive = @{}
foreach ($letter in @('D', 'F', 'G', 'H', 'I')) {
    $diskByDrive[$letter] = (Get-Partition -DriveLetter $letter -ErrorAction Stop).DiskNumber
}
if ($diskByDrive.D -in @($diskByDrive.F, $diskByDrive.G, $diskByDrive.H, $diskByDrive.I)) {
    throw 'A backup volume shares the physical source disk D:. Refusing initialization.'
}
if ($diskByDrive.F -ne $diskByDrive.H) {
    Write-Warning 'F: and H: no longer share the expected processing disk; update the architecture record.'
}
if ($diskByDrive.G -ne $diskByDrive.I) {
    Write-Warning 'G: and I: no longer share the expected secondary disk; update the architecture record.'
}

$paths = [ordered]@{
    RuntimeRoot = $RuntimeRoot
    Bin = Join-Path $RuntimeRoot 'bin'
    Secrets = Join-Path $RuntimeRoot '.secrets'
    Logs = Join-Path $RuntimeRoot 'logs'
    State = Join-Path $RuntimeRoot 'state'
    Staging = 'F:\ARCWorks_Backup_Staging'
    Local = 'H:\ARCWorks_Restic_Local'
    Replication = 'G:\ARCWorks_Restic_Replication'
    EaseUs = 'I:\ARCWorks_EaseUS_Images'
    Restore = 'I:\ARCWorks_Restore_Tests'
}
$RomsRoot = [IO.Path]::GetFullPath($RomsRoot)

foreach ($path in $paths.Values) {
    if (-not (Test-Path -LiteralPath $path)) {
        if ($PSCmdlet.ShouldProcess($path, 'Create protected backup directory')) {
            New-Item -ItemType Directory -Path $path -Force | Out-Null
        }
    }
}
foreach ($path in @($paths.RuntimeRoot, $paths.Staging, $paths.Local, $paths.Replication, $paths.EaseUs, $paths.Restore)) {
    Protect-Directory $path
}

$sourceRestic = Get-ResticSource
$script:ResticExe = Join-Path $paths.Bin 'restic.exe'
Copy-Item -LiteralPath $sourceRestic -Destination $script:ResticExe -Force
$resticVersion = & $script:ResticExe version
if ($LASTEXITCODE -ne 0 -or $resticVersion -notmatch '^restic 0\.19\.1\b') {
    throw "Unexpected Restic binary: $resticVersion"
}
Write-Step $resticVersion

$localPassword = Join-Path $paths.Secrets 'restic-local-password'
$replicationPassword = Join-Path $paths.Secrets 'restic-replication-password'
New-RandomSecret $localPassword
New-RandomSecret $replicationPassword
Protect-Directory $paths.Secrets

$configPath = Join-Path $RuntimeRoot 'backup.config.psd1'
$currentCodexRoot = Join-Path $env:USERPROFILE '.codex'
$config = @"
@{
    SchemaVersion = 1
    ControlRoot = '$RuntimeRoot'
    StagingRoot = '$($paths.Staging)'
    LocalRepository = '$($paths.Local)'
    ReplicationRepository = '$($paths.Replication)'
    EaseUsImageRoot = '$($paths.EaseUs)'
    RestoreTestRoot = '$($paths.Restore)'
    InstanceId = '$InstanceId'
    ComposeProjectName = '$ComposeProjectName'
    RomsRoot = '$RomsRoot'
    MonitoringRoot = '$MonitoringRoot'
    PortfolioRoot = '$PortfolioRoot'
    CodexRoot = '$currentCodexRoot'
    RomsDatabaseContainer = ''
    ZabbixDatabaseContainer = 'arcworks-monitoring-postgres'
    ResticHost = '$InstanceId'
    LocalPasswordFile = '$localPassword'
    ReplicationPasswordFile = '$replicationPassword'
    ResticExe = '$($script:ResticExe)'
    CloudRepository = ''
    CloudPasswordFile = ''
    CloudCredentialScript = ''
    Retention = @{ Hourly = 48; Daily = 14; Weekly = 8; Monthly = 12; Yearly = 2 }
}
"@
[IO.File]::WriteAllText($configPath, $config, [Text.UTF8Encoding]::new($false))

if (-not (Test-Path -LiteralPath (Join-Path $paths.Local 'config'))) {
    Write-Step 'Initializing primary local Restic repository.'
    Invoke-Restic @('-r', $paths.Local, '--password-file', $localPassword, 'init', '--repository-version', '2')
} else {
    Invoke-Restic @('-r', $paths.Local, '--password-file', $localPassword, 'snapshots', '--compact')
}

if (-not (Test-Path -LiteralPath (Join-Path $paths.Replication 'config'))) {
    Write-Step 'Initializing secondary repository with matching chunker parameters.'
    Invoke-Restic @(
        '-r', $paths.Replication,
        '--password-file', $replicationPassword,
        'init',
        '--from-repo', $paths.Local,
        '--from-password-file', $localPassword,
        '--copy-chunker-params',
        '--repository-version', '2'
    )
} else {
    Invoke-Restic @('-r', $paths.Replication, '--password-file', $replicationPassword, 'snapshots', '--compact')
}

$recoveryNotice = @'
ARCWorks Restic recovery keys are stored under:
C:\ProgramData\ARCWorks\Backup\.secrets

Do not print, email, or commit these files. Copy both password files to the
approved password manager and one offline recovery medium. A repository cannot
be recovered without its password.
'@
[IO.File]::WriteAllText((Join-Path $RuntimeRoot 'OFFLINE-KEY-EXPORT-REQUIRED.txt'), $recoveryNotice, [Text.UTF8Encoding]::new($false))

Write-Step 'Initialization complete. No passwords were displayed.'
Write-Step "Operational configuration: $configPath"
Write-Warning 'Cloud replication remains disabled until a real remote endpoint and credentials are configured.'
