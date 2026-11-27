using Trainer.Web.Core;
using Trainer.Web.Core.Simulation;

namespace Trainer.Web.Missions.PathTraversal;

/// <summary>
/// 3.1 Path traversal: <c>GET /archive/download?file=</c> combines the file name
/// with <c>/data/archive</c> on a virtual filesystem, so <c>../restricted/crew-evaluations.pdf</c>
/// (or an absolute path) escapes the public archive. One filesystem per language.
/// </summary>
public sealed partial class PathTraversalMission(FlagService flags, DemoAuth auth) : IMission
{
    private const string ArchiveRoot = "/data/archive";

    private readonly Lock _gate = new();
    private readonly Dictionary<Language, VirtualFileSystem> _disks = [];

    public string Slug => "path-traversal";
    public string Category => "API";
    public Difficulty Difficulty => Difficulty.Medium;

    public RequestTemplate DefaultRequest => new(
        "GET",
        $"{MissionRegistry.ApiPrefix}{Slug}/archive/download?file=1001.pdf",
        new Dictionary<string, string> { ["Authorization"] = $"Bearer {auth.GetToken(Slug, DemoAuth.Player)}" },
        null);

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
