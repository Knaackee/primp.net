using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System.Net;

namespace Primp.Tests.Cli.Integration;

public sealed class LocalEndpointFixture : IAsyncLifetime
{
    private WebApplication? _app;
    public string BaseUrl { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, 0));

        _app = builder.Build();

        _app.MapGet("/get", (HttpContext ctx) => Results.Json(new { path = ctx.Request.Path.Value, method = ctx.Request.Method, ok = true }));
        _app.MapPost("/post", async (HttpContext ctx) =>
        {
            using var reader = new StreamReader(ctx.Request.Body);
            var body = await reader.ReadToEndAsync();
            return Results.Json(new { path = ctx.Request.Path.Value, method = ctx.Request.Method, body, ok = true });
        });

        await _app.StartAsync();
        BaseUrl = _app.Urls.First(u => u.StartsWith("http://", StringComparison.OrdinalIgnoreCase));
    }

    public async Task DisposeAsync()
    {
        if (_app is null)
        {
            return;
        }

        await _app.StopAsync();
        await _app.DisposeAsync();
    }
}
