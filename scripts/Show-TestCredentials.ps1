[CmdletBinding()]
param(
    [string]$CredentialFile = (Join-Path $PSScriptRoot '..\.secrets\roms-test-credentials.dpapi.json')
)

$ErrorActionPreference = 'Stop'
$resolved = [System.IO.Path]::GetFullPath($CredentialFile)
if (-not (Test-Path -LiteralPath $resolved -PathType Leaf)) {
    throw "Protected ROMS test credential file not found: $resolved"
}

$protected = Get-Content -LiteralPath $resolved -Raw | ConvertFrom-Json
$rows = foreach ($account in $protected.Accounts) {
    $secure = ConvertTo-SecureString -String $account.ProtectedPassword
    $credential = [pscredential]::new($account.Username, $secure)
    [pscustomobject]@{
        Role     = $account.Role
        Username = $account.Username
        Password = $credential.GetNetworkCredential().Password
    }
}

Write-Host ''
Write-Host 'ROMS test credentials (local and public URLs use the same database):' -ForegroundColor Cyan
$rows | Format-Table -AutoSize
Write-Warning 'Close this terminal after copying the credential you need. Do not capture or commit this output.'
