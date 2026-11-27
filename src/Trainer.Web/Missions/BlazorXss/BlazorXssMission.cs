using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using Trainer.Web.Core;

namespace Trainer.Web.Missions.BlazorXss;

public sealed record Comment(int Id, string Author, string Text, DateTimeOffset At);

public sealed record Collected(DateTimeOffset At, Dictionary<string, string> Query);

/// <summary>
/// 3.5 Stored XSS in Blazor, the finale. Comments render like
/// <c>@((MarkupString)comment.Text)</c>. The moderator view (Lore's screen) is a page in a
/// sandboxed iframe that inserts every comment with <c>innerHTML</c>, exactly what
/// MarkupString does, so <c>&lt;script&gt;</c> doesn't run but event handlers do. The page
/// holds <c>window.modToken</c> (the flag). <c>/collect?...</c> records any request, and
/// <c>GET /collected</c> lists them.
/// </summary>
public sealed partial class BlazorXssMission(FlagService flags, DemoAuth auth) : IMission, IHasModeratorView
{
    private const int MaxCommentLength = 2000;
    private const int MaxCollected = 50;

    private readonly Lock _gate = new();
    private readonly string _moderatorKey = Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(16));
    private List<Comment> _comments = [];
    private List<Collected> _collected = [];
    private int _nextId;

    public string Slug => "blazor-xss";
    public string Category => "Blazor";
    public Difficulty Difficulty => Difficulty.Hard;
    public IReadOnlyList<MissionTool> Tools => [MissionTool.ModeratorView];

    public string ModeratorViewUrl => $"{MissionRegistry.ApiPrefix}{Slug}/moderator?key={_moderatorKey}";

    public RequestTemplate DefaultRequest => new(
        "POST",
        $"{MissionRegistry.ApiPrefix}{Slug}/comments",
        new Dictionary<string, string> { ["Authorization"] = $"Bearer {auth.GetToken(Slug, DemoAuth.Player)}" },
        """{ "text": "Jazz night at <b>20:00</b>. Bring your own trombone." }""");

    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        auth.GetToken(Slug, DemoAuth.Player);
        Reset();

        app.MapGet("/comments", (HttpRequest request) =>
        {
            if (auth.FindUser(Slug, request) is null) return Results.Unauthorized();
            lock (_gate) return Results.Ok(_comments.ToList());
        });

        // VULNERABLE (together with the MarkupString rendering): stored as-is.
        app.MapPost("/comments", async (HttpRequest request) =>
        {
            if (auth.FindUser(Slug, request) is not { } user) return Results.Unauthorized();
            string? text;
            try
            {
                using var doc = await JsonDocument.ParseAsync(request.Body);
                text = doc.RootElement.TryGetProperty("text", out var t) ? t.GetString() : null;
            }
            catch (JsonException)
            {
                text = null;
            }
            if (string.IsNullOrWhiteSpace(text) || text.Length > MaxCommentLength)
                return Results.BadRequest(new { error = $"Body must be JSON: {{ \"text\": \"...\" }}, at most {MaxCommentLength} characters." });

            lock (_gate)
            {
                var comment = new Comment(++_nextId, user.Name, text, DateTimeOffset.UtcNow);
                _comments.Add(comment);
                return Results.Ok(comment);
            }
        });

        // The listening post: anything may call it, it records the query string.
        app.MapGet("/collect", (HttpRequest request) =>
        {
            var query = request.Query.ToDictionary(q => q.Key, q => q.Value.ToString());
            lock (_gate)
            {
                _collected.Add(new Collected(DateTimeOffset.UtcNow, query));
                if (_collected.Count > MaxCollected) _collected.RemoveAt(0);
            }
            return Results.NoContent();
        });

        app.MapGet("/collected", () =>
        {
            lock (_gate) return Results.Ok(_collected.ToList());
        });

        // Lore's moderator screen, loaded in a sandboxed iframe on the mission page. The key keeps
        // the request console from simply fetching it.
        app.MapGet("/moderator", (string? key, string? lang) =>
        {
            if (!CryptographicOperations.FixedTimeEquals(
                    System.Text.Encoding.UTF8.GetBytes(key ?? ""), System.Text.Encoding.UTF8.GetBytes(_moderatorKey)))
                return Results.Json(new { error = "Moderators only." }, statusCode: StatusCodes.Status403Forbidden);

            List<Comment> comments;
            lock (_gate) comments = _comments.ToList();
            return Results.Content(ModeratorPage(comments, lang == "de" ? Language.De : Language.En), "text/html; charset=utf-8");
        });
    }

    private string ModeratorPage(List<Comment> comments, Language language)
    {
        string T(string en, string de) => WebUtility.HtmlEncode(language == Language.De ? de : en);
        // System.Text.Json escapes <, > and & by default, so no comment can close this script tag.
        var data = JsonSerializer.Serialize(comments.Select(c => new { author = c.Author, text = c.Text }));
        var token = JsonSerializer.Serialize(flags.GetFlag(Slug));

        return $$"""
            <!DOCTYPE html>
            <html lang="{{Languages.Code(language)}}">
            <head>
            <meta charset="utf-8">
            <title>Ten Forward – moderation</title>
            <style>
              body { font-family: system-ui, sans-serif; margin: 0.75rem; color: #222; background: #fff; }
              h1 { font-size: 1rem; margin: 0 0 0.25rem; }
              .who { font-weight: 600; color: #5a3d8a; }
              .comment { border-bottom: 1px solid #ddd; padding: 0.4rem 0; }
              .mod { color: #a33; font-size: 0.85rem; margin: 0 0 0.5rem; }
            </style>
            </head>
            <body>
            <h1>{{T("Ten Forward comment board: moderation", "Kommentarbrett Zehn Vorne: Moderation")}}</h1>
            <p class="mod">{{T("Moderator: L. (session active). Reading every comment.", "Moderator: L. (Sitzung aktiv). Liest jeden Kommentar.")}}</p>
            <div id="comments"></div>
            <script id="data" type="application/json">{{data}}</script>
            <script>
              window.modToken = {{token}};
              const list = document.getElementById("comments");
              for (const c of JSON.parse(document.getElementById("data").textContent)) {
                const item = document.createElement("div");
                item.className = "comment";
                const who = document.createElement("div");
                who.className = "who";
                who.textContent = c.author;
                const text = document.createElement("div");
                text.innerHTML = c.text;   // what (MarkupString) does
                item.append(who, text);
                list.append(item);
              }
              setTimeout(() => location.reload(), 5000);
            </script>
            </body>
            </html>
            """;
    }

    public void Reset()
    {
        lock (_gate)
        {
            var at = DateTimeOffset.UtcNow;
            _comments =
            [
                new(1, "guinan", "New drink special tonight: the <b>Warp Core Breach</b>. Limit one per customer. Two for Commander Riker.", at),
                new(2, "worf", "Prune juice is a <i>warrior's</i> drink. Whoever keeps replacing mine with apple juice: I know.", at),
                new(3, "data", "I have composed a poem for my cat, <i>Ode to Spot</i>. It has 212 stanzas. Readings on request.", at),
                new(4, "riker", "Jazz night on Friday. Bring your own trombone. Or don't, I'll bring mine.", at),
                new(5, "lore", "<b>Moderator note:</b> be nice, everyone. I read every comment. Every. Single. One. — L.", at),
            ];
            _nextId = _comments.Count;
            _collected = [];
        }
    }
}
