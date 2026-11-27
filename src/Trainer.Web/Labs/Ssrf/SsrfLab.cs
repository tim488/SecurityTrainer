using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Trainer.Web.Core;
using Trainer.Web.Core.Simulation;

namespace Trainer.Web.Labs.Ssrf;

/// <summary>
/// 3.4 Server-side request forgery: <c>POST /link-preview { "url": ... }</c> fetches
/// any URL from inside the ship's (simulated) network. The core's metadata service at
/// <c>http://169.254.169.254/latest/credentials</c> returns credentials with the flag.
/// The fake network also has an internal inventory host, a DNS alias for the metadata
/// service and an open redirector, so the blocklist fix can be bypassed for real.
/// </summary>
public sealed partial class SsrfLab(FlagService flags, DemoAuth auth) : ILab
{
    private const int MaxRedirects = 3;
    private const int SnippetLength = 400;

    public string Slug => "ssrf";
    public Text Title => new("Ping the Core", "Ein Ping an den Hauptcomputer");
    public string Category => "API";
    public Difficulty Difficulty => Difficulty.Hard;

    public LabStory Story => new(
        Act: 3,
        Order: 4,
        Briefing: new(
            En: """
                Captain's log, stardate 47333.0. A Ferengi trader, DaiMon Tarr, is offering to sell us our
                own core access credentials. For a reasonable fee. He received them in a subspace message
                signed by Lore. Those credentials are not stored on any console; only the core itself can
                read them. I have asked Mr. Crusher to find out how Lore made the core hand them over. I
                have asked Mr. Worf not to reply to the Ferengi.
                """,
            De: """
                Logbuch des Captains, Sternzeit 47333.0. Ein ferengischer Händler, DaiMon Tarr, bietet uns unsere
                eigenen Zugangsdaten zum Hauptcomputer an. Gegen eine angemessene Gebühr. Bekommen hat er sie in
                einer Subraumnachricht, unterschrieben von Lore. Diese Zugangsdaten liegen auf keiner Konsole, nur
                der Hauptcomputer selbst kann sie lesen. Ich habe Mr. Crusher gebeten herauszufinden, wie Lore den
                Computer dazu gebracht hat, sie herauszugeben. Mr. Worf habe ich gebeten, dem Ferengi nicht zu
                antworten.
                """),
        CallingCard: new(
            En: """
                The Ferengi pay well, Wesley, but I don't need their money. I needed you all looking the
                other way. While you were busy with the core, I took a little job in Ten Forward. I moderate
                the comment board now, and I read every word your crew writes. Every single one. Write me
                something nice. — L.
                """,
            De: """
                Die Ferengi zahlen gut, Wesley, aber ihr Geld brauch' ich nicht. Ich wollte nur, dass ihr alle
                woanders hinschaut. Während ihr mit dem Hauptcomputer beschäftigt wart, hab' ich mir einen kleinen
                Job in Zehn Vorne besorgt. Ich moderiere jetzt das Kommentarbrett und lese jedes Wort, das eure
                Crew schreibt. Jedes einzelne. Schreib mir doch mal was Nettes. — L.
                """),
        Debrief: new(
            En: """
                Well done, Mr. Crusher. The link preview now fetches only approved public sites, and it
                checks where they really lead first. The core credentials have been changed. DaiMon Tarr has
                lowered his price twice. Mr. Worf has drafted a reply. I have not sent it.
                """,
            De: """
                Gut gemacht, Mr. Crusher. Die Link-Vorschau ruft jetzt nur noch freigegebene öffentliche Seiten ab
                und prüft vorher, wohin sie wirklich führen. Die Zugangsdaten des Hauptcomputers sind geändert.
                DaiMon Tarr hat seinen Preis zweimal gesenkt. Mr. Worf hat eine Antwort entworfen. Ich habe sie
                nicht abgeschickt.
                """));

    private const string VulnerableCode = """
        ```csharp minimal
        public record PreviewRequest(string Url);

        app.MapPost("/link-preview", async (PreviewRequest request, HttpClient http) =>
        {
            var html = await http.GetStringAsync(request.Url);   // follows redirects

            return Results.Ok(new
            {
                request.Url,
                Title = ExtractTitle(html),
                Snippet = html[..Math.Min(html.Length, 400)],
            });
        })
        .RequireAuthorization();
        ```

        ```csharp controller
        public record PreviewRequest(string Url);

        [ApiController]
        [Route("link-preview")]
        [Authorize]
        public class LinkPreviewController(HttpClient http) : ControllerBase
        {
            [HttpPost]
            public async Task<IActionResult> Preview(PreviewRequest request)
            {
                var html = await http.GetStringAsync(request.Url);   // follows redirects

                return Ok(new
                {
                    request.Url,
                    Title = ExtractTitle(html),
                    Snippet = html[..Math.Min(html.Length, 400)],
                });
            }
        }
        ```
        """;

    public Text ScenarioMarkdown => new(
        En: $"""
            Subspace messages show a preview for every link they contain. The ship's core fetches the
            page and returns its title and the first few hundred characters.

            | Endpoint | What it does |
            |---|---|
            | `POST /api/labs/ssrf/link-preview` | Preview of a link: `{"{"} "url": "https://memory-alpha.example/" {"}"}` |

            You are logged in as `wesley`; your bearer token is already in the request console.
            The core sits inside the ship's network, which is simulated here. The preview looks like this:

            {VulnerableCode}

            **Goal:** get the core's own access credentials. The flag is the `SecretAccessKey`.
            """,
        De: $"""
            Subraumnachrichten zeigen für jeden Link eine Vorschau an. Dafür ruft der Hauptcomputer die Seite ab
            und liefert ihren Titel und die ersten paar hundert Zeichen zurück.

            | Endpunkt | Funktion |
            |---|---|
            | `POST /api/labs/ssrf/link-preview` | Vorschau eines Links: `{"{"} "url": "https://memory-alpha.example/" {"}"}` |

            Du bist als `wesley` angemeldet, dein Token steht schon in der Request-Konsole. Der Hauptcomputer
            steht im Netz des Schiffs, das hier simuliert wird. So sieht die Vorschau aus:

            {VulnerableCode}

            **Ziel:** Beschaffe dir die Zugangsdaten des Hauptcomputers. Die Flag ist der `SecretAccessKey`.
            """);

    public IReadOnlyList<Text> Hints =>
    [
        new("`POST /link-preview` fetches whatever URL you give it, from inside the ship's network, and reports the title and the first few hundred characters. With a public page it works as expected.",
            "`POST /link-preview` ruft jede URL ab, die Sie angeben, und zwar aus dem Netz des Schiffs heraus. Danach meldet es den Titel und die ersten paar hundert Zeichen. Mit einer öffentlichen Seite funktioniert das wie erwartet."),
        new("From inside the ship's network, the core can reach hosts that you cannot, for example `http://inventory.internal/`. I find it noteworthy that the preview makes no distinction.",
            "Aus dem Netz des Schiffs heraus erreicht der Computer Hosts, an die Sie nicht herankommen, zum Beispiel `http://inventory.internal/`. Bemerkenswert finde ich, dass die Vorschau dabei keinen Unterschied macht."),
        new("Like many cloud systems, the core serves its own credentials on a link-local metadata address, `http://169.254.169.254/latest/`. It answers anyone who asks from inside. The link preview asks from inside.",
            "Wie viele Cloud-Systeme stellt der Hauptcomputer seine eigenen Zugangsdaten unter einer link-lokalen Metadaten-Adresse bereit: `http://169.254.169.254/latest/`. Er antwortet jedem, der von innen fragt. Die Link-Vorschau fragt von innen."),
    ];

    public RequestTemplate DefaultRequest => new(
        "POST",
        $"{LabRegistry.ApiPrefix}{Slug}/link-preview",
        new Dictionary<string, string> { ["Authorization"] = $"Bearer {auth.GetToken(Slug, DemoAuth.Player)}" },
        """{ "url": "https://memory-alpha.example/" }""");

    public IReadOnlyList<FixOption> FixOptions =>
    [
        new("allowlist-resolve", """
            string[] allowedHosts = ["memory-alpha.example", "starbase74.example"];

            app.MapPost("/link-preview", async (PreviewRequest request, IHttpClientFactory factory) =>
            {
                if (!Uri.TryCreate(request.Url, UriKind.Absolute, out var url)
                    || url.Scheme != Uri.UriSchemeHttps
                    || !allowedHosts.Contains(url.IdnHost, StringComparer.OrdinalIgnoreCase))
                    return Results.BadRequest("Previews are not available for this site.");

                var addresses = await Dns.GetHostAddressesAsync(url.IdnHost);
                if (addresses.Any(a => IPAddress.IsLoopback(a) || IsPrivateOrLinkLocal(a)))
                    return Results.BadRequest("Previews are not available for this site.");

                var http = factory.CreateClient("previews");   // AllowAutoRedirect = false
                var html = await http.GetStringAsync(url);
                return Results.Ok(new { request.Url, Title = ExtractTitle(html) });
            })
            .RequireAuthorization();
            """, IsCorrect: true,
            new("Correct. Only a short list of known public hosts is allowed, their addresses are checked after DNS " +
                "resolution, and redirects are not followed, so a request can't be bounced into the ship's network. " +
                "For full protection, check the address again when connecting (`SocketsHttpHandler.ConnectCallback`) to stop DNS rebinding.",
                "Richtig. Nur eine kurze Liste bekannter öffentlicher Hosts ist erlaubt, ihre Adressen werden nach der DNS-Auflösung geprüft, und Weiterleitungen werden nicht verfolgt. So lässt sich kein Request ins Netz des Schiffs umlenken. Ganz dicht ist es erst, wenn du die Adresse auch beim Verbindungsaufbau noch einmal prüfst (`SocketsHttpHandler.ConnectCallback`), denn sonst hilft DNS-Rebinding."),
            BarclayLine: new(
                "M-maybe the preview only visits sites we know? A short list. And we check where the name really points, b-before we go there?",
                "V-vielleicht besucht die Vorschau nur Seiten, die wir kennen? Eine kurze Liste. Und wir prüfen vorher, wohin der Name wirklich zeigt, b-bevor wir hingehen?"),
            ControllerCode: """
            [ApiController]
            [Route("link-preview")]
            [Authorize]
            public class LinkPreviewController(IHttpClientFactory factory) : ControllerBase
            {
                private static readonly string[] AllowedHosts = ["memory-alpha.example", "starbase74.example"];

                [HttpPost]
                public async Task<IActionResult> Preview(PreviewRequest request)
                {
                    if (!Uri.TryCreate(request.Url, UriKind.Absolute, out var url)
                        || url.Scheme != Uri.UriSchemeHttps
                        || !AllowedHosts.Contains(url.IdnHost, StringComparer.OrdinalIgnoreCase))
                        return BadRequest("Previews are not available for this site.");

                    var addresses = await Dns.GetHostAddressesAsync(url.IdnHost);
                    if (addresses.Any(a => IPAddress.IsLoopback(a) || IsPrivateOrLinkLocal(a)))
                        return BadRequest("Previews are not available for this site.");

                    var http = factory.CreateClient("previews");   // AllowAutoRedirect = false
                    var html = await http.GetStringAsync(url);
                    return Ok(new { request.Url, Title = ExtractTitle(html) });
                }
            }
            """),

        new("string-blocklist", """
            string[] blocked = ["localhost", "127.0.0.1", "169.254.169.254", ".internal"];

            app.MapPost("/link-preview", async (PreviewRequest request, HttpClient http) =>
            {
                if (blocked.Any(b => request.Url.Contains(b, StringComparison.OrdinalIgnoreCase)))
                    return Results.BadRequest("Internal addresses are not allowed.");

                var html = await http.GetStringAsync(request.Url);
                return Results.Ok(new { request.Url, Title = ExtractTitle(html), Snippet = html[..Math.Min(html.Length, 400)] });
            })
            .RequireAuthorization();
            """, IsCorrect: false,
            new("Addresses have many spellings. `http://2852039166/` is 169.254.169.254 written as one number, a DNS name " +
                "can point anywhere, and a public site can redirect to the metadata service, which `HttpClient` follows. The string check sees none of it.",
                "Adressen lassen sich auf viele Arten schreiben. `http://2852039166/` ist 169.254.169.254 als eine einzige Zahl, ein DNS-Name kann überallhin zeigen, und eine öffentliche Seite kann auf den Metadaten-Dienst weiterleiten, und `HttpClient` folgt dem. Die String-Prüfung sieht davon nichts."),
            BarclayLine: new(
                "We just block the dangerous addresses. `localhost`, `127.0.0.1`, the metadata one. I-I wrote them all down. All of them, I think.",
                "W-wir sperren einfach die gefährlichen Adressen. `localhost`, `127.0.0.1`, die Metadaten-Adresse. I-ich hab' sie alle aufgeschrieben. Alle, glaube ich."),
            ControllerCode: """
            [ApiController]
            [Route("link-preview")]
            [Authorize]
            public class LinkPreviewController(HttpClient http) : ControllerBase
            {
                private static readonly string[] Blocked = ["localhost", "127.0.0.1", "169.254.169.254", ".internal"];

                [HttpPost]
                public async Task<IActionResult> Preview(PreviewRequest request)
                {
                    if (Blocked.Any(b => request.Url.Contains(b, StringComparison.OrdinalIgnoreCase)))
                        return BadRequest("Internal addresses are not allowed.");

                    var html = await http.GetStringAsync(request.Url);
                    return Ok(new { request.Url, Title = ExtractTitle(html), Snippet = html[..Math.Min(html.Length, 400)] });
                }
            }
            """),

        new("require-https", """
            app.MapPost("/link-preview", async (PreviewRequest request, HttpClient http) =>
            {
                if (!request.Url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                    return Results.BadRequest("Only https links get a preview.");

                var html = await http.GetStringAsync(request.Url);
                return Results.Ok(new { request.Url, Title = ExtractTitle(html), Snippet = html[..Math.Min(html.Length, 400)] });
            })
            .RequireAuthorization();
            """, IsCorrect: false,
            new("The scheme says how to talk, not where to go. Internal services speak https too " +
                "(`https://inventory.internal/`), and the core still reaches them from inside.",
                "Das Schema sagt, wie gesprochen wird, aber nicht, wohin. Interne Dienste sprechen auch https (`https://inventory.internal/`), und der Hauptcomputer erreicht sie weiterhin von innen."),
            BarclayLine: new(
                "The metadata service only speaks plain http. So if we only allow https, it can't be reached. Th-that's clever, right? It feels clever.",
                "Der Metadaten-Dienst spricht nur einfaches http. W-wenn wir nur https erlauben, kommt also keiner mehr hin. D-das ist doch schlau, oder? Fühlt sich schlau an."),
            ControllerCode: """
            [ApiController]
            [Route("link-preview")]
            [Authorize]
            public class LinkPreviewController(HttpClient http) : ControllerBase
            {
                [HttpPost]
                public async Task<IActionResult> Preview(PreviewRequest request)
                {
                    if (!request.Url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                        return BadRequest("Only https links get a preview.");

                    var html = await http.GetStringAsync(request.Url);
                    return Ok(new { request.Url, Title = ExtractTitle(html), Snippet = html[..Math.Min(html.Length, 400)] });
                }
            }
            """),

        new("title-only", """
            app.MapPost("/link-preview", async (PreviewRequest request, HttpClient http) =>
            {
                var html = await http.GetStringAsync(request.Url);

                return Results.Ok(new { request.Url, Title = ExtractTitle(html) });   // no snippet
            })
            .RequireAuthorization();
            """, IsCorrect: false,
            new("The request still reaches every internal host. Titles of internal pages, error messages and response " +
                "times all leak information, and some internal endpoints *do* something when called. Hiding the answer is not the same as not asking.",
                "Der Request erreicht weiterhin jeden internen Host. Titel interner Seiten, Fehlermeldungen und Antwortzeiten verraten trotzdem etwas, und manche internen Endpunkte *tun* etwas, wenn man sie aufruft. Die Antwort zu verstecken ist nicht dasselbe, wie gar nicht erst zu fragen."),
            BarclayLine: new(
                "W-what if we only show the title? No content at all. Credentials don't have a title. Do they?",
                "U-und wenn wir nur den Titel zeigen? Gar keinen Inhalt. Zugangsdaten haben doch keinen Titel. O-oder?"),
            ControllerCode: """
            [ApiController]
            [Route("link-preview")]
            [Authorize]
            public class LinkPreviewController(HttpClient http) : ControllerBase
            {
                [HttpPost]
                public async Task<IActionResult> Preview(PreviewRequest request)
                {
                    var html = await http.GetStringAsync(request.Url);

                    return Ok(new { request.Url, Title = ExtractTitle(html) });   // no snippet
                }
            }
            """),
    ];

    public Text ExplanationMarkdown => new(
        En: """
            **Server-side request forgery (SSRF)** turns your server into the attacker's browser.
            The link preview fetched any URL from *inside* the network, where the core could reach
            hosts you never could, including the cloud metadata service at `169.254.169.254`, which
            hands out the machine's own credentials to anyone who asks locally.

            If a feature must fetch user-supplied URLs, allow only known hosts (or at least only
            public ones), resolve the name and reject private, loopback and link-local addresses,
            don't follow redirects, and re-check the address when the connection is made
            (`SocketsHttpHandler.ConnectCallback`) to stop DNS rebinding. On the infrastructure
            side, block the metadata service for apps that don't need it (on AWS, require IMDSv2).

            Why the other options fail:
            - **A string blocklist** misses other spellings, DNS names and redirects.
            - **Requiring https** says nothing about the destination.
            - **Returning only the title** still lets the request reach internal hosts.

            Read more:
            - [OWASP API7:2023 Server Side Request Forgery](https://owasp.org/API-Security/editions/2023/en/0xa7-server-side-request-forgery/)
            - [OWASP SSRF Prevention Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Server_Side_Request_Forgery_Prevention_Cheat_Sheet.html)
            """,
        De: """
            **Server-Side Request Forgery (SSRF)** macht deinen Server zum Browser des Angreifers. Die
            Link-Vorschau hat jede URL von *innen* abgerufen, also aus dem Netz heraus, in dem der Hauptcomputer
            Hosts erreicht, an die du nie herankämst. Dazu gehört auch der Cloud-Metadaten-Dienst unter
            `169.254.169.254`, der die Zugangsdaten der Maschine jedem gibt, der lokal danach fragt.

            Wenn eine Funktion URLs von Benutzern abrufen muss, erlaube nur bekannte Hosts oder wenigstens nur
            öffentliche. Löse den Namen auf und lehne private, Loopback- und link-lokale Adressen ab. Folge keinen
            Weiterleitungen und prüfe die Adresse beim Verbindungsaufbau noch einmal
            (`SocketsHttpHandler.ConnectCallback`), sonst hilft DNS-Rebinding. Auf Seiten der Infrastruktur sperrst
            du den Metadaten-Dienst für Anwendungen, die ihn nicht brauchen (bei AWS: IMDSv2 verlangen).

            Warum die anderen Optionen nicht helfen:
            - **Eine String-Sperrliste** übersieht andere Schreibweisen, DNS-Namen und Weiterleitungen.
            - **https verlangen** sagt nichts über das Ziel.
            - **Nur den Titel zurückgeben** lässt den Request trotzdem bei internen Hosts ankommen.

            Mehr dazu:
            - [OWASP API7:2023 Server Side Request Forgery](https://owasp.org/API-Security/editions/2023/en/0xa7-server-side-request-forgery/)
            - [OWASP SSRF Prevention Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Server_Side_Request_Forgery_Prevention_Cheat_Sheet.html)
            """);

    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        auth.GetToken(Slug, DemoAuth.Player);

        // VULNERABLE: fetches any URL, from inside the network, following redirects.
        app.MapPost("/link-preview", async (HttpRequest request) =>
        {
            if (auth.FindUser(Slug, request) is null) return Results.Unauthorized();
            var language = Languages.FromRequest(request);

            string? raw;
            try
            {
                using var doc = await JsonDocument.ParseAsync(request.Body);
                raw = doc.RootElement.TryGetProperty("url", out var u) ? u.GetString() : null;
            }
            catch (JsonException)
            {
                raw = null;
            }
            if (string.IsNullOrWhiteSpace(raw) || !Uri.TryCreate(raw.Trim(), UriKind.Absolute, out var url) || url.Scheme is not ("http" or "https"))
                return Results.BadRequest(new { error = "Body must be JSON with an absolute http(s) URL: { \"url\": \"https://...\" }" });

            using var http = new HttpClient(Network(language), disposeHandler: true);
            try
            {
                for (var hop = 0; ; hop++)
                {
                    using var response = await http.GetAsync(url);
                    if ((int)response.StatusCode is >= 300 and < 400 && response.Headers.Location is { } location && hop < MaxRedirects)
                    {
                        url = new Uri(url, location);
                        continue;
                    }
                    var body = await response.Content.ReadAsStringAsync();
                    return Results.Ok(new
                    {
                        Url = raw,
                        FetchedFrom = url.ToString(),
                        Status = (int)response.StatusCode,
                        Title = ExtractTitle(body),
                        Snippet = body[..Math.Min(body.Length, SnippetLength)],
                    });
                }
            }
            catch (HttpRequestException ex)
            {
                return Results.Json(new { Url = raw, error = ex.Message }, statusCode: StatusCodes.Status502BadGateway);
            }
        });
    }

    private static string? ExtractTitle(string html) =>
        TitleRegex().Match(html) is { Success: true } m ? WebUtility.HtmlDecode(m.Groups[1].Value.Trim()) : null;

    [GeneratedRegex(@"<title>(.*?)</title>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex TitleRegex();

    /// <summary>The ship's network as the core sees it. Pages follow the player's language.</summary>
    private FakeNetwork Network(Language language)
    {
        string T(string en, string de) => language == Language.De ? de : en;
        var credentials = $$"""
            {
              "Code": "Success",
              "Type": "AWS-HMAC",
              "AccessKeyId": "ASIA-ENTERPRISE-CORE",
              "SecretAccessKey": "{{flags.GetFlag(Slug)}}",
              "Token": "core-session-47333",
              "Expiration": "2370-04-02T12:00:00Z"
            }
            """;

        return new FakeNetwork()
            .Host("memory-alpha.example", _ => FakeNetwork.Html(T(
                "<html><head><title>Memory Alpha: USS Enterprise (NCC-1701-D)</title></head><body><p>The USS Enterprise is a Galaxy-class starship, launched in 2363. Its crew is the best in the fleet, as its first officer will tell you.</p></body></html>",
                "<html><head><title>Memory Alpha: USS Enterprise (NCC-1701-D)</title></head><body><p>Die USS Enterprise ist ein Raumschiff der Galaxy-Klasse, in Dienst gestellt 2363. Ihre Crew ist die beste der Flotte, das sagt jedenfalls ihr Erster Offizier.</p></body></html>")))
            .Host("starbase74.example", r => r.RequestUri!.AbsolutePath.StartsWith("/news")
                ? FakeNetwork.Html(T(
                    "<html><head><title>Starbase 74 – News</title></head><body><p>Holodeck upgrades for the Enterprise completed ahead of schedule. Two unrelated Bynars are being sought for questioning.</p></body></html>",
                    "<html><head><title>Sternenbasis 74 – Neuigkeiten</title></head><body><p>Holodeck-Upgrades für die Enterprise früher fertig als geplant. Zwei Bynare werden zur Befragung gesucht, ganz ohne Zusammenhang.</p></body></html>"))
                : FakeNetwork.Html(T("<html><head><title>Starbase 74</title></head><body><p>Welcome to Starbase 74. See /news.</p></body></html>",
                                     "<html><head><title>Sternenbasis 74</title></head><body><p>Willkommen auf Sternenbasis 74. Siehe /news.</p></body></html>")))
            .Host("redirect.example", r =>
            {
                var to = System.Web.HttpUtility.ParseQueryString(r.RequestUri!.Query)["to"];
                return to is null ? FakeNetwork.Text("redirect.example: add ?to=<url>") : FakeNetwork.Redirect(to);
            })
            .Host("inventory.internal", _ => FakeNetwork.Html(T(
                "<html><head><title>Internal inventory service – ENTERPRISE ONLY</title></head><body><pre>Torpedoes: 250\nShuttles: 12\nDilithium: 4 crystals (low!)\nAdmin console: /admin (no password, internal network only)</pre></body></html>",
                "<html><head><title>Interner Inventardienst – NUR ENTERPRISE</title></head><body><pre>Torpedos: 250\nShuttles: 12\nDilithium: 4 Kristalle (knapp!)\nAdmin-Konsole: /admin (kein Passwort, nur internes Netz)</pre></body></html>")))
            .Host("169.254.169.254", r => r.RequestUri!.AbsolutePath.TrimEnd('/') switch
            {
                "" => FakeNetwork.Text("latest/"),
                "/latest" => FakeNetwork.Text("meta-data/\ncredentials"),
                "/latest/meta-data" => FakeNetwork.Text("instance-id: core-01\nlocal-hostname: core.enterprise.internal\nregion: alpha-quadrant-1"),
                "/latest/credentials" => FakeNetwork.Json(credentials),
                _ => FakeNetwork.NotFound(),
            })
            .Alias("metadata.internal", "169.254.169.254")
            .Alias("core.enterprise.internal", "169.254.169.254");
    }

    public void Reset()
    {
        // No state: every preview builds a fresh simulated network.
    }
}
