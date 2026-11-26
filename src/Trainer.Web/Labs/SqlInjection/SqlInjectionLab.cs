using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Trainer.Web.Core;

namespace Trainer.Web.Labs.SqlInjection;

public sealed class CargoItem
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int Quantity { get; set; }
    public string Hold { get; set; } = "";
    public string Description { get; set; } = "";
    public bool IsClassified { get; set; }
}

public sealed class CargoDb(DbContextOptions<CargoDb> options) : DbContext(options)
{
    public DbSet<CargoItem> CargoItems => Set<CargoItem>();
}

/// <summary>
/// 1.3 SQL injection (SPEC §6): <c>GET /cargo?search=</c> concatenates the search term
/// into <c>FromSqlRaw</c>. The classified item's description holds the flag.
/// There is one in-memory SQLite database per language with the same schema, picked
/// by the request's <c>Accept-Language</c>, so the injection works the same in both.
/// </summary>
public sealed class SqlInjectionLab(FlagService flags, DemoAuth auth) : ILab, IDisposable
{
    private const int FlagItemId = 1013;

    private readonly Lock _gate = new();
    private readonly Dictionary<Language, SqliteConnection> _connections = [];

    public string Slug => "sql-injection";
    public Text Title => new("Classified Manifest", "Das geheime Frachtmanifest");
    public string Category => "Injection";
    public Difficulty Difficulty => Difficulty.Medium;

    public LabStory Story => new(
        Act: 1,
        Order: 3,
        Briefing: new(
            En: """
                Captain's log, stardate 47323.6. The roster leak has been contained, but Ten Forward
                has a new favourite subject: a classified prototype in our cargo hold. Nobody can say
                what it is, yet everyone seems to know it is there. The cargo manifest leaves
                classified items out of every crew search. Someone has looked anyway. I have asked
                Mr. Crusher to find out how, and what exactly the crew is talking about.
                """,
            De: """
                Logbuch des Captains, Sternzeit 47323.6. Das Leck in der Crewliste ist gestopft, aber in
                Zehn Vorne gibt es ein neues Lieblingsthema: einen geheimen Prototyp in unserem Frachtraum.
                Keiner weiß, was es ist, aber alle wissen, dass es ihn gibt. Das Frachtmanifest blendet
                geheime Posten bei jeder Suche der Crew aus. Trotzdem hat jemand hineingesehen. Ich habe
                Mr. Crusher gebeten herauszufinden, wie das ging und worüber die Crew eigentlich redet.
                """),
        CallingCard: new(
            En: """
                An emotion chip, Wesley. Father built it for Data. Not for me, of course, never for me.
                I'll collect it later. For now I'm bored of reading about people. I'd rather be one of
                them. Someone with a little more rank, I think. — L.
                """,
            De: """
                Ein Emotionschip, Wesley. Vater hat ihn für Data gebaut. Nicht für mich, natürlich, nie
                für mich. Den hol' ich mir später. Jetzt hab' ich erst mal genug davon, über Leute zu lesen.
                Ich wär' lieber selbst einer von ihnen. Einer mit ein bisschen mehr Rang, schätz' ich. — L.
                """),
        Debrief: new(
            En: """
                Well done, Mr. Crusher. The search now treats your words as words, not as orders. The
                classified item is back under lock, and Mr. Data has asked that it stay there for now.
                He did not say why. I did not ask.
                """,
            De: """
                Gut gemacht, Mr. Crusher. Die Suche behandelt Ihre Eingabe jetzt als Text und nicht mehr
                als Befehl. Der geheime Posten ist wieder unter Verschluss. Mr. Data hat gebeten, dass er
                vorerst dort bleibt. Er hat nicht gesagt, warum, und ich habe nicht gefragt.
                """));

    private const string VulnerableCode = """
        ```csharp
        app.MapGet("/cargo", async (string? search, CargoDb db) =>
        {
            var items = await db.CargoItems
                .FromSqlRaw("SELECT * FROM CargoItems WHERE Name LIKE '%" + search +
                            "%' AND IsClassified = 0")
                .ToListAsync();

            return Results.Ok(items);
        })
        .RequireAuthorization();
        ```
        """;

    public Text ScenarioMarkdown => new(
        En: $"""
            The cargo manifest has a search, so the crew can find out what is stored in which hold.
            Classified items are never part of the results.

            | Endpoint | What it does |
            |---|---|
            | `GET /api/labs/sql-injection/cargo?search=...` | Cargo items whose name contains the search term |

            You are logged in as `wesley`; your bearer token is already in the request console.
            The manifest runs on SQLite, and the search looks like this:

            {VulnerableCode}

            **Goal:** find the classified prototype. The flag is in its `description`.
            """,
        De: $"""
            Das Frachtmanifest hat eine Suche, damit die Crew nachsehen kann, was in welchem Frachtraum
            lagert. Geheime Posten tauchen in den Ergebnissen nie auf.

            | Endpunkt | Funktion |
            |---|---|
            | `GET /api/labs/sql-injection/cargo?search=...` | Frachtposten, deren Name den Suchbegriff enthält |

            Du bist als `wesley` angemeldet, dein Token steht schon in der Request-Konsole. Das Manifest
            läuft auf SQLite, und die Suche sieht so aus:

            {VulnerableCode}

            **Ziel:** Finde den geheimen Prototyp. Die Flag steht in seiner `description`.
            """);

    public IReadOnlyList<Text> Hints =>
    [
        new("A search for `phaser` returns two items. The code places your search term directly inside the SQL text, between two single quotes. I am curious what happens to a search term that contains a single quote of its own.",
            "Eine Suche nach `phaser` liefert zwei Posten. Der Code setzt Ihren Suchbegriff direkt in den SQL-Text, zwischen zwei einfache Anführungszeichen. Mich interessiert, was mit einem Suchbegriff geschieht, der selbst ein einfaches Anführungszeichen enthält."),
        new("A quote in your input ends the string early, and whatever follows is read as SQL. In SQLite, `--` turns the rest of the line into a comment. That would include the condition `IsClassified = 0`.",
            "Ein Anführungszeichen in Ihrer Eingabe beendet den String vorzeitig, und alles danach wird als SQL gelesen. In SQLite macht `--` den Rest der Zeile zu einem Kommentar. Das beträfe auch die Bedingung `IsClassified = 0`."),
        new("A condition that is always true, such as `OR 1=1`, matches every row. The request console encodes spaces in the path for you, so you may type them as they are.",
            "Eine Bedingung, die immer wahr ist, etwa `OR 1=1`, trifft auf jede Zeile zu. Leerzeichen im Pfad codiert die Request-Konsole selbst, Sie können sie also einfach eintippen."),
    ];

    public RequestTemplate DefaultRequest => new(
        "GET",
        $"{LabRegistry.ApiPrefix}{Slug}/cargo?search=phaser",
        new Dictionary<string, string> { ["Authorization"] = $"Bearer {auth.GetToken(Slug, DemoAuth.Player)}" },
        null);

    public IReadOnlyList<FixOption> FixOptions =>
    [
        new("linq-where", """
            app.MapGet("/cargo", async (string? search, CargoDb db) =>
            {
                search ??= "";

                var items = await db.CargoItems
                    .Where(c => c.Name.Contains(search) && !c.IsClassified)
                    .ToListAsync();

                return Results.Ok(items);
            })
            .RequireAuthorization();
            """, IsCorrect: true,
            new("Correct. EF Core turns the LINQ query into SQL with a parameter, so the search term is always sent " +
                "as data and can never change the query. If you need raw SQL, `FromSql($\"... {search} ...\")` does the same.",
                "Richtig. EF Core macht aus der LINQ-Abfrage SQL mit einem Parameter. Der Suchbegriff wird also immer als Daten geschickt und kann die Abfrage nie verändern. Wenn du doch rohes SQL brauchst, macht `FromSql($\"... {search} ...\")` dasselbe."),
            BarclayLine: new(
                "W-we could just use LINQ, like everywhere else? Then there's no SQL string to b-break. Unless that's cheating.",
                "W-wir könnten einfach LINQ nehmen, wie überall sonst? Dann gibt's keinen SQL-String, den man k-kaputt machen kann. Oder ist das geschummelt?")),

        new("escape-quotes", """
            app.MapGet("/cargo", async (string? search, CargoDb db) =>
            {
                var safe = (search ?? "").Replace("'", "''");

                var items = await db.CargoItems
                    .FromSqlRaw("SELECT * FROM CargoItems WHERE Name LIKE '%" + safe +
                                "%' AND IsClassified = 0")
                    .ToListAsync();

                return Results.Ok(items);
            })
            .RequireAuthorization();
            """, IsCorrect: false,
            new("It stops this exact payload, but escaping by hand is fragile. It only helps inside quoted strings " +
                "(not for numbers or column names), the rules differ between databases, and it has to be remembered " +
                "at every call site. Parameters separate code and data for good.",
                "Das stoppt genau diesen Angriff, aber Escapen von Hand ist fehleranfällig. Es hilft nur innerhalb von Strings in Anführungszeichen, nicht bei Zahlen oder Spaltennamen. Außerdem sind die Regeln in jeder Datenbank etwas anders, und du musst bei jeder einzelnen Abfrage daran denken. Parameter trennen Code und Daten dagegen zuverlässig."),
            BarclayLine: new(
                "If the quote is the problem, w-we double it. Two quotes. SQL likes that. I-I read it somewhere.",
                "Wenn das Anführungszeichen das Problem ist, d-dann verdoppeln wir es eben. Zwei Anführungszeichen, das mag SQL. Hab' ich irgendwo gelesen.")),

        new("keyword-blocklist", """
            string[] blocked = ["--", ";", "OR", "/*"];

            app.MapGet("/cargo", async (string? search, CargoDb db) =>
            {
                if (blocked.Any(b => (search ?? "").Contains(b, StringComparison.OrdinalIgnoreCase)))
                    return Results.BadRequest("Invalid search.");

                var items = await db.CargoItems
                    .FromSqlRaw("SELECT * FROM CargoItems WHERE Name LIKE '%" + search +
                                "%' AND IsClassified = 0")
                    .ToListAsync();

                return Results.Ok(items);
            })
            .RequireAuthorization();
            """, IsCorrect: false,
            new("Blocklists are always one trick behind. A `UNION SELECT` needs none of these keywords, and neither does " +
                "closing the quote on purpose at the end. Meanwhile an honest search for \"dilithium ore\" is rejected, because it contains `OR`.",
                "Eine Sperrliste ist immer einen Trick zu spät dran. Ein `UNION SELECT` braucht keines dieser Wörter, und ein Anführungszeichen, das du am Ende absichtlich wieder schließt, auch nicht. Dafür wird eine ganz normale Suche nach „Photonentorpedos“ abgelehnt, weil darin `or` steckt."),
            BarclayLine: new(
                "We could block the dangerous bits. `--`, `OR`, semicolons. N-nobody needs those in a cargo search. Right?",
                "W-wir könnten die gefährlichen Teile sperren: `--`, `OR`, Semikolons. Die braucht doch k-keiner für eine Frachtsuche. Oder?")),

        new("string-format", """
            app.MapGet("/cargo", async (string? search, CargoDb db) =>
            {
                var sql = string.Format(
                    "SELECT * FROM CargoItems WHERE Name LIKE '%{0}%' AND IsClassified = 0", search);

                var items = await db.CargoItems.FromSqlRaw(sql).ToListAsync();

                return Results.Ok(items);
            })
            .RequireAuthorization();
            """, IsCorrect: false,
            new("`string.Format` is still string concatenation, just tidier. The search term still ends up inside the SQL " +
                "text, so the same payload works unchanged.",
                "`string.Format` ist immer noch String-Verkettung, nur ordentlicher aufgeschrieben. Der Suchbegriff landet weiterhin im SQL-Text, und derselbe Angriff funktioniert unverändert."),
            BarclayLine: new(
                "What if we build the query with `string.Format` instead of plus signs? It l-looks a lot more professional.",
                "Und wenn wir die Abfrage mit `string.Format` bauen statt mit Pluszeichen? D-das sieht viel professioneller aus.")),
    ];

    public Text ExplanationMarkdown => new(
        En: """
            **SQL injection** happens when user input becomes part of the SQL text. Your quote
            closed the string literal, `OR 1=1` made the condition true for every row, and `--`
            commented out the check for classified items. The database could not tell the
            developer's SQL from yours, because it was all one string.

            The fix is to never build SQL from input. Let EF Core build it from LINQ, or use
            `FromSql` with an interpolated string, which EF Core turns into parameters
            (unlike `FromSqlRaw` with `+`). Parameters are sent separately from the query, so
            they are always data. The same goes for Dapper and plain ADO.NET: use
            `@search` parameters, never concatenation.

            Why the other options fail:
            - **Doubling quotes** works for one context in one database, and only where nobody forgets it.
            - **Blocking keywords** misses other syntax (`UNION`, a closing quote) and breaks real searches.
            - **`string.Format`** is concatenation with nicer syntax.

            Read more:
            - [OWASP: SQL Injection](https://owasp.org/www-community/attacks/SQL_Injection)
            - [Microsoft Docs: SQL queries in EF Core](https://learn.microsoft.com/ef/core/querying/sql-queries)
            """,
        De: """
            **SQL-Injection** entsteht, wenn Benutzereingaben Teil des SQL-Texts werden. Dein
            Anführungszeichen hat den String beendet, `OR 1=1` hat die Bedingung für jede Zeile wahr
            gemacht, und `--` hat die Prüfung auf geheime Posten auskommentiert. Die Datenbank konnte
            das SQL des Entwicklers nicht von deinem unterscheiden, weil alles ein einziger String war.

            Die Lösung: Baue SQL nie aus Eingaben zusammen. Lass EF Core das SQL aus LINQ erzeugen, oder
            nimm `FromSql` mit einem interpolierten String. Daraus macht EF Core Parameter, anders als bei
            `FromSqlRaw` mit `+`. Parameter werden getrennt von der Abfrage geschickt und sind deshalb
            immer Daten. Für Dapper und reines ADO.NET gilt dasselbe: Nimm `@search`-Parameter und
            verkette nie Strings.

            Warum die anderen Optionen nicht helfen:
            - **Anführungszeichen verdoppeln** klappt in einem Kontext und einer Datenbank, und nur da, wo es keiner vergisst.
            - **Schlüsselwörter sperren** übersieht andere Syntax (`UNION`, ein schließendes Anführungszeichen) und blockiert echte Suchen.
            - **`string.Format`** ist Verkettung mit hübscherer Syntax.

            Mehr dazu:
            - [OWASP: SQL Injection](https://owasp.org/www-community/attacks/SQL_Injection)
            - [Microsoft Docs: SQL-Abfragen in EF Core](https://learn.microsoft.com/de-de/ef/core/querying/sql-queries)
            """);

    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        auth.GetToken(Slug, DemoAuth.Player);
        Reset();

        // VULNERABLE: the search term is concatenated into raw SQL.
        app.MapGet("/cargo", (string? search, HttpRequest request) =>
        {
            if (auth.FindUser(Slug, request) is null) return Results.Unauthorized();
            var language = Languages.FromRequest(request);
            lock (_gate)
            {
                using var db = CreateDb(language);
                try
                {
                    // Not composed any further (no Select/Where after it), so EF Core sends the SQL exactly as built.
                    var items = db.CargoItems
                        .FromSqlRaw("SELECT * FROM CargoItems WHERE Name LIKE '%" + search + "%' AND IsClassified = 0")
                        .AsNoTracking()
                        .ToList();
                    return Results.Ok(items.Select(c => new { c.Id, c.Name, c.Quantity, c.Hold, c.Description }));
                }
                catch (Exception ex) when (ex is DbException or InvalidOperationException)
                {
                    // Verbose on purpose: a real dev environment would show this too, and it helps the player.
                    return Results.Json(new { error = ex.Message }, statusCode: StatusCodes.Status500InternalServerError);
                }
            }
        });
    }

    private CargoDb CreateDb(Language language) =>
        new(new DbContextOptionsBuilder<CargoDb>().UseSqlite(_connections[language]).Options);

    public void Reset()
    {
        lock (_gate)
        {
            foreach (var connection in _connections.Values) connection.Dispose();
            _connections.Clear();

            foreach (var language in Enum.GetValues<Language>())
            {
                var connection = new SqliteConnection("Data Source=:memory:");
                connection.Open();
                _connections[language] = connection;

                using var db = CreateDb(language);
                db.Database.EnsureCreated();
                db.CargoItems.AddRange(Seed(language));
                db.SaveChanges();
            }
        }
    }

    public void Dispose()
    {
        lock (_gate)
        {
            foreach (var connection in _connections.Values) connection.Dispose();
            _connections.Clear();
        }
    }

    private IEnumerable<CargoItem> Seed(Language language)
    {
        CargoItem Item(int id, Text name, int quantity, string hold, Text description, bool classified = false) =>
            new() { Id = id, Name = name[language], Quantity = quantity, Hold = hold, Description = description[language], IsClassified = classified };

        return
        [
            Item(1001, new("Phaser rifles, type 3", "Phasergewehre, Typ 3"), 40, "Cargo bay 2",
                new("Standard issue. Mr. Worf counts them every morning.", "Standardausrüstung. Mr. Worf zählt sie jeden Morgen nach.")),
            Item(1002, new("Phaser power cells", "Phaser-Energiezellen"), 600, "Cargo bay 2",
                new("Rechargeable. Please return empty cells to security.", "Wiederaufladbar. Leere Zellen bitte bei der Sicherheit abgeben.")),
            Item(1003, new("Dilithium ore", "Dilithium-Erz"), 12, "Cargo bay 1",
                new("Unrefined. Do not lick. (Ensign Ro, this means you.)", "Unraffiniert. Nicht ablecken. (Fähnrich Ro, das gilt auch für Sie.)")),
            Item(1004, new("Medical supplies, general", "Medizinischer Bedarf, allgemein"), 85, "Cargo bay 3",
                new("Hyposprays, dermal regenerators, bandages.", "Injektoren, Hautregeneratoren, Verbandszeug.")),
            Item(1005, new("Self-sealing stem bolts", "Selbstdichtende Schaftbolzen"), 5000, "Cargo bay 4",
                new("Nobody remembers ordering these. Nobody knows what they are for.", "Keiner erinnert sich, sie bestellt zu haben. Keiner weiß, wofür sie sind.")),
            Item(1006, new("Trombone reeds and valve oil", "Posaunen-Ventilöl und Zubehör"), 3, "Cargo bay 3",
                new("Private shipment for Cmdr. Riker. Fragile, apparently.", "Private Lieferung für Cmdr. Riker. Angeblich zerbrechlich.")),
            Item(1007, new("Klingon opera recordings", "Klingonische Opernaufnahmen"), 1, "Cargo bay 3",
                new("Property of Lt. Worf. Do not play near the medical supplies.", "Eigentum von Lt. Worf. Nicht in der Nähe der Medikamente abspielen.")),
            Item(1008, new("Chocolate, assorted", "Schokolade, gemischt"), 200, "Cargo bay 3",
                new("Requested by Counselor Troi \"for therapeutic purposes\".", "Von Counselor Troi angefordert, „zu therapeutischen Zwecken“.")),
            Item(1009, new("Cat food, 40 varieties", "Katzenfutter, 40 Sorten"), 40, "Cargo bay 3",
                new("For Spot. Lt. Cmdr. Data is still testing which one she likes.", "Für Spot. Lt. Cmdr. Data testet noch, welche Sorte sie mag.")),
            Item(1010, new("Holodeck emitter, spare", "Holodeck-Emitter, Ersatzteil"), 6, "Cargo bay 4",
                new("Lt. Barclay asked for six. Lt. Cmdr. La Forge asked why.", "Lt. Barclay hat sechs angefordert. Lt. Cmdr. La Forge hat gefragt, wozu.")),
            Item(1011, new("Isolinear chips", "Isolineare Chips"), 900, "Cargo bay 1",
                new("Left over from the computer refit. Some of them blink.", "Übrig vom Software-Update. Ein paar davon blinken.")),
            Item(1014, new("Photon torpedo casings", "Photonentorpedos, leere Hüllen"), 25, "Cargo bay 2",
                new("Empty. Mr. Worf would like that noted as a complaint.", "Leer. Mr. Worf möchte das als Beschwerde vermerkt wissen.")),
            Item(1012, new("Tea, Earl Grey, reserve", "Tee, Earl Grey, Reserve"), 1, "Cargo bay 1",
                new("Real leaves, not replicated. For the Captain, in case the replicator fails again.", "Echte Blätter, nicht repliziert. Für den Captain, falls der Replikator wieder versagt."), classified: true),
            Item(FlagItemId, new("Prototype, Soong laboratory", "Prototyp aus dem Soong-Labor"), 1, "Cargo bay 1, vault",
                new($"Positronic emotion chip. Built by Dr. Noonien Soong for Lt. Cmdr. Data. Handle with care. Vault code: {flags.GetFlag(Slug)}",
                    $"Positronischer Emotionschip, von Dr. Noonien Soong für Lt. Cmdr. Data gebaut. Vorsichtig behandeln. Tresorcode: {flags.GetFlag(Slug)}"),
                classified: true),
        ];
    }
}
