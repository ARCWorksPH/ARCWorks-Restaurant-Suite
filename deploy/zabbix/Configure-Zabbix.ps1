[CmdletBinding()]
param(
    [string]$MonitoringServer = "192.168.1.2",
    [Security.SecureString]$InitialAdminPassword
)

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent $MyInvocation.MyCommand.Path
$ApiUri = "http://127.0.0.1:8085/api_jsonrpc.php"
$AdminPassword = [IO.File]::ReadAllText((Join-Path $Root ".secrets\zabbix_admin_password")).Trim()
$script:RequestId = 0

function Invoke-ZabbixApi {
    param(
        [Parameter(Mandatory)][string]$Method,
        [Parameter(Mandatory)]$Parameters,
        [AllowNull()][string]$AuthToken = $null
    )

    $script:RequestId++
    $body = [ordered]@{
        jsonrpc = "2.0"
        method = $Method
        params = $Parameters
        id = $script:RequestId
    }
    if ($AuthToken) { $body.auth = $AuthToken }

    $response = Invoke-RestMethod -Method Post -Uri $ApiUri -ContentType "application/json-rpc" -Body ($body | ConvertTo-Json -Depth 30 -Compress)
    if ($response.error) {
        throw "Zabbix API $Method failed: $($response.error.message) $($response.error.data)"
    }
    return $response.result
}

function Try-Login {
    param([string]$Password)
    try {
        return Invoke-ZabbixApi -Method "user.login" -Parameters @{ username = "Admin"; password = $Password }
    } catch {
        return $null
    }
}

function ConvertTo-PlainText {
    param([Parameter(Mandatory)][Security.SecureString]$SecureValue)

    $pointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($SecureValue)
    try {
        return [Runtime.InteropServices.Marshal]::PtrToStringBSTR($pointer)
    } finally {
        [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($pointer)
    }
}

$auth = Try-Login $AdminPassword
if (-not $auth) {
    if (-not $InitialAdminPassword) {
        $InitialAdminPassword = Read-Host "Enter the initial Zabbix Admin password" -AsSecureString
    }
    $initialPasswordText = ConvertTo-PlainText $InitialAdminPassword
    $auth = Try-Login $initialPasswordText
    if (-not $auth) { throw "Could not authenticate with either the generated or supplied initial Zabbix Admin password." }
    Invoke-ZabbixApi -Method "user.update" -Parameters @{ userid = "1"; current_passwd = $initialPasswordText; passwd = $AdminPassword } -AuthToken $auth | Out-Null
    $initialPasswordText = $null
    $auth = Try-Login $AdminPassword
    if (-not $auth) { throw "Admin password was changed but the verification login failed." }
}

$group = Invoke-ZabbixApi -Method "hostgroup.get" -Parameters @{ output = @("groupid", "name"); filter = @{ name = @("ARCWorks Infrastructure") } } -AuthToken $auth
if (-not $group) {
    $created = Invoke-ZabbixApi -Method "hostgroup.create" -Parameters @{ name = "ARCWorks Infrastructure" } -AuthToken $auth
    $groupId = $created.groupids[0]
} else {
    $groupId = $group[0].groupid
}

function Get-TemplateId {
    param([string]$TemplateName)
    $template = Invoke-ZabbixApi -Method "template.get" -Parameters @{ output = @("templateid", "host", "name"); filter = @{ host = @($TemplateName) } } -AuthToken $auth
    if (-not $template) {
        $template = Invoke-ZabbixApi -Method "template.get" -Parameters @{ output = @("templateid", "host", "name"); filter = @{ name = @($TemplateName) } } -AuthToken $auth
    }
    if ($template) { return $template[0].templateid }
    return $null
}

$dockerTemplateId = Get-TemplateId "Docker by Zabbix agent 2"
$dockerHost = Invoke-ZabbixApi -Method "host.get" -Parameters @{ output = @("hostid", "host"); filter = @{ host = @("ARCWorks Docker") } } -AuthToken $auth
if (-not $dockerHost) {
    $params = @{
        host = "ARCWorks Docker"
        name = "ARCWorks Docker Containers"
        groups = @(@{ groupid = $groupId })
        interfaces = @(@{ type = 1; main = 1; useip = 0; ip = ""; dns = "zabbix-agent2"; port = "10050" })
    }
    if ($dockerTemplateId) { $params.templates = @(@{ templateid = $dockerTemplateId }) }
    $created = Invoke-ZabbixApi -Method "host.create" -Parameters $params -AuthToken $auth
    $dockerHostId = $created.hostids[0]
} else {
    $dockerHostId = $dockerHost[0].hostid
}

# Docker Engine 29 removed two legacy kernel-memory fields that remain in the
# stock 7.0 LTS template. Disable only those two items to avoid false warnings.
$legacyDockerKeys = @("docker.kernel_mem.enabled", "docker.kernel_mem_tcp.enabled")
$legacyDockerItems = Invoke-ZabbixApi -Method "item.get" -Parameters @{ output = @("itemid", "key_", "status"); hostids = @($dockerHostId); filter = @{ key_ = $legacyDockerKeys } } -AuthToken $auth
foreach ($item in $legacyDockerItems) {
    if ($item.status -eq "0") {
        Invoke-ZabbixApi -Method "item.update" -Parameters @{ itemid = $item.itemid; status = 1 } -AuthToken $auth | Out-Null
    }
}

$activeWindowsTemplateId = Get-TemplateId "Windows by Zabbix agent active"
$passiveWindowsTemplateId = Get-TemplateId "Windows by Zabbix agent"

# The Docker workstation sends active checks to avoid a host-loopback edge case.
$localWindowsHostName = $env:COMPUTERNAME
$localWindowsHost = Invoke-ZabbixApi -Method "host.get" -Parameters @{ output = @("hostid", "host"); filter = @{ host = @($localWindowsHostName) } } -AuthToken $auth
if (-not $localWindowsHost) {
    $params = @{
        host = $localWindowsHostName
        name = "$localWindowsHostName - Windows Workstation"
        groups = @(@{ groupid = $groupId })
    }
    if ($activeWindowsTemplateId) { $params.templates = @(@{ templateid = $activeWindowsTemplateId }) }
    Invoke-ZabbixApi -Method "host.create" -Parameters $params -AuthToken $auth | Out-Null
}

# LAN mini PCs use passive checks. Their agents allow only the main PC, and the
# server container can resolve each Windows computer name directly. This also
# provides an explicit green/red Zabbix availability state.
$remoteWindowsHosts = @("KNUCKLES", "NURSEJOY", "TADASHI", "HANARI")
foreach ($windowsHostName in $remoteWindowsHosts) {
    $windowsHost = Invoke-ZabbixApi -Method "host.get" -Parameters @{
        output = @("hostid", "host")
        filter = @{ host = @($windowsHostName) }
        selectInterfaces = @("interfaceid", "type")
        selectParentTemplates = @("templateid", "host")
    } -AuthToken $auth

    if (-not $windowsHost) {
        $params = @{
            host = $windowsHostName
            name = "$windowsHostName - Windows Host"
            groups = @(@{ groupid = $groupId })
            interfaces = @(@{ type = 1; main = 1; useip = 0; ip = ""; dns = $windowsHostName; port = "10050" })
        }
        if ($passiveWindowsTemplateId) { $params.templates = @(@{ templateid = $passiveWindowsTemplateId }) }
        Invoke-ZabbixApi -Method "host.create" -Parameters $params -AuthToken $auth | Out-Null
        continue
    }

    $hostId = $windowsHost[0].hostid
    if (-not $windowsHost[0].interfaces) {
        Invoke-ZabbixApi -Method "hostinterface.create" -Parameters @{
            hostid = $hostId; type = 1; main = 1; useip = 0; ip = ""; dns = $windowsHostName; port = "10050"
        } -AuthToken $auth | Out-Null
    }

    $linkedActiveTemplate = @($windowsHost[0].parentTemplates | Where-Object { $_.host -eq "Windows by Zabbix agent active" })
    $linkedPassiveTemplate = @($windowsHost[0].parentTemplates | Where-Object { $_.host -eq "Windows by Zabbix agent" })
    if (-not $linkedPassiveTemplate) {
        $update = @{ hostid = $hostId; templates = @(@{ templateid = $passiveWindowsTemplateId }) }
        if ($linkedActiveTemplate) { $update.templates_clear = @(@{ templateid = $activeWindowsTemplateId }) }
        Invoke-ZabbixApi -Method "host.update" -Parameters $update -AuthToken $auth | Out-Null
    }
}

# Ignore only known non-actionable stopped service registrations. These are
# updater, stale installer, and non-container database remnants; ROMS Docker
# services and all other automatic Windows services remain monitored.
$ignoredWindowsServices = "brave|WSLService|WslInstaller|IAStorDataMgrSvc|MariaDB"
$allWindowsHostNames = @($localWindowsHostName) + $remoteWindowsHosts
$allWindowsHosts = Invoke-ZabbixApi -Method "host.get" -Parameters @{
    output = @("hostid", "host")
    filter = @{ host = $allWindowsHostNames }
    selectParentTemplates = @("templateid", "host")
} -AuthToken $auth

foreach ($windowsHost in $allWindowsHosts) {
    $windowsTemplate = @($windowsHost.parentTemplates | Where-Object {
        $_.host -in @("Windows by Zabbix agent", "Windows by Zabbix agent active")
    } | Select-Object -First 1)
    if (-not $windowsTemplate) { continue }

    $templateMacro = Invoke-ZabbixApi -Method "usermacro.get" -Parameters @{
        output = @("value")
        hostids = @($windowsTemplate[0].templateid)
        filter = @{ macro = @('{$SERVICE.NAME.NOT_MATCHES}') }
    } -AuthToken $auth
    if (-not $templateMacro) { continue }

    $macroValue = $templateMacro[0].value -replace '\)\$$', "|$ignoredWindowsServices)`$"
    $hostMacro = Invoke-ZabbixApi -Method "usermacro.get" -Parameters @{
        output = @("hostmacroid", "value")
        hostids = @($windowsHost.hostid)
        filter = @{ macro = @('{$SERVICE.NAME.NOT_MATCHES}') }
    } -AuthToken $auth

    if ($hostMacro) {
        if ($hostMacro[0].value -ne $macroValue) {
            Invoke-ZabbixApi -Method "usermacro.update" -Parameters @{
                hostmacroid = $hostMacro[0].hostmacroid; value = $macroValue
            } -AuthToken $auth | Out-Null
        }
    } else {
        Invoke-ZabbixApi -Method "usermacro.create" -Parameters @{
            hostid = $windowsHost.hostid
            macro = '{$SERVICE.NAME.NOT_MATCHES}'
            value = $macroValue
            description = "ARCWorks: known non-actionable stopped services"
        } -AuthToken $auth | Out-Null
    }
}

$serviceHost = Invoke-ZabbixApi -Method "host.get" -Parameters @{ output = @("hostid", "host"); filter = @{ host = @("ARCWorks Services") } } -AuthToken $auth
if (-not $serviceHost) {
    $created = Invoke-ZabbixApi -Method "host.create" -Parameters @{
        host = "ARCWorks Services"
        name = "ARCWorks Public Services"
        groups = @(@{ groupid = $groupId })
    } -AuthToken $auth
    $serviceHostId = $created.hostids[0]
} else {
    $serviceHostId = $serviceHost[0].hostid
}

$checks = @(
    @{ Name = "ROMS public"; Url = "https://roms.arkworksph.online/" },
    @{ Name = "Monitoring public"; Url = "https://monitor.arkworksph.online/" },
    @{ Name = "Portfolio public"; Url = "https://portfolio.arkworksph.online/" }
)

foreach ($check in $checks) {
    $existing = Invoke-ZabbixApi -Method "httptest.get" -Parameters @{ output = @("httptestid", "name"); hostids = @($serviceHostId); filter = @{ name = @($check.Name) } } -AuthToken $auth
    if (-not $existing) {
        Invoke-ZabbixApi -Method "httptest.create" -Parameters @{
            name = $check.Name
            hostid = $serviceHostId
            delay = "1m"
            retries = 2
            steps = @(@{
                name = "Availability"
                no = 1
                url = $check.Url
                follow_redirects = 1
                status_codes = "200-399"
                timeout = "15s"
            })
        } -AuthToken $auth | Out-Null
    }

    $description = "$($check.Name): unavailable"
    $trigger = Invoke-ZabbixApi -Method "trigger.get" -Parameters @{ output = @("triggerid", "description"); hostids = @($serviceHostId); filter = @{ description = @($description) } } -AuthToken $auth
    if (-not $trigger) {
        Invoke-ZabbixApi -Method "trigger.create" -Parameters @{
            description = $description
            expression = "last(/ARCWorks Services/web.test.fail[$($check.Name)])<>0"
            priority = 4
            manual_close = 1
        } -AuthToken $auth | Out-Null
    }
}

# The factory host assumes an agent is installed inside the server container.
# This deployment uses the dedicated Agent 2 container, so retain Zabbix's
# internal health template but clear the irrelevant Linux-agent template.
$factoryHost = Invoke-ZabbixApi -Method "host.get" -Parameters @{ output = @("hostid", "host"); filter = @{ host = @("Zabbix server") }; selectParentTemplates = @("templateid", "host") } -AuthToken $auth
if ($factoryHost) {
    $linuxTemplate = @($factoryHost[0].parentTemplates | Where-Object { $_.host -eq "Linux by Zabbix agent" })
    if ($linuxTemplate) {
        Invoke-ZabbixApi -Method "host.update" -Parameters @{ hostid = $factoryHost[0].hostid; templates_clear = @(@{ templateid = $linuxTemplate[0].templateid }) } -AuthToken $auth | Out-Null
    }
}

Write-Host "Zabbix initial configuration applied."
Write-Host "Created or verified: Docker monitoring, Windows hosts, public service checks, and high-severity availability triggers."
