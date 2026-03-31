#!/usr/bin/env bash
set -euo pipefail

ITERATIONS="${1:-20}"
WARMUP="${2:-3}"

REPO_ROOT="$(cd "$(dirname "$0")/.." && pwd)"
RESULTS_DIR="$REPO_ROOT/benchmarks/results"
mkdir -p "$RESULTS_DIR"

DOTNET_OUT="$RESULTS_DIR/primp-dotnet.json"
RUST_OUT="$RESULTS_DIR/primp-original-rust.json"
SUMMARY_OUT="$RESULTS_DIR/comparison-summary.json"
LOCAL_ENDPOINT="http://127.0.0.1:18080"

cleanup() {
    docker rm -f primp-local-endpoint >/dev/null 2>&1 || true
}

cleanup
trap cleanup EXIT

echo "Starting local benchmark endpoint container..."
docker run -d --name primp-local-endpoint -p 18080:8080 mendhak/http-https-echo:34 >/dev/null
sleep 2

echo "Running primp.net comparison benchmark (local endpoint)..."
dotnet run --project "$REPO_ROOT/benchmarks/Primp.Benchmarks/Primp.Benchmarks.csproj" -c Release -- compare --iterations "$ITERATIONS" --warmup "$WARMUP" --base-url "$LOCAL_ENDPOINT" --output "$DOTNET_OUT"

echo "Running original primp (Rust) comparison benchmark (local endpoint)..."
docker run --rm --add-host=host.docker.internal:host-gateway -v "$REPO_ROOT:/work" -w /work/benchmarks/original-primp -e PRIMP_BENCH_BASE_URL="http://host.docker.internal:18080" rust:1-bookworm bash -lc "export PATH=/usr/local/cargo/bin:/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin; cargo run --quiet --release -- --iterations \"$ITERATIONS\" --warmup \"$WARMUP\"" > "$RUST_OUT"

python3 - <<'PY' "$DOTNET_OUT" "$RUST_OUT" "$SUMMARY_OUT" "$ITERATIONS" "$WARMUP"
import json
import sys

_dotnet_path, _rust_path, _summary_path, _iterations, _warmup = sys.argv[1:]
with open(_dotnet_path, 'r', encoding='utf-8') as f:
    dotnet = json.load(f)
with open(_rust_path, 'r', encoding='utf-8') as f:
    rust = json.load(f)

def pct(candidate, baseline):
    return 0.0 if baseline == 0 else round(((candidate - baseline) / baseline) * 100, 2)

summary = {
    "iterations": int(_iterations),
    "warmup": int(_warmup),
    "dotnet": dotnet,
    "originalRust": rust,
    "deltaPercent": {
        "getAvgMs": pct(dotnet["Get"]["AvgMs"], rust["get"]["avg_ms"]),
        "postAvgMs": pct(dotnet["PostJson"]["AvgMs"], rust["post_json"]["avg_ms"]),
    },
}

with open(_summary_path, 'w', encoding='utf-8') as f:
    json.dump(summary, f, indent=2)

print(f"Comparison written to {_summary_path}")
PY
