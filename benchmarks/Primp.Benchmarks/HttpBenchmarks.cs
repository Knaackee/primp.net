using BenchmarkDotNet.Attributes;

namespace Primp.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
public class HttpBenchmarks : IDisposable
{
    private PrimpClient _client = null!;

    [GlobalSetup]
    public void Setup()
    {
        _client = PrimpClient.Builder()
            .WithImpersonate(Impersonate.Chrome146)
            .WithOS(ImpersonateOS.Windows)
            .WithTimeout(TimeSpan.FromSeconds(30))
            .Build();
    }

    [Benchmark(Description = "GET httpbin.org")]
    public async Task<int> Get()
    {
        using var response = await _client.GetAsync("https://httpbin.org/get");
        return (int)response.StatusCode;
    }

    [Benchmark(Description = "POST httpbin.org (JSON)")]
    public async Task<int> PostJson()
    {
        using var response = await _client.PostAsync(
            "https://httpbin.org/post",
            """{"benchmark":true}""",
            "application/json");
        return (int)response.StatusCode;
    }

    [Benchmark(Description = "GET + ReadAsString")]
    public async Task<int> GetAndRead()
    {
        using var response = await _client.GetAsync("https://httpbin.org/get");
        var body = response.ReadAsString();
        return body.Length;
    }

    public void Dispose()
    {
        _client?.Dispose();
    }
}
