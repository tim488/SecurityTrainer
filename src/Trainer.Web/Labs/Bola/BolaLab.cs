using Trainer.Web.Core;

namespace Trainer.Web.Labs.Bola;

public sealed record PersonalLog(int Id, string OwnerId, string Stardate, string Title, string Notes);

/// <summary>
/// 1.1 Broken Object Level Authorization (SPEC §6): <c>GET /logs/{id}</c> returns
/// any log to any logged-in user. Riker's log 1003 holds the flag.
/// </summary>
public sealed class BolaLab(FlagService flags, DemoAuth auth) : ILab
{
    private const string Riker = "riker";
    private const int FlagLogId = 1003;

    private readonly Lock _gate = new();
    private List<PersonalLog> _logs = [];

    public string Slug => "bola";
    public string Title => "Someone Else's Personal Log";
    public string Category => "API";
    public Difficulty Difficulty => Difficulty.Easy;

    public LabStory Story => new(
        Act: 1,
        Order: 1,
        Briefing: """
            Captain's log, stardate 47321.4. One hour after the refit, an unidentified
            signature appeared in the computer core. Now an ensign reports that she can
            open personal logs that are not her own. Personal logs are private by
            regulation. If an ensign can read them, so can whatever is hiding in the
            core. I have asked Mr. Crusher to confirm the report from his own account,
            before Commander Riker hears of it. Quietly.
            """,
        CallingCard: """
            Hello, Wesley. Reading the Commander's diary? So was I. Forty minutes on
            trombone technique. Riveting. But one log at a time is so slow. Somewhere on
            this ship there is a list of everyone aboard. I think I'll read that next.
            — L.
            """,
        Debrief: """
            Well done, Mr. Crusher. Every log now answers only to its owner. Commander
            Riker asked me to thank you, and to ask how much of his log you read. I told
            him you were thorough. He did not take it as a compliment.
            """);

    public string ScenarioMarkdown => """
        The ship's personal log service is a small Web API. Every crew member can list
        their own logs and open one by its ID:

        | Endpoint | What it does |
        |---|---|
        | `GET /api/labs/bola/logs` | Lists the logs of the logged-in user |
        | `GET /api/labs/bola/logs/{id}` | Returns one log with its full notes |

        You are logged in as `wesley`; your bearer token is already in the request console.

        The endpoint that returns a single log looks like this:

        ```csharp
        app.MapGet("/logs/{id:int}", async (int id, LogDb db) =>
            await db.Logs.FindAsync(id) is { } log
                ? Results.Ok(log)
                : Results.NotFound())
           .RequireAuthorization();
        ```

        **Goal:** read a personal log that belongs to someone else. The flag is in the
        `notes` of one of Commander Riker's logs.
        """;

    public IReadOnlyList<string> Hints =>
    [
        "`GET /logs` returns only your own entries, 1001 and 1002. I note that the identifiers are sequential integers. Sequences rarely stop where one's own entries do.",
        "`GET /logs/{id}` verifies that the caller is logged in. I have not observed it verifying anything else.",
        "Commander Riker's most recent log directly follows yours in the sequence. Requesting it with your own token would be instructive. I would prefer that the Commander not learn I suggested this.",
    ];

    public RequestTemplate DefaultRequest => new(
        "GET",
        $"{LabRegistry.ApiPrefix}{Slug}/logs",
        new Dictionary<string, string> { ["Authorization"] = $"Bearer {auth.GetToken(Slug, DemoAuth.Player)}" },
        null);

    public IReadOnlyList<FixOption> FixOptions =>
    [
        new("owner-check", """
            app.MapGet("/logs/{id:int}", async (int id, ClaimsPrincipal user, LogDb db) =>
            {
                var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);

                var log = await db.Logs
                    .FirstOrDefaultAsync(l => l.Id == id && l.OwnerId == userId);

                return log is null ? Results.NotFound() : Results.Ok(log);
            })
            .RequireAuthorization();
            """, IsCorrect: true,
            "Correct. The query itself only matches logs owned by the caller, so every request is checked " +
            "against the object it asks for. Returning 404 (not 403) also avoids confirming that log 1003 exists."),

        new("guid-ids", """
            public class PersonalLog
            {
                public Guid Id { get; set; } = Guid.NewGuid();   // was: int
                // ...
            }

            app.MapGet("/logs/{id:guid}", async (Guid id, LogDb db) =>
                await db.Logs.FindAsync(id) is { } log
                    ? Results.Ok(log)
                    : Results.NotFound())
               .RequireAuthorization();
            """, IsCorrect: false,
            "Obscurity, not authorization. GUIDs are hard to guess but easy to leak: URLs, browser history, " +
            "logs, shared links and other API responses all carry them. Anyone holding the ID can still read the log.",
            BarclayLine: "W-what if the log numbers weren't numbers? Long random ones. Nobody could guess those. C-could they?"),

        new("hide-in-ui", """
            @* LogList.razor *@
            @foreach (var log in logs.Where(l => l.OwnerId == currentUserId))
            {
                <a href="logs/@log.Id">@log.Title</a>
            }
            """, IsCorrect: false,
            "The UI is not the security boundary. You never used the UI in this lab: you called the API directly, " +
            "and the API still returns any log to anyone who asks.",
            BarclayLine: "The log viewer only shows your own logs anyway. M-maybe we just make really, really sure it only shows those?"),

        new("authorize-attribute", """
            builder.Services.AddAuthorizationBuilder()
                .AddPolicy("Crew", p => p.RequireRole("crew"));

            app.MapGet("/logs/{id:int}", [Authorize(Policy = "Crew")] async (int id, LogDb db) =>
                await db.Logs.FindAsync(id) is { } log
                    ? Results.Ok(log)
                    : Results.NotFound());
            """, IsCorrect: false,
            "Authentication and role checks decide who may call the endpoint, not which log they may read. " +
            "Wesley is crew, so he passes the policy and still gets Riker's log.",
            BarclayLine: "I'll put an `[Authorize]` on it, sir. That's what it's for, isn't it? Authorizing?"),
    ];

    public string ExplanationMarkdown => """
        **Broken Object Level Authorization (BOLA)**, also known as IDOR, is the most
        common API vulnerability. The endpoint checked *that* you were logged in, but
        never *whose* log you asked for. Changing `1001` to `1003` was enough.

        The fix belongs on the server, in every request that loads an object by ID:
        filter by the current user (or check ownership after loading) and return
        `404 Not Found` when it doesn't match. For more complex rules, ASP.NET Core's
        resource-based authorization (`IAuthorizationService.AuthorizeAsync(user, log, policy)`)
        keeps the check in one place.

        Why the other options fail:
        - **Random IDs** make guessing harder, but IDs leak. Secrecy of an identifier is not permission.
        - **Hiding data in the UI** protects nothing: attackers call the API directly.
        - **`[Authorize]` / roles** are about *who* may call the endpoint, not *which object* they may access.

        Read more:
        - [OWASP API1:2023 Broken Object Level Authorization](https://owasp.org/API-Security/editions/2023/en/0xa1-broken-object-level-authorization/)
        - [Microsoft Docs: Resource-based authorization in ASP.NET Core](https://learn.microsoft.com/aspnet/core/security/authorization/resourcebased)
        """;

    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        auth.GetToken(Slug, DemoAuth.Player);
        auth.GetToken(Slug, Riker);
        Reset();

        // Correctly scoped: only the caller's own logs.
        app.MapGet("/logs", (HttpRequest request) =>
        {
            if (auth.FindUser(Slug, request) is not { } user) return Results.Unauthorized();
            lock (_gate)
            {
                return Results.Ok(_logs
                    .Where(l => l.OwnerId == user.Name)
                    .Select(l => new { l.Id, l.Stardate, l.Title })
                    .ToList());
            }
        });

        // VULNERABLE: authenticated, but no ownership check.
        app.MapGet("/logs/{id:int}", (int id, HttpRequest request) =>
        {
            if (auth.FindUser(Slug, request) is null) return Results.Unauthorized();
            lock (_gate)
            {
                return _logs.FirstOrDefault(l => l.Id == id) is { } log
                    ? Results.Ok(log)
                    : Results.NotFound();
            }
        });
    }

    public void Reset()
    {
        lock (_gate)
        {
            _logs =
            [
                new(1001, DemoAuth.Player, "47310.2", "First week on the refit crew",
                    "Lt. Barclay let me rewrite the log indexer. He says it's the most important job on the ship. I think he was being nice."),
                new(1002, DemoAuth.Player, "47318.9", "Nobody listens",
                    "Told Geordi the new core timings looked off. He said 'later, Wes.' It's always later."),
                new(FlagLogId, Riker, "47320.5", "Personal — do not share",
                    $"Forty minutes of trombone practice. The bridge of 'Night Bird' is finally coming together. Note to self: {flags.GetFlag(Slug)}"),
            ];
        }
    }
}
