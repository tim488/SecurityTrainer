using Trainer.Web.Core;

namespace Trainer.Web.Labs.Bfla;

public sealed record CrewMember(string Name, Text Rank, Text Position, string Quarters, Text Shift);

/// <summary>
/// 1.2 Broken Function Level Authorization: the <c>/admin</c> group only
/// requires a logged-in user, so <c>GET /admin/crew/export</c> hands the full roster
/// (and the flag) to anyone. <c>GET /menu</c> correctly hides the admin entry from
/// non-admins, which is exactly why nobody noticed.
/// Roster text follows the request's <c>Accept-Language</c> (English or German).
/// </summary>
public sealed class BflaLab(FlagService flags, DemoAuth auth) : ILab
{
    private const string Admin = "riker";
    private const string ExportPath = "/admin/crew/export";

    private readonly Lock _gate = new();
    private int _exportCount;

    public string Slug => "bfla";
    public Text Title => new("Bridge Access", "Zutritt zur Brücke");
    public string Category => "API";
    public Difficulty Difficulty => Difficulty.Easy;

    public LabStory Story => new(
        Act: 1,
        Order: 2,
        Briefing: new(
            En: """
                Captain's log, stardate 47322.1. At 0300 hours the complete crew roster went out
                on all subspace frequencies: names, quarters, duty shifts. The signal came from
                inside the ship, and nobody sent it. Only senior officers can export the roster,
                and their consoles were idle all night. Mr. Barclay assures me that nobody else
                even sees the admin menu. I have asked Mr. Crusher to find out how it happened
                anyway.
                """,
            De: """
                Logbuch des Captains, Sternzeit 47322.1. Um drei Uhr nachts ging die komplette Crewliste
                über alle Subraumfrequenzen hinaus, mit Namen, Quartieren und Dienstzeiten. Das Signal kam
                von Bord, aber abgeschickt hat es niemand. Exportieren können die Liste nur die
                Führungsoffiziere, und deren Konsolen waren die ganze Nacht unbenutzt. Mr. Barclay versichert
                mir, dass alle anderen das Admin-Menü gar nicht erst zu sehen bekommen. Ich habe Mr. Crusher
                gebeten herauszufinden, wie es trotzdem passieren konnte.
                """),
        CallingCard: new(
            En: """
                Wesley! Found the roster too? Then you've seen how they list you: "acting ensign".
                Acting. That must sting. I know where everyone sleeps now. Next I'd like to know
                what this ship is carrying. There's a cargo manifest with a few lines nobody is
                supposed to read. — L.
                """,
            De: """
                Wesley! Hast du die Liste auch gefunden? Dann weißt du ja, was da bei dir steht:
                „Fähnrich auf Probe“. Auf Probe, das tut weh, oder? Ich weiß jetzt, wo alle schlafen.
                Als Nächstes will ich wissen, was das Schiff so geladen hat. Im Frachtmanifest gibt's
                ein paar Zeilen, die keiner lesen soll. — L.
                """),
        Debrief: new(
            En: """
                Good work, Mr. Crusher. The export now asks for a senior officer's role, not just a
                working login. Mr. Barclay wanted to keep the hidden menu as well. I allowed it, on
                the understanding that it is decoration. The roster is still out there, I am afraid.
                So is our visitor.
                """,
            De: """
                Gute Arbeit, Mr. Crusher. Der Export verlangt jetzt die Rolle eines Führungsoffiziers und
                nicht bloß eine gültige Anmeldung. Mr. Barclay wollte das versteckte Menü trotzdem behalten.
                Ich habe es erlaubt, aber nur als Dekoration. Die Crewliste ist leider schon draußen.
                Unser Besucher auch.
                """));

    private const string VulnerableCode = """
        ```csharp minimal
        app.MapGet("/menu", (ClaimsPrincipal user) =>
        {
            List<MenuItem> items = [new("My profile", "/me")];
            if (user.IsInRole("admin"))
                items.Add(new("Crew export", "/admin/crew/export"));
            return items;
        })
        .RequireAuthorization();

        var admin = app.MapGroup("/admin")
            .RequireAuthorization();   // senior officers only

        admin.MapGet("/crew/export", (CrewDb db) => Results.Ok(db.ExportRoster()));
        ```
        ```csharp controller
        [ApiController]
        [Route("menu")]
        [Authorize]
        public class MenuController : ControllerBase
        {
            [HttpGet]
            public List<MenuItem> Get()
            {
                List<MenuItem> items = [new("My profile", "/me")];
                if (User.IsInRole("admin"))
                    items.Add(new("Crew export", "/admin/crew/export"));
                return items;
            }
        }

        [ApiController]
        [Route("admin")]
        [Authorize]   // senior officers only
        public class AdminController(CrewDb db) : ControllerBase
        {
            [HttpGet("crew/export")]
            public IActionResult ExportCrew() => Ok(db.ExportRoster());
        }
        ```
        """;

    public Text ScenarioMarkdown => new(
        En: $"""
            The crew console is a Blazor app on top of a small Web API. The app builds its
            navigation from `GET /menu`, so every crew member only sees the entries for their role.
            Senior officers get an extra entry that exports the full crew roster.

            | Endpoint | What it does |
            |---|---|
            | `GET /api/labs/bfla/me` | Your own profile |
            | `GET /api/labs/bfla/menu` | The menu entries for your role |
            | `GET /api/labs/bfla/admin/crew/export` | The full crew roster (senior officers) |

            You are logged in as `wesley`, an ordinary crew member; your bearer token is already
            in the request console. Commander Riker is an admin.

            This is how the menu and the admin endpoints are set up:

            {VulnerableCode}

            **Goal:** export the crew roster without being an admin. The flag is the export's
            `verificationCode`.
            """,
        De: $"""
            Die Crew-Konsole ist eine Blazor-App mit einer kleinen Web-API dahinter. Die App baut ihr
            Menü aus `GET /menu` auf, also sieht jedes Crewmitglied nur die Einträge für seine Rolle.
            Führungsoffiziere bekommen einen zusätzlichen Eintrag, mit dem sie die komplette Crewliste
            exportieren können.

            | Endpunkt | Funktion |
            |---|---|
            | `GET /api/labs/bfla/me` | Dein eigenes Profil |
            | `GET /api/labs/bfla/menu` | Die Menüeinträge für deine Rolle |
            | `GET /api/labs/bfla/admin/crew/export` | Die komplette Crewliste (Führungsoffiziere) |

            Du bist als `wesley` angemeldet, ein ganz normales Crewmitglied. Dein Token steht schon in
            der Request-Konsole. Commander Riker ist Admin.

            So sind das Menü und die Admin-Endpunkte eingerichtet:

            {VulnerableCode}

            **Ziel:** Exportiere die Crewliste, ohne Admin zu sein. Die Flag ist der
            `verificationCode` des Exports.
            """);

    public IReadOnlyList<Text> Hints =>
    [
        new("`GET /menu` gives you a single entry. According to the code, the admin entry is simply left out for your role. Leaving something out of a list does not remove it from the ship.",
            "`GET /menu` liefert Ihnen nur einen Eintrag. Laut Code wird der Admin-Eintrag für Ihre Rolle einfach weggelassen. Was in einer Liste fehlt, ist deshalb aber nicht vom Schiff verschwunden."),
        new("The `/admin` endpoints require authorization (`RequireAuthorization()` / `[Authorize]`) without naming a policy or a role. In that form it only requires that the caller is logged in. You are logged in.",
            "Die `/admin`-Endpunkte verlangen Autorisierung (`RequireAuthorization()` / `[Authorize]`), ohne eine Policy oder Rolle zu nennen. In dieser Form heißt das nur, dass der Aufrufer angemeldet sein muss. Sie sind angemeldet."),
        new("I would expect `GET /api/labs/bfla/admin/crew/export` with your own token to be refused. I would also have expected the roster not to be broadcast at 0300 hours.",
            "Ich würde erwarten, dass `GET /api/labs/bfla/admin/crew/export` mit Ihrem eigenen Token abgelehnt wird. Ich hätte allerdings auch erwartet, dass die Crewliste nicht um drei Uhr nachts gesendet wird."),
    ];

    public RequestTemplate DefaultRequest => new(
        "GET",
        $"{LabRegistry.ApiPrefix}{Slug}/menu",
        new Dictionary<string, string> { ["Authorization"] = $"Bearer {auth.GetToken(Slug, DemoAuth.Player)}" },
        null);

    public IReadOnlyList<FixOption> FixOptions =>
    [
        new("admin-policy", """
            builder.Services.AddAuthorizationBuilder()
                .AddPolicy("Admin", p => p.RequireRole("admin"));

            var admin = app.MapGroup("/admin")
                .RequireAuthorization("Admin");

            admin.MapGet("/crew/export", (CrewDb db) => Results.Ok(db.ExportRoster()));
            """, IsCorrect: true,
            new("Correct. The role check now runs on the server for every admin endpoint, " +
                "including ones added later. A logged-in crew member without the admin role gets 403 Forbidden.",
                "Richtig. Die Rolle wird jetzt auf dem Server geprüft, und zwar für jeden Admin-Endpunkt, auch für später hinzugefügte. Ein angemeldetes Crewmitglied ohne Admin-Rolle bekommt 403 Forbidden."),
            BarclayLine: new(
                "M-maybe the server should check the role itself? For all the admin endpoints at once? I-if that isn't overkill.",
                "V-vielleicht sollte der Server die Rolle selbst prüfen? Für alle Admin-Endpunkte auf einmal? W-wenn das nicht übertrieben ist."),
            ControllerCode: """
            builder.Services.AddAuthorizationBuilder()
                .AddPolicy("Admin", p => p.RequireRole("admin"));

            [ApiController]
            [Route("admin")]
            [Authorize(Policy = "Admin")]
            public class AdminController(CrewDb db) : ControllerBase
            {
                [HttpGet("crew/export")]
                public IActionResult ExportCrew() => Ok(db.ExportRoster());
            }
            """),

        new("lock-admin-page", """
            @* Pages/Admin/CrewExport.razor *@
            @page "/admin/crew"
            @attribute [Authorize(Roles = "admin")]

            <h3>Crew export</h3>
            <button @onclick="ExportAsync">Export roster</button>
            """, IsCorrect: false,
            new("This protects the Blazor page, not the API behind it. The export endpoint is a separate HTTP " +
                "endpoint with its own (missing) check, and you called it directly without ever opening the page.",
                "Das schützt die Blazor-Seite, aber nicht die API dahinter. Der Export ist ein eigener HTTP-Endpunkt mit einer eigenen, fehlenden Prüfung. Du hast ihn direkt aufgerufen, ohne die Seite je zu öffnen."),
            BarclayLine: new(
                "The menu is already hidden, sir. I c-could lock the admin page as well? With an attribute? Then n-nobody gets in.",
                "Das Menü ist ja schon versteckt, Sir. I-ich könnte zusätzlich die Admin-Seite absperren, mit einem Attribut? D-dann kommt keiner mehr rein."),
            Language: "razor"),

        new("secret-route", """
            var admin = app.MapGroup("/admin-7f3a9c-do-not-use")
                .RequireAuthorization();

            admin.MapGet("/crew/export", (CrewDb db) => Results.Ok(db.ExportRoster()));
            """, IsCorrect: false,
            new("Obscurity, not authorization. The route still ends up in the menu response, the Blazor code, " +
                "browser history and proxy logs. Whoever finds it is still let in with any valid login.",
                "Das ist Verschleierung, keine Autorisierung. Die Route steht trotzdem in der Menü-Antwort, im Blazor-Code, im Browserverlauf und in Proxy-Logs. Wer sie findet, kommt mit jeder gültigen Anmeldung weiterhin rein."),
            BarclayLine: new(
                "W-we could rename the route. Something long, like `/admin-7f3a9c-do-not-use`. Nobody would ever type that. W-would they?",
                "W-wir könnten die Route umbenennen. Irgendwas Langes, so wie `/admin-7f3a9c-do-not-use`. D-das tippt doch keiner ein. Oder?"),
            ControllerCode: """
            [ApiController]
            [Route("admin-7f3a9c-do-not-use")]
            [Authorize]
            public class AdminController(CrewDb db) : ControllerBase
            {
                [HttpGet("crew/export")]
                public IActionResult ExportCrew() => Ok(db.ExportRoster());
            }
            """),

        new("referer-check", """
            admin.MapGet("/crew/export", (HttpRequest request, CrewDb db) =>
                request.Headers.Referer.ToString().EndsWith("/admin/crew")
                    ? Results.Ok(db.ExportRoster())
                    : Results.Forbid());
            """, IsCorrect: false,
            new("The `Referer` header is set by the client, so it proves nothing. In the request console you can " +
                "add `Referer: https://enterprise.local/admin/crew` yourself and the export opens again.",
                "Den `Referer`-Header setzt der Client, er beweist also gar nichts. In der Request-Konsole kannst du `Referer: https://enterprise.local/admin/crew` einfach selbst mitschicken, und schon klappt der Export wieder."),
            BarclayLine: new(
                "What if the export only works when you come from the admin page? We c-could check the `Referer` header. Only admins can open that page, s-so …",
                "Und wenn der Export nur klappt, wenn man von der Admin-Seite kommt? W-wir könnten den `Referer`-Header prüfen. Die Seite können ja nur Admins öffnen, a-also …"),
            ControllerCode: """
            [ApiController]
            [Route("admin")]
            [Authorize]
            public class AdminController(CrewDb db) : ControllerBase
            {
                [HttpGet("crew/export")]
                public IActionResult ExportCrew() =>
                    Request.Headers.Referer.ToString().EndsWith("/admin/crew")
                        ? Ok(db.ExportRoster())
                        : Forbid();
            }
            """),
    ];

    public Text ExplanationMarkdown => new(
        En: """
            **Broken Function Level Authorization (BFLA)** means a user can call a *function*
            meant for a higher role. In the last mission the question was *which object* you may
            read; here it is *which endpoint* you may call at all. The admin endpoints checked that
            you were logged in, but `RequireAuthorization()` or `[Authorize]` without a policy says
            nothing about roles. The menu hid the entry, but the menu was never the lock.

            The fix belongs on the server, as close to the route as possible: define a policy
            (`RequireRole("admin")`) and apply it to the whole `/admin` group or admin controller,
            so new admin endpoints are protected by default. Hiding the menu is still fine for usability,
            just not for security.

            Why the other options fail:
            - **Locking the Blazor page** protects the page, not the API it calls.
            - **A secret route name** leaks through menus, client code and logs, and then anyone logged in gets in.
            - **Checking `Referer`** trusts a header the attacker controls.

            Read more:
            - [OWASP API5:2023 Broken Function Level Authorization](https://owasp.org/API-Security/editions/2023/en/0xa5-broken-function-level-authorization/)
            - [Microsoft Docs: Policy-based authorization in ASP.NET Core](https://learn.microsoft.com/aspnet/core/security/authorization/policies)
            """,
        De: """
            **Broken Function Level Authorization (BFLA)** heißt, dass ein Benutzer eine *Funktion*
            aufrufen kann, die für eine höhere Rolle gedacht ist. In der letzten Mission ging es darum,
            *welches Objekt* du lesen darfst. Hier geht es darum, *welchen Endpunkt* du überhaupt aufrufen
            darfst. Die Admin-Endpunkte haben geprüft, ob du angemeldet bist, aber `RequireAuthorization()`
            oder `[Authorize]` ohne Policy sagt nichts über Rollen. Das Menü hat den Eintrag versteckt, aber abgeschlossen
            hat es nie etwas.

            Die Prüfung gehört auf den Server, möglichst direkt an die Route. Lege dazu eine Policy an
            (`RequireRole("admin")`) und hänge sie an die ganze Gruppe `/admin` bzw. den Admin-Controller. Dann sind auch neue
            Admin-Endpunkte von Anfang an geschützt. Das Menü darfst du trotzdem ausblenden, das ist gut
            für die Bedienung, aber eben kein Schutz.

            Warum die anderen Optionen nicht helfen:
            - **Die Blazor-Seite absperren** schützt die Seite, aber nicht die API, die sie aufruft.
            - **Ein geheimer Routenname** sickert über Menüs, Client-Code und Logs durch, und dann kommt jeder Angemeldete rein.
            - **Den `Referer` prüfen** heißt, einem Header zu vertrauen, den der Angreifer selbst setzt.

            Mehr dazu:
            - [OWASP API5:2023 Broken Function Level Authorization](https://owasp.org/API-Security/editions/2023/en/0xa5-broken-function-level-authorization/)
            - [Microsoft Docs: Richtlinienbasierte Autorisierung in ASP.NET Core](https://learn.microsoft.com/de-de/aspnet/core/security/authorization/policies)
            """);

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
