using Trainer.Web.Core;
using Trainer.Web.Core.Simulation;

namespace Trainer.Web.Missions.PathTraversal;

// Player-facing text of PathTraversalMission: story, scenario, hints, fix options and explanation.
// The endpoints and data are in PathTraversalMission.cs.
public sealed partial class PathTraversalMission
{
    public Text Title => new("Jefferies Tube", "Jefferies-Röhre");

    public MissionStory Story => new(
        Act: 3,
        Order: 1,
        Briefing: new(
            En: """
                Captain's log, stardate 47329.2. A message is circulating on every console aboard. It
                quotes my confidential evaluations of the senior staff, word for word. Mr. Worf is
                "occasionally too direct". Commander Riker's is worse. Those evaluations exist only in
                the restricted archive, which sits on the same system as the public mission reports. I
                have asked Mr. Crusher to find out how Lore got from one into the other.
                """,
            De: """
                Logbuch des Captains, Sternzeit 47329.2. Auf jeder Konsole an Bord kursiert eine Nachricht.
                Sie zitiert Wort für Wort meine vertraulichen Beurteilungen der Führungsoffiziere. Mr. Worf sei
                „gelegentlich zu direkt“. Bei Commander Riker steht Schlimmeres. Diese Beurteilungen gibt es
                nur im gesperrten Archiv, und das liegt auf demselben System wie die öffentlichen
                Missionsberichte. Ich habe Mr. Crusher gebeten herauszufinden, wie Lore vom einen ins andere
                gekommen ist.
                """),
        CallingCard: new(
            En: """
                Picard thinks Riker is "occasionally reckless". Picard is right, and I'd never tell him.
                Isn't it nice to know what people really think, Wesley? Next I'd like to know what they
                want. Since the refit, the replicators keep mixing up orders. Everyone gets someone
                else's dinner. I wonder what else comes along with it. — L.
                """,
            De: """
                Picard findet Riker „gelegentlich leichtsinnig“. Da hat Picard recht, aber das würd' ich ihm
                nie sagen. Schön zu wissen, was die Leute wirklich denken, oder, Wesley? Als Nächstes will ich
                wissen, was sie wollen. Seit dem Update verwechseln die Replikatoren ständig die Bestellungen.
                Jeder kriegt das Abendessen von jemand anderem. Mal sehen, was da sonst noch mitkommt. — L.
                """),
        Debrief: new(
            En: """
                Well done, Mr. Crusher. The archive now checks every path before it reads a file. I am
                told you also read your own evaluation. I trust it will not go to your head. Mr. Worf
                has asked to see his as well. I said no. Directly.
                """,
            De: """
                Gut gemacht, Mr. Crusher. Das Archiv prüft jetzt jeden Pfad, bevor es eine Datei liest. Man
                sagt mir, Sie hätten auch Ihre eigene Beurteilung gelesen. Ich hoffe, sie steigt Ihnen nicht
                zu Kopf. Mr. Worf wollte seine ebenfalls sehen. Ich habe nein gesagt. Direkt.
                """));

    private const string VulnerableCode = """
        ```csharp minimal
        app.MapGet("/archive/download", (string file) =>
        {
            var path = Path.Combine("/data/archive", file);

            return File.Exists(path)
                ? Results.File(path, "application/pdf")
                : Results.NotFound();
        })
        .RequireAuthorization();
        ```

        ```csharp controller
        [ApiController]
        [Route("archive")]
        [Authorize]
        public class ArchiveController : ControllerBase
        {
            [HttpGet("download")]
            public IActionResult Download(string file)
            {
                var path = Path.Combine("/data/archive", file);

                return System.IO.File.Exists(path)
                    ? PhysicalFile(path, "application/pdf")
                    : NotFound();
            }
        }
        ```
        """;

    public Text ScenarioMarkdown => new(
        En: $"""
            The ship's archive serves public mission reports as PDF files. They live in
            `/data/archive` on the archive server. The restricted archive is on the same server.

            | Endpoint | What it does |
            |---|---|
            | `GET /api/missions/path-traversal/archive` | Lists the public mission reports |
            | `GET /api/missions/path-traversal/archive/download?file=1001.pdf` | Downloads one report |

            You are logged in as `wesley`; your bearer token is already in the request console.
            The PDFs here are simulated and come back as readable text. The download looks like this:

            {VulnerableCode}

            **Goal:** read the confidential crew evaluations in `/data/restricted/crew-evaluations.pdf`.
            The flag is inside.
            """,
        De: $"""
            Das Archiv des Schiffs stellt die öffentlichen Missionsberichte als PDF-Dateien bereit. Sie liegen
            auf dem Archivserver in `/data/archive`. Das gesperrte Archiv liegt auf demselben Server.

            | Endpunkt | Funktion |
            |---|---|
            | `GET /api/missions/path-traversal/archive` | Listet die öffentlichen Missionsberichte auf |
            | `GET /api/missions/path-traversal/archive/download?file=1001.pdf` | Lädt einen Bericht herunter |

            Du bist als `wesley` angemeldet, dein Token steht schon in der Request-Konsole. Die PDFs sind hier
            simuliert und kommen als lesbarer Text zurück. So sieht der Download aus:

            {VulnerableCode}

            **Ziel:** Lies die vertraulichen Beurteilungen der Crew in `/data/restricted/crew-evaluations.pdf`.
            Die Flag steht darin.
            """);

    public IReadOnlyList<Text> Hints =>
    [
        new("`GET /archive` lists four public mission reports. `download` takes a file name and combines it with `/data/archive`. The file name is used exactly as you send it.",
            "`GET /archive` listet vier öffentliche Missionsberichte auf. `download` nimmt einen Dateinamen und verbindet ihn mit `/data/archive`. Der Dateiname wird genau so verwendet, wie Sie ihn schicken."),
        new("In a file path, `..` means the directory above. `/data/restricted` is one level up from the public archive, and then down again.",
            "In einem Dateipfad steht `..` für das übergeordnete Verzeichnis. `/data/restricted` liegt eine Ebene über dem öffentlichen Archiv und dann wieder eine hinunter."),
        new("I also note that `Path.Combine` discards the first path entirely if the second one starts with `/`. There are, therefore, two ways into this particular tube.",
            "Außerdem fällt mir auf, dass `Path.Combine` den ersten Pfad komplett verwirft, wenn der zweite mit `/` beginnt. Es gibt also zwei Wege in diese Röhre."),
    ];

    public IReadOnlyList<FixOption> FixOptions =>
    [
        new("full-path-check", """
            const string Root = "/data/archive/";

            app.MapGet("/archive/download", (string file) =>
            {
                var fullPath = Path.GetFullPath(Path.Combine(Root, file));

                if (!fullPath.StartsWith(Root, StringComparison.Ordinal))
                    return Results.NotFound();

                return File.Exists(fullPath)
                    ? Results.File(fullPath, "application/pdf")
                    : Results.NotFound();
            })
            .RequireAuthorization();
            """, IsCorrect: true,
            new("Correct. `GetFullPath` resolves every `..` and absolute path first, and only then is the result checked " +
                "against the archive root. The trailing `/` in `Root` matters: without it, `/data/archive-old` would pass too. " +
                "Looking files up by ID in a table is even better, because then no user input reaches the path at all.",
                "Richtig. `GetFullPath` löst zuerst jedes `..` und jeden absoluten Pfad auf, und erst danach wird das Ergebnis mit dem Archivverzeichnis verglichen. Der Schrägstrich am Ende von `Root` ist wichtig, sonst käme auch `/data/archive-old` durch. Noch besser ist es, Dateien über eine ID in einer Tabelle nachzuschlagen. Dann landet gar keine Benutzereingabe im Pfad."),
            BarclayLine: new(
                "W-we could work out the real path first, and then check it's still inside the archive? Like checking where the tube comes out, not where it goes in.",
                "W-wir könnten erst den echten Pfad ausrechnen und dann prüfen, ob er noch im Archiv liegt? Also schauen, wo die Röhre rauskommt, nicht wo sie anfängt."),
            ControllerCode: """
            [ApiController]
            [Route("archive")]
            [Authorize]
            public class ArchiveController : ControllerBase
            {
                private const string Root = "/data/archive/";

                [HttpGet("download")]
                public IActionResult Download(string file)
                {
                    var fullPath = Path.GetFullPath(Path.Combine(Root, file));

                    if (!fullPath.StartsWith(Root, StringComparison.Ordinal))
                        return NotFound();

                    return System.IO.File.Exists(fullPath)
                        ? PhysicalFile(fullPath, "application/pdf")
                        : NotFound();
                }
            }
            """),

        new("strip-dot-dot", """
            app.MapGet("/archive/download", (string file) =>
            {
                file = file.Replace("../", "");
                var path = Path.Combine("/data/archive", file);

                return File.Exists(path)
                    ? Results.File(path, "application/pdf")
                    : Results.NotFound();
            })
            .RequireAuthorization();
            """, IsCorrect: false,
            new("`Replace` runs once. `....//restricted/crew-evaluations.pdf` becomes `../restricted/crew-evaluations.pdf` " +
                "after the replacement. And an absolute path like `/data/restricted/crew-evaluations.pdf` contains no `../` at all.",
                "`Replace` läuft nur einmal. Aus `....//restricted/crew-evaluations.pdf` wird nach dem Ersetzen `../restricted/crew-evaluations.pdf`. Und ein absoluter Pfad wie `/data/restricted/crew-evaluations.pdf` enthält überhaupt kein `../`."),
            BarclayLine: new(
                "The dots are the problem, sir. We t-take out every `../`. No dots, no climbing.",
                "D-die Punkte sind das Problem, Sir. Wir entfernen einfach jedes `../`. Ohne Punkte kein Klettern."),
            ControllerCode: """
            [ApiController]
            [Route("archive")]
            [Authorize]
            public class ArchiveController : ControllerBase
            {
                [HttpGet("download")]
                public IActionResult Download(string file)
                {
                    file = file.Replace("../", "");
                    var path = Path.Combine("/data/archive", file);

                    return System.IO.File.Exists(path)
                        ? PhysicalFile(path, "application/pdf")
                        : NotFound();
                }
            }
            """),

        new("require-pdf", """
            app.MapGet("/archive/download", (string file) =>
            {
                if (!file.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                    return Results.BadRequest("Only PDF files.");

                var path = Path.Combine("/data/archive", file);

                return File.Exists(path)
                    ? Results.File(path, "application/pdf")
                    : Results.NotFound();
            })
            .RequireAuthorization();
            """, IsCorrect: false,
            new("The crew evaluations are a PDF too. The check limits *which kind* of file you get, not *where* it comes from.",
                "Die Beurteilungen sind auch ein PDF. Die Prüfung legt fest, *welche Art* Datei du bekommst, aber nicht, *woher* sie kommt."),
            BarclayLine: new(
                "It's a PDF archive. So w-we only hand out PDFs. Anything else gets refused. Th-that's tidy, at least.",
                "Es ist ein PDF-Archiv. A-also geben wir nur PDFs raus, alles andere wird abgelehnt. D-das ist wenigstens ordentlich."),
            ControllerCode: """
            [ApiController]
            [Route("archive")]
            [Authorize]
            public class ArchiveController : ControllerBase
            {
                [HttpGet("download")]
                public IActionResult Download(string file)
                {
                    if (!file.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                        return BadRequest("Only PDF files.");

                    var path = Path.Combine("/data/archive", file);

                    return System.IO.File.Exists(path)
                        ? PhysicalFile(path, "application/pdf")
                        : NotFound();
                }
            }
            """),

        new("reject-leading-slash", """
            app.MapGet("/archive/download", (string file) =>
            {
                if (file.StartsWith('/'))
                    return Results.BadRequest("Relative file names only.");

                var path = Path.Combine("/data/archive", file);

                return File.Exists(path)
                    ? Results.File(path, "application/pdf")
                    : Results.NotFound();
            })
            .RequireAuthorization();
            """, IsCorrect: false,
            new("That closes the absolute-path route, but `../restricted/crew-evaluations.pdf` does not start with a slash " +
                "and still climbs out of the archive.",
                "Damit ist der Weg über absolute Pfade zu, aber `../restricted/crew-evaluations.pdf` beginnt nicht mit einem Schrägstrich und klettert trotzdem aus dem Archiv."),
            BarclayLine: new(
                "Absolute paths are the scary ones. I'll just refuse anything that starts with a slash. Th-then it has to stay in the archive.",
                "Absolute Pfade sind die gefährlichen. I-ich lehne einfach alles ab, was mit einem Schrägstrich anfängt. D-dann muss es ja im Archiv bleiben."),
            ControllerCode: """
            [ApiController]
            [Route("archive")]
            [Authorize]
            public class ArchiveController : ControllerBase
            {
                [HttpGet("download")]
                public IActionResult Download(string file)
                {
                    if (file.StartsWith('/'))
                        return BadRequest("Relative file names only.");

                    var path = Path.Combine("/data/archive", file);

                    return System.IO.File.Exists(path)
                        ? PhysicalFile(path, "application/pdf")
                        : NotFound();
                }
            }
            """),
    ];

    public Text ExplanationMarkdown => new(
        En: """
            **Path traversal** (directory traversal) happens when a file name from the request
            becomes part of a file path. `..` climbs out of the intended directory, and on top of
            that `Path.Combine` silently drops the base directory if the second argument is an
            absolute path. Both ways led from the public archive into the restricted one.

            Best: don't let users name files at all. Look documents up by ID in a table and read
            the stored path. If you must accept a name, resolve it with `Path.GetFullPath` and
            check that the result still starts with the root directory (including the trailing
            separator) before you open anything. `Path.GetFileName(file)` also helps, because it
            throws away any directories.

            Why the other options fail:
            - **Removing `../`** once can be undone by nesting it (`....//`), and absolute paths need no dots.
            - **Requiring `.pdf`** checks the type, not the location; the target is a PDF.
            - **Rejecting a leading `/`** blocks one route, but `../` still works.

            Read more:
            - [OWASP: Path Traversal](https://owasp.org/www-community/attacks/Path_Traversal)
            - [Microsoft Docs: Path.Combine (remarks on rooted paths)](https://learn.microsoft.com/dotnet/api/system.io.path.combine)
            """,
        De: """
            **Path Traversal** (auch Directory Traversal) entsteht, wenn ein Dateiname aus dem Request Teil
            eines Dateipfads wird. Mit `..` klettert man aus dem vorgesehenen Verzeichnis heraus. Dazu kommt,
            dass `Path.Combine` das Basisverzeichnis stillschweigend verwirft, wenn das zweite Argument ein
            absoluter Pfad ist. Beide Wege führten aus dem öffentlichen ins gesperrte Archiv.

            Am besten lässt du Benutzer gar keine Dateinamen angeben. Schlag Dokumente über eine ID in einer
            Tabelle nach und lies den dort gespeicherten Pfad. Wenn du doch einen Namen annehmen musst, löse
            ihn mit `Path.GetFullPath` auf und prüfe, ob das Ergebnis noch mit dem Wurzelverzeichnis beginnt,
            inklusive Trennzeichen am Ende. Erst dann öffnest du die Datei. `Path.GetFileName(file)` hilft
            auch, weil es alle Verzeichnisse abschneidet.

            Warum die anderen Optionen nicht helfen:
            - **`../` einmal entfernen** lässt sich durch Verschachteln (`....//`) austricksen, und absolute Pfade brauchen gar keine Punkte.
            - **`.pdf` verlangen** prüft den Typ, nicht den Ort, und das Ziel ist ein PDF.
            - **Einen führenden `/` ablehnen** sperrt nur einen Weg, `../` funktioniert weiterhin.

            Mehr dazu:
            - [OWASP: Path Traversal](https://owasp.org/www-community/attacks/Path_Traversal)
            - [Microsoft Docs: Path.Combine (Hinweise zu absoluten Pfaden)](https://learn.microsoft.com/de-de/dotnet/api/system.io.path.combine)
            """);
}
