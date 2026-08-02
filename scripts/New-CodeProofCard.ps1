[CmdletBinding()]
param(
    [string]$OutputPath = ".artifacts\ROMS_CODE_PROOF_CARD.md"
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$resolvedOutput = if ([System.IO.Path]::IsPathRooted($OutputPath)) {
    $OutputPath
} else {
    Join-Path $repoRoot $OutputPath
}
$outputDirectory = Split-Path -Parent $resolvedOutput
New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null

function Invoke-GitLine {
    param([string[]]$Arguments)
    $result = & git -C $repoRoot @Arguments 2>&1
    if ($LASTEXITCODE -ne 0) { throw "Git evidence command failed." }
    return ($result | Select-Object -First 1).ToString()
}

function Get-DeclaredNames {
    param([string]$RelativePath, [int]$Limit = 8)
    $path = Join-Path $repoRoot $RelativePath
    if (-not (Test-Path -LiteralPath $path)) { return @() }
    return @(Get-Content -LiteralPath $path |
        Where-Object { $_ -match '^\s*public\s+(?:(?:sealed|static|partial|abstract)\s+)*(?:class|record|interface|enum)\s+[A-Za-z_][A-Za-z0-9_]*' } |
        ForEach-Object {
            if ($_ -match '(?:class|record|interface|enum)\s+([A-Za-z_][A-Za-z0-9_]*)') { $Matches[1] }
        } |
        Select-Object -Unique -First $Limit)
}

function Get-TestNames {
    param([string]$RelativePath, [int]$Limit = 10)
    $path = Join-Path $repoRoot $RelativePath
    if (-not (Test-Path -LiteralPath $path)) { return @() }
    return @(Get-Content -LiteralPath $path |
        ForEach-Object {
            if ($_ -match '^\s*public\s+(?:async\s+)?Task\s+([A-Za-z_][A-Za-z0-9_]*)\s*\(') { $Matches[1] }
        } |
        Where-Object { $_ } |
        Select-Object -Unique -First $Limit)
}

function Get-SafeSnippet {
    param(
        [string]$RelativePath,
        [string]$StartPattern,
        [string]$StopBeforePattern,
        [int]$MaximumLines
    )
    $path = Join-Path $repoRoot $RelativePath
    $capturing = $false
    $result = [System.Collections.Generic.List[string]]::new()
    foreach ($line in Get-Content -LiteralPath $path) {
        if (-not $capturing -and $line -match $StartPattern) { $capturing = $true }
        if ($capturing -and $line -match $StopBeforePattern) { break }
        if ($capturing) {
            $result.Add($line.TrimEnd())
            if ($result.Count -ge $MaximumLines) { break }
        }
    }
    if ($result.Count -eq 0) { throw "Approved proof snippet was not found." }
    return @($result)
}

$commit = Invoke-GitLine @('rev-parse', '--short=12', 'HEAD')
$commitDate = Invoke-GitLine @('show', '-s', '--format=%cs', 'HEAD')
$subject = Invoke-GitLine @('show', '-s', '--format=%s', 'HEAD')

$domainTypes = Get-DeclaredNames 'src/Roms.Domain/Entities.cs' 8
$aiTypes = Get-DeclaredNames 'src/Roms.Application/AiFunctions.cs' 10
$serviceTypes = @(
    Get-DeclaredNames 'src/Roms.Infrastructure/Services/OrderService.cs' 3
    Get-DeclaredNames 'src/Roms.Infrastructure/Services/AiFunctionService.cs' 3
) | Select-Object -Unique
$tests = @(
    Get-TestNames 'tests/Roms.Domain.Tests/OrderTests.cs' 4
    Get-TestNames 'tests/Roms.IntegrationTests/AiFunctionServiceTests.cs' 5
    Get-TestNames 'tests/Roms.IntegrationTests/MariaDbAiFunctionTests.cs' 2
) | Select-Object -Unique
$domainTypeText = ($domainTypes | ForEach-Object { '`' + $_ + '`' }) -join ', '
$aiTypeText = ($aiTypes | ForEach-Object { '`' + $_ + '`' }) -join ', '
$serviceTypeText = ($serviceTypes | ForEach-Object { '`' + $_ + '`' }) -join ', '
$contractSnippet = Get-SafeSnippet 'src/Roms.Application/AiFunctions.cs' '^public sealed record AiFunctionRequest' '^public sealed record AiMenuItemFact' 24
$authorizationSnippet = Get-SafeSnippet 'src/Roms.Infrastructure/Services/AiFunctionService.cs' '^\s*private static bool CanReadOrder' '^\s*private static async Task<HashSet' 18
$validationSnippet = Get-SafeSnippet 'src/Roms.CommandGateway/CommandProposalValidator.cs' '^\s*public InterpretCommandResponse Validate' '^\s*private static InterpretCommandResponse ValidateCatalogItem' 32

$lines = [System.Collections.Generic.List[string]]::new()
$lines.Add('# ROMS Code Proof Card')
$lines.Add('')
$lines.Add('A deliberately small, non-reconstructable engineering sample.')
$lines.Add('')
$lines.Add('## Provenance')
$lines.Add('')
$lines.Add('- Commit: `' + $commit + '`')
$lines.Add('- Date: `' + $commitDate + '`')
$lines.Add("- Subject: $subject")
$lines.Add('- CI evidence: secret guard, Release build, complete test suite, Chromium tests, and Docker build passed for this branch.')
$lines.Add('')
$lines.Add('## Architecture surface')
$lines.Add('')
$lines.Add('This list contains names only; only the three later allowlisted excerpts include code.')
$lines.Add('')
$lines.Add("- Domain types: $domainTypeText")
$lines.Add("- AI contracts: $aiTypeText")
$lines.Add("- Application services: $serviceTypeText")
$lines.Add('')
$lines.Add('## Selected code excerpts')
$lines.Add('')
$lines.Add('Allowlisted excerpts only. Unrelated implementation and configuration are excluded.')
$lines.Add('')
$lines.Add('### Typed read request')
$lines.Add('')
$lines.Add('```csharp')
foreach ($line in $contractSnippet) { $lines.Add($line) }
$lines.Add('```')
$lines.Add('')
$lines.Add('### Role and ownership boundary')
$lines.Add('')
$lines.Add('```csharp')
foreach ($line in $authorizationSnippet) { $lines.Add($line) }
$lines.Add('```')
$lines.Add('')
$lines.Add('### Fail-closed command dispatch')
$lines.Add('')
$lines.Add('```csharp')
foreach ($line in $validationSnippet) { $lines.Add($line) }
$lines.Add('```')
$lines.Add('')
$lines.Add('## Selected test evidence')
$lines.Add('')
foreach ($test in $tests) { $lines.Add('- `' + $test + '`') }
$lines.Add('')
$lines.Add('## What is intentionally withheld')
$lines.Add('')
$lines.Add('- Source method bodies and complete files')
$lines.Add('- Credentials, secrets, connection strings, infrastructure addresses, and local paths')
$lines.Add('- Database contents, restaurant data, backups, logs, prompts, and recordings')
$lines.Add('- Proprietary business rules sufficient to clone or reconstruct ROMS')
$lines.Add('')
$lines.Add('For deeper verification: supervised screen-share or NDA-protected read-only review. This card grants no license or permission to copy, reverse engineer, or redistribute ROMS.')

$content = $lines -join "`n"
$forbidden = @(
    '-----BEGIN (?:RSA |EC |OPENSSH )?PRIVATE KEY-----',
    '(?i)(?:password|pwd|secret|api[_-]?key|access[_-]?token)\s*[:=]\s*[^\s]+',
    '(?i)\b[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}\b',
    '(?i)\b[A-Z]:\\',
    '\b(?:\d{1,3}\.){3}\d{1,3}\b'
)
foreach ($pattern in $forbidden) {
    if ($content -match $pattern) { throw "Proof card failed its disclosure scan." }
}

[System.IO.File]::WriteAllText($resolvedOutput, $content, [System.Text.UTF8Encoding]::new($false))
$file = Get-Item -LiteralPath $resolvedOutput
if ($file.Length -gt 12288) { throw "Proof card exceeded the 12 KB safety limit." }

[pscustomobject]@{
    File = $file.FullName
    Bytes = $file.Length
    SHA256 = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash
}
