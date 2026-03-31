namespace Primp.Tests.Integration;

/// <summary>
/// Integration tests that require the native primp_ffi library.
/// These tests run against a local HTTP endpoint.
/// </summary>
[Trait("Category", "Integration")]
public class PrimpClientTests : IDisposable, IClassFixture<LocalEndpointFixture>
{
    private readonly PrimpClient _client;
    private readonly string _baseUrl;

    public PrimpClientTests(LocalEndpointFixture fixture)
    {
        _baseUrl = fixture.BaseUrl;
        _client = PrimpClient.Builder()
            .WithImpersonate(Impersonate.Chrome146)
            .WithOS(ImpersonateOS.Windows)
            .WithTimeout(TimeSpan.FromSeconds(30))
            .FollowRedirects(true)
            .Build();
    }

    [Fact]
    public async Task GetAsync_ReturnsSuccessStatus()
    {
        using var response = await _client.GetAsync($"{_baseUrl}/get");

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetAsync_ReturnsBody()
    {
        using var response = await _client.GetAsync($"{_baseUrl}/get");

        var body = response.ReadAsString();
        Assert.False(string.IsNullOrEmpty(body));
        Assert.Contains("\"path\":\"/get\"", body);
    }

    [Fact]
    public async Task GetAsync_ReturnsHeaders()
    {
        using var response = await _client.GetAsync($"{_baseUrl}/get");

        Assert.NotNull(response.Headers);
        Assert.True(response.Headers.Count > 0);
    }

    [Fact]
    public async Task GetAsync_ReturnsUrl()
    {
        using var response = await _client.GetAsync($"{_baseUrl}/get");

        Assert.Contains("/get", response.Url ?? "");
    }

    [Fact]
    public async Task PostAsync_WithStringBody()
    {
        using var response = await _client.PostAsync(
            $"{_baseUrl}/post",
            """{"key":"value"}""",
            "application/json");

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        var body = response.ReadAsString();
        Assert.Contains("value", body);
    }

    [Fact]
    public async Task PostAsync_WithByteBody()
    {
        var bodyBytes = System.Text.Encoding.UTF8.GetBytes("hello");
        using var response = await _client.PostAsync($"{_baseUrl}/post", bodyBytes);

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task PutAsync_ReturnsSuccess()
    {
        using var response = await _client.PutAsync(
            $"{_baseUrl}/put",
            """{"updated":true}""",
            "application/json");

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task PatchAsync_ReturnsSuccess()
    {
        using var response = await _client.PatchAsync($"{_baseUrl}/patch");

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task DeleteAsync_ReturnsSuccess()
    {
        using var response = await _client.DeleteAsync($"{_baseUrl}/delete");

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task HeadAsync_ReturnsSuccess()
    {
        using var response = await _client.HeadAsync($"{_baseUrl}/get");

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ReadAsJson_ParsesResponse()
    {
        using var response = await _client.GetAsync($"{_baseUrl}/get");

        var json = response.ReadFromJson<System.Text.Json.JsonElement>();
        Assert.NotEqual(default, json);
    }

    [Fact]
    public async Task ReadAsBytes_ReturnsData()
    {
        using var response = await _client.GetAsync($"{_baseUrl}/get");

        var bytes = response.ReadAsBytes();
        Assert.True(bytes.Length > 0);
    }

    public void Dispose()
    {
        _client.Dispose();
    }
}
