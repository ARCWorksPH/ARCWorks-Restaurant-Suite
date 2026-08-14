#Requires -RunAsAdministrator
[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
foreach ($rule in @(
    @{ Name = "ARCWorks Zabbix Dashboard (LAN)"; Port = 8085 },
    @{ Name = "ARCWorks Zabbix Active Agents (LAN)"; Port = 10051 }
)) {
    if (-not (Get-NetFirewallRule -DisplayName $rule.Name -ErrorAction SilentlyContinue)) {
        New-NetFirewallRule -DisplayName $rule.Name -Direction Inbound -Action Allow -Protocol TCP -LocalPort $rule.Port -Profile Private -RemoteAddress LocalSubnet | Out-Null
    }
}

Get-NetFirewallRule -DisplayName "ARCWorks Zabbix*" | Select-Object DisplayName, Enabled, Profile, Direction, Action
