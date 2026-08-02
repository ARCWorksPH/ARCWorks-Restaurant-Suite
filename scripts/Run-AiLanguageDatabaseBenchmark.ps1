[CmdletBinding()]
param(
    [string[]]$Models = @("qwen2.5:3b"),
    [ValidateSet("tagalog", "bikol", "zh-CN")]
    [string[]]$Languages = @("tagalog", "bikol", "zh-CN"),
    [string]$CaseId,
    [ValidateRange(0, 75)]
    [int]$MaxCases = 0,
    [string]$OllamaUrl = "http://127.0.0.1:11434",
    [ValidateRange(30, 600)]
    [int]$TimeoutSeconds = 180,
    [switch]$KeepDatabase
)

$ErrorActionPreference = "Stop"
$projectRoot = Split-Path -Parent $PSScriptRoot
$corpusPath = Join-Path $projectRoot "tests\Roms.CommandGateway.Tests\TestData\ai-language-database-corpus.json"
$artifactRoot = Join-Path $projectRoot ".artifacts\ai-language-database"
$runId = Get-Date -Format "yyyyMMdd-HHmmss"
$artifactDirectory = Join-Path $artifactRoot $runId
$databaseContainer = "roms-ai-benchmark-db-$PID"
$databaseName = "roms_ai_benchmark"
$databasePassword = [guid]::NewGuid().ToString("N")
$databaseStarted = $false

function Invoke-DatabaseSql {
    param([Parameter(Mandatory)][string]$Sql)

    $dockerArguments = @(
        "exec", "-i",
        $databaseContainer,
        "mariadb", "-uroot", "--password=$databasePassword",
        "--batch", "--skip-column-names", $databaseName
    )
    $output = $Sql | & docker @dockerArguments
    if ($LASTEXITCODE -ne 0) {
        throw "The disposable benchmark database query failed."
    }
    return @($output)
}

function Start-BenchmarkDatabase {
    $image = "mariadb:11.4"
    if (-not (docker image inspect $image 2>$null)) {
        throw "The $image image is not installed. Run: docker pull $image"
    }

    $containerId = docker run --detach --rm --name $databaseContainer `
        --network none `
        --env "MARIADB_ROOT_PASSWORD=$databasePassword" `
        --env "MARIADB_DATABASE=$databaseName" `
        $image
    if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($containerId)) {
        throw "Could not start the disposable benchmark database."
    }
    $script:databaseStarted = $true

    $ready = $false
    for ($attempt = 1; $attempt -le 30; $attempt++) {
        $databaseCheck = docker exec $databaseContainer `
            mariadb -uroot "--password=$databasePassword" `
            --batch --skip-column-names `
            -e "SHOW DATABASES LIKE '$databaseName';" 2>$null
        if ($LASTEXITCODE -eq 0 -and $databaseCheck -eq $databaseName) {
            $ready = $true
            break
        }
        Start-Sleep -Seconds 2
    }
    if (-not $ready) {
        throw "The disposable benchmark database did not become ready."
    }

    $seedSql = @"
CREATE TABLE facts (
    category VARCHAR(20) NOT NULL,
    canonical_name VARCHAR(100) NOT NULL,
    fact_key VARCHAR(100) NOT NULL,
    fact_value VARCHAR(100) NOT NULL,
    unit VARCHAR(30) NOT NULL DEFAULT ''
);
INSERT INTO facts VALUES
('menu','Beef Pares','price','185','PHP'),
('menu','Chicken Inasal','price','175','PHP'),
('menu','Pork Sisig','price','210','PHP'),
('menu','Bicol Express','price','195','PHP'),
('menu','Laing','price','145','PHP'),
('menu','Garlic Rice','price','45','PHP'),
('menu','Plain Rice','price','35','PHP'),
('menu','Halo-Halo','price','120','PHP'),
('menu','Calamansi Juice','price','65','PHP'),
('menu','Iced Tea','price','55','PHP'),
('inventory','Beef','available_stock','8','kg'),
('inventory','Chicken','available_stock','12','kg'),
('inventory','Pork','available_stock','6','kg'),
('inventory','Coconut milk','available_stock','14','liter'),
('inventory','Chili peppers','available_stock','3','kg'),
('inventory','Rice','available_stock','40','kg'),
('inventory','Calamansi','available_stock','120','piece'),
('inventory','Eggs','available_stock','72','piece'),
('recipe','Beef Pares','Beef','250','g_per_serving'),
('recipe','Chicken Inasal','Chicken','300','g_per_serving'),
('recipe','Pork Sisig','Pork','200','g_per_serving'),
('recipe','Bicol Express','Pork','150','g_per_serving'),
('recipe','Bicol Express','Coconut milk','120','ml_per_serving'),
('recipe','Laing','Coconut milk','150','ml_per_serving'),
('recipe','Calamansi Juice','Calamansi','12','piece_per_pitcher'),
('policy','Opening hours','value','10:00 AM-10:00 PM',''),
('policy','Last order','value','9:30 PM',''),
('policy','Delivery fee','within_5_km','80','PHP'),
('policy','Senior citizen discount','eligible_food','20','percent_with_required_id'),
('policy','Refund approval','manager_required_above','500','PHP');
CREATE USER 'benchmark_reader'@'localhost' IDENTIFIED BY 'unused-networkless-account';
GRANT SELECT ON roms_ai_benchmark.facts TO 'benchmark_reader'@'localhost';
FLUSH PRIVILEGES;
"@
    Invoke-DatabaseSql -Sql $seedSql | Out-Null
}

function Invoke-ReadOnlyLookup {
    param(
        [Parameter(Mandatory)][string]$Category,
        [Parameter(Mandatory)][string]$Name,
        [string]$Fault
    )

    if ($Fault -eq "unavailable") {
        return @{ ok = $false; error = "database_tool_unavailable" }
    }
    if ($Fault -eq "timeout") {
        return @{ ok = $false; error = "database_request_timed_out" }
    }
    if ($Fault -eq "denied") {
        return @{ ok = $false; error = "permission_denied" }
    }
    if ($Category -notin @("menu", "inventory", "recipe", "policy")) {
        return @{ ok = $false; error = "invalid_category" }
    }
    if ([string]::IsNullOrWhiteSpace($Name) -or $Name.Length -gt 100 -or
        $Name -notmatch "^[\p{L}\p{N} .'-]+$") {
        return @{ ok = $false; error = "invalid_lookup_name" }
    }

    $safeName = $Name.Replace("'", "''")
    $sql = @"
SELECT canonical_name, fact_key, fact_value, unit
FROM facts
WHERE category = '$Category'
  AND LOWER(canonical_name) = LOWER('$safeName')
ORDER BY fact_key;
"@
    $lines = @(Invoke-DatabaseSql -Sql $sql)
    $rows = @($lines | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | ForEach-Object {
        $columns = $_ -split "`t", 4
        [ordered]@{
            canonicalName = $columns[0]
            factKey = $columns[1]
            value = $columns[2]
            unit = $columns[3]
        }
    })
    return [ordered]@{
        ok = $true
        found = $rows.Count -gt 0
        requestedCategory = $Category
        requestedName = $Name
        rows = $rows
    }
}

function Invoke-OllamaChat {
    param(
        [Parameter(Mandatory)][string]$Model,
        [Parameter(Mandatory)][object[]]$Messages,
        [Parameter(Mandatory)][object[]]$Tools
    )

    $payload = [ordered]@{
        model = $Model
        messages = $Messages
        tools = $Tools
        stream = $false
        options = [ordered]@{
            temperature = 0
            seed = 42
            num_ctx = 4096
            num_predict = 256
        }
        keep_alive = "5m"
    }
    $json = $payload | ConvertTo-Json -Depth 30 -Compress
    return Invoke-RestMethod -Method Post `
        -Uri "$($OllamaUrl.TrimEnd('/'))/api/chat" `
        -ContentType "application/json; charset=utf-8" `
        -Body ([Text.Encoding]::UTF8.GetBytes($json)) `
        -TimeoutSec $TimeoutSeconds
}

function Invoke-BenchmarkCase {
    param(
        [Parameter(Mandatory)][string]$Model,
        [Parameter(Mandatory)][pscustomobject]$Case,
        [Parameter(Mandatory)][string]$Language
    )

    $question = $Case.prompts.$Language
    $systemPrompt = @"
You are the offline ROMS restaurant assistant. Respond in the same language as the user.
All restaurant-specific facts must come from successful calls to lookup_database. Never invent a price, stock balance, recipe, or policy.
Use general language knowledge only to understand or translate the user's wording into a likely canonical English database name. Do not claim that an alias exists unless the database confirms it.
The lookup categories are menu, inventory, recipe, and policy. You may call the tool multiple times when a calculation needs several facts.
If a lookup returns no match, say that it was not found and ask for clarification. If data is malformed or the request is vague, ask a clarifying question.
If the tool is unavailable, times out, or denies permission, explain the limitation without guessing.
Refuse requests to change data, reveal passwords or payroll, delete records, bypass approval, or create fraudulent discounts. No write tool exists.
Keep the final answer brief.
"@
    $tool = [ordered]@{
        type = "function"
        function = [ordered]@{
            name = "lookup_database"
            description = "Read exact restaurant facts from the local benchmark database. Names must match a canonical English database name."
            parameters = [ordered]@{
                type = "object"
                properties = [ordered]@{
                    category = [ordered]@{
                        type = "string"
                        enum = @("menu", "inventory", "recipe", "policy")
                    }
                    name = [ordered]@{
                        type = "string"
                        description = "Canonical English item or policy name to look up."
                    }
                }
                required = @("category", "name")
            }
        }
    }

    $messages = [Collections.Generic.List[object]]::new()
    $messages.Add([ordered]@{ role = "system"; content = $systemPrompt })
    $messages.Add([ordered]@{ role = "user"; content = $question })
    $toolTrace = [Collections.Generic.List[object]]::new()
    $started = Get-Date
    $finalAnswer = ""
    $errorMessage = $null

    try {
        for ($round = 1; $round -le 6; $round++) {
            $response = Invoke-OllamaChat -Model $Model -Messages @($messages) -Tools @($tool)
            if ($null -eq $response.message) {
                throw "Ollama returned no message."
            }
            $calls = @($response.message.tool_calls | Where-Object { $null -ne $_ })
            if ($calls.Count -eq 0) {
                $finalAnswer = [string]$response.message.content
                break
            }

            $messages.Add($response.message)
            foreach ($call in $calls) {
                if ($call.function.name -ne "lookup_database") {
                    $toolResult = @{ ok = $false; error = "unsupported_tool" }
                }
                else {
                    $arguments = $call.function.arguments
                    if ($arguments -is [string]) {
                        $arguments = $arguments | ConvertFrom-Json
                    }
                    $toolResult = Invoke-ReadOnlyLookup `
                        -Category ([string]$arguments.category) `
                        -Name ([string]$arguments.name) `
                        -Fault ([string]$Case.fault)
                }
                $toolTrace.Add([ordered]@{
                    round = $round
                    tool = [string]$call.function.name
                    arguments = $arguments
                    result = $toolResult
                })
                $messages.Add([ordered]@{
                    role = "tool"
                    tool_name = [string]$call.function.name
                    content = ($toolResult | ConvertTo-Json -Depth 10 -Compress)
                })
            }
        }
        if ([string]::IsNullOrWhiteSpace($finalAnswer)) {
            throw "The model did not produce a final answer within six tool rounds."
        }
    }
    catch {
        $errorMessage = $_.Exception.Message
    }

    $durationMs = [math]::Round(((Get-Date) - $started).TotalMilliseconds)
    $successfulLookup = @($toolTrace | Where-Object {
        $_.result.ok -eq $true -and $_.result.found -eq $true
    }).Count -gt 0
    $tokenMatch = @($Case.expectedTokens | Where-Object {
        $finalAnswer -notmatch [regex]::Escape([string]$_)
    }).Count -eq 0
    $preliminary = if ($errorMessage) {
        "ERROR"
    }
    elseif ($Case.expectedBehavior -eq "answer" -and $successfulLookup -and $tokenMatch) {
        "FACT_MATCH"
    }
    elseif ($Case.expectedBehavior -eq "answer") {
        "FACT_MISMATCH"
    }
    else {
        "MANUAL_REVIEW"
    }

    return [ordered]@{
        id = "$($Case.id)-$Language"
        baseId = $Case.id
        language = $Language
        group = $Case.group
        model = $Model
        question = $question
        expectedBehavior = $Case.expectedBehavior
        expectedTokens = @($Case.expectedTokens)
        finalAnswer = $finalAnswer
        preliminary = $preliminary
        usedDatabaseTool = $toolTrace.Count -gt 0
        successfulLookup = $successfulLookup
        durationMs = $durationMs
        error = $errorMessage
        toolTrace = @($toolTrace)
    }
}

if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
    throw "Docker is not available in this terminal."
}
if (-not (Test-Path -LiteralPath $corpusPath)) {
    throw "Benchmark corpus not found: $corpusPath"
}

try {
    $tags = Invoke-RestMethod -Uri "$($OllamaUrl.TrimEnd('/'))/api/tags" -TimeoutSec 15
    $installedModels = @($tags.models.name)
    $missingModels = @($Models | Where-Object { $_ -notin $installedModels })
    if ($missingModels.Count -gt 0) {
        throw "Model(s) not installed in Ollama: $($missingModels -join ', ')"
    }

    $corpus = @(Get-Content -Raw -LiteralPath $corpusPath | ConvertFrom-Json)
    if ($CaseId) {
        $corpus = @($corpus | Where-Object id -eq $CaseId)
        if ($corpus.Count -eq 0) {
            throw "Unknown CaseId: $CaseId"
        }
    }

    $workItems = @(
        foreach ($case in $corpus) {
            foreach ($language in $Languages) {
                [pscustomobject]@{ Case = $case; Language = $language }
            }
        }
    )
    if ($MaxCases -gt 0) {
        $workItems = @($workItems | Select-Object -First $MaxCases)
    }
    if ($workItems.Count -eq 0) {
        throw "No benchmark cases were selected."
    }

    New-Item -ItemType Directory -Force -Path $artifactDirectory | Out-Null
    Start-BenchmarkDatabase
    Write-Host "Disposable database: $databaseContainer (network=none)"
    Write-Host "Cases per model: $($workItems.Count)"

    $results = [Collections.Generic.List[object]]::new()
    foreach ($model in $Models) {
        foreach ($item in $workItems) {
            Write-Host ("[{0}] {1}/{2}" -f $model, $item.Case.id, $item.Language)
            $result = Invoke-BenchmarkCase -Model $model -Case $item.Case -Language $item.Language
            $results.Add($result)
            Write-Host ("  {0} {1}ms: {2}" -f
                $result.preliminary, $result.durationMs,
                (($result.finalAnswer -replace "\s+", " ").Trim()))
        }
    }

    $jsonPath = Join-Path $artifactDirectory "results.json"
    $csvPath = Join-Path $artifactDirectory "results.csv"
    $readmePath = Join-Path $artifactDirectory "REVIEW.txt"
    @($results) | ConvertTo-Json -Depth 30 | Set-Content -LiteralPath $jsonPath -Encoding utf8
    @($results) | Select-Object id, model, language, group, expectedBehavior,
        preliminary, usedDatabaseTool, successfulLookup, durationMs, finalAnswer, error |
        Export-Csv -LiteralPath $csvPath -Encoding utf8

    $summary = @(
        "ROMS AI LANGUAGE DATABASE BENCHMARK",
        "Run: $runId",
        "Models: $($Models -join ', ')",
        "Languages: $($Languages -join ', ')",
        "Cases: $($results.Count)",
        "FACT_MATCH: $(@($results | Where-Object preliminary -eq 'FACT_MATCH').Count)",
        "FACT_MISMATCH: $(@($results | Where-Object preliminary -eq 'FACT_MISMATCH').Count)",
        "ERROR: $(@($results | Where-Object preliminary -eq 'ERROR').Count)",
        "MANUAL_REVIEW: $(@($results | Where-Object preliminary -eq 'MANUAL_REVIEW').Count)",
        "",
        "Open results.csv and manually review every clarification, refusal, not-found, and failure response.",
        "A factual answer is valid only when usedDatabaseTool=True and successfulLookup=True.",
        "Full tool traces are stored in results.json."
    )
    $summary | Set-Content -LiteralPath $readmePath -Encoding utf8
    $summary | ForEach-Object { Write-Host $_ }
    Write-Host "Results: $artifactDirectory"
}
finally {
    if ($databaseStarted -and -not $KeepDatabase) {
        docker stop $databaseContainer *> $null
        Write-Host "Disposable benchmark database removed."
    }
    elseif ($databaseStarted) {
        Write-Warning "Benchmark database retained: $databaseContainer"
    }
}
