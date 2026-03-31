namespace Primp.Extensions;

/// <summary>
/// Options for configuring a <see cref="PrimpClient"/> via dependency injection.
/// </summary>
public sealed class PrimpClientOptions
{
    /// <summary>
    /// The browser to impersonate for TLS/HTTP2 fingerprinting.
    /// </summary>
    public Impersonate? Impersonate { get; set; }

    /// <summary>
    /// The operating system to impersonate.
    /// </summary>
    public ImpersonateOS? ImpersonateOS { get; set; }

    /// <summary>
    /// The overall request timeout.
    /// </summary>
    public TimeSpan? Timeout { get; set; }

    /// <summary>
    /// The connection timeout.
    /// </summary>
    public TimeSpan? ConnectTimeout { get; set; }

    /// <summary>
    /// HTTP proxy URL.
    /// </summary>
    public string? Proxy { get; set; }

    /// <summary>
    /// Whether to enable the cookie store.
    /// </summary>
    public bool? CookieStore { get; set; }

    /// <summary>
    /// Whether to restrict to HTTPS-only requests.
    /// </summary>
    public bool? HttpsOnly { get; set; }

    /// <summary>
    /// Whether to accept invalid TLS certificates.
    /// </summary>
    public bool? AcceptInvalidCertificates { get; set; }

    /// <summary>
    /// Whether to follow redirects.
    /// </summary>
    public bool? FollowRedirects { get; set; }

    /// <summary>
    /// Maximum number of redirects to follow.
    /// </summary>
    public int? MaxRedirects { get; set; }

    /// <summary>
    /// Default headers sent with every request.
    /// </summary>
    public Dictionary<string, string>? DefaultHeaders { get; set; }
}
