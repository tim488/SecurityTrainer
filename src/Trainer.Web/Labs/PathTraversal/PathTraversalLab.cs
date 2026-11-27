using Trainer.Web.Core;
using Trainer.Web.Core.Simulation;

namespace Trainer.Web.Labs.PathTraversal;

/// <summary>
/// 3.1 Path traversal: <c>GET /archive/download?file=</c> combines the file name
/// with <c>/data/archive</c> on a virtual filesystem, so <c>../restricted/crew-evaluations.pdf</c>
/// (or an absolute path) escapes the public archive. One filesystem per language.
/// </summary>
public sealed class PathTraversalLab(FlagService flags, DemoAuth auth) : ILab
{
    private const string ArchiveRoot = "/data/archive";

    private readonly Lock _gate = new();
    private readonly Dictionary<Language, VirtualFileSystem> _disks = [];

    public string Slug => "path-traversal";
    public Text Title => new("Jefferies Tube", "Jefferies-Röhre");
    public string Category => "API";
    public Difficulty Difficulty => Difficulty.Medium;

    public LabStory Story => new(
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
            | `GET /api/labs/path-traversal/archive` | Lists the public mission reports |
            | `GET /api/labs/path-traversal/archive/download?file=1001.pdf` | Downloads one report |

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
            | `GET /api/labs/path-traversal/archive` | Listet die öffentlichen Missionsberichte auf |
            | `GET /api/labs/path-traversal/archive/download?file=1001.pdf` | Lädt einen Bericht herunter |

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

    public RequestTemplate DefaultRequest => new(
        "GET",
        $"{LabRegistry.ApiPrefix}{Slug}/archive/download?file=1001.pdf",
        new Dictionary<string, string> { ["Authorization"] = $"Bearer {auth.GetToken(Slug, DemoAuth.Player)}" },
        null);

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

    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        auth.GetToken(Slug, DemoAuth.Player);
        Reset();

        app.MapGet("/archive", (HttpRequest request) =>
        {
            if (auth.FindUser(Slug, request) is null) return Results.Unauthorized();
            var disk = Disk(request);
            return Results.Ok(disk.List(ArchiveRoot).Select(path => new
            {
                File = path[(ArchiveRoot.Length + 1)..],
                Title = FirstLine(disk.ReadAllText(path)!),
            }));
        });

        // VULNERABLE: the file name goes straight into the path.
        app.MapGet("/archive/download", (string? file, HttpRequest request) =>
        {
            if (auth.FindUser(Slug, request) is null) return Results.Unauthorized();
            if (string.IsNullOrEmpty(file)) return Results.BadRequest(new { error = "Query parameter 'file' is required." });

            var path = VirtualFileSystem.Combine(ArchiveRoot, file);
            var content = Disk(request).ReadAllText(path);
            return content is null
                ? Results.NotFound(new { error = $"File not found: {path}" })
                : Results.Text(content, "application/pdf");
        });
    }

    private VirtualFileSystem Disk(HttpRequest request)
    {
        var language = Languages.FromRequest(request);
        lock (_gate) return _disks[language];
    }

    private static string FirstLine(string content) =>
        content.Split('\n').Skip(1).FirstOrDefault(l => l.Length > 0)?.Trim() ?? "";

    public void Reset()
    {
        lock (_gate)
        {
            foreach (var language in Enum.GetValues<Language>())
            {
                var disk = new VirtualFileSystem();
                string Pdf(Text text) => "%PDF-1.7 (simulated)\n" + text[language];

                disk.WriteAllText("/data/archive/1001.pdf", Pdf(new(
                    "Mission report: Farpoint Station\nFirst mission of the Enterprise-D. The station turned out to be a living being. Resolved peacefully.",
                    "Missionsbericht: Farpoint-Station\nErste Mission der Enterprise-D. Die Station stellte sich als Lebewesen heraus. Friedlich gelöst.")));
                disk.WriteAllText("/data/archive/1002.pdf", Pdf(new(
                    "Mission report: Omicron Theta\nRecovered a disassembled android identical to Lt. Cmdr. Data. Reassembled. This was a mistake.",
                    "Missionsbericht: Omicron Theta\nEinen zerlegten Androiden geborgen, der Lt. Cmdr. Data gleicht. Wieder zusammengesetzt. Das war ein Fehler.")));
                disk.WriteAllText("/data/archive/1003.pdf", Pdf(new(
                    "Mission report: Ten Forward karaoke night\nNo casualties. Lt. Worf declined to sing. Lt. Barclay sang twice, by accident.",
                    "Missionsbericht: Karaoke-Abend in Zehn Vorne\nKeine Verluste. Lt. Worf hat das Singen abgelehnt. Lt. Barclay hat zweimal gesungen, aus Versehen.")));
                disk.WriteAllText("/data/archive/1004.pdf", Pdf(new(
                    "Mission report: Computer core refit\nCompleted three days early. Minor issues expected. (Annotation, later: not minor.)",
                    "Missionsbericht: Software-Update des Bordcomputers\nDrei Tage zu früh abgeschlossen. Kleinere Probleme erwartet. (Später ergänzt: nicht klein.)")));
                disk.WriteAllText("/data/restricted/crew-evaluations.pdf", Pdf(new(
                    $"""
                    CONFIDENTIAL. Crew evaluations, senior staff. Captain J.-L. Picard.
                    Cmdr. W. T. Riker: outstanding first officer. Occasionally reckless. Treats the chain of command as a suggestion. Will not sit in a chair normally.
                    Lt. Cmdr. Data: exemplary. Asks to be evaluated more often.
                    Lt. Worf: loyal and capable. Occasionally too direct.
                    Lt. R. Barclay: talented. Needs encouragement, and fewer holodeck simulations of his colleagues.
                    Acting Ens. W. Crusher: exceptional. Do not tell him. He would become insufferable.
                    Archive seal: {flags.GetFlag(Slug)}
                    """,
                    $"""
                    VERTRAULICH. Beurteilungen der Führungsoffiziere. Captain J.-L. Picard.
                    Cmdr. W. T. Riker: hervorragender Erster Offizier. Gelegentlich leichtsinnig. Hält die Befehlskette eher für einen Vorschlag. Setzt sich nicht normal auf einen Stuhl.
                    Lt. Cmdr. Data: vorbildlich. Bittet darum, öfter beurteilt zu werden.
                    Lt. Worf: loyal und fähig. Gelegentlich zu direkt.
                    Lt. R. Barclay: begabt. Braucht Ermutigung und weniger Holodeck-Simulationen seiner Kollegen.
                    Fähnrich auf Probe W. Crusher: außergewöhnlich. Nicht weitersagen, sonst wird er unerträglich.
                    Archivsiegel: {flags.GetFlag(Slug)}
                    """)));
                _disks[language] = disk;
            }
        }
    }
}
