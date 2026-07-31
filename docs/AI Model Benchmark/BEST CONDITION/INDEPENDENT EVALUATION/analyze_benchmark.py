"""Recompute comparable metrics from the July 31 ROMS model benchmark.

The script treats the TXT transcripts and CSV samplers as the source of truth.
It intentionally does not create a composite score because tool reliability,
database adherence, and concurrency were not measured in this benchmark.
"""

from __future__ import annotations

import csv
import re
import statistics
from pathlib import Path


HERE = Path(__file__).resolve().parent
RAW = HERE.parent / "RAW DATA"

MODELS = {
    "tinyllama:1.1b": {
        "txt": "tinyllama_1_1b -BENCHMARK_SCORE.txt",
        "csv": "tinyllama_1_1b -BENCHMARK_SCORE.csv",
        "quality": "Rejected",
        "quality_note": "Major factual inventions, including a fabricated blue-moon mechanism.",
    },
    "llama3.2:3b": {
        "txt": "llama3_2_3b-BENCHMARK_SCORE.txt",
        "csv": "llama3_2_3b-BENCHMARK_SCORE.csv",
        "quality": "Mixed",
        "quality_note": "Readable, but contains historical misattributions and scientific errors.",
    },
    "phi3:3.8b": {
        "txt": "phi3_3_8b-BENCHMARK_SCORE.txt",
        "csv": "phi3_3_8b-BENCHMARK_SCORE.csv",
        "quality": "Mixed",
        "quality_note": "Coherent but verbose, with multiple scientific and historical inaccuracies.",
    },
    "qwen2.5:7b": {
        "txt": "qwen2_5_7b -BENCHMARK_SCORE.txt",
        "csv": "qwen2_5_7b -BENCHMARK_SCORE.csv",
        "quality": "Best of this set",
        "quality_note": "Most accurate essay, though still imperfect and not a ROMS task.",
    },
    "qwen2.5-coder:7b": {
        "txt": "qwen2_5-coder_7b-BENCHMARK_SCORE.txt",
        "csv": "qwen2_5-coder_7b-BENCHMARK_SCORE.csv",
        "quality": "Good",
        "quality_note": "Structured, but incorrectly says blue scatters more effectively than violet.",
    },
}


def seconds(value: str) -> float:
    if value.strip().endswith("ms"):
        return float(value.strip()[:-2]) / 1000
    match = re.fullmatch(r"(?:(\d+)m)?([\d.]+)s", value.strip())
    if not match:
        raise ValueError(f"Unsupported duration: {value!r}")
    return (float(match.group(1) or 0) * 60) + float(match.group(2))


def last_match(pattern: str, text: str) -> str:
    matches = re.findall(pattern, text, flags=re.MULTILINE)
    if not matches:
        raise ValueError(f"Pattern not found: {pattern}")
    return matches[-1]


def percentile(values: list[float], fraction: float) -> float:
    ordered = sorted(values)
    index = round((len(ordered) - 1) * fraction)
    return ordered[index]


rows: list[dict[str, object]] = []
for model, config in MODELS.items():
    transcript = (RAW / config["txt"]).read_text(encoding="utf-8", errors="replace")
    # The Llama transcript contains earlier TinyLlama runs. Its final PowerShell
    # session is the actual Llama run represented by the matching CSV.
    if model == "llama3.2:3b":
        transcript = transcript[transcript.rfind("PS C:\\Users\\GBServerPH>") :]

    parameter_text = last_match(r"^\s+parameters\s+([\d.]+B)", transcript)
    context_length = int(last_match(r"^\s+context length\s+(\d+)", transcript))
    quantization = last_match(r"^\s+quantization\s+(\S+)", transcript)
    capability_block = transcript.split("Capabilities", 1)[1].split("\n\n", 1)[0]
    capabilities = ", ".join(
        line.strip() for line in capability_block.splitlines() if line.strip()
    )

    total_s = seconds(last_match(r"^total duration:\s+(\S+)", transcript))
    load_s = seconds(last_match(r"^load duration:\s+(\S+)", transcript))
    eval_tokens = int(last_match(r"^eval count:\s+(\d+)", transcript))
    eval_rate = float(last_match(r"^eval rate:\s+([\d.]+)", transcript))
    prompt_rate = float(last_match(r"^prompt eval rate:\s+([\d.]+)", transcript))

    with (RAW / config["csv"]).open(encoding="utf-8-sig", newline="") as stream:
        samples = list(csv.DictReader(stream))
    cpu = [float(sample["CPU_%"]) for sample in samples]
    active_cpu = [value for value in cpu if value >= 20]
    ram = [float(sample["RAM_Used_GB"]) for sample in samples]
    gpu_observed = sum(bool(sample["GPU_%"].strip()) for sample in samples)
    rss_observed = sum(float(sample["Ollama_RSS_MB"] or 0) > 0 for sample in samples)

    rows.append(
        {
            "model": model,
            "parameters": parameter_text,
            "quantization": quantization,
            "context_length": context_length,
            "capabilities": capabilities,
            "total_seconds": round(total_s, 2),
            "load_seconds": round(load_s, 2),
            "output_tokens": eval_tokens,
            "eval_rate_tps": eval_rate,
            "prompt_rate_tps": prompt_rate,
            "active_cpu_mean_pct": round(statistics.mean(active_cpu), 1),
            "active_cpu_p95_pct": round(percentile(active_cpu, 0.95), 1),
            "peak_cpu_pct": max(cpu),
            "initial_ram_used_gb": ram[0],
            "peak_ram_used_gb": max(ram),
            "ram_peak_minus_initial_gb": round(max(ram) - ram[0], 2),
            "gpu_samples_recorded": gpu_observed,
            "ollama_rss_samples_nonzero": rss_observed,
            "independent_essay_assessment": config["quality"],
            "assessment_note": config["quality_note"],
        }
    )

output = HERE / "derived_model_summary.csv"
with output.open("w", encoding="utf-8", newline="") as stream:
    writer = csv.DictWriter(stream, fieldnames=list(rows[0]))
    writer.writeheader()
    writer.writerows(rows)

print(f"Wrote {output}")
for row in sorted(rows, key=lambda item: float(item["eval_rate_tps"]), reverse=True):
    print(
        f"{row['model']:21} {row['eval_rate_tps']:>5} t/s  "
        f"tools={'tools' in str(row['capabilities']).split(', ')}  "
        f"quality={row['independent_essay_assessment']}"
    )
