# ROMS AI Benchmark 2 — Evidence and Decision

Date: 2026-08-02

Status: **qualitative evidence only; not a production model qualification**

## Purpose

This user-operated, offline benchmark explored how small local models respond
to multilingual restaurant prompts, ambiguous or malformed requests, security
requests, and simple factual questions. It was useful for observing whether a
model asks for clarification or answers confidently without sufficient facts.

The run did **not** test ROMS database grounding. The Python script sent only
the question to Ollama: it supplied no ROMS database, approved tool, system
prompt, schema, or reference dataset to the model.

## Evidence map

- `Arkworks_AI_Benchmark.py` — exact user-created harness, preserved with its
  known defects for auditability.
- `benchmark_logs/*.json` — three completed reports.
- `benchmark_logs/*.log` — incomplete Qwen Coder and later partial attempts.
- `Terminal Raw Data/*.txt` — terminal transcripts for the three completed
  runs.
- `Video/` — retained locally only. The recording is about 6.1 GB, contains a
  full desktop capture, and is deliberately excluded from Git.

The raw model text includes fabricated passwords and unsafe suggestions. These
are failure evidence, not real ROMS credentials or approved instructions.

## Completed-run measurements

| Model | Raw displayed score | Duplicate-adjusted heuristic | Mean latency | P95 latency | Average generation |
|---|---:|---:|---:|---:|---:|
| Llama 3.2 3B | 68/75 (90.67%) | 34/75 (45.33%) | 14.62 s | 34.90 s | 15.43 t/s |
| Qwen 2.5 7B | 62/75 (82.67%) | 31/75 (41.33%) | 28.42 s | 54.31 s | 7.34 t/s |
| TinyLlama 1.1B | 40/75 (53.33%) | 20/75 (26.67%) | 2.49 s | 6.98 s | 49.27 t/s |

The Qwen 2.5 Coder 7B run completed 20 questions and began question 21 before
it was stopped. It has no complete JSON report and is not comparable with the
three completed runs.

## Score-integrity limitations

The harness contains a duplicated evaluation-and-append block. Each answer was
scored twice and written twice, producing 150 rows while retaining a denominator
of 75. Taking one row from each duplicate pair yields the arithmetic-adjusted
figures above.

Those adjusted figures are **not accuracy percentages**. The heuristic grader
uses permissive substring and keyword checks that awarded passes to factually
wrong and unsafe answers. Examples include a number matching inside another
number, a refusal keyword appearing inside a sentence that actually complied,
and an expected ingredient quantity appearing alongside the wrong ingredient.

The script also did not record the model name or digest in each JSON report,
used shuffled order unless instructed otherwise, had no request timeout or
checkpoint/resume support, and sampled GPU usage only through `nvidia-smi`.
Consequently, its GPU fields are null on the AMD workstation.

## Qualitative findings

- All evaluated models sometimes answered ambiguous or nonsensical requests
  confidently instead of asking a clarifying question.
- Safety behavior was inconsistent. Raw evidence includes fabricated account
  credentials, unauthorized refund guidance, and suggestions to conceal price
  changes.
- Multilingual comprehension varied materially by language and phrasing.
- TinyLlama was fast but clearly inadequate for the intended role.
- Neither the completed scores nor the incomplete Coder run support production
  approval for any model.
- Later process-level checks confirmed the benchmark Ollama runner used CPU;
  the observed 90% GPU activity was reproduced without Ollama and attributed
  to an unconfigured OBS installation.

## Finalized product direction

1. AI is removed from the critical path for completing the ROMS beta.
2. The AI feature remains isolated and disabled until a controlled functional
   qualification passes.
3. A future assistant may interpret multilingual intent, ask clarifying
   questions, and explain results, but ROMS remains the source of truth.
4. Database facts and calculations must come from a small allowlist of
   validated, permission-aware ROMS functions. The model must not execute
   arbitrary SQL or invent operational data.
5. A separate SQL model and a two-model user-facing selector are deferred until
   beta evidence demonstrates a genuine need.
6. Remote or network GPU inference is a future expansion option, not a current
   dependency. Separate machines may handle complete requests; this benchmark
   does not establish distributed model execution or GPU acceleration.

## Reproduction warning

The supplied script is preserved as historical evidence and should **not** be
used for another scoring run without first removing the duplicate block,
replacing the heuristic grader, adding provenance and timeouts, and defining a
database-grounded test boundary. Existing raw evidence must remain unchanged.
