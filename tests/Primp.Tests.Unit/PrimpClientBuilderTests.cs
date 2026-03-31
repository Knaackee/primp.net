namespace Primp.Tests.Unit;

public class PrimpClientBuilderTests
{
    [Fact]
    public void Builder_ReturnsNewInstance()
    {
        var builder = PrimpClient.Builder();
        Assert.NotNull(builder);
    }

    [Fact]
    public void FluentApi_ReturnsSameBuilder()
    {
        var builder = PrimpClient.Builder();

        var result = builder
            .WithImpersonate(Impersonate.Chrome146)
            .WithOS(ImpersonateOS.Windows)
            .WithTimeout(TimeSpan.FromSeconds(30))
            .WithConnectTimeout(TimeSpan.FromSeconds(10))
            .WithCookieStore(true)
            .HttpsOnly(true)
            .AcceptInvalidCertificates(false)
            .FollowRedirects(true)
            .MaxRedirects(10)
            .WithDefaultHeaders(new Dictionary<string, string>
            {
                ["Accept"] = "text/html"
            });

        Assert.Same(builder, result);
    }
}
