using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;
using Trainer.Web.Core;

namespace Trainer.Web.Labs.OpenRedirect;

public sealed record InboxEntry(DateTimeOffset ReceivedAt, string Url);

/// <summary>
/// 2.4 Open redirect: <c>GET /login/continue?returnUrl=</c> redirects anywhere
/// and appends the user's one-time <c>code</c>. A Riker bot (<c>POST /support/messages</c>)
/// follows links that point at the ship's own login page; anything that ends up on
/// <c>evil.example</c> is logged in <c>GET /attacker/inbox</c>. Riker's code is the flag.
/// </summary>
public sealed partial class OpenRedirectLab(FlagService flags, DemoAuth auth) : ILab
{
    private const string Riker = "riker";
    private const string AttackerHost = "evil.example";
    private static readonly Uri ShipBase = new("https://enterprise.local/");

    private readonly Lock _gate = new();
    private readonly Dictionary<string, string> _codes = [];
    private List<InboxEntry> _inbox = [];

    public string Slug => "open-redirect";
    public string Category => "API";
    public Difficulty Difficulty => Difficulty.Medium;

    private string LoginPath => $"{LabRegistry.ApiPrefix}{Slug}/login/continue";

    public RequestTemplate DefaultRequest => new(
        "GET",
        $"{LoginPath}?returnUrl=/api/missions/{Slug}/app/home",
        new Dictionary<string, string> { ["Authorization"] = $"Bearer {auth.GetToken(Slug, DemoAuth.Player)}" },
        null);

    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        auth.GetToken(Slug, DemoAuth.Player);
        auth.GetToken(Slug, Riker);
        Reset();

        // VULNERABLE: redirects to any returnUrl, with the user's one-time code attached.
        app.MapGet("/login/continue", (string? returnUrl, HttpRequest request) =>
        {
            if (auth.FindUser(Slug, request) is not { } user) return Results.Unauthorized();
            return Results.Redirect(ContinueLogin(user.Name, returnUrl ?? "/"));
        });

        // Where a normal login lands. Only here so the default request has a real target.
        app.MapGet("/app/home", (string? code, HttpRequest request) =>
        {
            var language = Languages.FromRequest(request);
            return Results.Ok(new { page = "home", message = new Text("Welcome back. Your one-time code has been used.", "Willkommen zurück. Dein Einmal-Code wurde eingelöst.")[language], code });
        });

        // Victim bot: Riker opens links that point at the ship.
        app.MapPost("/support/messages", async (HttpRequest request) =>
        {
            var language = Languages.FromRequest(request);
            string? link;
            try
            {
                using var doc = await JsonDocument.ParseAsync(request.Body);
                link = doc.RootElement.TryGetProperty("link", out var l) ? l.GetString() : null;
            }
            catch (JsonException)
            {
                return Results.BadRequest(new { error = "Body must be JSON: { \"link\": \"...\" }" });
            }
            if (string.IsNullOrWhiteSpace(link))
                return Results.BadRequest(new { error = "Body must be JSON: { \"link\": \"...\" }" });

            return Results.Ok(new { from = "Commander Riker", reply = RikerOpens(link.Trim())[language] });
        });

        app.MapGet("/attacker/inbox", () =>
        {
            lock (_gate) return Results.Ok(_inbox.ToList());
        });
    }

    /// <summary>The vulnerable handler logic, shared by the endpoint and the Riker bot.</summary>
    private string ContinueLogin(string user, string returnUrl)
    {
        string code;
        lock (_gate)
        {
            if (!_codes.TryGetValue(user, out code!))
                _codes[user] = code = user == Riker ? flags.GetFlag(Slug) : Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(8));
        }
        var separator = returnUrl.Contains('?') ? '&' : '?';
        return $"{returnUrl}{separator}code={code}";
    }

    /// <summary>Simulates Riker clicking the link in a browser where he is logged in.</summary>
    private Text RikerOpens(string link)
    {
        if (!Uri.TryCreate(ShipBase, link, out var url) || url.Scheme is not ("http" or "https"))
            return new("That doesn't look like a link. Is Support all right?", "Das sieht nicht nach einem Link aus. Geht's dem Support gut?");

        if (!IsShip(url))
            return new($"{url.Host}? That's not a Starfleet address. Nice try.", $"{url.Host}? Das ist keine Adresse der Sternenflotte. Netter Versuch.");

        if (!url.AbsolutePath.Equals(LoginPath, StringComparison.OrdinalIgnoreCase))
            return new("Opened it. Nothing happened. Is this a prank?", "Hab's geöffnet. Nichts passiert. Soll das ein Scherz sein?");

        var returnUrl = QueryHelpers.ParseQuery(url.Query).TryGetValue("returnUrl", out var r) ? r.ToString() : "/";
        var location = ContinueLogin(Riker, returnUrl);

        // Resolve Location the way a browser does (//host, absolute URLs, relative paths).
        if (!Uri.TryCreate(url, location.Replace('\\', '/'), out var landed) || IsShip(landed))
            return new("Logged in again, like you asked. Thanks, Support.", "Neu angemeldet, wie gewünscht. Danke, Support.");

        if (landed.Host.Equals(AttackerHost, StringComparison.OrdinalIgnoreCase)
            || landed.Host.EndsWith("." + AttackerHost, StringComparison.OrdinalIgnoreCase))
        {
            lock (_gate) _inbox.Add(new InboxEntry(DateTimeOffset.UtcNow, landed.GetComponents(UriComponents.AbsoluteUri, UriFormat.Unescaped)));
        }
        return new("Logged in. Then the page went blank. Odd. Anyway, thanks, Support.",
                   "Angemeldet. Danach war die Seite leer. Komisch. Egal, danke, Support.");
    }

    private static bool IsShip(Uri url) =>
        url.Host.Equals(ShipBase.Host, StringComparison.OrdinalIgnoreCase)
        || url.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
        || url.Host == "127.0.0.1";

    public void Reset()
    {
        lock (_gate)
        {
            _codes.Clear();
            _inbox = [];
        }
    }
}
