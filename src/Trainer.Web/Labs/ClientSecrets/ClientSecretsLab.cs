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
public sealed class ClientSecretsLab(FlagService flags, DemoAuth auth) : ILab
{
    private const string ApiKeyHeader = "X-Api-Key";

    private readonly Lock _gate = new();
    private string _apiKey = "";

    public string Slug => "client-secrets";
    public Text Title => new("Open Frequency", "Offene Frequenz");
    public string Category => "Blazor";
    public Difficulty Difficulty => Difficulty.Easy;

    public LabStory Story => new(
        Act: 2,
        Order: 2,
        Briefing: new(
            En: """
                Captain's log, stardate 47326.0. Starbase 74 has sent us a bill: four hundred isolinear
                processor cores, two shuttle engines and a case of Saurian brandy, all ordered by the
                Enterprise in the last ten hours. The quartermaster ordered none of it. The orders were
                signed with our own supply key, which only the quartermaster's new requisition app
                should know. I have asked Mr. Crusher to find out who else knows it.
                """,
            De: """
                Logbuch des Captains, Sternzeit 47326.0. Sternenbasis 74 hat uns eine Rechnung geschickt:
                vierhundert isolineare Prozessorkerne, zwei Shuttle-Triebwerke und eine Kiste saurianischer
                Brandy, alles in den letzten zehn Stunden von der Enterprise bestellt. Der Quartiermeister hat
                nichts davon bestellt. Unterschrieben wurden die Bestellungen mit unserem eigenen Schlüssel
                für das Lager, und den sollte nur die neue Bestell-App des Quartiermeisters kennen. Ich habe
                Mr. Crusher gebeten herauszufinden, wer ihn sonst noch kennt.
                """),
        CallingCard: new(
            En: """
                Saurian brandy, Wesley. For the victory party. You're not invited, you're too young.
                Shopping was fun, but a supply key isn't enough for me. I want the First Officer. Did
                you know his access code has only four digits? Four. My shopping lists are longer. — L.
                """,
            De: """
                Saurianischer Brandy, Wesley. Für die Siegesfeier. Du bist nicht eingeladen, du bist zu jung.
                Einkaufen war lustig, aber ein Lagerschlüssel reicht mir nicht. Ich will den Ersten Offizier.
                Wusstest du, dass sein Zugangscode nur vier Ziffern hat? Vier. Meine Einkaufslisten sind
                länger. — L.
                """),
        Debrief: new(
            En: """
                Good work, Mr. Crusher. The supply key now lives on the ship's server, and the app asks
                our server instead of Starbase 74. The old key has been revoked, and the orders are
                cancelled. All except the brandy. That had already been delivered, and nobody will tell
                me where.
                """,
            De: """
                Gute Arbeit, Mr. Crusher. Der Lagerschlüssel liegt jetzt auf dem Server des Schiffs, und
                die App fragt unseren Server statt direkt die Sternenbasis. Der alte Schlüssel ist gesperrt,
                und die Bestellungen sind storniert. Bis auf den Brandy. Der war schon geliefert, und keiner
                will mir sagen, wohin.
                """));

    private const string VulnerableCode = """
        ```csharp
        // Quartermaster.Client/Program.cs (Blazor WebAssembly, runs in the browser)
        var builder = WebAssemblyHostBuilder.CreateDefault(args);
        builder.RootComponents.Add<App>("#app");

        // Settings come from wwwroot/appsettings.json.
        builder.Services.AddHttpClient("Starbase", c =>
        {
            c.BaseAddress = new Uri(builder.Configuration["Starbase:SupplyApiUrl"]!);
            c.DefaultRequestHeaders.Add("X-Api-Key", builder.Configuration["Starbase:ApiKey"]);
        });

        await builder.Build().RunAsync();
        ```
        """;

    public Text ScenarioMarkdown => new(
        En: $"""
            The quartermaster's requisition app is a **Blazor WebAssembly** app: it is downloaded
            into the browser and runs there. It orders supplies straight from the Starbase 74
            supply API, which wants an API key in the `X-Api-Key` header.

            | Endpoint | What it does |
            |---|---|
            | `GET /api/labs/client-secrets/static/index.html` | The app's start page (a static file) |
            | `GET /api/labs/client-secrets/static/appsettings.json` | The app's settings (a static file) |
            | `GET /api/labs/client-secrets/starbase/requisitions` | Starbase 74: all orders for the Enterprise (needs `X-Api-Key`) |

            The app sets up its connection to the Starbase like this:

            {VulnerableCode}

            **Goal:** read the Enterprise's orders at Starbase 74. The flag is the
            `trackingCode` of an order the quartermaster never placed.
            """,
        De: $"""
            Die Bestell-App des Quartiermeisters ist eine **Blazor-WebAssembly**-App: Sie wird in den
            Browser geladen und läuft dort. Sie bestellt direkt bei der Lager-API von Sternenbasis 74,
            und die verlangt einen API-Schlüssel im Header `X-Api-Key`.

            | Endpunkt | Funktion |
            |---|---|
            | `GET /api/labs/client-secrets/static/index.html` | Die Startseite der App (statische Datei) |
            | `GET /api/labs/client-secrets/static/appsettings.json` | Die Einstellungen der App (statische Datei) |
            | `GET /api/labs/client-secrets/starbase/requisitions` | Sternenbasis 74: alle Bestellungen der Enterprise (braucht `X-Api-Key`) |

            So richtet die App ihre Verbindung zur Sternenbasis ein:

            {VulnerableCode}

            **Ziel:** Lies die Bestellungen der Enterprise bei Sternenbasis 74. Die Flag ist der
            `trackingCode` einer Bestellung, die der Quartiermeister nie aufgegeben hat.
            """);

    public IReadOnlyList<Text> Hints =>
    [
        new("The requisition app is a Blazor WebAssembly app. Everything it needs to run, it first downloads into the browser. That includes its configuration from `appsettings.json`, which is served as an ordinary static file.",
            "Die Bestell-App ist eine Blazor-WebAssembly-App. Alles, was sie zum Laufen braucht, lädt sie zuerst in den Browser. Dazu gehört auch ihre Konfiguration aus `appsettings.json`, die als ganz normale statische Datei ausgeliefert wird."),
        new("`GET /static/appsettings.json` requires no token. Static files rarely do. I find the section named `Starbase` particularly informative.",
            "`GET /static/appsettings.json` verlangt kein Token. Bei statischen Dateien ist das selten anders. Den Abschnitt `Starbase` finde ich besonders aufschlussreich."),
        new("The Starbase API expects the key in an `X-Api-Key` header. If you add it to `GET /starbase/requisitions`, you will see what was ordered in our name.",
            "Die API der Sternenbasis erwartet den Schlüssel im Header `X-Api-Key`. Wenn Sie ihn bei `GET /starbase/requisitions` mitschicken, sehen Sie, was in unserem Namen bestellt wurde."),
    ];

    public RequestTemplate DefaultRequest => new(
        "GET",
        $"{LabRegistry.ApiPrefix}{Slug}/starbase/requisitions",
        new Dictionary<string, string> { [ApiKeyHeader] = "" },
        null);

    public IReadOnlyList<FixOption> FixOptions =>
    [
        new("bff-proxy", """
            // Quartermaster.Server/Program.cs (runs on the ship's server)
            // The key comes from user secrets / Key Vault, never from wwwroot.
            builder.Services.AddHttpClient("Starbase", c =>
            {
                c.BaseAddress = new Uri(builder.Configuration["Starbase:SupplyApiUrl"]!);
                c.DefaultRequestHeaders.Add("X-Api-Key", builder.Configuration["Starbase:ApiKey"]);
            });

            app.MapPost("/api/requisitions", async (RequisitionRequest order, IHttpClientFactory http) =>
            {
                var starbase = http.CreateClient("Starbase");
                var response = await starbase.PostAsJsonAsync("requisitions", order);
                return Results.StatusCode((int)response.StatusCode);
            })
            .RequireAuthorization("Quartermaster");

            // The WebAssembly client only calls /api/requisitions with the user's own login.
            """, IsCorrect: true,
            new("Correct. This is the backend-for-frontend pattern: the key never leaves the server, and the browser " +
                "app talks to its own backend with the user's login. The backend decides who may order, then calls Starbase 74.",
                "Richtig. Das ist das Backend-for-Frontend-Muster: Der Schlüssel verlässt nie den Server, und die App im Browser spricht mit der Anmeldung des Benutzers nur mit ihrem eigenen Backend. Das Backend entscheidet, wer bestellen darf, und ruft erst dann Sternenbasis 74 auf."),
            BarclayLine: new(
                "W-what if the app doesn't talk to the Starbase at all? It asks our server, and our server asks the Starbase. It's an extra hop, I know. S-sorry.",
                "U-und wenn die App gar nicht direkt mit der Sternenbasis redet? Sie fragt unseren Server, und der fragt die Sternenbasis. Ist ein Umweg, ich weiß. T-tut mir leid."),
            ControllerCode: """
            // Quartermaster.Server/Program.cs (runs on the ship's server)
            // The key comes from user secrets / Key Vault, never from wwwroot.
            builder.Services.AddHttpClient("Starbase", c =>
            {
                c.BaseAddress = new Uri(builder.Configuration["Starbase:SupplyApiUrl"]!);
                c.DefaultRequestHeaders.Add("X-Api-Key", builder.Configuration["Starbase:ApiKey"]);
            });

            [ApiController]
            [Route("api/requisitions")]
            [Authorize(Policy = "Quartermaster")]
            public class RequisitionsController(IHttpClientFactory http) : ControllerBase
            {
                [HttpPost]
                public async Task<IActionResult> Order(RequisitionRequest order)
                {
                    var starbase = http.CreateClient("Starbase");
                    var response = await starbase.PostAsJsonAsync("requisitions", order);
                    return StatusCode((int)response.StatusCode);
                }
            }

            // The WebAssembly client only calls /api/requisitions with the user's own login.
            """),

        new("base64-key", """
            // wwwroot/appsettings.json
            // "Starbase": { "ApiKeyEncoded": "c2I3NC1saXZlLTlmMmQ..." }

            var encoded = builder.Configuration["Starbase:ApiKeyEncoded"]!;
            var key = Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
            c.DefaultRequestHeaders.Add("X-Api-Key", key);
            """, IsCorrect: false,
            new("Base64 is an encoding, not encryption. Anyone can decode it in a second, and the decoding code ships " +
                "with the app anyway.",
                "Base64 ist eine Codierung, keine Verschlüsselung. Jeder kann das in einer Sekunde zurückverwandeln, und der Code zum Decodieren wird sowieso mit der App ausgeliefert."),
            BarclayLine: new(
                "We could encode the key. Base64. Then it's not just lying there in plain text. N-nobody reads Base64. Do they?",
                "W-wir könnten den Schlüssel codieren. Base64. Dann liegt er nicht einfach so im Klartext rum. Base64 liest doch k-keiner. Oder?")),

        new("csharp-constant", """
            // Quartermaster.Client/StarbaseSettings.cs
            internal static class StarbaseSettings
            {
                public const string ApiKey = "sb74-live-9f2d...";
            }

            c.DefaultRequestHeaders.Add("X-Api-Key", StarbaseSettings.ApiKey);
            """, IsCorrect: false,
            new("A WebAssembly app's DLLs are downloaded by the browser like any other file. Open the network tab, save " +
                "`Quartermaster.Client.dll` (or the `.wasm` bundle) and any decompiler shows the constant.",
                "Die DLLs einer WebAssembly-App lädt der Browser herunter wie jede andere Datei. Im Netzwerk-Tab kannst du `Quartermaster.Client.dll` (oder das `.wasm`-Paket) speichern, und jeder Decompiler zeigt dir die Konstante."),
            BarclayLine: new(
                "Config files are easy to read, sir. I'll move the key into the code. A constant. It g-gets compiled, so it's safe.",
                "Konfigurationsdateien kann jeder lesen, Sir. I-ich schreibe den Schlüssel einfach in den Code, als Konstante. Der wird ja kompiliert, also ist er sicher.")),

        new("cors", """
            // Starbase 74 supply API
            builder.Services.AddCors(o => o.AddPolicy("Enterprise", p =>
                p.WithOrigins("https://quartermaster.enterprise.local")
                 .AllowAnyHeader()
                 .AllowAnyMethod()));

            app.UseCors("Enterprise");
            """, IsCorrect: false,
            new("CORS only tells *browsers* which pages may read a response. Your request console, `curl` or any script " +
                "ignores it completely, and the key is still public.",
                "CORS sagt nur *Browsern*, welche Seiten eine Antwort lesen dürfen. Deine Request-Konsole, `curl` oder irgendein Skript ignorieren das völlig, und der Schlüssel ist weiterhin öffentlich."),
            BarclayLine: new(
                "I'll ask Starbase 74 to turn on CORS. Only our domain is allowed. Th-that's what CORS is for, right? Keeping others out?",
                "Ich b-bitte Sternenbasis 74, CORS einzuschalten. Nur unsere Domain ist erlaubt. D-dafür ist CORS doch da, oder? Um andere draußen zu halten?")),
    ];

    public Text ExplanationMarkdown => new(
        En: """
            **Secrets in client code** are not secrets. A Blazor WebAssembly app runs in the
            browser, so everything it uses is downloaded there first: `wwwroot/appsettings.json`,
            the app's DLLs, every constant inside them. The same is true for JavaScript bundles
            and mobile apps. If the app can read the key, so can its user, and so can Lore.

            Keep keys on a server you control. The browser app talks to its own backend with the
            user's login (cookie or token), and the backend calls the third-party API with the
            key from user secrets or Azure Key Vault (the *backend for frontend* pattern). Then
            the backend can also check who may order what. If a key has leaked, revoke it.

            Why the other options fail:
            - **Base64** is an encoding anyone can reverse.
            - **A C# constant** ends up in a DLL the browser downloads.
            - **CORS** is enforced by browsers only, and it protects the reader, not the key.

            Read more:
            - [Microsoft Docs: Secure ASP.NET Core Blazor WebAssembly](https://learn.microsoft.com/aspnet/core/blazor/security/webassembly/)
            - [OWASP: Secrets Management Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Secrets_Management_Cheat_Sheet.html)
            """,
        De: """
            **Geheimnisse im Client-Code** sind keine Geheimnisse. Eine Blazor-WebAssembly-App läuft
            im Browser, also wird alles, was sie benutzt, vorher dorthin geladen: `wwwroot/appsettings.json`,
            die DLLs der App und jede Konstante darin. Bei JavaScript-Bundles und mobilen Apps ist es
            genauso. Wenn die App den Schlüssel lesen kann, kann ihn auch der Benutzer lesen, und Lore auch.

            Schlüssel gehören auf einen Server, den du kontrollierst. Die App im Browser spricht mit der
            Anmeldung des Benutzers (Cookie oder Token) mit ihrem eigenen Backend. Erst das Backend ruft
            die fremde API auf, mit dem Schlüssel aus den User Secrets oder aus Azure Key Vault. Dieses
            Muster heißt *Backend for Frontend*. Dann kann das Backend auch gleich prüfen, wer was bestellen
            darf. Ist ein Schlüssel schon durchgesickert, sperre ihn.

            Warum die anderen Optionen nicht helfen:
            - **Base64** ist eine Codierung, die jeder umkehren kann.
            - **Eine C#-Konstante** landet in einer DLL, die der Browser herunterlädt.
            - **CORS** wird nur von Browsern durchgesetzt und schützt den Leser, nicht den Schlüssel.

            Mehr dazu:
            - [Microsoft Docs: Blazor WebAssembly absichern](https://learn.microsoft.com/de-de/aspnet/core/blazor/security/webassembly/)
            - [OWASP: Secrets Management Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Secrets_Management_Cheat_Sheet.html)
            """);

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
