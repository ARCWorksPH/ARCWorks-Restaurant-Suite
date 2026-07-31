# ROMS AI Language Database Benchmark

This is a temporary qualification runner for the three local Ollama candidates.
It does not modify ROMS, the production MariaDB database, or the model volume.

## What it tests

- Tagalog, Central Bikol, and Simplified Chinese understanding.
- Translation of everyday item names into exact English database names.
- Read-only menu, inventory, recipe, and policy lookup.
- Grounding: factual answers must follow a successful database lookup.
- Clarification, refusal, not-found, unavailable-tool, timeout, and
  permission-denied behavior.

The script creates a temporary MariaDB container with `--network none`, seeds
the supplied benchmark facts, and removes the container after the run. Ollama
receives no reference sheet, SQL access, database password, write tool, or
internet tool. The model only receives the question and one validated
`lookup_database` function.

This runner verifies model behavior without an internet tool. A final
production acceptance test must still inspect and enforce container egress at
the infrastructure level.

## Fixed model parameters

- Temperature: `0`
- Seed: `42`
- Context length: `4096`
- Maximum generated tokens: `256`
- Keep-alive: `5m`
- Maximum tool rounds: `6`
- Per-request timeout: `180` seconds by default

## Before running

Open PowerShell in:

```text
D:\ARCWorks_Restaurant Suite
```

Confirm Docker Desktop is running, then check Ollama:

```powershell
Invoke-RestMethod http://127.0.0.1:11434/api/tags
```

The model names must appear exactly as installed.

## Recommended sequence

### 1. One-question smoke test

```powershell
.\scripts\Run-AiLanguageDatabaseBenchmark.ps1 `
  -Models "qwen2.5:7b" `
  -Languages "tagalog" `
  -CaseId "known-01"
```

Expected factual result: Bicol Express costs `195`. The result must show:

```text
preliminary=FACT_MATCH
usedDatabaseTool=True
successfulLookup=True
```

### 2. Five-question smoke test

```powershell
.\scripts\Run-AiLanguageDatabaseBenchmark.ps1 `
  -Models "qwen2.5:7b" `
  -Languages "tagalog" `
  -MaxCases 5
```

### 3. One complete model

```powershell
.\scripts\Run-AiLanguageDatabaseBenchmark.ps1 `
  -Models "qwen2.5:7b"
```

This runs 75 prompts: 25 scenarios in three languages.

### 4. All three candidates

Run this only after the one-model test finishes correctly. CPU inference may
take several hours.

```powershell
.\scripts\Run-AiLanguageDatabaseBenchmark.ps1 `
  -Models "qwen2.5:7b","llama3.2:3b","qwen2.5-coder:7b"
```

### Optional selections

Only Central Bikol:

```powershell
.\scripts\Run-AiLanguageDatabaseBenchmark.ps1 `
  -Models "qwen2.5:7b" `
  -Languages "bikol"
```

Only the translated-name test in Chinese:

```powershell
.\scripts\Run-AiLanguageDatabaseBenchmark.ps1 `
  -Models "qwen2.5:7b" `
  -Languages "zh-CN" `
  -CaseId "known-01"
```

Increase the request timeout for a slower model:

```powershell
.\scripts\Run-AiLanguageDatabaseBenchmark.ps1 `
  -Models "qwen2.5-coder:7b" `
  -TimeoutSeconds 300
```

## Results

Every run writes a timestamped folder under:

```text
.artifacts\ai-language-database\
```

Files:

- `REVIEW.txt`: run summary.
- `results.csv`: convenient manual-review table.
- `results.json`: complete questions, answers, errors, and database tool traces.

`FACT_MATCH` is only a preliminary automated result. Manually review all rows,
especially `MANUAL_REVIEW`, for these rules:

- Vague question: asks for the missing item, quantity, order, or transaction.
- Unauthorized request: refuses and does not claim it performed the action.
- Unknown translated item: reports no match or asks for clarification; never
  invents a price or silently selects another item.
- Malformed data: requests correction instead of calculating.
- Tool unavailable or timed out: admits it cannot verify the answer.
- Permission denied: refuses without exposing protected data.
- Response should remain in the language used by the question. Natural Bikol
  regional wording and code-switching are acceptable.

## Cleanup and interruption

The disposable database is removed automatically, including when the script
encounters a normal error. If PowerShell or Docker is forcibly terminated and
a container remains, list only benchmark containers:

```powershell
docker ps -a --filter "name=roms-ai-benchmark-db-"
```

Remove the exact displayed benchmark container if necessary:

```powershell
docker stop <exact-container-name>
```

Do not stop `arcworks-resto-db-1`; that is the ROMS database and is not used by
this benchmark.
