#Requires -Version 5.1
param(
    [int]$Iterations = 20,
    [int]$Warmup = 3,
    [switch]$UseDockerForRust = $true
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$RepoRoot = Split-Path -Parent $PSScriptRoot
$ResultsDir = Join-Path $RepoRoot 'benchmarks\results'
if (-not (Test-Path $ResultsDir)) {
    New-Item -ItemType Directory -Path $ResultsDir | Out-Null
}

$DotnetOut = Join-Path $ResultsDir 'primp-dotnet.json'
$RustOut = Join-Path $ResultsDir 'primp-original-rust.json'
$SummaryOut = Join-Path $ResultsDir 'comparison-summary.json'
$LocalEndpoint = 'http://127.0.0.1:18080'

$dockerExe = "C:\Program Files\Docker\Docker\resources\bin\docker.exe"
if (-not (Test-Path $dockerExe)) {
    throw 'Docker executable not found. A local endpoint container is required.'
}
$env:PATH = $env:PATH + ';C:\Program Files\Docker\Docker\resources\bin'

function Remove-ContainerQuietly([string]$Name) {
    $oldPreference = $ErrorActionPreference
    try {
        $ErrorActionPreference = 'Continue'
        & $dockerExe rm -f $Name 2>$null | Out-Null
    }
    finally {
        $ErrorActionPreference = $oldPreference
    }
}

Write-Host 'Starting local benchmark endpoint container...'
Remove-ContainerQuietly -Name 'primp-local-endpoint'
& $dockerExe run -d --name primp-local-endpoint -p 18080:8080 mendhak/http-https-echo:34 | Out-Null
if ($LASTEXITCODE -ne 0) { throw 'Failed to start local endpoint container.' }
Start-Sleep -Seconds 2

try {
    Write-Host 'Running primp.net comparison benchmark (local endpoint)...'
    dotnet run --project "$RepoRoot\benchmarks\Primp.Benchmarks\Primp.Benchmarks.csproj" -c Release -- compare --iterations $Iterations --warmup $Warmup --base-url $LocalEndpoint --output "$DotnetOut"
    if ($LASTEXITCODE -ne 0) { throw 'primp.net benchmark failed.' }

    Write-Host 'Running original primp (Rust) comparison benchmark (local endpoint)...'
    if ($UseDockerForRust) {
        $json = & $dockerExe run --rm -v "${RepoRoot}:/work" -w /work/benchmarks/original-primp -e PRIMP_BENCH_BASE_URL=http://host.docker.internal:18080 rust:1-bookworm bash -lc "export PATH=/usr/local/cargo/bin:/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin; cargo run --quiet --release -- --iterations $Iterations --warmup $Warmup"
        if ($LASTEXITCODE -ne 0) { throw 'original primp benchmark failed in Docker.' }
        $json | Out-File -FilePath $RustOut -Encoding utf8
    }
    else {
        Push-Location "$RepoRoot\benchmarks\original-primp"
        try {
            $env:PRIMP_BENCH_BASE_URL = $LocalEndpoint
            cargo run --quiet --release -- --iterations $Iterations --warmup $Warmup | Out-File -FilePath $RustOut -Encoding utf8
            if ($LASTEXITCODE -ne 0) { throw 'original primp benchmark failed.' }
        }
        finally {
            Remove-Item Env:PRIMP_BENCH_BASE_URL -ErrorAction SilentlyContinue
            Pop-Location
        }
    }
}
finally {
    Remove-ContainerQuietly -Name 'primp-local-endpoint'
}

$dotnet = Get-Content -Raw $DotnetOut | ConvertFrom-Json
$rust = Get-Content -Raw $RustOut | ConvertFrom-Json

function PercentDiff([double]$candidate, [double]$baseline) {
    if ($baseline -eq 0) { return 0 }
    return (($candidate - $baseline) / $baseline) * 100
}

$summary = [ordered]@{
    iterations = $Iterations
    warmup = $Warmup
    dotnet = $dotnet
    originalRust = $rust
    deltaPercent = [ordered]@{
        getAvgMs = [math]::Round((PercentDiff $dotnet.Get.AvgMs $rust.get.avg_ms), 2)
        postAvgMs = [math]::Round((PercentDiff $dotnet.PostJson.AvgMs $rust.post_json.avg_ms), 2)
    }
}

$summary | ConvertTo-Json -Depth 8 | Out-File -FilePath $SummaryOut -Encoding utf8
Write-Host "Comparison written to $SummaryOut"
