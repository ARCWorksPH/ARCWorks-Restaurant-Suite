# Local AI Benchmark Evidence

Date: 2026-07-31

This folder preserves the first controlled local-model benchmark used to
shortlist models for the ROMS AI laboratory.

## Decision

- Provisional laboratory model: `qwen2.5:7b`
- Retained challengers: `llama3.2:3b` and `qwen2.5-coder:7b`
- Removed from local Ollama storage: `tinyllama:1.1b` and `phi3:3.8b`
- Production approval: **not granted**

The provisional choice is based on the strongest factual response in this
single-session benchmark. It must still pass ROMS-specific grounding, tool,
permission, failure-recovery, and concurrency tests.

## Evidence map

- `RAW DATA/`: source transcripts, resource samples, and generated charts.
- `FULL EVAL 5 MODELS.docx` and `.pdf`: supplied five-model evaluation.
- `USE CASE EVAL.docx`: supplied use-case assessment.
- `INDEPENDENT EVALUATION/INDEPENDENT_EVALUATION.md`: independent findings,
  evidence limitations, shortlist, and required next benchmark.
- `INDEPENDENT EVALUATION/analyze_benchmark.py`: reproducible metric extraction.
- `INDEPENDENT EVALUATION/derived_model_summary.csv`: derived comparison table.
- `tools/ARKTECH-RESOURCE-MONITOR-MINI.py` at repository root: sampling tool
  used to capture CPU, RAM, optional GPU, and Ollama process data.

## Reproduce the derived summary

From the repository root:

```powershell
python ".\docs\AI Model Benchmark\BEST CONDITION\INDEPENDENT EVALUATION\analyze_benchmark.py"
```

The script reads the committed TXT and CSV files and rewrites
`derived_model_summary.csv`.

## Evidence boundaries

- Runs were single-session and mostly single-trial.
- The prompt was general writing, not a restaurant-operation task.
- No database-tool accuracy, permissions, refusal, malformed-result recovery,
  or overlapping-user behavior was measured.
- GPU, VRAM, and Ollama RSS fields were not populated consistently enough for
  model comparison.
- The raw Llama transcript also contains earlier TinyLlama sessions; the
  analysis script selects its final PowerShell session.

Copied terminal prompts were anonymized before publication. Personal author
metadata was removed from the Word and PDF reports. The redundant
`BEST CONDITION.zip` is intentionally not versioned because this folder is the
editable source of truth.
