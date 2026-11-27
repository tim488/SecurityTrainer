using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Trainer.Web.Core;

namespace Trainer.Web.Missions.SqlInjection;

// Player-facing text of SqlInjectionMission: story, scenario, hints, fix options and explanation.
// The endpoints and data are in SqlInjectionMission.cs.
public sealed partial class SqlInjectionMission
{
    public Text Title => new("Classified Manifest", "Das geheime Frachtmanifest");

    public MissionStory Story => new(
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
        ```csharp minimal
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

        ```csharp controller
        [ApiController]
        [Route("cargo")]
        [Authorize]
        public class CargoController(CargoDb db) : ControllerBase
        {
            [HttpGet]
            public async Task<IActionResult> Search(string? search)
            {
                var items = await db.CargoItems
                    .FromSqlRaw("SELECT * FROM CargoItems WHERE Name LIKE '%" + search +
                                "%' AND IsClassified = 0")
                    .ToListAsync();

                return Ok(items);
            }
        }
        ```
        """;

    public Text ScenarioMarkdown => new(
        En: $"""
            The cargo manifest has a search, so the crew can find out what is stored in which hold.
            Classified items are never part of the results.

            | Endpoint | What it does |
            |---|---|
            | `GET /api/missions/sql-injection/cargo?search=...` | Cargo items whose name contains the search term |

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
            | `GET /api/missions/sql-injection/cargo?search=...` | Frachtposten, deren Name den Suchbegriff enthält |

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
                "W-wir könnten einfach LINQ nehmen, wie überall sonst? Dann gibt's keinen SQL-String, den man k-kaputt machen kann. Oder ist das geschummelt?"),
            ControllerCode: """
            [ApiController]
            [Route("cargo")]
            [Authorize]
            public class CargoController(CargoDb db) : ControllerBase
            {
                [HttpGet]
                public async Task<IActionResult> Search(string? search)
                {
                    search ??= "";

                    var items = await db.CargoItems
                        .Where(c => c.Name.Contains(search) && !c.IsClassified)
                        .ToListAsync();

                    return Ok(items);
                }
            }
            """),

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
                "Wenn das Anführungszeichen das Problem ist, d-dann verdoppeln wir es eben. Zwei Anführungszeichen, das mag SQL. Hab' ich irgendwo gelesen."),
            ControllerCode: """
            [ApiController]
            [Route("cargo")]
            [Authorize]
            public class CargoController(CargoDb db) : ControllerBase
            {
                [HttpGet]
                public async Task<IActionResult> Search(string? search)
                {
                    var safe = (search ?? "").Replace("'", "''");

                    var items = await db.CargoItems
                        .FromSqlRaw("SELECT * FROM CargoItems WHERE Name LIKE '%" + safe +
                                    "%' AND IsClassified = 0")
                        .ToListAsync();

                    return Ok(items);
                }
            }
            """),

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
                "W-wir könnten die gefährlichen Teile sperren: `--`, `OR`, Semikolons. Die braucht doch k-keiner für eine Frachtsuche. Oder?"),
            ControllerCode: """
            [ApiController]
            [Route("cargo")]
            [Authorize]
            public class CargoController(CargoDb db) : ControllerBase
            {
                private static readonly string[] Blocked = ["--", ";", "OR", "/*"];

                [HttpGet]
                public async Task<IActionResult> Search(string? search)
                {
                    if (Blocked.Any(b => (search ?? "").Contains(b, StringComparison.OrdinalIgnoreCase)))
                        return BadRequest("Invalid search.");

                    var items = await db.CargoItems
                        .FromSqlRaw("SELECT * FROM CargoItems WHERE Name LIKE '%" + search +
                                    "%' AND IsClassified = 0")
                        .ToListAsync();

                    return Ok(items);
                }
            }
            """),

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
                "Und wenn wir die Abfrage mit `string.Format` bauen statt mit Pluszeichen? D-das sieht viel professioneller aus."),
            ControllerCode: """
            [ApiController]
            [Route("cargo")]
            [Authorize]
            public class CargoController(CargoDb db) : ControllerBase
            {
                [HttpGet]
                public async Task<IActionResult> Search(string? search)
                {
                    var sql = string.Format(
                        "SELECT * FROM CargoItems WHERE Name LIKE '%{0}%' AND IsClassified = 0", search);

                    var items = await db.CargoItems.FromSqlRaw(sql).ToListAsync();

                    return Ok(items);
                }
            }
            """),
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

            One more thing helped you: the API returned the raw database error. That is handy
            while you learn, but a real app should log the details and send the caller only a
            generic error. Otherwise every typo in an attack tells the attacker what the query
            looks like.

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

            Geholfen hat dir außerdem, dass die API den rohen Datenbankfehler zurückgegeben hat. Beim Lernen
            ist das praktisch. Eine echte App sollte die Details aber nur ins Log schreiben und dem Aufrufer
            einen allgemeinen Fehler schicken. Sonst verrät jeder Fehlversuch dem Angreifer, wie die Abfrage
            aussieht.

            Warum die anderen Optionen nicht helfen:
            - **Anführungszeichen verdoppeln** klappt in einem Kontext und einer Datenbank, und nur da, wo es keiner vergisst.
            - **Schlüsselwörter sperren** übersieht andere Syntax (`UNION`, ein schließendes Anführungszeichen) und blockiert echte Suchen.
            - **`string.Format`** ist Verkettung mit hübscherer Syntax.

            Mehr dazu:
            - [OWASP: SQL Injection](https://owasp.org/www-community/attacks/SQL_Injection)
            - [Microsoft Docs: SQL-Abfragen in EF Core](https://learn.microsoft.com/de-de/ef/core/querying/sql-queries)
            """);
}
