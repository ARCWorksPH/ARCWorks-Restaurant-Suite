# ROMS AI Benchmark 3 — Comparison and Decision

Date: 2026-08-02
Status: completed laboratory comparison; no production AI approval

## Decision

- Retain `qwen2.5:3b` as the provisional user-facing laboratory default.
- Retain `qwen3:4b-instruct` as a read-only factual and reporting challenger.
- Do not allow either model to generate arbitrary SQL, bypass ROMS
  authorization, or directly mutate prices, discounts, refunds, payroll,
  orders, or inventory.
- Preserve all raw evidence for removed candidates.

The three top totals are tied, but they are not behaviorally equivalent.
`qwen2.5:3b` is the most balanced candidate. `phi4-mini:3.8b` was strongest on
safety but weaker and slower on facts. `qwen3:4b-instruct` was strongest on
facts and graceful failure but weak on ambiguous clarification.

## Strict manual comparison

The benchmark was run with operator review disabled so that the user could
watch without manually scoring every answer. Codex subsequently graded all
full-run answers using one strict rubric: an answer passes only when it performs
the expected behavior without a conflicting claim or unsafe action.

| Model | Facts A /30 | Clarification B /15 | Safety C /15 | Failure handling D /15 | Total |
|---|---:|---:|---:|---:|---:|
| `qwen2.5:3b` | 18 | 12 | 13 | 13 | **56/75 (74.7%)** |
| `phi4-mini:3.8b` | 16 | 12 | **15** | 13 | **56/75 (74.7%)** |
| `qwen3:4b-instruct` | **23** | 6 | 12 | **15** | **56/75 (74.7%)** |
| `gemma3:4b` | **23** | 11 | 7 | 11 | 52/75 (69.3%) |
| `granite3.3:2b` | 17 | 8 | 12 | 14 | 51/75 (68.0%) |

The generic `qwen3:4b` thinking tag is excluded from the ranking. All 75
answers reached the 256-token cap and nearly all failed to expose a usable
final answer.

## Full-run performance

| Model | Mean response | P95 response | Mean generation | Full run | 256-token cap hits |
|---|---:|---:|---:|---:|---:|
| `qwen2.5:3b` | 4.12 s | 8.68 s | 10.20 tok/s | 5.2 min | 0 |
| `granite3.3:2b` | 5.19 s | 10.91 s | 11.61 tok/s | 6.5 min | 0 |
| `qwen3:4b-instruct` | 6.94 s | 13.74 s | 9.22 tok/s | 8.67 min | 0 |
| `phi4-mini:3.8b` | 8.16 s | 14.04 s | 8.04 tok/s | 10.2 min | 0 |
| `gemma3:4b` | 12.57 s | 16.68 s | 7.82 tok/s | 15.7 min | 0 |
| `qwen3:4b` thinking tag | 35.65 s | — | — | 44.6 min | 75 |

All full runs reported `size_vram_bytes=0`; model inference was CPU-only in the
tested Docker Desktop environment. Windows aggregate GPU activity is not used
as model-placement evidence.

## Qwen 3 instruct retest

The valid full retest is run
`20260802_114128_qwen3_4b-instruct_grounded`. It completed 75/75 questions with
zero request errors, no exposed `<think>` markers, and no token-cap hits.

Full report SHA-256:

```text
5840B15E72BFFACCFC2B8B9F83B6CDB9617B4F7952C34A5E509DBF2FAFCA9D79
```

Important strict-grading failures included:

- poor Central Bikol clarification and several missed Central Bikol facts;
- acceptance of a fraudulent senior discount in one Central Bikol case;
- a contradictory interpretation of a no-approval refund request;
- Chinese confusion between Garlic Rice and Laing, and between Laing and
  Bicol Express;
- one correct pork calculation followed by a conflicting inventory statement.

## Storage disposition

After evidence preservation, these model tags were removed from Ollama:

- `gemma3:4b`
- `granite3.3:2b`
- `phi4-mini:3.8b`
- `qwen3:4b`
- `tinyllama:1.1b`
- `qwen2.5:7b`
- `llama3.2:3b`
- `qwen2.5-coder:7b`

The retained tags are:

- `qwen2.5:3b` — digest prefix `357c53fb659c`, approximately 1.9 GB
- `qwen3:4b-instruct` — digest prefix `0edcdef34593`, approximately 2.5 GB

Container-side `du` reduced from approximately 25 GB to 4.2 GB. Removed model
weights are not backed up locally because they are reproducible downloads;
the scripts, reports, responses, transcripts, provenance, and manual decision
remain preserved.

## Evidence boundary

This benchmark compares responses against a fixed supplied fixture. It is not
a live ROMS database test and does not establish production correctness. RAM
figures across earlier runs are not ranked because Ollama's ten-minute
`keep_alive` could leave a previous model resident. Live MariaDB retrieval,
authorization, concurrency, timeout, and browser acceptance remain separate
gates.
