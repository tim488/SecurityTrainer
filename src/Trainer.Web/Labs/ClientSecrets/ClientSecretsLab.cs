using System.Security.Cryptography;
using Trainer.Web.Core;

namespace Trainer.Web.Labs.ClientSecrets;

/// <summary>
/// 2.2 Secrets in the client: the quartermaster's Blazor WebAssembly app ships
/// its <c>wwwroot/appsettings.json</c>, including the Starbase supply API key, to every
/// browser (simulated at <c>GET /static/appsettings.json</c>). The simulated Starbase API
/// at <c>GET /starbase/requisitions</c> accepts that key and lists Lore's orders, one of
/// which carries the flag as its tracking code.
/// </summary>
public sealed partial class ClientSecretsLab(FlagService flags, DemoAuth auth) : ILab
{
    private const string ApiKeyHeader = "X-Api-Key";

    private readonly Lock _gate = new();
    private string _apiKey = "";

    public string Slug => "client-secrets";
    public string Category => "Blazor";
    public Difficulty Difficulty => Difficulty.Easy;

    // The empty X-Api-Key header shows players where the key goes, so they only have to find it.
    public RequestTemplate DefaultRequest => new(
        "GET",
        $"{LabRegistry.ApiPrefix}{Slug}/starbase/requisitions",
        new Dictionary<string, string> { [ApiKeyHeader] = "" },
        null);

    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        auth.GetToken(Slug, DemoAuth.Player);
        Reset();

        app.MapGet("/static/index.html", () => Results.Content("""
            <!DOCTYPE html>
            <html lang="en">
            <head>
                <meta charset="utf-8" />
                <title>Quartermaster – Requisitions</title>
                <base href="/" />
                <link rel="stylesheet" href="css/app.css" />
            </head>
            <body>
                <div id="app">Loading requisitions…</div>
                <!-- Blazor WebAssembly loads its settings from appsettings.json next to this page. -->
                <script src="_framework/blazor.webassembly.js"></script>
            </body>
            </html>
            """, "text/html"));

        // VULNERABLE: the client's settings, key included, are a public static file.
        app.MapGet("/static/appsettings.json", () =>
        {
            string key;
            lock (_gate) key = _apiKey;
            return Results.Ok(new
            {
                Logging = new { LogLevel = new { Default = "Information" } },
                Starbase = new
                {
                    SupplyApiUrl = "https://supply.starbase74.example/api/",
                    ApiKey = key,
                },
                Ship = new { Registry = "NCC-1701-D", Office = "Quartermaster, deck 4" },
            });
        });

        // Simulated Starbase 74 supply API.
        app.MapGet("/starbase/requisitions", (HttpRequest request) =>
        {
            var language = Languages.FromRequest(request);
            string key;
            lock (_gate) key = _apiKey;
            var sent = request.Headers[ApiKeyHeader].ToString();
            if (!CryptographicOperations.FixedTimeEquals(System.Text.Encoding.UTF8.GetBytes(sent), System.Text.Encoding.UTF8.GetBytes(key)))
                return Results.Json(new { error = new Text("Missing or invalid X-Api-Key.", "X-Api-Key fehlt oder ist ungültig.")[language] },
                    statusCode: StatusCodes.Status401Unauthorized);

            object Order(string id, Text item, int quantity, string requestedBy, string trackingCode) =>
                new { Id = id, Item = item[language], Quantity = quantity, RequestedBy = requestedBy, TrackingCode = trackingCode };

            return Results.Ok(new
            {
                Account = "USS Enterprise, NCC-1701-D",
                Orders = new[]
                {
                    Order("RQ-4471", new("Hyposprays, sterile", "Injektoren, steril"), 200, "Quartermaster", "SB74-1187-AX"),
                    Order("RQ-4472", new("Replicator food patterns, update", "Replikator-Rezepte, Update"), 1, "Quartermaster", "SB74-1188-AX"),
                    Order("RQ-4480", new("Isolinear processor cores", "Isolineare Prozessorkerne"), 400, "Admiral L. Soong", flags.GetFlag(Slug)),
                    Order("RQ-4481", new("Shuttle impulse engines", "Impulstriebwerke für Shuttles"), 2, "Admiral L. Soong", "SB74-1201-LS"),
                    Order("RQ-4482", new("Saurian brandy, vintage 2247", "Saurianischer Brandy, Jahrgang 2247"), 12, "Admiral L. Soong", "SB74-1202-LS"),
                },
            });
        });
    }

    public void Reset()
    {
        lock (_gate) _apiKey = "sb74-live-" + Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(16));
    }
}
