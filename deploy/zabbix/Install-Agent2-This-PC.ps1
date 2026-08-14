#Requires -RunAsAdministrator
[CmdletBinding()]
param(
    [string]$MonitoringServer = "192.168.1.2",
    [string]$AgentHostname = $env:COMPUTERNAME
)

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent $MyInvocation.MyCommand.Path
$Download = Join-Path $Root "downloads\zabbix_agent2-7.0.29-windows-amd64-openssl.msi"
$Url = "https://cdn.zabbix.com/zabbix/binaries/stable/7.0/7.0.29/zabbix_agent2-7.0.29-windows-amd64-openssl.msi"
$PersistentBufferRoot = Join-Path $env:ProgramData "Zabbix"
$PersistentBufferFile = Join-Path $PersistentBufferRoot "agent2-buffer.db"
[IO.Directory]::CreateDirectory($PersistentBufferRoot) | Out-Null

if (-not (Test-Path -LiteralPath $Download)) {
    Invoke-WebRequest -Uri $Url -OutFile $Download -UseBasicParsing
}

$signature = Get-AuthenticodeSignature -FilePath $Download
if ($signature.Status -ne "Valid" -or $signature.SignerCertificate.Subject -notmatch "Zabbix") {
    throw "The Agent 2 installer signature is not valid for Zabbix. Status: $($signature.Status)"
}

$arguments = @(
    "/i", ('"' + $Download + '"'),
    "/qn+",
    "ADDLOCAL=ALL",
    "SERVER=$MonitoringServer",
    "SERVERACTIVE=$MonitoringServer`:10051",
    "HOSTNAME=$AgentHostname",
    "STARTAGENTS=0",
    "ENABLEPERSISTENTBUFFER=1",
    "PERSISTENTBUFFERFILE=$PersistentBufferFile",
    "PERSISTENTBUFFERPERIOD=24h",
    "STARTUPTYPE=automatic",
    "SKIP=fw",
    "ENABLEPATH=1",
    "/l*v", ('"' + (Join-Path $Root "logs\agent2-install.log") + '"')
)

$process = Start-Process msiexec.exe -ArgumentList $arguments -Wait -PassThru
if ($process.ExitCode -notin 0, 3010) {
    throw "Agent 2 installation failed with MSI exit code $($process.ExitCode)."
}

$agentService = Get-Service -Name "Zabbix Agent 2"
$agentService.WaitForStatus([ServiceProcess.ServiceControllerStatus]::Running, [TimeSpan]::FromSeconds(15))
$agentService | Select-Object Name, Status, StartType
