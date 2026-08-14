#Requires -RunAsAdministrator
[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
$ConfigPath = "C:\Program Files\Zabbix Agent 2\zabbix_agent2.conf"
$BufferRoot = Join-Path $env:ProgramData "Zabbix"
$BufferPath = Join-Path $BufferRoot "agent2-buffer.db"

if (-not (Test-Path -LiteralPath $ConfigPath)) {
    throw "Agent 2 configuration was not found at $ConfigPath"
}

[IO.Directory]::CreateDirectory($BufferRoot) | Out-Null
$lines = [Collections.Generic.List[string]]::new()
$lines.AddRange([string[]][IO.File]::ReadAllLines($ConfigPath))
$setting = "PersistentBufferFile=$BufferPath"
$index = -1
for ($i = 0; $i -lt $lines.Count; $i++) {
    if ($lines[$i] -match '^\s*PersistentBufferFile=') {
        $index = $i
        break
    }
}
if ($index -ge 0) {
    $lines[$index] = $setting
} else {
    $lines.Add($setting)
}
[IO.File]::WriteAllLines($ConfigPath, $lines, [Text.UTF8Encoding]::new($false))

Start-Service -Name "Zabbix Agent 2"
$service = Get-Service -Name "Zabbix Agent 2"
$service.WaitForStatus([ServiceProcess.ServiceControllerStatus]::Running, [TimeSpan]::FromSeconds(15))

Start-Sleep -Seconds 3
$service.Refresh()
if ($service.Status -ne [ServiceProcess.ServiceControllerStatus]::Running) {
    $logPath = "C:\Program Files\Zabbix Agent 2\zabbix_agent2.log"
    if (Test-Path -LiteralPath $logPath) {
        Get-Content -LiteralPath $logPath -Tail 30
    }
    throw "Zabbix Agent 2 did not remain running."
}

$service | Select-Object Name, Status, StartType
