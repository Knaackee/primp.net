using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using Primp.Interop;

namespace Primp;

/// <summary>
/// Represents an HTTP response from a primp request.
/// Owns the native response handle and must be disposed after use.
/// </summary>
public sealed class PrimpResponse : IDisposable
{
    private readonly ResponseSafeHandle _handle;
    private bool _disposed;
    private IReadOnlyDictionary<string, string>? _headers;
    private byte[]? _body;

    internal PrimpResponse(nint responseHandle)
    {
        _handle = new ResponseSafeHandle(responseHandle);
    }

    /// <summary>
    /// The HTTP status code.
    /// </summary>
    public HttpStatusCode StatusCode
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return (HttpStatusCode)NativeMethods.ResponseStatus(_handle.DangerousGetHandle());
        }
    }

    /// <summary>
    /// The response headers.
    /// </summary>
    public IReadOnlyDictionary<string, string> Headers
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (_headers is not null) return _headers;

            var json = NativeMethods.ResponseHeaders(_handle.DangerousGetHandle());
            if (string.IsNullOrEmpty(json))
            {
                _headers = new Dictionary<string, string>();
            }
            else
            {
                _headers = JsonSerializer.Deserialize<Dictionary<string, string>>(json)
                    ?? new Dictionary<string, string>();
            }
            return _headers;
        }
    }

    /// <summary>
    /// The final URL after any redirects.
    /// </summary>
    public string? Url
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return NativeMethods.ResponseUrl(_handle.DangerousGetHandle());
        }
    }

    /// <summary>
    /// Reads the response body as a byte array.
    /// The body is cached after the first read.
    /// </summary>
    public byte[] ReadAsBytes()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_body is not null) return _body;

        NativeMethods.ResponseBody(_handle.DangerousGetHandle(), out var ptr, out var len);
        if (ptr == nint.Zero || len == 0)
        {
            _body = [];
        }
        else
        {
            _body = new byte[(int)len];
            Marshal.Copy(ptr, _body, 0, (int)len);
        }
        return _body;
    }

    /// <summary>
    /// Reads the response body as a UTF-8 string.
    /// </summary>
    public string ReadAsString()
    {
        return Encoding.UTF8.GetString(ReadAsBytes());
    }

    /// <summary>
    /// Reads the response body as a byte array (async-compatible wrapper).
    /// </summary>
    public Task<byte[]> ReadAsBytesAsync()
    {
        return Task.FromResult(ReadAsBytes());
    }

    /// <summary>
    /// Reads the response body as a UTF-8 string (async-compatible wrapper).
    /// </summary>
    public Task<string> ReadAsStringAsync()
    {
        return Task.FromResult(ReadAsString());
    }

    /// <summary>
    /// Deserializes the response body from JSON.
    /// </summary>
    public T? ReadFromJson<T>(JsonSerializerOptions? options = null)
    {
        var bytes = ReadAsBytes();
        return JsonSerializer.Deserialize<T>(bytes, options);
    }

    /// <summary>
    /// Deserializes the response body from JSON (async-compatible wrapper).
    /// </summary>
    public Task<T?> ReadFromJsonAsync<T>(JsonSerializerOptions? options = null)
    {
        return Task.FromResult(ReadFromJson<T>(options));
    }

    /// <summary>
    /// Content length from the body bytes.
    /// </summary>
    public long ContentLength => ReadAsBytes().Length;

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            _handle.Dispose();
        }
    }
}
