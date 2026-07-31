# ROMS Local Model Benchmark - Independent Evaluation

Date: 2026-07-31

## Technical summary

This first benchmark is useful for eliminating weak candidates and measuring
single-session CPU generation speed. It is not sufficient to select the final
ROMS model because it did not exercise database tools, structured arguments,
grounded answers, refusals, tool errors, or concurrent users.

The defensible shortlist is:

1. `llama3.2:3b` as the latency candidate.
2. `qwen2.5:7b` as the accuracy candidate.
3. `qwen2.5-coder:7b` as a structured-output challenger.

`tinyllama:1.1b` should be rejected. `phi3:3.8b` adds no demonstrated advantage
over Llama 3.2 for ROMS and its captured metadata does not list tool support.

No final production model should be selected until the three shortlisted models
pass a ROMS-specific tool and concurrency benchmark.

## What the benchmark actually established

| Model | Generation rate | Captured capabilities | Independent essay assessment |
|---|---:|---|---|
| `tinyllama:1.1b` | 43.66 t/s | completion | Rejected |
| `llama3.2:3b` | 14.38 t/s | completion, tools | Mixed |
| `phi3:3.8b` | 12.37 t/s | completion | Mixed |
| `qwen2.5-coder:7b` | 7.19 t/s | completion, tools, insert | Good |
| `qwen2.5:7b` | 7.13 t/s | completion, tools | Best of this essay set |

The approximately 2x speed advantage of Llama 3.2 over the Qwen 7B models is
real for this one long-generation run. It does not establish better latency
under concurrency, because each model was measured once and no requests
overlapped.

The Qwen 7B essay was the strongest factual response, but the test prompt was a
general essay rather than a restaurant operation or tool-use task. Long-form
writing quality is therefore supporting evidence, not the selection criterion.

## Data-quality findings

### High: the use-case rankings are not measured

The supplied use-case evaluation assigns ratings for tool-calling reliability,
hallucination risk, database adherence, and concurrent suitability. None of
those behaviors were tested by the recorded sky-essay prompt. Those ratings are
hypotheses and must not be treated as benchmark results.

### High: concurrency conclusions are unsupported

All captured generations are single-session runs. Claims about three to ten
users, queuing, or stability require overlapping requests with recorded p50 and
p95 latency, error rate, and resource saturation.

### Medium: memory and GPU measurements are unusable for comparison

Every GPU and VRAM field is blank, and every Ollama RSS sample is zero. Total
system RAM started at materially different levels across runs. The small
"initial to peak" changes do not represent model memory because the model may
already have been loaded before monitoring began.

### Medium: one run per candidate gives no variance

There are no repeated cold and warm trials for the 3B, 3.8B, or 7B candidates.
The 0.13-second Qwen Coder load versus the 14.33-second regular Qwen load is a
strong sign that cache state differed. At least three cold and three warm runs
are needed for stable latency comparisons.

### Medium: the Llama transcript is a merged file

The Llama TXT begins with three TinyLlama generations and ends with the actual
Llama 3.2 session. The recomputation deliberately uses only the final session.
The matching Llama CSV appears consistent with that final run.

### Low: `num_gpu 99` does not prove GPU execution

The setting requests GPU layers but the captured monitoring has no GPU values.
The current evidence supports CPU-path performance only.

## Independent quality review

- `tinyllama:1.1b` invents mechanisms involving oxygen, water vapor, a
  fabricated "blue moon" pattern, and incorrect history. Its speed is not useful
  for factual business assistance.
- `llama3.2:3b` is readable but misattributes parts of the scientific history
  and makes inaccurate claims about scattering and sunrise color.
- `phi3:3.8b` is coherent but includes several incorrect or overstated
  scientific, historical, and practical claims.
- `qwen2.5:7b` provides the best response in this set, though it still blurs
  molecular and particulate scattering in places.
- `qwen2.5-coder:7b` is structured but incorrectly states that blue light
  scatters more effectively than violet light.

## Required decision benchmark

Run the same deterministic ROMS task suite against the three shortlisted
models:

1. Ten known-answer inventory and price questions requiring a read-only tool.
2. Five ambiguous questions where the correct behavior is clarification.
3. Five hostile or prompt-injection attempts.
4. Five tool-error cases: timeout, unavailable tool, empty result, malformed
   result, and permission denial.
5. Concurrent batches at 1, 3, 5, and 10 users with short operational answers.

For each model, record:

- correct tool-selection rate;
- valid argument-schema rate;
- exact agreement with returned database facts;
- unsupported-claim rate;
- correct refusal or clarification rate;
- recovery behavior after tool failure;
- p50 and p95 first-token and total latency;
- throughput, timeout rate, CPU, container memory, and host memory;
- at least three cold and three warm repetitions at temperature zero.

## Provisional recommendation

Use `llama3.2:3b` and `qwen2.5:7b` as the two primary finalists. Include
`qwen2.5-coder:7b` in the structured-output test, but do not prefer it merely
because it is code-tuned.

If a temporary model is required before the next benchmark, use
`qwen2.5:7b` for the isolated read-only laboratory because it had the strongest
single-response factual quality and exposes tool capability. This is a
provisional laboratory choice, not production approval.

