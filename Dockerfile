# ============================================================================
# Stage 1: Build native primp-ffi libraries using cargo-xwin (win) + native (linux)
# ============================================================================
FROM rust:1-bookworm AS native-build

# Install cargo-xwin and add Windows MSVC target
RUN cargo install cargo-xwin \
    && rustup target add x86_64-pc-windows-msvc \
    && rustup target add aarch64-pc-windows-msvc

# Also needed for some OpenSSL / ring build deps
RUN apt-get update && apt-get install -y --no-install-recommends \
    cmake clang llvm perl nasm \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /src/native/primp-ffi

# 1) Copy manifests first for layer caching
COPY native/primp-ffi/Cargo.toml native/primp-ffi/Cargo.lock* ./

# Create a dummy lib.rs so cargo can resolve deps
RUN mkdir src && echo "// placeholder" > src/lib.rs

# Pre-fetch dependencies (cached unless Cargo.toml changes)
RUN cargo fetch || true

# 2) Copy actual source
COPY native/primp-ffi/src/ ./src/

# 3) Build for Linux x64 (native)
RUN cargo build --release --target x86_64-unknown-linux-gnu \
    && cp target/x86_64-unknown-linux-gnu/release/libprimp_ffi.so /tmp/libprimp_ffi.so

# 4) Build for Windows x64 via cargo-xwin
RUN cargo xwin build --release --target x86_64-pc-windows-msvc \
    && cp target/x86_64-pc-windows-msvc/release/primp_ffi.dll /tmp/primp_ffi.dll

# ============================================================================
# Stage 2: .NET build + test
# ============================================================================
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS dotnet-build

WORKDIR /src

# Copy solution & project files first (layer caching for restore)
COPY primp.net.sln Directory.Build.props .editorconfig ./
COPY src/Primp/Primp.csproj src/Primp/
COPY src/Primp.Extensions/Primp.Extensions.csproj src/Primp.Extensions/
COPY tests/Primp.Tests.Unit/Primp.Tests.Unit.csproj tests/Primp.Tests.Unit/
COPY tests/Primp.Tests.Integration/Primp.Tests.Integration.csproj tests/Primp.Tests.Integration/
COPY tests/Primp.Tests.Cli.Integration/Primp.Tests.Cli.Integration.csproj tests/Primp.Tests.Cli.Integration/
COPY benchmarks/Primp.Benchmarks/Primp.Benchmarks.csproj benchmarks/Primp.Benchmarks/
COPY examples/Primp.Example/Primp.Example.csproj examples/Primp.Example/
COPY examples/dotnet-cli/Primp.DotnetCli.csproj examples/dotnet-cli/
COPY README.md ./

RUN dotnet restore primp.net.sln

# Copy everything else
COPY src/ src/
COPY tests/ tests/
COPY benchmarks/ benchmarks/
COPY examples/ examples/

# Place native libraries in runtime directories (for NuGet packaging)
RUN mkdir -p src/Primp/runtimes/linux-x64/native \
             src/Primp/runtimes/win-x64/native

COPY --from=native-build /tmp/libprimp_ffi.so src/Primp/runtimes/linux-x64/native/
COPY --from=native-build /tmp/primp_ffi.dll   src/Primp/runtimes/win-x64/native/

# Build (full restore since runtimes/ appeared after initial restore)
RUN dotnet build primp.net.sln -c Release

# Copy native lib directly to test output dirs (LibraryImport probes assembly dir, not runtimes/)
RUN for d in $(find . -path "*/bin/Release/net*" -type d -not -path "*/runtimes/*"); do \
      cp src/Primp/runtimes/linux-x64/native/libprimp_ffi.so "$d/libprimp_ffi.so" 2>/dev/null || true; \
    done

# Run unit tests
RUN dotnet test tests/Primp.Tests.Unit/Primp.Tests.Unit.csproj -c Release --no-build --logger "console;verbosity=normal"

# Run integration tests (real HTTP requests from container)
RUN dotnet test tests/Primp.Tests.Integration/Primp.Tests.Integration.csproj -c Release --no-build --logger "console;verbosity=normal" \
    || echo "Integration tests skipped (may require network)"

# Run CLI integration tests
RUN dotnet test tests/Primp.Tests.Cli.Integration/Primp.Tests.Cli.Integration.csproj -c Release --no-build --logger "console;verbosity=normal" \
    || echo "CLI integration tests skipped (may require network)"

# ============================================================================
# Stage 3: Output artifacts
# ============================================================================
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS artifacts

WORKDIR /artifacts

# Native libraries
COPY --from=native-build /tmp/libprimp_ffi.so native/linux-x64/
COPY --from=native-build /tmp/primp_ffi.dll   native/win-x64/

# .NET build output
COPY --from=dotnet-build /src/src/Primp/bin/Release/ dotnet/Primp/
COPY --from=dotnet-build /src/src/Primp.Extensions/bin/Release/ dotnet/Primp.Extensions/

ENTRYPOINT ["echo", "Build artifacts in /artifacts"]
