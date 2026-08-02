$ErrorActionPreference = 'Stop'

$trackedSettings = @(
    git ls-files -- '*appsettings*.json' |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
)

$violations = foreach ($path in $trackedSettings) {
    $settings = Get-Content -LiteralPath $path -Raw | ConvertFrom-Json
    $password = [string]$settings.Seed.AdminPassword

    if (-not [string]::IsNullOrWhiteSpace($password)) {
        $path
    }
}

if ($violations.Count -gt 0) {
    Write-Error (
        "Committed Seed:AdminPassword values are forbidden. " +
        "Use dotnet user-secrets for local development and environment variables " +
        "or a secret manager for deployment. Files: " +
        ($violations -join ', ')
    )
}

Write-Output "Committed seed-password check passed ($($trackedSettings.Count) settings files inspected)."
