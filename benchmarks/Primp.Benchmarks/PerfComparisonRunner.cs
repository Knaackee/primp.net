using System.Diagnostics;
using System.Text.Json;

namespace Primp.Benchmarks;

internal static class PerfComparisonRunner
{
    private sealed record Metric(double AvgMs, double MinMs, double MaxMs);

    private sealed record ResultPayload(string Client, int Iterations, int Warmup, Metric Get, Metric PostJson);

    internal static async Task<int> RunAsync(string[] args)
    {
        var iterations = GetIntArg(args, "--iterations", 20);
        var warmup = GetIntArg(args, "--warmup", 3);
        var outputPath = GetStringArg(args, "--output");
        var baseUrl = GetStringArg(args, "--base-url") ?? Environment.GetEnvironmentVariable("PRIMP_BENCH_BASE_URL") ?? "http://127.0.0.1:18080";

        using var client = PrimpClient.Builder()
            .WithImpersonate(Impersonate.Chrome146)
            .WithOS(ImpersonateOS.Windows)
            .WithTimeout(TimeSpan.FromSeconds(30))
            .FollowRedirects(true)
            .Build();

        for (var i = 0; i < warmup; i++)
        {
            using var warmGet = await client.GetAsync($"{baseUrl}/get");
            using var warmPost = await client.PostAsync(
                $"{baseUrl}/post",
                "{\"benchmark\":true}",
                "application/json");

            EnsureSuccess(warmGet.StatusCode, "warmup GET");
            EnsureSuccess(warmPost.StatusCode, "warmup POST");
        }

        var getTimes = new List<double>(iterations);
        for (var i = 0; i < iterations; i++)
        {
            var sw = Stopwatch.StartNew();
            using var response = await client.GetAsync($"{baseUrl}/get");
            sw.Stop();
            EnsureSuccess(response.StatusCode, "GET");
            getTimes.Add(sw.Elapsed.TotalMilliseconds);
        }

        var postTimes = new List<double>(iterations);
        for (var i = 0; i < iterations; i++)
        {
            var sw = Stopwatch.StartNew();
            using var response = await client.PostAsync(
                $"{baseUrl}/post",
                "{\"benchmark\":true}",
                "application/json");
            sw.Stop();
            EnsureSuccess(response.StatusCode, "POST");
            postTimes.Add(sw.Elapsed.TotalMilliseconds);
        }

        var payload = new ResultPayload(
            "primp.net",
            iterations,
            warmup,
            ToMetric(getTimes),
            ToMetric(postTimes));

        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
        Console.WriteLine(json);

        if (!string.IsNullOrWhiteSpace(outputPath))
        {
            var fullPath = Path.GetFullPath(outputPath);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            await File.WriteAllTextAsync(fullPath, json);
        }

        return 0;
    }

    private static int GetIntArg(string[] args, string name, int fallback)
    {
        for (var i = 0; i < args.Length - 1; i++)
        {
            if (!string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (int.TryParse(args[i + 1], out var parsed) && parsed > 0)
            {
                return parsed;
            }
        }

        return fallback;
    }

    private static string? GetStringArg(string[] args, string name)
    {
        for (var i = 0; i < args.Length - 1; i++)
        {
            if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
            {
                return args[i + 1];
            }
        }

        return null;
    }

    private static Metric ToMetric(IReadOnlyList<double> values)
    {
        if (values.Count == 0)
        {
            return new Metric(0, 0, 0);
        }

        var sum = values.Sum();
        return new Metric(sum / values.Count, values.Min(), values.Max());
    }

    private static void EnsureSuccess(System.Net.HttpStatusCode statusCode, string operation)
    {
        if ((int)statusCode is >= 200 and < 300)
        {
            return;
        }

        throw new InvalidOperationException($"{operation} failed with status {(int)statusCode} ({statusCode}).");
    }
}
