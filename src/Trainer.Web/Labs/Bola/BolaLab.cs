using Trainer.Web.Core;

namespace Trainer.Web.Labs.Bola;

public sealed record PersonalLog(int Id, string OwnerId, string Stardate, Text Title, Text Notes)
{
    public object ToJson(Language language) => new { Id, OwnerId, Stardate, Title = Title[language], Notes = Notes[language] };
}

/// <summary>
/// 1.1 Broken Object Level Authorization (SPEC §6): <c>GET /logs/{id}</c> returns
/// any log to any logged-in user. Riker's log 1003 holds the flag. Any other ID
/// returns the next of ten joke logs from the rest of the crew, in turn.
/// Log text follows the request's <c>Accept-Language</c> (English or German).
/// </summary>
public sealed class BolaLab(FlagService flags, DemoAuth auth) : ILab
{
    private const string Riker = "riker";
    private const int FlagLogId = 1003;

    private readonly Lock _gate = new();
    private List<PersonalLog> _logs = [];
    private int _nextCrewLog;

    public string Slug => "bola";
    public Text Title => new("Someone Else's Personal Log", "Ein fremdes Logbuch");
    public string Category => "API";
    public Difficulty Difficulty => Difficulty.Easy;

    public LabStory Story => new(
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
        ```csharp
        app.MapGet("/logs/{id:int}", async (int id, LogDb db) =>
            await db.Logs.FindAsync(id) is { } log
                ? Results.Ok(log)
                : Results.NotFound())
           .RequireAuthorization();
        ```
        """;

    public Text ScenarioMarkdown => new(
        En: $"""
            The ship's personal log service is a small Web API. Every crew member can list
            their own logs and open one by its ID:

            | Endpoint | What it does |
            |---|---|
            | `GET /api/labs/bola/logs` | Lists the logs of the logged-in user |
            | `GET /api/labs/bola/logs/{"{id}"}` | Returns one log with its full notes |

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
            | `GET /api/labs/bola/logs` | Listet deine Logbücher auf |
            | `GET /api/labs/bola/logs/{"{id}"}` | Gibt ein Logbuch samt Notizen zurück |

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
            new("Correct. The query itself only matches logs owned by the caller, so every request is checked " +
                "against the object it asks for. Returning 404 (not 403) also avoids confirming that log 1003 exists.",
                "Richtig. Die Abfrage sucht nur in den Logbüchern des Aufrufers, also wird bei jeder Anfrage der Besitzer geprüft. Und weil 404 statt 403 zurückkommt, verrät die API nicht einmal, dass Logbuch 1003 existiert."),
            BarclayLine: new(
                "Or w-we just check who owns the log, in every query? That's p-probably too simple, isn't it?",
                "Oder w-wir prüfen einfach bei jeder Abfrage, wem das Logbuch gehört? D-das ist bestimmt zu einfach, oder?")),

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
                "U-und wenn die IDs gar keine Zahlen wären, sondern lange Zufallswerte? D-die errät doch keiner. Oder?")),

        new("hide-in-ui", """
            @* LogList.razor *@
            @foreach (var log in logs.Where(l => l.OwnerId == currentUserId))
            {
                <a href="logs/@log.Id">@log.Title</a>
            }
            """, IsCorrect: false,
            new("The UI is not the security boundary. You never used the UI in this lab: you called the API directly, " +
                "and the API still returns any log to anyone who asks.",
                "Die Oberfläche ist keine Sicherheitsgrenze. Du hast die API in diesem Lab direkt aufgerufen, und die gibt weiterhin jedem jedes Logbuch heraus."),
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
                "Ich k-könnte ein `[Authorize]` draufsetzen, Sir. Dafür ist das doch da, oder? Zum … Autorisieren?")),
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

    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        auth.GetToken(Slug, DemoAuth.Player);
        auth.GetToken(Slug, Riker);
        Reset();

        // Correctly scoped: only the caller's own logs.
        app.MapGet("/logs", (HttpRequest request) =>
        {
            if (auth.FindUser(Slug, request) is not { } user) return Results.Unauthorized();
            var language = Languages.FromRequest(request);
            lock (_gate)
            {
                return Results.Ok(_logs
                    .Where(l => l.OwnerId == user.Name)
                    .Select(l => new { l.Id, l.Stardate, Title = l.Title[language] })
                    .ToList());
            }
        });

        // VULNERABLE: authenticated, but no ownership check.
        app.MapGet("/logs/{id:int}", (int id, HttpRequest request) =>
        {
            if (auth.FindUser(Slug, request) is null) return Results.Unauthorized();
            var language = Languages.FromRequest(request);
            lock (_gate)
            {
                var log = _logs.FirstOrDefault(l => l.Id == id) ?? NextCrewLog(id);
                return Results.Ok(log.ToJson(language));
            }
        });
    }

    /// <summary>Loops through the joke logs: each unknown ID gets the next one. Call inside the lock.</summary>
    private PersonalLog NextCrewLog(int id)
    {
        var (owner, stardate, title, notes) = CrewLogs[_nextCrewLog];
        _nextCrewLog = (_nextCrewLog + 1) % CrewLogs.Length;
        return new PersonalLog(id, owner, stardate, title, notes);
    }

    private static readonly (string Owner, string Stardate, Text Title, Text Notes)[] CrewLogs =
    [
        ("data", "47302.6", new("Humour, attempt 4,211", "Humor, Versuch 4.211"),
            new("I told Commander La Forge a joke about a Ferengi, a Vulcan and a tribble. He did not laugh. I have adjusted my timing by 0.3 seconds and will try again tomorrow.",
                "Ich habe Commander La Forge einen Witz über einen Ferengi, einen Vulkanier und einen Tribble erzählt. Er hat nicht gelacht. Ich habe mein Timing um 0,3 Sekunden angepasst und versuche es morgen erneut.")),
        ("barclay", "47309.1", new("Things I did not say", "Was ich nicht gesagt habe"),
            new("Wanted to tell Commander La Forge that the refit was rushed. Practised it eleven times on the holodeck. In the real meeting I said 'sounds great, sir'. The holodeck Geordi was much more understanding.",
                "Ich wollte Commander La Forge sagen, dass das Update überhastet war, und habe es elfmal auf dem Holodeck geübt. Im echten Meeting habe ich dann „klingt super, Sir“ gesagt. Der Holodeck-Geordi war viel verständnisvoller.")),
        ("worf", "47311.4", new("Complaint", "Beschwerde"),
            new("Someone has replaced my prune juice with ordinary apple juice. Again. This is the third time. I will find them.",
                "Jemand hat meinen Pflaumensaft durch gewöhnlichen Apfelsaft ersetzt. Schon wieder, zum dritten Mal. Ich werde ihn finden.")),
        ("troi", "47312.8", new("Chocolate", "Schokolade"),
            new("My session notes are confidential, so this log is only about chocolate. Today's sundae had four kinds. I sense that I regret nothing.",
                "Meine Sitzungsnotizen sind vertraulich, also geht es in diesem Logbuch nur um Schokolade. Der Eisbecher heute hatte vier Sorten. Ich spüre, dass ich nichts bereue.")),
        ("picard", "47315.0", new("Replicator", "Replikator"),
            new("Ordered: tea, Earl Grey, hot. Received: tea, Earl Grey, lukewarm. Third time this week. Mr. Barclay apologised nine times and fled.",
                "Bestellt: Tee, Earl Grey, heiß. Bekommen: Tee, Earl Grey, lauwarm. Zum dritten Mal diese Woche. Mr. Barclay hat sich neunmal entschuldigt und ist geflohen.")),
        ("geordi", "47316.3", new("Lights on deck 8", "Licht auf Deck 8"),
            new("Since the refit, the lights on deck 8 flicker at 61 hertz. Through my VISOR it looks like a disco. Nobody else can see it, so I'm the only one dancing.",
                "Seit dem Update flackert das Licht auf Deck 8 mit 61 Hertz. Durch mein VISOR sieht das aus wie in einer Disco. Außer mir sieht es keiner, also tanze ich allein.")),
        ("guinan", "47319.7", new("Closing time", "Sperrstunde"),
            new("A man who looked exactly like Data ordered a drink and left a generous tip. Data never tips. I've made a note.",
                "Ein Mann, der genau wie Data aussah, hat einen Drink bestellt und großzügig Trinkgeld gegeben. Data gibt nie Trinkgeld. Ich habe es mir gemerkt.")),
        ("beverly", "47320.1", new("Medical log, unofficial", "Ärztliches Logbuch, inoffiziell"),
            new("Treated Lt. Barclay for refit-related stress. Prescription: sleep, fewer holodeck simulations of his colleagues and no coffee after 22:00. He took notes. In triplicate.",
                "Lt. Barclay wegen Update-Stress behandelt. Verordnung: Schlaf, weniger Holodeck-Simulationen seiner Kollegen und kein Kaffee nach 22 Uhr. Er hat mitgeschrieben, in dreifacher Ausfertigung.")),
        ("obrien", "47321.0", new("Transporter room 3", "Transporterraum 3"),
            new("Commander Riker asked again whether the transporter could 'tidy up' his beard during transport. No. Transporters are not barbers. Filed under 'strange requests'. That folder is getting full.",
                "Commander Riker hat wieder gefragt, ob der Transporter beim Beamen seinen Bart „etwas in Form bringen“ könnte. Nein, Transporter sind keine Friseure. Abgelegt unter „Seltsame Anfragen“. Der Ordner wird langsam voll.")),
        ("spot", "47321.2", new("Feeding schedule (dictated)", "Fütterungsplan (diktiert)"),
            new("Mrrrow. Mrow. Mrrrrrow. [Transcribed from audio. Speaker identified as: Spot.]",
                "Mrrrau. Mrau. Mrrrrrau. [Aus einer Audioaufnahme transkribiert. Erkannter Sprecher: Spot.]")),
    ];

    public void Reset()
    {
        lock (_gate)
        {
            _nextCrewLog = 0;
            _logs =
            [
                new(1001, DemoAuth.Player, "47310.2",
                    new("First week on the refit crew", "Erste Woche im Update-Team"),
                    new("Lt. Barclay let me rewrite the log indexer. He says it's the most important job on the ship. I think he was being nice.",
                        "Lt. Barclay hat mich den Logbuch-Index neu schreiben lassen. Er sagt, das sei der wichtigste Job auf dem Schiff. Ich glaube, er wollte nur nett sein.")),
                new(1002, DemoAuth.Player, "47318.9",
                    new("Nobody listens", "Keiner hört zu"),
                    new("Told Geordi the timings of the new computer software looked off. He said 'later, Wes.' It's always later.",
                        "Ich habe Geordi gesagt, dass mit den Timings der neuen Bordcomputer-Software etwas nicht stimmt. Er meinte nur: „Später, Wes.“ Es ist immer später.")),
                new(FlagLogId, Riker, "47320.5",
                    new("Personal — do not share", "Privat – nicht weitergeben"),
                    new($"Forty minutes of trombone practice. The bridge of 'Night Bird' is finally coming together. Note to self: {flags.GetFlag(Slug)}",
                        $"Vierzig Minuten Posaune geübt. Die Bridge von „Night Bird“ sitzt endlich. Notiz an mich: {flags.GetFlag(Slug)}")),
                new(1004, Riker, "47320.9",
                    new("Poker night", "Pokerabend"),
                    new("Won 300 credits from Geordi and lost 400 to Dr. Crusher. Data folded a royal flush because 'the probability suggested you were not bluffing, Commander.' I was bluffing.",
                        "300 Credits von Geordi gewonnen und 400 an Dr. Crusher verloren. Data hat einen Royal Flush weggelegt, weil „die Wahrscheinlichkeit dafür sprach, dass Sie nicht bluffen, Commander“. Ich habe geblufft.")),
                new(1005, Riker, "47321.1",
                    new("Re: the chair", "Betreff: der Stuhl"),
                    new("Counselor Troi asked again why I swing my leg over the chair instead of sitting down like a normal person. I told her it's tactical. It is not tactical. I just like it.",
                        "Counselor Troi hat wieder gefragt, warum ich das Bein über den Stuhl schwinge, statt mich wie ein normaler Mensch hinzusetzen. Ich habe gesagt, das sei taktisch. Ist es nicht, ich mag es einfach.")),
                new(1006, Riker, "47321.3",
                    new("Beard, day 14", "Bart, Tag 14"),
                    new("Still undecided. The Captain hasn't said a word, which is either approval or the most disapproving silence in Starfleet history. Worf says it makes me look 'almost Klingon'. I'm keeping it.",
                        "Immer noch unentschlossen. Der Captain hat kein Wort gesagt, und das ist entweder Zustimmung oder das missbilligendste Schweigen der Sternenflottengeschichte. Worf meint, ich sehe damit „fast klingonisch“ aus. Der Bart bleibt.")),
                new(1007, Riker, "47321.4",
                    new("Cooking for the senior staff", "Kochabend für die Führungsoffiziere"),
                    new("Made my famous eggs again. Worf ate them and called them 'acceptable'. Everyone else suddenly had urgent duties. Next time: gagh omelette.",
                        "Wieder meine berühmten Eier gemacht. Worf hat sie gegessen und „annehmbar“ genannt. Alle anderen hatten plötzlich dringende Termine. Nächstes Mal: Gagh-Omelett.")),
            ];
        }
    }
}
