namespace Primp.Tests.Integration;

/// <summary>
/// Tests browser impersonation behavior against a local endpoint.
/// </summary>
[Trait("Category", "Integration")]
public class ImpersonationTests : IDisposable, IClassFixture<LocalEndpointFixture>
{
    private readonly PrimpClient _client;
    private readonly string _baseUrl;

    public ImpersonationTests(LocalEndpointFixture fixture)
    {
        _baseUrl = fixture.BaseUrl;
        _client = PrimpClient.Builder()
            .WithImpersonate(Impersonate.Chrome146)
            .WithOS(ImpersonateOS.Windows)
            .WithTimeout(TimeSpan.FromSeconds(30))
            .Build();
    }

    [Fact]
    public async Task LocalGet_WorksWithImpersonation()
    {
        using var response = await _client.GetAsync($"{_baseUrl}/get");

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        var body = response.ReadAsString();
        Assert.False(string.IsNullOrEmpty(body));
        Assert.Contains("\"path\":\"/get\"", body);
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

        using var response = await client.GetAsync($"{_baseUrl}/get");
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    public void Dispose()
    {
        _client.Dispose();
    }
}
