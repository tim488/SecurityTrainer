using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Trainer.Web.Core;
using Trainer.Web.Core.Simulation;

namespace Trainer.Web.Missions.Ssrf;

/// <summary>
/// 3.4 Server-side request forgery: <c>POST /link-preview { "url": ... }</c> fetches
/// any URL from inside the ship's (simulated) network. The core's metadata service at
/// <c>http://169.254.169.254/latest/credentials</c> returns credentials with the flag.
/// The fake network also has an internal inventory host, a DNS alias for the metadata
/// service and an open redirector, so the blocklist fix can be bypassed for real.
/// </summary>
public sealed partial class SsrfMission(FlagService flags, DemoAuth auth) : IMission
{
    private const int MaxRedirects = 3;
    private const int SnippetLength = 400;

    public string Slug => "ssrf";
    public string Category => "API";
    public Difficulty Difficulty => Difficulty.Hard;

    public RequestTemplate DefaultRequest => new(
        "POST",
        $"{MissionRegistry.ApiPrefix}{Slug}/link-preview",
        new Dictionary<string, string> { ["Authorization"] = $"Bearer {auth.GetToken(Slug, DemoAuth.Player)}" },
        """{ "url": "https://memory-alpha.example/" }""");

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
