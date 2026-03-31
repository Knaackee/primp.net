using BenchmarkDotNet.Attributes;

namespace Primp.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
public class HttpBenchmarks : IDisposable
{
    private PrimpClient _client = null!;
    private string _baseUrl = null!;

    [GlobalSetup]
    public void Setup()
    {
        _baseUrl = Environment.GetEnvironmentVariable("PRIMP_BENCH_BASE_URL") ?? "http://127.0.0.1:18080";
        _client = PrimpClient.Builder()
            .WithImpersonate(Impersonate.Chrome146)
            .WithOS(ImpersonateOS.Windows)
            .WithTimeout(TimeSpan.FromSeconds(30))
            .Build();
    }

    [Benchmark(Description = "GET local /get")]
    public async Task<int> Get()
    {
        using var response = await _client.GetAsync($"{_baseUrl}/get");
        return (int)response.StatusCode;
    }

    [Benchmark(Description = "POST local /post (JSON)")]
    public async Task<int> PostJson()
    {
        using var response = await _client.PostAsync(
            $"{_baseUrl}/post",
            """{"benchmark":true}""",
            "application/json");
        return (int)response.StatusCode;
    }

    [Benchmark(Description = "GET local + ReadAsString")]
    public async Task<int> GetAndRead()
    {
        using var response = await _client.GetAsync($"{_baseUrl}/get");
        var body = response.ReadAsString();
        return body.Length;
    }

    public void Dispose()
    {
        _client?.Dispose();
    }
}
