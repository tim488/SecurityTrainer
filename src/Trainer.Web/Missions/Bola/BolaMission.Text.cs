using Trainer.Web.Core;

namespace Trainer.Web.Missions.Bola;

// Player-facing text of BolaMission: story, scenario, hints, fix options and explanation.
// The endpoints and data are in BolaMission.cs.
public sealed partial class BolaMission
{
    public Text Title => new("Someone Else's Personal Log", "Ein fremdes Logbuch");

    public MissionStory Story => new(
        Act: 1,
        Order: 1,
        Briefing: new(
            En: """
                Captain's log, stardate 47321.4. One hour after the refit, an unidentified
                signature appeared in the computer core. Now an ensign reports that she can
                open personal logs that are not her own. Personal logs are private by
                regulation. If an ensign can read them, so can whatever is hiding in the
                core. I have asked Mr. Crusher to confirm the report from his own account,
                before Commander Riker hears of it. Quietly.
                """,
            De: """
                Logbuch des Captains, Sternzeit 47321.4. Eine Stunde nach dem Software-Update ist im
                Bordcomputer eine unbekannte Signatur aufgetaucht. Jetzt meldet eine junge Offizierin, dass
                sie fremde persönliche Logbücher lesen kann. Die sind laut Vorschrift eigentlich
                vertraulich. Was sie lesen kann, kann auch der Eindringling lesen. Ich habe Mr. Crusher
                gebeten, das mit seinem eigenen Zugang zu prüfen. Und zwar diskret, bevor Commander Riker
                davon erfährt.
                """),
        CallingCard: new(
            En: """
                Hello, Wesley. Reading the Commander's diary? So was I. Forty minutes on
                trombone technique. Riveting. But one log at a time is so slow. Somewhere on
                this ship there is a list of everyone aboard. I think I'll read that next.
                — L.
                """,
            De: """
                Hallo, Wesley. Na, im Tagebuch des Commanders gestöbert? Ich auch. Vierzig Minuten über
                Posaunentechnik, wirklich packend. Aber Logbuch für Logbuch dauert mir zu lange. Irgendwo
                auf diesem Schiff gibt's doch eine Liste der ganzen Crew. Die nehm' ich mir als Nächstes vor.
                — L.
                """),
        Debrief: new(
            En: """
                Well done, Mr. Crusher. Every log now answers only to its owner. Commander
                Riker asked me to thank you, and to ask how much of his log you read. I told
                him you were thorough. He did not take it as a compliment.
                """,
            De: """
                Gute Arbeit, Mr. Crusher. Jetzt kann jeder nur noch sein eigenes Logbuch lesen. Commander
                Riker lässt Ihnen danken und möchte wissen, wie viel Sie von seinem Logbuch gelesen haben.
                Ich sagte ihm, Sie seien sehr gründlich gewesen. Als Kompliment hat er das nicht verstanden.
                """));

    private const string VulnerableCode = """
        ```csharp minimal
        app.MapGet("/logs/{id:int}", async (int id, LogDb db) =>
            await db.Logs.FindAsync(id) is { } log
                ? Results.Ok(log)
                : Results.NotFound())
           .RequireAuthorization();
        ```
        ```csharp controller
        [ApiController]
        [Route("logs")]
        [Authorize]
        public class LogsController(LogDb db) : ControllerBase
        {
            [HttpGet("{id:int}")]
            public async Task<IActionResult> Get(int id) =>
                await db.Logs.FindAsync(id) is { } log ? Ok(log) : NotFound();
        }
        ```
        """;

    public Text ScenarioMarkdown => new(
        En: $"""
            The ship's personal log service is a small Web API. Every crew member can list
            their own logs and open one by its ID:

            | Endpoint | What it does |
            |---|---|
            | `GET /api/missions/bola/logs` | Lists the logs of the logged-in user |
            | `GET /api/missions/bola/logs/{"{id}"}` | Returns one log with its full notes |

            You are logged in as `wesley`; your bearer token is already in the request console.

            The endpoint that returns a single log looks like this:

            {VulnerableCode}

            **Goal:** read a personal log that belongs to someone else. The flag is in the
            `notes` of one of Commander Riker's logs.
            """,
        De: $"""
            Die persönlichen Logbücher der Crew laufen über eine kleine Web-API. Jedes Crewmitglied
            kann damit seine Logbücher auflisten und einzeln abrufen:

            | Endpunkt | Funktion |
            |---|---|
            | `GET /api/missions/bola/logs` | Listet deine Logbücher auf |
            | `GET /api/missions/bola/logs/{"{id}"}` | Gibt ein Logbuch samt Notizen zurück |

            Du bist als `wesley` angemeldet, dein Token steht schon in der Request-Konsole.

            So sieht der Endpunkt für ein einzelnes Logbuch aus:

            {VulnerableCode}

            **Ziel:** Lies ein Logbuch, das nicht dir gehört. Die Flag steht im Feld `notes` eines
            Logbuchs von Commander Riker.
            """);

    public IReadOnlyList<Text> Hints =>
    [
        new("`GET /logs` returns only your own entries, 1001 and 1002. I note that the identifiers are sequential integers. Sequences rarely stop where one's own entries do.",
            "`GET /logs` liefert nur Ihre eigenen Einträge, 1001 und 1002. Mir fällt auf, dass die IDs fortlaufend nummeriert sind. Eine solche Reihe endet selten genau bei den eigenen Einträgen."),
        new("`GET /logs/{id}` verifies that the caller is logged in. I have not observed it verifying anything else.",
            "`GET /logs/{id}` prüft, ob der Aufrufer angemeldet ist. Weitere Prüfungen habe ich nicht festgestellt."),
        new("Commander Riker's logs start directly after yours. Requesting the first one with your own token would be instructive. I would prefer that the Commander not learn I suggested this.",
            "Commander Rikers Logbücher beginnen direkt nach Ihren. Das erste mit Ihrem eigenen Token abzurufen, wäre aufschlussreich. Bitte sagen Sie dem Commander aber nicht, dass der Vorschlag von mir kommt."),
    ];

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
            new("Correct. The query itself only matches logs owned by the caller, so every request is checked " +
                "against the object it asks for. Returning 404 (not 403) also avoids confirming that log 1003 exists.",
                "Richtig. Die Abfrage sucht nur in den Logbüchern des Aufrufers, also wird bei jeder Anfrage der Besitzer geprüft. Und weil 404 statt 403 zurückkommt, verrät die API nicht einmal, dass Logbuch 1003 existiert."),
            BarclayLine: new(
                "Or w-we just check who owns the log, in every query? That's p-probably too simple, isn't it?",
                "Oder w-wir prüfen einfach bei jeder Abfrage, wem das Logbuch gehört? D-das ist bestimmt zu einfach, oder?"),
            ControllerCode: """
            [ApiController]
            [Route("logs")]
            [Authorize]
            public class LogsController(LogDb db) : ControllerBase
            {
                [HttpGet("{id:int}")]
                public async Task<IActionResult> Get(int id)
                {
                    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                    var log = await db.Logs
                        .FirstOrDefaultAsync(l => l.Id == id && l.OwnerId == userId);

                    return log is null ? NotFound() : Ok(log);
                }
            }
            """),

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
            new("Obscurity, not authorization. GUIDs are hard to guess but easy to leak: URLs, browser history, " +
                "logs, shared links and other API responses all carry them. Anyone holding the ID can still read the log.",
                "Das ist Verschleierung, keine Autorisierung. GUIDs sind schwer zu erraten, sickern aber leicht durch, zum Beispiel über URLs, den Browserverlauf, Logs, geteilte Links oder andere API-Antworten. Wer die ID hat, kann das Logbuch weiterhin lesen."),
            BarclayLine: new(
                "W-what if the log numbers weren't numbers? Long random ones. Nobody could guess those. C-could they?",
                "U-und wenn die IDs gar keine Zahlen wären, sondern lange Zufallswerte? D-die errät doch keiner. Oder?"),
            ControllerCode: """
            public class PersonalLog
            {
                public Guid Id { get; set; } = Guid.NewGuid();   // was: int
                // ...
            }

            [ApiController]
            [Route("logs")]
            [Authorize]
            public class LogsController(LogDb db) : ControllerBase
            {
                [HttpGet("{id:guid}")]
                public async Task<IActionResult> Get(Guid id) =>
                    await db.Logs.FindAsync(id) is { } log ? Ok(log) : NotFound();
            }
            """),

        new("hide-in-ui", """
            @* LogList.razor *@
            @foreach (var log in logs.Where(l => l.OwnerId == currentUserId))
            {
                <a href="logs/@log.Id">@log.Title</a>
            }
            """, IsCorrect: false,
            new("The UI is not the security boundary. You never used the UI in this mission: you called the API directly, " +
                "and the API still returns any log to anyone who asks.",
                "Die Oberfläche ist keine Sicherheitsgrenze. Du hast die API in dieser Mission direkt aufgerufen, und die gibt weiterhin jedem jedes Logbuch heraus."),
            BarclayLine: new(
                "The log viewer only shows your own logs anyway. M-maybe we just make really, really sure it only shows those?",
                "Die Ansicht zeigt doch sowieso nur die eigenen Logbücher. V-vielleicht sorgen wir einfach dafür, dass das auch so bleibt?"),
            Language: "razor"),

        new("authorize-attribute", """
            builder.Services.AddAuthorizationBuilder()
                .AddPolicy("Crew", p => p.RequireRole("crew"));

            app.MapGet("/logs/{id:int}", [Authorize(Policy = "Crew")] async (int id, LogDb db) =>
                await db.Logs.FindAsync(id) is { } log
                    ? Results.Ok(log)
                    : Results.NotFound());
            """, IsCorrect: false,
            new("Authentication and role checks decide who may call the endpoint, not which log they may read. " +
                "Wesley is crew, so he passes the policy and still gets Riker's log.",
                "Anmeldung und Rollen regeln, wer den Endpunkt aufrufen darf, aber nicht, welche Logbücher er lesen darf. Wesley gehört zur Crew, kommt also durch die Policy und bekommt trotzdem Rikers Logbuch."),
            BarclayLine: new(
                "I'll put an `[Authorize]` on it, sir. That's what it's for, isn't it? Authorizing?",
                "Ich k-könnte ein `[Authorize]` draufsetzen, Sir. Dafür ist das doch da, oder? Zum … Autorisieren?"),
            ControllerCode: """
            builder.Services.AddAuthorizationBuilder()
                .AddPolicy("Crew", p => p.RequireRole("crew"));

            [ApiController]
            [Route("logs")]
            [Authorize(Policy = "Crew")]
            public class LogsController(LogDb db) : ControllerBase
            {
                [HttpGet("{id:int}")]
                public async Task<IActionResult> Get(int id) =>
                    await db.Logs.FindAsync(id) is { } log ? Ok(log) : NotFound();
            }
            """),
    ];

    public Text ExplanationMarkdown => new(
        En: """
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
            """,
        De: """
            **Broken Object Level Authorization (BOLA)**, auch IDOR genannt, ist die häufigste
            Schwachstelle in APIs. Der Endpunkt hat geprüft, *dass* du angemeldet bist, aber nicht,
            *wessen* Logbuch du abrufst. Es hat gereicht, `1001` durch `1003` zu ersetzen.

            Die Prüfung gehört auf den Server, und zwar überall, wo ein Objekt über seine ID geladen
            wird. Filtere dazu nach dem aktuellen Benutzer oder prüfe nach dem Laden, wem das Objekt
            gehört. Passt es nicht, gibst du `404 Not Found` zurück. Für komplexere Regeln bietet
            ASP.NET Core die ressourcenbasierte Autorisierung
            (`IAuthorizationService.AuthorizeAsync(user, log, policy)`), die die Prüfung an einer Stelle bündelt.

            Warum die anderen Optionen nicht helfen:
            - **Zufällige IDs** sind schwer zu erraten, sickern aber durch. Wer eine ID kennt, ist deshalb noch nicht berechtigt.
            - **Daten in der Oberfläche ausblenden** schützt nichts, denn Angreifer rufen die API direkt auf.
            - **`[Authorize]` und Rollen** regeln, *wer* den Endpunkt aufrufen darf, aber nicht, *auf welches Objekt* er zugreifen darf.

            Mehr dazu:
            - [OWASP API1:2023 Broken Object Level Authorization](https://owasp.org/API-Security/editions/2023/en/0xa1-broken-object-level-authorization/)
            - [Microsoft Docs: Ressourcenbasierte Autorisierung in ASP.NET Core](https://learn.microsoft.com/de-de/aspnet/core/security/authorization/resourcebased)
            """);
}
