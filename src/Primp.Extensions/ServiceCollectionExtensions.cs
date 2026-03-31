using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Primp.Extensions;

/// <summary>
/// Extension methods for registering <see cref="PrimpClient"/> with dependency injection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers a singleton <see cref="PrimpClient"/> configured via <see cref="PrimpClientOptions"/>.
    /// </summary>
    public static IServiceCollection AddPrimpClient(
        this IServiceCollection services,
        Action<PrimpClientOptions>? configure = null)
    {
        if (configure is not null)
            services.Configure(configure);

        services.AddSingleton(sp =>
        {
            var options = sp.GetService<IOptions<PrimpClientOptions>>()?.Value ?? new PrimpClientOptions();
            return BuildClient(options);
        });

        return services;
    }

    private static PrimpClient BuildClient(PrimpClientOptions options)
    {
        var builder = PrimpClient.Builder();

        if (options.Impersonate.HasValue)
            builder.WithImpersonate(options.Impersonate.Value);

        if (options.ImpersonateOS.HasValue)
            builder.WithOS(options.ImpersonateOS.Value);

        if (options.Timeout.HasValue)
            builder.WithTimeout(options.Timeout.Value);

        if (options.ConnectTimeout.HasValue)
            builder.WithConnectTimeout(options.ConnectTimeout.Value);

        if (options.Proxy is not null)
            builder.WithProxy(options.Proxy);

        if (options.CookieStore.HasValue)
            builder.WithCookieStore(options.CookieStore.Value);

        if (options.HttpsOnly.HasValue)
            builder.HttpsOnly(options.HttpsOnly.Value);

        if (options.AcceptInvalidCertificates.HasValue)
            builder.AcceptInvalidCertificates(options.AcceptInvalidCertificates.Value);

        if (options.FollowRedirects.HasValue)
            builder.FollowRedirects(options.FollowRedirects.Value);

        if (options.MaxRedirects.HasValue)
            builder.MaxRedirects(options.MaxRedirects.Value);

        if (options.DefaultHeaders is not null)
            builder.WithDefaultHeaders(options.DefaultHeaders);

        return builder.Build();
    }
}
