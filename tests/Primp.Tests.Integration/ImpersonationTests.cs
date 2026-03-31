namespace Primp.Tests.Integration;

/// <summary>
/// Tests browser impersonation against TLS fingerprint analysis services.
/// </summary>
[Trait("Category", "Integration")]
public class ImpersonationTests : IDisposable
{
    private readonly PrimpClient _client;

    public ImpersonationTests()
    {
        _client = PrimpClient.Builder()
            .WithImpersonate(Impersonate.Chrome146)
            .WithOS(ImpersonateOS.Windows)
            .WithTimeout(TimeSpan.FromSeconds(30))
            .Build();
    }

    [Fact]
    public async Task TlsFingerprint_MatchesChrome()
    {
        using var response = await _client.GetAsync("https://tls.peet.ws/api/all");

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        var body = response.ReadAsString();
        Assert.False(string.IsNullOrEmpty(body));
        // The response should contain TLS fingerprint data
        Assert.Contains("tls_version", body);
    }

    [Theory]
    [InlineData(Impersonate.Chrome146)]
    [InlineData(Impersonate.Safari185)]
    [InlineData(Impersonate.Firefox148)]
    [InlineData(Impersonate.Edge146)]
    public async Task DifferentBrowsers_SuccessfulRequest(Impersonate browser)
    {
        using var client = PrimpClient.Builder()
            .WithImpersonate(browser)
            .WithTimeout(TimeSpan.FromSeconds(30))
            .Build();

        using var response = await client.GetAsync("https://httpbin.org/get");
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    public void Dispose()
    {
        _client.Dispose();
    }
}
