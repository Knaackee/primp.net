using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using Primp.Interop;

namespace Primp;

/// <summary>
/// HTTP client with browser impersonation capabilities.
/// Wraps the original primp Rust library via native interop.
/// </summary>
public sealed class PrimpClient : IDisposable
{
    private readonly ClientSafeHandle _handle;
    private bool _disposed;

    internal PrimpClient(nint clientHandle)
    {
        _handle = new ClientSafeHandle(clientHandle);
    }

    /// <summary>
    /// Creates a new <see cref="PrimpClientBuilder"/> for configuring a client.
    /// </summary>
    public static PrimpClientBuilder Builder() => new();

    /// <summary>
    /// Sends an HTTP GET request.
    /// </summary>
    public Task<PrimpResponse> GetAsync(string url, IDictionary<string, string>? headers = null)
        => RequestAsync("GET", url, null, headers);

    /// <summary>
    /// Sends an HTTP POST request with an optional body.
    /// </summary>
    public Task<PrimpResponse> PostAsync(string url, byte[]? body = null, IDictionary<string, string>? headers = null)
        => RequestAsync("POST", url, body, headers);

    /// <summary>
    /// Sends an HTTP POST request with a string body.
    /// </summary>
    public Task<PrimpResponse> PostAsync(string url, string body, string contentType = "application/json", IDictionary<string, string>? headers = null)
    {
        var allHeaders = MergeHeaders(headers, "content-type", contentType);
        return RequestAsync("POST", url, Encoding.UTF8.GetBytes(body), allHeaders);
    }

    /// <summary>
    /// Sends an HTTP PUT request.
    /// </summary>
    public Task<PrimpResponse> PutAsync(string url, byte[]? body = null, IDictionary<string, string>? headers = null)
        => RequestAsync("PUT", url, body, headers);

    /// <summary>
    /// Sends an HTTP PUT request with a string body.
    /// </summary>
    public Task<PrimpResponse> PutAsync(string url, string body, string contentType = "application/json", IDictionary<string, string>? headers = null)
    {
        var allHeaders = MergeHeaders(headers, "content-type", contentType);
        return RequestAsync("PUT", url, Encoding.UTF8.GetBytes(body), allHeaders);
    }

    /// <summary>
    /// Sends an HTTP PATCH request.
    /// </summary>
    public Task<PrimpResponse> PatchAsync(string url, byte[]? body = null, IDictionary<string, string>? headers = null)
        => RequestAsync("PATCH", url, body, headers);

    /// <summary>
    /// Sends an HTTP DELETE request.
    /// </summary>
    public Task<PrimpResponse> DeleteAsync(string url, IDictionary<string, string>? headers = null)
        => RequestAsync("DELETE", url, null, headers);

    /// <summary>
    /// Sends an HTTP HEAD request.
    /// </summary>
    public Task<PrimpResponse> HeadAsync(string url, IDictionary<string, string>? headers = null)
        => RequestAsync("HEAD", url, null, headers);

    /// <summary>
    /// Sends an HTTP OPTIONS request.
    /// </summary>
    public Task<PrimpResponse> OptionsAsync(string url, IDictionary<string, string>? headers = null)
        => RequestAsync("OPTIONS", url, null, headers);

    /// <summary>
    /// Sends a generic HTTP request.
    /// </summary>
    public Task<PrimpResponse> RequestAsync(
        string method,
        string url,
        byte[]? body = null,
        IDictionary<string, string>? headers = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        return Task.Run(() =>
        {
            string? headersJson = null;
            if (headers is { Count: > 0 })
            {
                headersJson = JsonSerializer.Serialize(headers);
            }

            nint bodyPtr = nint.Zero;
            GCHandle bodyGcHandle = default;
            nuint bodyLen = 0;

            try
            {
                if (body is { Length: > 0 })
                {
                    bodyGcHandle = GCHandle.Alloc(body, GCHandleType.Pinned);
                    bodyPtr = bodyGcHandle.AddrOfPinnedObject();
                    bodyLen = (nuint)body.Length;
                }

                var rc = NativeMethods.Request(
                    _handle.DangerousGetHandle(),
                    method,
                    url,
                    bodyPtr,
                    bodyLen,
                    headersJson,
                    out var responseHandle);

                PrimpException.ThrowIfError(rc);
                return new PrimpResponse(responseHandle);
            }
            finally
            {
                if (bodyGcHandle.IsAllocated)
                    bodyGcHandle.Free();
            }
        });
    }

    /// <summary>
    /// Returns the native FFI library version.
    /// </summary>
    public static string? NativeVersion => NativeMethods.FfiVersion();

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            _handle.Dispose();
        }
    }

    private static Dictionary<string, string> MergeHeaders(
        IDictionary<string, string>? existing,
        string key,
        string value)
    {
        var result = existing is not null
            ? new Dictionary<string, string>(existing)
            : new Dictionary<string, string>();
        result.TryAdd(key, value);
        return result;
    }
}
