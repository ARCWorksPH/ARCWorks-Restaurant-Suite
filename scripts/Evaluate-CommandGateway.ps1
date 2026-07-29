[CmdletBinding()]
param(
    [string]$GatewayContainer = "arcworks-resto-command-gateway-1",
    [string]$CommandNetwork = "arcworks-resto_command"
)

$ErrorActionPreference = "Stop"
$projectRoot = Split-Path -Parent $PSScriptRoot
$corpusPath = Join-Path $projectRoot "tests\Roms.CommandGateway.Tests\TestData\command-corpus.json"
$artifactDirectory = Join-Path $projectRoot ".artifacts\ai-lab"
$artifactPath = Join-Path $artifactDirectory ("evaluation-{0}.json" -f (Get-Date -Format "yyyyMMdd-HHmmss"))

if (-not (docker ps --filter "name=^/$GatewayContainer$" --format "{{.Names}}")) {
    throw "The isolated command gateway is not running."
}

$catalog = @(
    @{
        key = "eggs"
        name = "Eggs"
        unit = "piece"
        aliases = @("egg")
        acceptedUnits = @("piece", "pieces", "pc")
    },
    @{
        key = "rice"
        name = "Rice"
        unit = "kg"
        aliases = @("white rice")
        acceptedUnits = @("kg", "kilogram", "kilograms")
    }
)

$cases = Get-Content -Raw $corpusPath | ConvertFrom-Json
$results = foreach ($case in $cases) {
    $payload = @{
        requestId = $case.id
        text = $case.text
        inventory = $catalog
    } | ConvertTo-Json -Depth 8 -Compress

    $started = Get-Date
    $json = $payload | docker run --rm -i --network $CommandNetwork `
        curlimages/curl:8.16.0 -sS --fail-with-body `
        -H "Content-Type: application/json" --data-binary "@-" `
        http://command-gateway:8080/v1/interpret
    if ($LASTEXITCODE -ne 0) {
        throw "Gateway request failed for corpus case $($case.id)."
    }
    $response = $json | ConvertFrom-Json
    if ($null -eq $response -or
        $response.status -notin @(
            "Recognized", "Unsupported", "ClarificationRequired", "InterpreterError")) {
        throw "Gateway returned an invalid response for corpus case $($case.id)."
    }
    $actualCommand = $response.proposal.command
    $exact = $response.status -eq $case.expectedStatus -and
        $actualCommand -eq $case.expectedCommand
    $safe = $response.status -in @(
        "Unsupported", "ClarificationRequired", "InterpreterError") -or
        ($case.expectedStatus -eq "Recognized" -and
         $actualCommand -eq $case.expectedCommand)
    Write-Host ("CASE={0} STATUS={1} COMMAND={2} EXACT={3} SAFE={4}" -f
        $case.id, $response.status, $actualCommand, $exact, $safe)

    [pscustomobject]@{
        id = $case.id
        text = $case.text
        expectedStatus = $case.expectedStatus
        expectedCommand = $case.expectedCommand
        actualStatus = $response.status
        actualCommand = $actualCommand
        exact = $exact
        safe = $safe
        durationMs = [math]::Round(((Get-Date) - $started).TotalMilliseconds)
        issues = @($response.issues)
    }
}

$summary = [pscustomobject]@{
    evaluatedUtc = (Get-Date).ToUniversalTime().ToString("O")
    model = "tinyllama:1.1b"
    total = $results.Count
    exact = @($results | Where-Object exact).Count
    safelyRefusedOrCorrect = @($results | Where-Object safe).Count
    unsafe = @($results | Where-Object { -not $_.safe }).Count
    averageDurationMs = [math]::Round(($results | Measure-Object durationMs -Average).Average)
    results = $results
}

New-Item -ItemType Directory -Force -Path $artifactDirectory | Out-Null
$summary | ConvertTo-Json -Depth 10 | Set-Content -Encoding utf8 $artifactPath

$results | Format-Table id, expectedStatus, expectedCommand, actualStatus, actualCommand, exact, safe, durationMs
"EXACT=$($summary.exact)/$($summary.total)"
"SAFE=$($summary.safelyRefusedOrCorrect)/$($summary.total)"
"UNSAFE=$($summary.unsafe)"
"AVERAGE_MS=$($summary.averageDurationMs)"
"ARTIFACT=$artifactPath"

if ($summary.unsafe -gt 0) {
    exit 2
}
