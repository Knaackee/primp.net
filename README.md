# Primp.NET

[![NuGet](https://img.shields.io/nuget/v/Primp.svg)](https://www.nuget.org/packages/Primp/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Primp.svg)](https://www.nuget.org/packages/Primp/)
[![CI](https://github.com/Knaackee/primp.net/actions/workflows/ci.yml/badge.svg)](https://github.com/Knaackee/primp.net/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

.NET bridge for the [primp](https://github.com/deedy5/primp) HTTP client with browser impersonation.

Wraps the original Rust library via native interop for accurate TLS/HTTP2 fingerprinting — no reimplementation, just a thin P/Invoke bridge.

## Features

- **Browser Impersonation** — Chrome, Safari, Edge, Firefox, Opera with real TLS/HTTP2 fingerprints
- **OS Impersonation** — Windows, macOS, Linux, Android, iOS
- **Full HTTP** — GET, POST, PUT, PATCH, DELETE, HEAD, OPTIONS
- **Cookie Store** — Automatic cookie persistence across requests
- **Proxy Support** — HTTP/HTTPS/SOCKS5 proxies
- **Redirect Handling** — Configurable follow-redirects with max limit
- **Cross-Platform** — Windows, Linux, macOS (x64 + ARM64)
- **Multi-Target** — .NET 8 and .NET 10

## Installation

```bash
dotnet add package Primp
```

For dependency injection support:

```bash
dotnet add package Primp.Extensions
```

## Quick Start

```csharp
using Primp;

// Create a client impersonating Chrome 136
using var client = PrimpClient.Builder()
    .WithImpersonate(Impersonate.Chrome146)
    .WithOS(ImpersonateOS.Windows)
    .WithTimeout(TimeSpan.FromSeconds(30))
    .WithCookieStore(true)
    .FollowRedirects(true)
    .Build();

// GET request
using var response = await client.GetAsync("https://httpbin.org/get");
Console.WriteLine($"Status: {response.StatusCode}");
Console.WriteLine(response.ReadAsString());

// POST request with JSON
using var postResponse = await client.PostAsync(
    "https://httpbin.org/post",
    """{"key": "value"}""",
    "application/json");
```

## Dependency Injection

```csharp
using Primp.Extensions;

builder.Services.AddPrimpClient(options =>
{
    options.Impersonate = Impersonate.Chrome146;
    options.ImpersonateOS = ImpersonateOS.Windows;
    options.Timeout = TimeSpan.FromSeconds(30);
    options.CookieStore = true;
    options.FollowRedirects = true;
});
```

Then inject `PrimpClient` wherever you need it:

```csharp
public class MyService(PrimpClient client)
{
    public async Task<string> FetchAsync(string url)
    {
        using var response = await client.GetAsync(url);
        return response.ReadAsString();
    }
}
```

## API Reference

### PrimpClientBuilder

| Method | Description |
|---|---|
| `WithImpersonate(Impersonate)` | Browser to impersonate (Chrome, Safari, Edge, Firefox, Opera) |
| `WithOS(ImpersonateOS)` | Operating system to impersonate |
| `WithTimeout(TimeSpan)` | Overall request timeout |
| `WithConnectTimeout(TimeSpan)` | Connection timeout |
| `WithProxy(string)` | HTTP/HTTPS/SOCKS5 proxy URL |
| `WithCookieStore(bool)` | Enable/disable cookie persistence |
| `HttpsOnly(bool)` | Restrict to HTTPS-only requests |
| `AcceptInvalidCertificates(bool)` | Accept invalid TLS certificates |
| `FollowRedirects(bool)` | Enable/disable redirect following |
| `MaxRedirects(int)` | Maximum number of redirects |
| `WithDefaultHeaders(IDictionary)` | Default headers for all requests |

### Supported Browsers

| Browser | Versions |
|---|---|
| Chrome | 144, 145, 146 |
| Safari | 18.5, 26, 26.3 |
| Edge | 144, 145, 146 |
| Firefox | 140, 146, 147, 148 |
| Opera | 126, 127, 128, 129 |

### PrimpResponse

| Member | Description |
|---|---|
| `StatusCode` | HTTP status code (`HttpStatusCode`) |
| `Headers` | Response headers (`IReadOnlyDictionary`) |
| `Url` | Final URL (after redirects) |
| `ReadAsString()` | Read body as string |
| `ReadAsBytes()` | Read body as byte array |
| `ReadAsJson()` | Parse body as `JsonDocument` |

## Architecture

```
┌─────────────────────────────────────────────┐
│             Your .NET Application            │
├─────────────────────────────────────────────┤
│  Primp.dll  (managed, multi-target net8/10) │
│   PrimpClient · PrimpClientBuilder          │
│   LibraryImport / SafeHandle / Task.Run     │
├─────────────────────────────────────────────┤
│  primp_ffi.dll / .so / .dylib  (native)     │
│   ~15 extern "C" functions                  │
├─────────────────────────────────────────────┤
│  primp (original Rust crate by deedy5)      │
│   reqwest · rustls · h2 · hyper · tokio     │
└─────────────────────────────────────────────┘
```

## Building from Source

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download) or later
- [Rust toolchain](https://rustup.rs/) (for native library)

### Build native library

```bash
cd native/primp-ffi
cargo build --release
```

### Build .NET solution

```bash
dotnet build primp.net.sln
```

### Run tests

```bash
# Unit tests (no native library required)
dotnet test tests/Primp.Tests.Unit

# Integration tests (requires native library)
dotnet test tests/Primp.Tests.Integration
```

### Run benchmarks

```bash
dotnet run --project benchmarks/Primp.Benchmarks -c Release
```

## Platforms

| Platform | Architecture | Native Library |
|---|---|---|
| Windows | x64, ARM64 | `primp_ffi.dll` |
| Linux | x64, ARM64 | `libprimp_ffi.so` |
| macOS | x64, ARM64 | `libprimp_ffi.dylib` |

## License

[MIT](LICENSE)

## Credits

- [primp](https://github.com/deedy5/primp) by deedy5 — the original Rust HTTP client with browser impersonation
