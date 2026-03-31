using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace Primp.Tests.Cli.Integration;

[Trait("Category", "Integration")]
public class CliTests : IClassFixture<LocalEndpointFixture>
{
    private static readonly string RepoRoot = FindRepoRoot();
    private static readonly string CliProject = Path.Combine(RepoRoot, "examples", "dotnet-cli", "Primp.DotnetCli.csproj");
    private static readonly string NativeLibSource = GetNativeLibSource();
    private static readonly string CliOutputNativeLib = Path.Combine(
        RepoRoot,
        "examples",
        "dotnet-cli",
        "bin",
        "Release",
        "net10.0",
        Path.GetFileName(NativeLibSource));

    private readonly string _baseUrl;

    public CliTests(LocalEndpointFixture fixture)
    {
        _baseUrl = fixture.BaseUrl;
        if (File.Exists(NativeLibSource))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(CliOutputNativeLib)!);
            File.Copy(NativeLibSource, CliOutputNativeLib, overwrite: true);
        }
    }

    [Fact]
    public async Task Help_PrintsCommands()
    {
        var result = await RunCliAsync("help");
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Commands:", result.StdOut);
        Assert.Contains("get <url>", result.StdOut);
    }

    [Fact]
    public async Task Version_ReturnsNativeVersion()
    {
        var result = await RunCliAsync("version");
        Assert.True(result.ExitCode == 0, $"ExitCode={result.ExitCode}\nSTDOUT:\n{result.StdOut}\nSTDERR:\n{result.StdErr}");
        Assert.False(string.IsNullOrWhiteSpace(result.StdOut));
    }

    [Fact]
    public async Task Get_ReturnsJsonWithStatus()
    {
        var result = await RunCliAsync($"get {_baseUrl}/get");
        Assert.True(result.ExitCode == 0, $"ExitCode={result.ExitCode}\nSTDOUT:\n{result.StdOut}\nSTDERR:\n{result.StdErr}");

        using var json = JsonDocument.Parse(result.StdOut);
        Assert.Equal(200, json.RootElement.GetProperty("status").GetInt32());
        Assert.Contains("/get", json.RootElement.GetProperty("url").GetString());
    }

    [Fact]
    public async Task Post_ReturnsJsonWithStatus()
    {
        var payload = "{\"from\":\"cli-test\"}";
        var result = await RunCliAsync($"post {_baseUrl}/post \"{payload}\"");
        Assert.True(result.ExitCode == 0, $"ExitCode={result.ExitCode}\nSTDOUT:\n{result.StdOut}\nSTDERR:\n{result.StdErr}");

        using var json = JsonDocument.Parse(result.StdOut);
        Assert.Equal(200, json.RootElement.GetProperty("status").GetInt32());
        Assert.Contains("/post", json.RootElement.GetProperty("url").GetString());
    }

    private static async Task<(int ExitCode, string StdOut, string StdErr)> RunCliAsync(string args)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"run --project \"{CliProject}\" -c Release --no-build -- {args}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = RepoRoot,
        };

        using var process = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start CLI process.");
        var stdout = await process.StandardOutput.ReadToEndAsync();
        var stderr = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        return (process.ExitCode, stdout.Trim(), stderr.Trim());
    }

    private static string FindRepoRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "primp.net.sln")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root (primp.net.sln).");
    }

    private static string GetNativeLibSource()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return Path.Combine(RepoRoot, "src", "Primp", "runtimes", "win-x64", "native", "primp_ffi.dll");
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return Path.Combine(RepoRoot, "src", "Primp", "runtimes", "osx-x64", "native", "libprimp_ffi.dylib");
        }

        return Path.Combine(RepoRoot, "src", "Primp", "runtimes", "linux-x64", "native", "libprimp_ffi.so");
    }
}
