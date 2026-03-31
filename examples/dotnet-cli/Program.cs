using System.Net;
using System.Text.Json;
using Primp;

var exitCode = await RunAsync(args);
return exitCode;

static async Task<int> RunAsync(string[] args)
{
    if (args.Length == 0 || args[0] is "help" or "--help" or "-h")
    {
        PrintHelp();
        return 0;
    }

    using var client = PrimpClient.Builder()
        .WithImpersonate(Impersonate.Chrome146)
        .WithOS(ImpersonateOS.Windows)
        .WithTimeout(TimeSpan.FromSeconds(30))
        .FollowRedirects(true)
        .Build();

    try
    {
        switch (args[0].ToLowerInvariant())
        {
            case "version":
                Console.WriteLine(PrimpClient.NativeVersion);
                return 0;

            case "get":
                return await HandleGetAsync(client, args);

            case "post":
                return await HandlePostAsync(client, args);

            case "headers":
                return await HandleHeadersAsync(client, args);

            case "tls":
                return await HandleTlsAsync(client);

            default:
                Console.Error.WriteLine($"Unknown command: {args[0]}");
                PrintHelp();
                return 2;
        }
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine(ex.Message);
        return 1;
    }
}

static async Task<int> HandleGetAsync(PrimpClient client, string[] args)
{
    if (args.Length < 2)
    {
        Console.Error.WriteLine("Usage: get <url>");
        return 2;
    }

    using var response = await client.GetAsync(args[1]);
    return WriteResponse(response);
}

static async Task<int> HandlePostAsync(PrimpClient client, string[] args)
{
    if (args.Length < 3)
    {
        Console.Error.WriteLine("Usage: post <url> <json>");
        return 2;
    }

    using var response = await client.PostAsync(args[1], args[2], "application/json");
    return WriteResponse(response);
}

static async Task<int> HandleHeadersAsync(PrimpClient client, string[] args)
{
    if (args.Length < 2)
    {
        Console.Error.WriteLine("Usage: headers <url>");
        return 2;
    }

    using var response = await client.GetAsync(args[1]);
    var payload = new
    {
        status = (int)response.StatusCode,
        url = response.Url,
        headers = response.Headers,
    };

    Console.WriteLine(JsonSerializer.Serialize(payload));
    return response.StatusCode == HttpStatusCode.OK ? 0 : 1;
}

static async Task<int> HandleTlsAsync(PrimpClient client)
{
    using var response = await client.GetAsync("https://tls.peet.ws/api/all");
    return WriteResponse(response);
}

static int WriteResponse(PrimpResponse response)
{
    var payload = new
    {
        status = (int)response.StatusCode,
        url = response.Url,
        body = response.ReadAsString(),
    };

    Console.WriteLine(JsonSerializer.Serialize(payload));
    return response.StatusCode == HttpStatusCode.OK ? 0 : 1;
}

static void PrintHelp()
{
    Console.WriteLine("Primp.NET CLI");
    Console.WriteLine("Commands:");
    Console.WriteLine("  version                    Print native primp_ffi version");
    Console.WriteLine("  get <url>                  Execute GET request");
    Console.WriteLine("  post <url> <json>          Execute POST request with JSON body");
    Console.WriteLine("  headers <url>              Execute GET and print headers");
    Console.WriteLine("  tls                        Fetch TLS fingerprint payload");
    Console.WriteLine("  help                       Show this help");
}
