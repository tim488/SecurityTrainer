using Trainer.Web.Core;

namespace Trainer.Web.Missions.Bfla;

public sealed record CrewMember(string Name, Text Rank, Text Position, string Quarters, Text Shift);

/// <summary>
/// 1.2 Broken Function Level Authorization: the <c>/admin</c> group only
/// requires a logged-in user, so <c>GET /admin/crew/export</c> hands the full roster
/// (and the flag) to anyone. <c>GET /menu</c> correctly hides the admin entry from
/// non-admins, which is exactly why nobody noticed.
/// Roster text follows the request's <c>Accept-Language</c> (English or German).
/// </summary>
public sealed partial class BflaMission(FlagService flags, DemoAuth auth) : IMission
{
    private const string Admin = "riker";
    private const string ExportPath = "/admin/crew/export";

    private readonly Lock _gate = new();
    private int _exportCount;

    public string Slug => "bfla";
    public string Category => "API";
    public Difficulty Difficulty => Difficulty.Easy;

    public RequestTemplate DefaultRequest => new(
        "GET",
        $"{MissionRegistry.ApiPrefix}{Slug}/menu",
        new Dictionary<string, string> { ["Authorization"] = $"Bearer {auth.GetToken(Slug, DemoAuth.Player)}" },
        null);

    private bool IsAdmin(DemoUser user) => user.Name == Admin;

    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        auth.GetToken(Slug, DemoAuth.Player);
        auth.GetToken(Slug, Admin);
        Reset();

        app.MapGet("/me", (HttpRequest request) =>
        {
            if (auth.FindUser(Slug, request) is not { } user) return Results.Unauthorized();
            var language = Languages.FromRequest(request);
            var me = Roster.First(c => c.Name == (IsAdmin(user) ? "William T. Riker" : "Wesley Crusher"));
            return Results.Ok(new
            {
                User = user.Name,
                Role = IsAdmin(user) ? "admin" : "crew",
                me.Name,
                Rank = me.Rank[language],
                Position = me.Position[language],
                me.Quarters,
            });
        });

        // Correct: the menu only lists what the role may use.
        app.MapGet("/menu", (HttpRequest request) =>
        {
            if (auth.FindUser(Slug, request) is not { } user) return Results.Unauthorized();
            var language = Languages.FromRequest(request);
            List<object> items = [new { Label = new Text("My profile", "Mein Profil")[language], Href = "/me" }];
            if (IsAdmin(user))
                items.Add(new { Label = new Text("Crew export", "Crew-Export")[language], Href = ExportPath });
            return Results.Ok(items);
        });

        // VULNERABLE: any logged-in user may call it, no role check.
        app.MapGet(ExportPath, (HttpRequest request) =>
        {
            if (auth.FindUser(Slug, request) is not { } user) return Results.Unauthorized();
            var language = Languages.FromRequest(request);
            int exportNumber;
            lock (_gate) exportNumber = ++_exportCount;
            return Results.Ok(new
            {
                Classification = new Text("Senior officers only", "Nur für Führungsoffiziere")[language],
                ExportedBy = user.Name,
                ExportNumber = exportNumber,
                VerificationCode = flags.GetFlag(Slug),
                Crew = Roster.Select(c => new
                {
                    c.Name,
                    Rank = c.Rank[language],
                    Position = c.Position[language],
                    c.Quarters,
                    Shift = c.Shift[language],
                }),
            });
        });
    }

    private static readonly Text Alpha = new("Alpha shift", "Alpha-Schicht");
    private static readonly Text Beta = new("Beta shift", "Beta-Schicht");
    private static readonly Text Gamma = new("Gamma shift", "Gamma-Schicht");

    private static readonly CrewMember[] Roster =
    [
        new("Jean-Luc Picard", new("Captain", "Captain"), new("Commanding officer", "Kommandant"), "Deck 9, 0901", Alpha),
        new("William T. Riker", new("Commander", "Commander"), new("First officer", "Erster Offizier"), "Deck 9, 0912", Alpha),
        new("Data", new("Lieutenant Commander", "Lieutenant Commander"), new("Second officer, operations", "Zweiter Offizier, Einsatzoffizier"), "Deck 2, 0213", new("All shifts (does not sleep)", "Alle Schichten (schläft nicht)")),
        new("Geordi La Forge", new("Lieutenant Commander", "Lieutenant Commander"), new("Chief engineer", "Chefingenieur"), "Deck 4, 0418", Alpha),
        new("Worf", new("Lieutenant", "Lieutenant"), new("Chief of security", "Sicherheitschef"), "Deck 7, 0733", Beta),
        new("Deanna Troi", new("Lieutenant Commander", "Lieutenant Commander"), new("Ship's counselor", "Counselor"), "Deck 8, 0820", Alpha),
        new("Beverly Crusher", new("Commander", "Commander"), new("Chief medical officer", "Leitende Ärztin"), "Deck 3, 0305", Alpha),
        new("Reginald Barclay", new("Lieutenant", "Lieutenant"), new("Systems diagnostic engineer", "Systemingenieur"), "Deck 11, 1104", new("Gamma shift (voluntarily)", "Gamma-Schicht (freiwillig)")),
        new("Miles O'Brien", new("Chief Petty Officer", "Chief Petty Officer"), new("Transporter chief", "Transporterchef"), "Deck 6, 0617", Beta),
        new("Wesley Crusher", new("Acting ensign", "Fähnrich auf Probe"), new("Bridge, conn (supervised)", "Brücke, Steuer (unter Aufsicht)"), "Deck 3, 0306", Gamma),
        new("Guinan", new("—", "—"), new("Ten Forward", "Zehn Vorne"), "Deck 10, 1001", new("Whenever she likes", "Wann es ihr passt")),
        new("Spot", new("—", "—"), new("Cat", "Katze"), "Deck 2, 0213", new("Asleep", "Schläft")),
    ];

    public void Reset()
    {
        lock (_gate) _exportCount = 0;
    }
}
