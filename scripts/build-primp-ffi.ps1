#Requires -Version 5.1
<#
.SYNOPSIS
    Builds primp-ffi + .NET solution via Docker (cargo-xwin for Windows cross-compile).
.DESCRIPTION
    Runs a multi-stage Docker build that:
      1. Compiles primp-ffi for linux-x64 (native) and win-x64 (cargo-xwin)
      2. Builds the .NET solution
      3. Runs unit tests
    Optionally copies the native artifacts out of the container.
.PARAMETER SkipTests
    Skip running tests inside the container.
.PARAMETER CopyArtifacts
    Copy native libraries from the build to ./artifacts/.
#>

$SkipTests = $args -contains '-SkipTests'
$CopyArtifacts = $args -contains '-CopyArtifacts'

Set-StrictMode -Version Latest

$RepoRoot = Split-Path -Parent $PSScriptRoot
$ImageName = 'primp-net-build'

function Write-Step([string]$Message) {
    Write-Host "`n==> $Message" -ForegroundColor Cyan
}

function Exit-OnFailure([string]$Message) {
    if ($LASTEXITCODE -ne 0) {
        Write-Host "ERROR: $Message (exit code $LASTEXITCODE)" -ForegroundColor Red
        exit $LASTEXITCODE
    }
}

# ─── 1. Check for Docker ─────────────────────────────────────────────────────

Write-Step 'Checking for Docker...'

$dockerCmd = Get-Command docker -ErrorAction SilentlyContinue
if (-not $dockerCmd) {
    Write-Host 'ERROR: Docker is not installed or not in PATH.' -ForegroundColor Red
    Write-Host 'Install Docker Desktop from https://www.docker.com/products/docker-desktop/' -ForegroundColor Yellow
    exit 1
}

docker info >$null 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host 'ERROR: Docker daemon is not running. Please start Docker Desktop.' -ForegroundColor Red
    exit 1
}

Write-Host "Found $(docker --version)" -ForegroundColor Green

# ─── 2. Docker build ─────────────────────────────────────────────────────────

Write-Step 'Building via Docker (Rust native + cargo-xwin + .NET)...'
Write-Host '  This may take several minutes on first run (caching kicks in after).' -ForegroundColor DarkGray

$buildTarget = if ($SkipTests) { 'native-build' } else { 'artifacts' }
docker build --target $buildTarget -t "${ImageName}:latest" -f "$RepoRoot\Dockerfile" $RepoRoot
Exit-OnFailure 'Docker build failed'

Write-Host 'Docker build succeeded.' -ForegroundColor Green

# ─── 3. Copy artifacts (optional) ────────────────────────────────────────────

if ($CopyArtifacts) {
    Write-Step 'Copying native artifacts from container...'

    $outDir = Join-Path $RepoRoot 'artifacts'
    if (-not (Test-Path $outDir)) { New-Item -ItemType Directory -Path $outDir -Force | Out-Null }

    $cid = docker create "${ImageName}:latest"
    try {
        docker cp "${cid}:/artifacts/native" "$outDir"
        Exit-OnFailure 'Failed to copy artifacts'
        Write-Host "Artifacts copied to $outDir\native\" -ForegroundColor Green
    }
    finally {
        docker rm $cid >$null 2>&1
    }
}

Write-Host "`nAll done." -ForegroundColor Green
