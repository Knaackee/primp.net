using Microsoft.Extensions.DependencyInjection;
using Primp.Extensions;

namespace Primp.Tests.Unit;

public class PrimpClientOptionsTests
{
    [Fact]
    public void DefaultOptions_AllNull()
    {
        var options = new PrimpClientOptions();

        Assert.Null(options.Impersonate);
        Assert.Null(options.ImpersonateOS);
        Assert.Null(options.Timeout);
        Assert.Null(options.ConnectTimeout);
        Assert.Null(options.Proxy);
        Assert.Null(options.CookieStore);
        Assert.Null(options.HttpsOnly);
        Assert.Null(options.AcceptInvalidCertificates);
        Assert.Null(options.FollowRedirects);
        Assert.Null(options.MaxRedirects);
        Assert.Null(options.DefaultHeaders);
    }

    [Fact]
    public void Options_SetAllProperties()
    {
        var options = new PrimpClientOptions
        {
            Impersonate = Primp.Impersonate.Chrome146,
            ImpersonateOS = Primp.ImpersonateOS.Windows,
            Timeout = TimeSpan.FromSeconds(30),
            ConnectTimeout = TimeSpan.FromSeconds(10),
            Proxy = "http://proxy:8080",
            CookieStore = true,
            HttpsOnly = true,
            AcceptInvalidCertificates = false,
            FollowRedirects = true,
            MaxRedirects = 5,
            DefaultHeaders = new Dictionary<string, string> { ["Accept"] = "*/*" }
        };

        Assert.Equal(Primp.Impersonate.Chrome146, options.Impersonate);
        Assert.Equal(Primp.ImpersonateOS.Windows, options.ImpersonateOS);
        Assert.Equal(TimeSpan.FromSeconds(30), options.Timeout);
        Assert.Equal(TimeSpan.FromSeconds(10), options.ConnectTimeout);
        Assert.Equal("http://proxy:8080", options.Proxy);
        Assert.True(options.CookieStore);
        Assert.True(options.HttpsOnly);
        Assert.False(options.AcceptInvalidCertificates);
        Assert.True(options.FollowRedirects);
        Assert.Equal(5, options.MaxRedirects);
        Assert.Single(options.DefaultHeaders!);
    }
}
