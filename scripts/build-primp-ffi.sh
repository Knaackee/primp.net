#!/usr/bin/env bash
#
# Builds primp-ffi + .NET solution via Docker (cargo-xwin for Windows cross-compile).
# Usage: ./scripts/build-primp-ffi.sh [--skip-tests] [--copy-artifacts]
#
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "$0")/.." && pwd)"
IMAGE_NAME="primp-net-build"
SKIP_TESTS=false
COPY_ARTIFACTS=false

for arg in "$@"; do
    case "$arg" in
        --skip-tests)      SKIP_TESTS=true ;;
        --copy-artifacts)  COPY_ARTIFACTS=true ;;
    esac
done

step() {
    printf '\n\033[36m==> %s\033[0m\n' "$1"
}

# ─── 1. Check for Docker ─────────────────────────────────────────────────────

step "Checking for Docker..."

if ! command -v docker &>/dev/null; then
    echo "ERROR: Docker is not installed or not in PATH." >&2
    echo "Install Docker from https://docs.docker.com/get-docker/" >&2
    exit 1
fi

if ! docker info &>/dev/null; then
    echo "ERROR: Docker daemon is not running." >&2
    exit 1
fi

echo "Found $(docker --version)"

# ─── 2. Docker build ─────────────────────────────────────────────────────────

step "Building via Docker (Rust native + cargo-xwin + .NET)..."
echo "  This may take several minutes on first run (caching kicks in after)."

BUILD_TARGET="artifacts"
if [ "$SKIP_TESTS" = true ]; then
    BUILD_TARGET="native-build"
fi

docker build --target "$BUILD_TARGET" -t "${IMAGE_NAME}:latest" -f "$REPO_ROOT/Dockerfile" "$REPO_ROOT"

echo "Docker build succeeded."

# ─── 3. Copy artifacts (optional) ────────────────────────────────────────────

if [ "$COPY_ARTIFACTS" = true ]; then
    step "Copying native artifacts from container..."

    OUT_DIR="$REPO_ROOT/artifacts"
    mkdir -p "$OUT_DIR"

    CID=$(docker create "${IMAGE_NAME}:latest")
    docker cp "${CID}:/artifacts/native" "$OUT_DIR/"
    docker rm "$CID" >/dev/null 2>&1
    echo "Artifacts copied to $OUT_DIR/native/"
fi

echo -e "\n\033[32mAll done.\033[0m"
