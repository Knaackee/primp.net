using Primp;

// Create a client impersonating Chrome 136 on Windows
using var client = PrimpClient.Builder()
    .WithImpersonate(Impersonate.Chrome146)
    .WithOS(ImpersonateOS.Windows)
    .WithTimeout(TimeSpan.FromSeconds(30))
    .WithCookieStore(true)
    .FollowRedirects(true)
    .Build();

const string localBaseUrl = "http://127.0.0.1:18080";

Console.WriteLine($"Primp FFI version: {PrimpClient.NativeVersion}");

// GET request
Console.WriteLine("\n--- GET Request ---");
using (var response = await client.GetAsync($"{localBaseUrl}/get"))
{
    Console.WriteLine($"Status: {response.StatusCode}");
    Console.WriteLine($"URL: {response.Url}");
    Console.WriteLine($"Headers: {response.Headers.Count} entries");
    Console.WriteLine($"Body preview: {response.ReadAsString()[..200]}...");
}

// POST request with JSON
Console.WriteLine("\n--- POST Request ---");
using (var response = await client.PostAsync(
    $"{localBaseUrl}/post",
    """{"message": "Hello from Primp.NET!"}""",
    "application/json"))
{
    Console.WriteLine($"Status: {response.StatusCode}");
    Console.WriteLine($"Body preview: {response.ReadAsString()[..200]}...");
}

// TLS fingerprint check
Console.WriteLine("\n--- TLS Fingerprint ---");
using (var response = await client.GetAsync("https://tls.peet.ws/api/all"))
{
    Console.WriteLine($"Status: {response.StatusCode}");
    var body = response.ReadAsString();
    Console.WriteLine($"TLS data length: {body.Length} chars");
}

// Different browsers
Console.WriteLine("\n--- Multiple Browsers ---");
Impersonate[] browsers = [Impersonate.Chrome146, Impersonate.Safari185, Impersonate.Firefox148, Impersonate.Edge146];

foreach (var browser in browsers)
{
    using var browserClient = PrimpClient.Builder()
        .WithImpersonate(browser)
        .WithTimeout(TimeSpan.FromSeconds(15))
        .Build();

    using var response = await browserClient.GetAsync($"{localBaseUrl}/get");
    Console.WriteLine($"  {browser}: {response.StatusCode}");
}

Console.WriteLine("\nDone!");
