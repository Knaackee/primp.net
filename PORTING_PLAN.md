# 🪞 Primp.NET — Bridge-Plan

> .NET 8 / .NET 10 Bridge zur Rust-Library [primp](https://github.com/deedy5/primp) — HTTP-Client mit Browser-Impersonation.
> **Kein Port, kein Nachbau.** Eine dünne C-FFI-Schicht exportiert die Original-Library, .NET ruft sie via P/Invoke auf.

---

## 1. Übersicht: Was ist Primp?

Primp ist ein HTTP-Client, der Webbrowser imitieren kann, indem er:

- **TLS-Fingerprints** (JA3/JA4) realer Browser nachbildet
- **HTTP/2-Fingerprints** (SETTINGS-Frame-Reihenfolge, Pseudo-Header-Reihenfolge, Window-Sizes) matcht
- **Browser-Header-Profile** (User-Agent, Accept, etc.) korrekt setzt
- **ECH GREASE** für Chrome/Firefox aktiviert

### Unterstützte Browser-Profile

| Browser  | Versionen                                               |
|----------|---------------------------------------------------------|
| Chrome   | chrome_144, chrome_145, chrome_146, chrome              |
| Safari   | safari_18.5, safari_26, safari_26.3, safari             |
| Edge     | edge_144, edge_145, edge_146, edge                      |
| Firefox  | firefox_140, firefox_146, firefox_147, firefox_148, firefox |
| Opera    | opera_126, opera_127, opera_128, opera_129, opera       |
| Random   | random                                                  |

### OS-Typen

`android`, `ios`, `linux`, `macos`, `windows`, `random`

---

## 2. Architektur: Reine Bridge (kein .NET-Port)

### Warum Bridge statt Port?

TLS-Fingerprinting, HTTP/2-SETTINGS-Reihenfolge und ECH GREASE sind in .NET (`SslStream`, `HttpClient`) **nicht steuerbar**. Primp nutzt dafür modifizierte Forks von `rustls`, `h2`, `hyper` und `reqwest`. Diesen Stack nachzubauen wäre unmöglich und unsinnig.

### Stattdessen: Original nutzen, nur exportieren

```
┌─────────────────────────────────────────────────┐
│  Endbenutzer-Code (.NET)                        │
│   var client = PrimpClient.Builder()            │
│       .Impersonate(Impersonate.Chrome146)       │
│       .Build();                                 │
│   var resp = await client.GetAsync(url);        │
├─────────────────────────────────────────────────┤
│  Primp.dll  (eine managed .NET DLL)             │
│   • PrimpClient, PrimpResponse, Enums           │
│   • P/Invoke → primp_ffi                        │
│   • SafeHandle, IDisposable, async Wrapper      │
├─────────────────────────────────────────────────┤
│  primp_ffi.dll / .so / .dylib  (native)         │
│   • ~15 extern "C" Funktionen (~200 LOC Rust)   │
│   • Dependency: primp = { git = "..." }         │
│   • Interne Tokio-Runtime (async → sync Bridge) │
├─────────────────────────────────────────────────┤
│  primp (Original Rust-Crate, unverändert)       │
│   └→ primp-reqwest, primp-rustls, primp-h2 …    │
└─────────────────────────────────────────────────┘
```

**Null Logik wird nachgebaut.** Der FFI-Wrapper ist nur Typ-Konvertierung (Rust-Typen ↔ C-Typen) und `runtime.block_on()` für async.

---

## 3. Ziel-Verzeichnisstruktur

```
primp.net/
├── .github/
│   └── workflows/
│       ├── ci.yml                      # Build + Test (Matrix: .NET 8 + 10)
│       ├── native-build.yml            # Cross-compile Rust → native Libs (6 Plattformen)
│       ├── release.yml                 # Tag → NuGet + GitHub Release
│       ├── pack.yml                    # Manuelles NuGet-Packaging
│       └── benchmark.yml              # Scheduled Performance-Benchmarks
├── src/
│   ├── Primp/                          # Einziges .NET-Projekt (eine DLL)
│   │   ├── Primp.csproj
│   │   ├── PrimpClient.cs             # High-Level async Client
│   │   ├── PrimpClientBuilder.cs      # Builder-Pattern
│   │   ├── PrimpResponse.cs           # Response-Objekt
│   │   ├── Impersonate.cs             # Browser-Enum
│   │   ├── ImpersonateOS.cs           # OS-Enum
│   │   ├── PrimpException.cs          # Exception-Typen
│   │   └── Interop/                   # P/Invoke-Schicht (internal)
│   │       ├── NativeMethods.cs       # LibraryImport-Deklarationen
│   │       ├── ClientSafeHandle.cs    # SafeHandle für Client
│   │       ├── ResponseSafeHandle.cs  # SafeHandle für Response
│   │       └── PrimpStringHandle.cs   # SafeHandle für allozierte Strings
│   └── Primp.Extensions/              # DI, Options-Pattern (optional, eigenes NuGet)
│       ├── Primp.Extensions.csproj
│       ├── ServiceCollectionExtensions.cs
│       └── PrimpClientOptions.cs
├── native/
│   └── primp-ffi/                     # Dünner Rust C-FFI Wrapper (~200 LOC)
│       ├── Cargo.toml                 # Dependency: primp (Original)
│       ├── src/
│       │   └── lib.rs                 # ~15 extern "C" Funktionen
│       └── cbindgen.toml              # Auto-generierte C-Header
├── tests/
│   ├── Primp.Tests.Unit/              # Unit-Tests
│   └── Primp.Tests.Integration/       # E2E gegen tls.peet.ws
├── benchmarks/
│   └── Primp.Benchmarks/             # BenchmarkDotNet
├── examples/
│   └── Primp.Example/                # Demo-App
├── Directory.Build.props              # Globale MSBuild-Properties
├── .editorconfig
├── primp.net.sln
├── README.md
├── LICENSE
└── PORTING_PLAN.md
```

**Ein .NET-Projekt, eine DLL:** `Primp.dll` enthält sowohl die P/Invoke-Schicht als auch die öffentliche API. Kein separates `Primp.Native`-Projekt nötig — die Interop-Klassen sind `internal`.

---

## 4. Die FFI-Brücke (Rust-Seite)

### 4.1 `native/primp-ffi/Cargo.toml`

```toml
[package]
name = "primp-ffi"
version = "0.1.0"
edition = "2021"

[lib]
crate-type = ["cdylib"]    # → .dll / .so / .dylib

[dependencies]
primp = { git = "https://github.com/deedy5/primp" }
tokio = { version = "1", features = ["rt-multi-thread"] }

[profile.release]
lto = true
strip = true
codegen-units = 1
```

### 4.2 FFI-Funktionen (~15 Stück, ~200 LOC)

```rust
// native/primp-ffi/src/lib.rs — Vollständiges Konzept
use std::ffi::{c_char, CStr, CString};
use std::sync::OnceLock;
use primp::{Client, Impersonate, ImpersonateOS};
use tokio::runtime::Runtime;

static RUNTIME: OnceLock<Runtime> = OnceLock::new();
fn rt() -> &'static Runtime {
    RUNTIME.get_or_init(|| Runtime::new().expect("Tokio runtime"))
}

// === Client Builder ===
#[no_mangle] pub extern "C"
fn primp_builder_new() -> *mut primp::ClientBuilder {
    Box::into_raw(Box::new(Client::builder()))
}

#[no_mangle] pub extern "C"
fn primp_builder_impersonate(b: *mut primp::ClientBuilder, name: *const c_char) -> i32 {
    // Parse "chrome_146" → Impersonate::ChromeV146
    // Schreibt ins Builder-Objekt, Fehlercode zurück
}

#[no_mangle] pub extern "C"
fn primp_builder_os(b: *mut primp::ClientBuilder, name: *const c_char) -> i32 { ... }

#[no_mangle] pub extern "C"
fn primp_builder_timeout_ms(b: *mut primp::ClientBuilder, ms: u64) -> i32 { ... }

#[no_mangle] pub extern "C"
fn primp_builder_proxy(b: *mut primp::ClientBuilder, url: *const c_char) -> i32 { ... }

#[no_mangle] pub extern "C"
fn primp_builder_cookie_store(b: *mut primp::ClientBuilder, enabled: i32) -> i32 { ... }

#[no_mangle] pub extern "C"
fn primp_builder_build(b: *mut primp::ClientBuilder, out: *mut *mut Client) -> i32 {
    let builder = unsafe { *Box::from_raw(b) };  // konsumiert den Builder
    match builder.build() {
        Ok(client) => { unsafe { *out = Box::into_raw(Box::new(client)) }; 0 }
        Err(_) => -1
    }
}

// === HTTP Requests ===
#[no_mangle] pub extern "C"
fn primp_request(
    client: *const Client, method: *const c_char, url: *const c_char,
    body_ptr: *const u8, body_len: usize,
    out_resp: *mut *mut FfiResponse,
) -> i32 {
    // Eine generische Funktion für alle HTTP-Methoden
    // method = "GET", "POST", "PUT", "PATCH", "DELETE", "HEAD"
    // body_ptr/body_len = NULL/0 für bodyless Requests
    // Ruft rt().block_on(request.send()) auf
    // Liest Response-Body komplett, speichert als FfiResponse
}

// === Response ===
pub struct FfiResponse {
    pub status: u16,
    pub headers_json: CString,   // Headers als JSON-String
    pub body: Vec<u8>,
    pub url: CString,
}

#[no_mangle] pub extern "C"
fn primp_response_status(r: *const FfiResponse) -> u16 { ... }

#[no_mangle] pub extern "C"
fn primp_response_body(r: *const FfiResponse, out_ptr: *mut *const u8, out_len: *mut usize) { ... }

#[no_mangle] pub extern "C"
fn primp_response_headers(r: *const FfiResponse) -> *const c_char { ... }

#[no_mangle] pub extern "C"
fn primp_response_url(r: *const FfiResponse) -> *const c_char { ... }

// === Cleanup ===
#[no_mangle] pub extern "C" fn primp_client_free(c: *mut Client) { ... }
#[no_mangle] pub extern "C" fn primp_response_free(r: *mut FfiResponse) { ... }
```

### 4.3 Build-Targets (native Libraries)

| Plattform | Rust Target | Output | NuGet RID |
|-----------|------------|--------|-----------|
| Windows x64 | `x86_64-pc-windows-msvc` | `primp_ffi.dll` | `win-x64` |
| Windows ARM64 | `aarch64-pc-windows-msvc` | `primp_ffi.dll` | `win-arm64` |
| Linux x64 | `x86_64-unknown-linux-gnu` | `libprimp_ffi.so` | `linux-x64` |
| Linux ARM64 | `aarch64-unknown-linux-gnu` | `libprimp_ffi.so` | `linux-arm64` |
| macOS x64 | `x86_64-apple-darwin` | `libprimp_ffi.dylib` | `osx-x64` |
| macOS ARM64 | `aarch64-apple-darwin` | `libprimp_ffi.dylib` | `osx-arm64` |

---

## 5. Die .NET-Seite (ein Projekt, eine DLL)

### 5.1 `Interop/NativeMethods.cs` (internal)

```csharp
internal static partial class NativeMethods
{
    private const string Lib = "primp_ffi";

    [LibraryImport(Lib)] internal static partial nint PrimpBuilderNew();
    [LibraryImport(Lib, StringMarshalling = StringMarshalling.Utf8)]
    internal static partial int PrimpBuilderImpersonate(nint builder, string name);
    [LibraryImport(Lib, StringMarshalling = StringMarshalling.Utf8)]
    internal static partial int PrimpBuilderOs(nint builder, string name);
    [LibraryImport(Lib)] internal static partial int PrimpBuilderTimeoutMs(nint builder, ulong ms);
    [LibraryImport(Lib, StringMarshalling = StringMarshalling.Utf8)]
    internal static partial int PrimpBuilderProxy(nint builder, string url);
    [LibraryImport(Lib)] internal static partial int PrimpBuilderCookieStore(nint builder, int enabled);
    [LibraryImport(Lib)] internal static partial int PrimpBuilderBuild(nint builder, out nint client);

    [LibraryImport(Lib, StringMarshalling = StringMarshalling.Utf8)]
    internal static partial int PrimpRequest(
        nint client, string method, string url,
        nint bodyPtr, nuint bodyLen,
        out nint response);

    [LibraryImport(Lib)] internal static partial ushort PrimpResponseStatus(nint response);
    [LibraryImport(Lib)] internal static partial void PrimpResponseBody(nint response, out nint ptr, out nuint len);
    [LibraryImport(Lib)] internal static partial nint PrimpResponseHeaders(nint response);
    [LibraryImport(Lib)] internal static partial nint PrimpResponseUrl(nint response);

    [LibraryImport(Lib)] internal static partial void PrimpClientFree(nint client);
    [LibraryImport(Lib)] internal static partial void PrimpResponseFree(nint response);
}
```

### 5.2 Öffentliche API: `PrimpClient`

```csharp
using Primp;

// Builder-Pattern
var client = PrimpClient.Builder()
    .Impersonate(Impersonate.Chrome146)
    .WithOS(ImpersonateOS.Windows)
    .WithTimeout(TimeSpan.FromSeconds(30))
    .WithProxy("http://proxy:8080")
    .WithCookieStore(true)
    .Build();

// Requests
var response = await client.GetAsync("https://tls.peet.ws/api/all");
Console.WriteLine(response.StatusCode);           // 200
Console.WriteLine(await response.ReadAsStringAsync());
Console.WriteLine(response.Headers["content-type"]);

// POST mit Body
var postResponse = await client.PostAsync("https://httpbin.org/post",
    new StringContent("{\"key\":\"value\"}", Encoding.UTF8, "application/json"));

// Cleanup
client.Dispose();
```

### 5.3 API-Mapping (Rust → C# — 1:1 Durchreichung)

| Rust (primp) | C# (Primp) |
|-------------|------------|
| `Client::builder()` | `PrimpClient.Builder()` |
| `.impersonate(Impersonate::ChromeV146)` | `.Impersonate(Impersonate.Chrome146)` |
| `.impersonate_os(ImpersonateOS::Windows)` | `.WithOS(ImpersonateOS.Windows)` |
| `.timeout(Duration)` | `.WithTimeout(TimeSpan)` |
| `.connect_timeout(Duration)` | `.WithConnectTimeout(TimeSpan)` |
| `.proxy(Proxy)` | `.WithProxy(string)` |
| `.cookie_store(bool)` | `.WithCookieStore(bool)` |
| `.redirect(Policy)` | `.WithRedirectPolicy(RedirectPolicy)` |
| `.default_headers(HeaderMap)` | `.WithDefaultHeaders(IDictionary)` |
| `.danger_accept_invalid_certs(bool)` | `.AcceptInvalidCertificates(bool)` |
| `.https_only(bool)` | `.HttpsOnly(bool)` |
| `.build()` | `.Build()` |
| `client.get(url).send().await` | `await client.GetAsync(url)` |
| `client.post(url).body(b).send().await` | `await client.PostAsync(url, content)` |
| `response.status()` | `response.StatusCode` |
| `response.text().await` | `await response.ReadAsStringAsync()` |
| `response.bytes().await` | `await response.ReadAsByteArrayAsync()` |
| `response.headers()` | `response.Headers` |

### 5.4 Enums

```csharp
public enum Impersonate
{
    Chrome144, Chrome145, Chrome146, Chrome,
    Safari185, Safari26, Safari263, Safari,
    Edge144, Edge145, Edge146, Edge,
    Firefox140, Firefox146, Firefox147, Firefox148, Firefox,
    Opera126, Opera127, Opera128, Opera129, Opera,
    Random
}

public enum ImpersonateOS
{
    Android, iOS, Linux, MacOS, Windows, Random
}
```

---

## 6. NuGet-Packaging-Strategie

### Ein Package für den Benutzer, Runtime-Packages für die nativen Libs

| NuGet-Package | Inhalt |
|---------------|--------|
| **Primp** | `Primp.dll` (managed) + Dependencies auf Runtime-Packages |
| **Primp.runtime.win-x64** | `runtimes/win-x64/native/primp_ffi.dll` |
| **Primp.runtime.win-arm64** | `runtimes/win-arm64/native/primp_ffi.dll` |
| **Primp.runtime.linux-x64** | `runtimes/linux-x64/native/libprimp_ffi.so` |
| **Primp.runtime.linux-arm64** | `runtimes/linux-arm64/native/libprimp_ffi.so` |
| **Primp.runtime.osx-x64** | `runtimes/osx-x64/native/libprimp_ffi.dylib` |
| **Primp.runtime.osx-arm64** | `runtimes/osx-arm64/native/libprimp_ffi.dylib` |
| **Primp.Extensions** | DI, Options-Pattern (optional) |

**Endbenutzer-Installation:**
```
dotnet add package Primp
```

.NET wählt automatisch die passende native Library via RID-Resolution. Die Runtime-Packages werden transitiv mitgezogen.

---

## 7. Async-Strategie (FFI-Brücke)

### Problem
Primp ist intern async (Tokio). C FFI kennt kein async.

### Lösung

**Rust-Seite:** Globale Tokio-Runtime, `block_on()` pro FFI-Call:
```rust
static RUNTIME: OnceLock<Runtime> = OnceLock::new();
fn rt() -> &'static Runtime {
    RUNTIME.get_or_init(|| Runtime::new().expect("Tokio runtime"))
}

// Jeder FFI-Call blockiert intern:
rt().block_on(client.get(url).send())
```

**C#-Seite:** `Task.Run()` verhindert, dass der aufrufende Thread blockiert wird:
```csharp
public async Task<PrimpResponse> GetAsync(string url)
{
    return await Task.Run(() =>
    {
        var rc = NativeMethods.PrimpRequest(_handle, "GET", url, nint.Zero, 0, out var resp);
        PrimpException.ThrowIfError(rc);
        return new PrimpResponse(resp);
    });
}
```

---

## 8. CI/CD-Pipelines (GitHub Actions)

### 8.1 `ci.yml` — Continuous Integration

```
Trigger:   push → main, pull_request
Matrix:    .NET 8.0.x + 10.0.x
Steps:     Restore → Build → Test → Upload .trx
```

### 8.2 `native-build.yml` — Native Cross-Compilation

```
Trigger:   push → main (paths: native/**), workflow_dispatch
Matrix:    6 Plattformen (siehe Tabelle oben)
Steps:
  1. Install Rust toolchain + target
  2. cargo build --release -p primp-ffi --target ${{ target }}
  3. Upload native artifact (per RID)
```

### 8.3 `release.yml` — Release & NuGet Publish

```
Trigger:   push tag v*, workflow_dispatch (version input)
Steps:
  1. Trigger native-build.yml (oder Download cached Artifacts)
  2. dotnet restore / build / test
  3. dotnet pack Primp + Primp.runtime.{rid} Packages
  4. GitHub Release (softprops/action-gh-release)
  5. dotnet nuget push → nuget.org
```

### 8.4 `pack.yml` — Manuelles Pack

```
Trigger:   workflow_dispatch
Steps:     Restore → Pack → Upload Artifacts
```

### 8.5 `benchmark.yml` — Performance

```
Trigger:   schedule (nightly), workflow_dispatch
Steps:     BenchmarkDotNet → Upload Results
```

---

## 9. Implementierungsphasen

### Phase 1: Fundament

- [ ] Repository-Struktur: Solution, `src/Primp/`, `native/primp-ffi/`, `Directory.Build.props`
- [ ] `primp-ffi` Rust-Crate: Builder + GET + Response + Free (~15 FFI-Funktionen)
- [ ] `Primp.dll`: P/Invoke, SafeHandles, `PrimpClient`, `PrimpClientBuilder`, `PrimpResponse`
- [ ] Erster E2E-Test: Chrome-146 GET gegen `tls.peet.ws`

### Phase 2: Vollständige API

- [ ] Alle HTTP-Methoden (POST, PUT, PATCH, DELETE, HEAD) via generische `primp_request`
- [ ] Request-Body-Support (String, Bytes, JSON, Form)
- [ ] Response: Bytes, JSON-Deserialisierung (`ReadFromJsonAsync<T>`)
- [ ] Alle Builder-Optionen: Proxy, Cookies, Redirect, Timeout, TLS, Headers
- [ ] Alle Browser-Profile + OS-Typen als Enums
- [ ] SafeHandle + IDisposable Lifecycle

### Phase 3: CI/CD & Packaging

- [ ] `native-build.yml` — Cross-Compile für 6 Plattformen
- [ ] NuGet Runtime-Packages (`Primp.runtime.{rid}`)
- [ ] `ci.yml` — Matrix Build + Test (.NET 8 + 10)
- [ ] `release.yml` — Tag → NuGet + GitHub Release
- [ ] README mit Badges, Installation, Quick Start

### Phase 4: Hardening

- [ ] Unit-Tests (gemockter Native Layer)
- [ ] Integration-Tests (tls.peet.ws Fingerprint-Vergleich)
- [ ] BenchmarkDotNet + `benchmark.yml`
- [ ] `Primp.Extensions` — DI-Integration (optional)
- [ ] Erste stabile NuGet-Veröffentlichung

---

## 10. Globale Build-Konfiguration

### `Directory.Build.props`

```xml
<Project>
  <PropertyGroup>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <Deterministic>true</Deterministic>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>
</Project>
```

### `.editorconfig`

```ini
root = true

[*]
charset = utf-8
end_of_line = lf
insert_final_newline = true
trim_trailing_whitespace = true

[*.cs]
indent_style = space
indent_size = 4
```

### Target Frameworks

```xml
<!-- Primp + Primp.Extensions -->
<TargetFrameworks>net8.0;net10.0</TargetFrameworks>

<!-- Tests / Benchmarks -->
<TargetFramework>net10.0</TargetFramework>
```

---

## 11. Risiken & Mitigationen

| Risiko | Impact | Mitigation |
|--------|--------|------------|
| Cross-Compile für 6 Plattformen | Mittel | primp nutzt aws-lc-rs + rustls (kein OpenSSL); GitHub Actions native Runner |
| Native-Lib-Größe (> 10 MB) | Niedrig | LTO + strip + codegen-units=1; separates NuGet pro RID |
| Async/Sync FFI-Mismatch | Mittel | Globale Tokio-Runtime; Task.Run auf .NET-Seite |
| Memory Leaks | Mittel | SafeHandle + Finalizer + IDisposable |
| Upstream Breaking Changes | Mittel | Gepinnte primp-Version in Cargo.toml; Parity-Tests |

---

## 12. Abhängigkeiten

### Rust (nur `native/primp-ffi/`)

| Dependency | Zweck |
|-----------|-------|
| `primp` (git) | **Die Original-Library** — wird unverändert genutzt |
| `tokio` | Async-Runtime für `block_on()` |

### .NET

| Package | Zweck |
|---------|-------|
| `System.Text.Json` | JSON-Deserialisierung in `ReadFromJsonAsync<T>` |
| `Microsoft.Extensions.DependencyInjection.Abstractions` | DI (nur Primp.Extensions) |
| `xunit` + `Microsoft.NET.Test.Sdk` | Tests |
| `BenchmarkDotNet` | Benchmarks |

---

## 13. Erfolgskriterien

1. **`dotnet add package Primp`** — eine Installation, läuft auf Win/Linux/macOS (x64 + ARM64)
2. **Identischer JA3/JA4-Fingerprint** wie das Rust-Original (verifiziert gegen tls.peet.ws)
3. **Eine managed DLL** (`Primp.dll`) für beide Frameworks (.NET 8 + 10)
4. **Kein Memory Leak** — SafeHandle-basiertes Lifecycle-Management
5. **CI/CD** — Grüne Builds auf allen Plattformen, automatische NuGet-Releases bei Git-Tags
6. **Minimaler Wartungsaufwand** — bei neuen Browser-Profilen upstream nur primp-Version bumpen
