using System.Text.Json;
using Primp.Interop;

namespace Primp;

/// <summary>
/// Fluent builder for configuring a <see cref="PrimpClient"/>.
/// </summary>
public sealed class PrimpClientBuilder
{
    private Impersonate? _impersonate;
    private ImpersonateOS? _impersonateOs;
    private TimeSpan? _timeout;
    private TimeSpan? _connectTimeout;
    private string? _proxy;
    private bool? _cookieStore;
    private bool? _httpsOnly;
    private bool? _dangerAcceptInvalidCerts;
    private bool? _followRedirects;
    private int? _maxRedirects;
    private Dictionary<string, string>? _defaultHeaders;

    internal PrimpClientBuilder() { }

    /// <summary>
    /// Sets the browser to impersonate for TLS/HTTP2 fingerprinting.
    /// </summary>
    public PrimpClientBuilder WithImpersonate(Impersonate impersonate)
    {
        _impersonate = impersonate;
        return this;
    }

    /// <summary>
    /// Sets the operating system to impersonate.
    /// </summary>
    public PrimpClientBuilder WithOS(ImpersonateOS os)
    {
        _impersonateOs = os;
        return this;
    }

    /// <summary>
    /// Sets the overall request timeout.
    /// </summary>
    public PrimpClientBuilder WithTimeout(TimeSpan timeout)
    {
        _timeout = timeout;
        return this;
    }

    /// <summary>
    /// Sets the connection timeout.
    /// </summary>
    public PrimpClientBuilder WithConnectTimeout(TimeSpan timeout)
    {
        _connectTimeout = timeout;
        return this;
    }

    /// <summary>
    /// Sets an HTTP proxy URL.
    /// </summary>
    public PrimpClientBuilder WithProxy(string proxyUrl)
    {
        _proxy = proxyUrl;
        return this;
    }

    /// <summary>
    /// Enables or disables the cookie store.
    /// </summary>
    public PrimpClientBuilder WithCookieStore(bool enabled = true)
    {
        _cookieStore = enabled;
        return this;
    }

    /// <summary>
    /// Restricts the client to HTTPS-only requests.
    /// </summary>
    public PrimpClientBuilder HttpsOnly(bool enabled = true)
    {
        _httpsOnly = enabled;
        return this;
    }

    /// <summary>
    /// Accepts invalid TLS certificates. Use with caution.
    /// </summary>
    public PrimpClientBuilder AcceptInvalidCertificates(bool enabled = true)
    {
        _dangerAcceptInvalidCerts = enabled;
        return this;
    }

    /// <summary>
    /// Enables or disables following redirects.
    /// </summary>
    public PrimpClientBuilder FollowRedirects(bool enabled = true)
    {
        _followRedirects = enabled;
        return this;
    }

    /// <summary>
    /// Sets the maximum number of redirects to follow.
    /// </summary>
    public PrimpClientBuilder MaxRedirects(int max)
    {
        _maxRedirects = max;
        return this;
    }

    /// <summary>
    /// Sets default headers sent with every request.
    /// </summary>
    public PrimpClientBuilder WithDefaultHeaders(IDictionary<string, string> headers)
    {
        _defaultHeaders = new Dictionary<string, string>(headers);
        return this;
    }

    /// <summary>
    /// Builds and returns a configured <see cref="PrimpClient"/>.
    /// </summary>
    public PrimpClient Build()
    {
        var builderHandle = NativeMethods.BuilderNew();
        if (builderHandle == nint.Zero)
            throw new PrimpException("Failed to create native client builder");

        try
        {
            if (_impersonate.HasValue)
            {
                var rc = NativeMethods.BuilderImpersonate(builderHandle, _impersonate.Value.ToNativeString());
                PrimpException.ThrowIfError(rc);
            }

            if (_impersonateOs.HasValue)
            {
                var rc = NativeMethods.BuilderImpersonateOs(builderHandle, _impersonateOs.Value.ToNativeString());
                PrimpException.ThrowIfError(rc);
            }

            if (_timeout.HasValue)
            {
                var rc = NativeMethods.BuilderTimeoutMs(builderHandle, (ulong)_timeout.Value.TotalMilliseconds);
                PrimpException.ThrowIfError(rc);
            }

            if (_connectTimeout.HasValue)
            {
                var rc = NativeMethods.BuilderConnectTimeoutMs(builderHandle, (ulong)_connectTimeout.Value.TotalMilliseconds);
                PrimpException.ThrowIfError(rc);
            }

            if (_proxy is not null)
            {
                var rc = NativeMethods.BuilderProxy(builderHandle, _proxy);
                PrimpException.ThrowIfError(rc);
            }

            if (_cookieStore.HasValue)
            {
                var rc = NativeMethods.BuilderCookieStore(builderHandle, _cookieStore.Value ? 1 : 0);
                PrimpException.ThrowIfError(rc);
            }

            if (_httpsOnly.HasValue)
            {
                var rc = NativeMethods.BuilderHttpsOnly(builderHandle, _httpsOnly.Value ? 1 : 0);
                PrimpException.ThrowIfError(rc);
            }

            if (_dangerAcceptInvalidCerts.HasValue)
            {
                var rc = NativeMethods.BuilderDangerAcceptInvalidCerts(builderHandle, _dangerAcceptInvalidCerts.Value ? 1 : 0);
                PrimpException.ThrowIfError(rc);
            }

            if (_followRedirects.HasValue)
            {
                var rc = NativeMethods.BuilderFollowRedirects(builderHandle, _followRedirects.Value ? 1 : 0);
                PrimpException.ThrowIfError(rc);
            }

            if (_maxRedirects.HasValue)
            {
                var rc = NativeMethods.BuilderMaxRedirects(builderHandle, (uint)_maxRedirects.Value);
                PrimpException.ThrowIfError(rc);
            }

            if (_defaultHeaders is not null)
            {
                var json = JsonSerializer.Serialize(_defaultHeaders);
                var rc = NativeMethods.BuilderDefaultHeadersJson(builderHandle, json);
                PrimpException.ThrowIfError(rc);
            }

            // Build consumes the builder handle
            var buildRc = NativeMethods.BuilderBuild(builderHandle, out var clientHandle);
            PrimpException.ThrowIfError(buildRc);

            return new PrimpClient(clientHandle);
        }
        catch
        {
            // If build was not called (exception before Build), free the builder
            // Note: BuilderBuild consumes the builder, so we only free on prior exceptions
            // The builder is consumed by BuilderBuild even on failure in our FFI design,
            // so we just re-throw here.
            throw;
        }
    }
}
