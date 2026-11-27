using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;
using Trainer.Web.Core;

namespace Trainer.Web.Labs.OpenRedirect;

public sealed record InboxEntry(DateTimeOffset ReceivedAt, string Url);

/// <summary>
/// 2.4 Open redirect: <c>GET /login/continue?returnUrl=</c> redirects anywhere
/// and appends the user's one-time <c>code</c>. A Riker bot (<c>POST /support/messages</c>)
/// follows links that point at the ship's own login page; anything that ends up on
/// <c>evil.example</c> is logged in <c>GET /attacker/inbox</c>. Riker's code is the flag.
/// </summary>
public sealed class OpenRedirectLab(FlagService flags, DemoAuth auth) : ILab
{
    private const string Riker = "riker";
    private const string AttackerHost = "evil.example";
    private static readonly Uri ShipBase = new("https://enterprise.local/");

    private readonly Lock _gate = new();
    private readonly Dictionary<string, string> _codes = [];
    private List<InboxEntry> _inbox = [];

    public string Slug => "open-redirect";
    public Text Title => new("Wrong Coordinates", "Falsche Koordinaten");
    public string Category => "API";
    public Difficulty Difficulty => Difficulty.Medium;

    private string LoginPath => $"{LabRegistry.ApiPrefix}{Slug}/login/continue";

    public LabStory Story => new(
        Act: 2,
        Order: 4,
        Briefing: new(
            En: """
                Captain's log, stardate 47327.9. This morning Commander Riker received a message from
                Starfleet Support asking him to log in again after the refit. He clicked the link,
                logged in on our own login page and thought no more of it. An hour later his account
                opened the shuttle bay doors on deck 13. The Commander was in the gym at the time. I
                have asked Mr. Crusher to find out where that link really led.
                """,
            De: """
                Logbuch des Captains, Sternzeit 47327.9. Commander Riker hat heute Morgen eine Nachricht vom
                Sternenflotten-Support bekommen: Er möge sich nach dem Software-Update bitte neu anmelden. Er
                hat auf den Link geklickt, sich auf unserer eigenen Anmeldeseite angemeldet und nicht weiter
                darüber nachgedacht. Eine Stunde später hat sein Konto die Tore des Shuttlehangars auf Deck 13
                geöffnet. Der Commander war zu der Zeit im Fitnessraum. Ich habe Mr. Crusher gebeten
                herauszufinden, wohin dieser Link wirklich geführt hat.
                """),
        CallingCard: new(
            En: """
                Got him, didn't you? Clever boy. It took me three tries, you needed one. I'm almost proud.
                But I'm tired of borrowing faces, Wesley. I want what's underneath. Deep in the core there's
                an archive where this crew keeps everything it would rather forget. I'll start there. — L.
                """,
            De: """
                Hast ihn erwischt, was? Kluger Junge. Ich hab' drei Versuche gebraucht, du nur einen. Fast bin
                ich stolz. Aber ich hab' genug davon, mir fremde Gesichter zu leihen. Ich will wissen, was
                darunter steckt. Tief im Bordcomputer gibt's ein Archiv, in dem diese Crew alles aufhebt, was
                sie lieber vergessen würde. Da fang' ich an. — L.
                """),
        Debrief: new(
            En: """
                Well done, Mr. Crusher. The login page now only sends people back to pages on this ship.
                Commander Riker has asked Starfleet Support to stop sending him links. Starfleet Support
                says it never sent him any. He is taking it personally.
                """,
            De: """
                Gut gemacht, Mr. Crusher. Die Anmeldeseite schickt jetzt nur noch auf Seiten dieses Schiffs
                zurück. Commander Riker hat den Sternenflotten-Support gebeten, ihm keine Links mehr zu schicken.
                Der Support sagt, er habe ihm nie welche geschickt. Der Commander nimmt das persönlich.
                """));

    private const string VulnerableCode = """
        ```csharp minimal
        // After login, the mobile app hands over to the browser and back again.
        app.MapGet("/login/continue", (string returnUrl, ClaimsPrincipal user, CodeStore codes) =>
        {
            var code = codes.IssueFor(user);              // one-time login code
            var separator = returnUrl.Contains('?') ? '&' : '?';

            return Results.Redirect($"{returnUrl}{separator}code={code}");
        })
        .RequireAuthorization();
        ```
        ```csharp controller
        // After login, the mobile app hands over to the browser and back again.
        [ApiController]
        [Route("login")]
        public class LoginController(CodeStore codes) : ControllerBase
        {
            [HttpGet("continue"), Authorize]
            public IActionResult Continue(string returnUrl)
            {
                var code = codes.IssueFor(User);              // one-time login code
                var separator = returnUrl.Contains('?') ? '&' : '?';

                return Redirect($"{returnUrl}{separator}code={code}");
            }
        }
        ```
        """;

    public Text ScenarioMarkdown => new(
        En: $"""
            After logging in, the ship's login page sends you back to where you came from
            (`returnUrl`) and adds a one-time `code` that the app exchanges for a session.
            Whoever has the code is logged in as you.

            | Endpoint | What it does |
            |---|---|
            | `GET /api/labs/open-redirect/login/continue?returnUrl=...` | Redirects to `returnUrl` with your one-time `code` |
            | `POST /api/labs/open-redirect/support/messages` | Sends Commander Riker a message as "Starfleet Support": `{"{"} "link": "..." {"}"}` |
            | `GET /api/labs/open-redirect/attacker/inbox` | Every request that reached `https://evil.example` |

            Commander Riker is logged in and opens links in support messages, but only if they
            point at the ship (`https://enterprise.local/...`). The request console does not follow
            redirects, so you can see the `Location` header. The login endpoint looks like this:

            {VulnerableCode}

            **Goal:** get Commander Riker's one-time code. It is the flag.
            """,
        De: $"""
            Nach der Anmeldung schickt dich die Anmeldeseite des Schiffs dorthin zurück, wo du hergekommen
            bist (`returnUrl`), und hängt einen Einmal-`code` an. Den tauscht die App gegen eine Sitzung ein.
            Wer den Code hat, ist also als du angemeldet.

            | Endpunkt | Funktion |
            |---|---|
            | `GET /api/labs/open-redirect/login/continue?returnUrl=...` | Leitet mit deinem Einmal-`code` zu `returnUrl` weiter |
            | `POST /api/labs/open-redirect/support/messages` | Schickt Commander Riker eine Nachricht als „Sternenflotten-Support“: `{"{"} "link": "..." {"}"}` |
            | `GET /api/labs/open-redirect/attacker/inbox` | Jeder Request, der bei `https://evil.example` angekommen ist |

            Commander Riker ist angemeldet und öffnet Links in Support-Nachrichten, aber nur, wenn sie aufs
            Schiff zeigen (`https://enterprise.local/...`). Die Request-Konsole folgt keinen Weiterleitungen,
            du siehst also den Header `Location`. So sieht der Anmelde-Endpunkt aus:

            {VulnerableCode}

            **Ziel:** Beschaffe dir Commander Rikers Einmal-Code. Er ist die Flag.
            """);

    public IReadOnlyList<Text> Hints =>
    [
        new("`GET /login/continue` sends you to `returnUrl` and appends a one-time `code`. You can see this in the `Location` header of your own request. I note that the code never checks where `returnUrl` points.",
            "`GET /login/continue` schickt Sie zu `returnUrl` und hängt einen Einmal-`code` an. Bei Ihrem eigenen Request sehen Sie das im Header `Location`. Mir fällt auf, dass der Code nie prüft, wohin `returnUrl` zeigt."),
        new("Commander Riker does not click links to unknown addresses. A link to our own login page, however, he trusts completely. `POST /support/messages` delivers one to him as Starfleet Support.",
            "Commander Riker klickt keine Links zu fremden Adressen. Einem Link auf unsere eigene Anmeldeseite vertraut er dagegen völlig. Mit `POST /support/messages` stellen Sie ihm einen solchen Link als Sternenflotten-Support zu."),
        new("Anything that arrives at `https://evil.example` appears in `GET /attacker/inbox`. A link to our login page with `returnUrl=https://evil.example/` would therefore be instructive. I find it uncomfortable how well this would work.",
            "Alles, was bei `https://evil.example` ankommt, erscheint in `GET /attacker/inbox`. Ein Link auf unsere Anmeldeseite mit `returnUrl=https://evil.example/` wäre deshalb aufschlussreich. Es ist mir unangenehm, wie gut das funktionieren würde."),
    ];

    public RequestTemplate DefaultRequest => new(
        "GET",
        $"{LoginPath}?returnUrl=/api/labs/{Slug}/app/home",
        new Dictionary<string, string> { ["Authorization"] = $"Bearer {auth.GetToken(Slug, DemoAuth.Player)}" },
        null);

    public IReadOnlyList<FixOption> FixOptions =>
    [
        new("local-redirect", """
            static bool IsLocalUrl(string url) =>
                url.StartsWith('/') && !url.StartsWith("//") && !url.StartsWith("/\\");

            app.MapGet("/login/continue", (string? returnUrl, ClaimsPrincipal user, CodeStore codes) =>
            {
                var target = returnUrl is not null && IsLocalUrl(returnUrl) ? returnUrl : "/";
                var code = codes.IssueFor(user);
                var separator = target.Contains('?') ? '&' : '?';

                return Results.LocalRedirect($"{target}{separator}code={code}");
            })
            .RequireAuthorization();
            """, IsCorrect: true,
            new("Correct. Only paths on this site are accepted, protocol-relative tricks like `//evil.example` are " +
                "rejected, and anything else falls back to `/`. `Url.IsLocalUrl` applies the same check, and " +
                "`Results.LocalRedirect` / `LocalRedirect()` enforces it again and throws for external URLs.",
                "Richtig. Es werden nur Pfade auf dieser Seite akzeptiert, Tricks wie `//evil.example` fallen durch, und alles andere landet bei `/`. `Url.IsLocalUrl` prüft nach derselben Regel, und `Results.LocalRedirect` bzw. `LocalRedirect()` setzt sie noch einmal durch und wirft bei externen URLs eine Exception."),
            BarclayLine: new(
                "C-could we only allow addresses on our own ship? Local ones? I-I think there's a helper for that. There's always a helper.",
                "K-könnten wir nur Adressen auf unserem eigenen Schiff erlauben? Also lokale? I-ich glaube, dafür gibt's eine Hilfsfunktion. Es gibt immer eine Hilfsfunktion."),
            ControllerCode: """
            [ApiController]
            [Route("login")]
            public class LoginController(CodeStore codes) : ControllerBase
            {
                [HttpGet("continue"), Authorize]
                public IActionResult Continue(string? returnUrl)
                {
                    var target = Url.IsLocalUrl(returnUrl) ? returnUrl : "/";
                    var code = codes.IssueFor(User);
                    var separator = target.Contains('?') ? '&' : '?';

                    return LocalRedirect($"{target}{separator}code={code}");
                }
            }
            """),

        new("starts-with-slash", """
            app.MapGet("/login/continue", (string returnUrl, ClaimsPrincipal user, CodeStore codes) =>
            {
                if (!returnUrl.StartsWith("/"))
                    returnUrl = "/";

                var code = codes.IssueFor(user);
                var separator = returnUrl.Contains('?') ? '&' : '?';
                return Results.Redirect($"{returnUrl}{separator}code={code}");
            })
            .RequireAuthorization();
            """, IsCorrect: false,
            new("`//evil.example/` starts with a slash too. Browsers read it as a protocol-relative URL and go straight to " +
                "evil.example, code included. (`/\\evil.example` works in many browsers as well.)",
                "`//evil.example/` fängt auch mit einem Schrägstrich an. Browser lesen das als protokollrelative URL und gehen direkt zu evil.example, samt Code. (`/\\evil.example` klappt in vielen Browsern übrigens auch.)"),
            BarclayLine: new(
                "Local addresses start with a slash. S-so we check for the slash. One character. Done.",
                "Lokale Adressen fangen mit einem Schrägstrich an. A-also prüfen wir auf den Schrägstrich. Ein Zeichen, fertig."),
            ControllerCode: """
            [ApiController]
            [Route("login")]
            public class LoginController(CodeStore codes) : ControllerBase
            {
                [HttpGet("continue"), Authorize]
                public IActionResult Continue(string returnUrl)
                {
                    if (!returnUrl.StartsWith("/"))
                        returnUrl = "/";

                    var code = codes.IssueFor(User);
                    var separator = returnUrl.Contains('?') ? '&' : '?';
                    return Redirect($"{returnUrl}{separator}code={code}");
                }
            }
            """),

        new("contains-domain", """
            app.MapGet("/login/continue", (string returnUrl, ClaimsPrincipal user, CodeStore codes) =>
            {
                if (!returnUrl.Contains("enterprise.local"))
                    returnUrl = "/";

                var code = codes.IssueFor(user);
                var separator = returnUrl.Contains('?') ? '&' : '?';
                return Results.Redirect($"{returnUrl}{separator}code={code}");
            })
            .RequireAuthorization();
            """, IsCorrect: false,
            new("Lore can register `enterprise.local.evil.example`, or simply send `https://evil.example/?enterprise.local`. " +
                "Both contain the text, and both go to his server.",
                "Lore kann sich `enterprise.local.evil.example` zulegen oder einfach `https://evil.example/?enterprise.local` schicken. Beide enthalten den Text, und beide führen auf seinen Server."),
            BarclayLine: new(
                "We just check that the address says `enterprise.local` somewhere. If it's ours, it says so. R-right?",
                "W-wir prüfen einfach, ob `enterprise.local` irgendwo in der Adresse steht. Wenn sie uns gehört, steht das doch drin. Oder?"),
            ControllerCode: """
            [ApiController]
            [Route("login")]
            public class LoginController(CodeStore codes) : ControllerBase
            {
                [HttpGet("continue"), Authorize]
                public IActionResult Continue(string returnUrl)
                {
                    if (!returnUrl.Contains("enterprise.local"))
                        returnUrl = "/";

                    var code = codes.IssueFor(User);
                    var separator = returnUrl.Contains('?') ? '&' : '?';
                    return Redirect($"{returnUrl}{separator}code={code}");
                }
            }
            """),

        new("strip-scheme", """
            app.MapGet("/login/continue", (string returnUrl, ClaimsPrincipal user, CodeStore codes) =>
            {
                returnUrl = returnUrl.Replace("https://", "").Replace("http://", "");

                var code = codes.IssueFor(user);
                var separator = returnUrl.Contains('?') ? '&' : '?';
                return Results.Redirect($"{returnUrl}{separator}code={code}");
            })
            .RequireAuthorization();
            """, IsCorrect: false,
            new("`//evil.example/` has no scheme to remove and still leaves the site. Upper case (`HTTPS://`) slips " +
                "through too. Removing text is a blocklist, not a rule about where you may go.",
                "`//evil.example/` hat gar kein Schema, das man entfernen könnte, und führt trotzdem woandershin. Großbuchstaben (`HTTPS://`) rutschen ebenfalls durch. Text zu entfernen ist eine Sperrliste und keine Regel dafür, wohin man darf."),
            BarclayLine: new(
                "No `https://`, no other planet. I'll just r-remove it. Without the scheme it can't go anywhere. C-can it?",
                "Ohne `https://` kein anderer Planet. I-ich entferne das einfach. Ohne Schema kann der Link doch nirgendwo hin. O-oder?"),
            ControllerCode: """
            [ApiController]
            [Route("login")]
            public class LoginController(CodeStore codes) : ControllerBase
            {
                [HttpGet("continue"), Authorize]
                public IActionResult Continue(string returnUrl)
                {
                    returnUrl = returnUrl.Replace("https://", "").Replace("http://", "");

                    var code = codes.IssueFor(User);
                    var separator = returnUrl.Contains('?') ? '&' : '?';
                    return Redirect($"{returnUrl}{separator}code={code}");
                }
            }
            """),
    ];

    public Text ExplanationMarkdown => new(
        En: """
            An **open redirect** lets anyone use your site to send people somewhere else. On its
            own that is a phishing aid: the link starts with a trusted address. Here it was worse,
            because the redirect carried a one-time login code. Riker trusted the link because it
            really was the ship's login page, and the page handed his code to Lore's server.

            Only redirect to local paths (`Results.LocalRedirect` / `LocalRedirect()`, `Url.IsLocalUrl`), and fall back
            to `/` for anything else. If you need external targets, use an exact allowlist of full
            URLs. That is what OAuth and OpenID Connect do with registered `redirect_uri`s, and
            they never accept a pattern or a partial match.

            Why the other options fail:
            - **`StartsWith("/")`** lets `//evil.example` through, which browsers treat as an external URL.
            - **`Contains("enterprise.local")`** matches `enterprise.local.evil.example` and query strings.
            - **Removing `https://`** is a blocklist; `//evil.example` doesn't even need a scheme.

            Read more:
            - [OWASP: Unvalidated Redirects and Forwards Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Unvalidated_Redirects_and_Forwards_Cheat_Sheet.html)
            - [Microsoft Docs: Prevent open redirect attacks in ASP.NET Core](https://learn.microsoft.com/aspnet/core/security/preventing-open-redirects)
            """,
        De: """
            Ein **Open Redirect** erlaubt jedem, deine Seite zu benutzen, um Leute woandershin zu schicken.
            Allein ist das schon eine Hilfe für Phishing, denn der Link beginnt mit einer vertrauenswürdigen
            Adresse. Hier war es noch schlimmer, weil die Weiterleitung einen Einmal-Code für die Anmeldung
            mitgenommen hat. Riker hat dem Link vertraut, weil es wirklich die Anmeldeseite des Schiffs war,
            und genau diese Seite hat seinen Code an Lores Server übergeben.

            Leite nur auf lokale Pfade weiter (`Results.LocalRedirect` / `LocalRedirect()`, `Url.IsLocalUrl`) und falle bei allem
            anderen auf `/` zurück. Wenn du externe Ziele brauchst, nimm eine exakte Allowlist vollständiger
            URLs. So machen es auch OAuth und OpenID Connect mit registrierten `redirect_uri`s, und die
            akzeptieren nie ein Muster oder einen Teiltreffer.

            Warum die anderen Optionen nicht helfen:
            - **`StartsWith("/")`** lässt `//evil.example` durch, und das behandeln Browser als externe URL.
            - **`Contains("enterprise.local")`** passt auch auf `enterprise.local.evil.example` und auf Query-Strings.
            - **`https://` entfernen** ist eine Sperrliste, und `//evil.example` braucht gar kein Schema.

            Mehr dazu:
            - [OWASP: Unvalidated Redirects and Forwards Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Unvalidated_Redirects_and_Forwards_Cheat_Sheet.html)
            - [Microsoft Docs: Open-Redirect-Angriffe in ASP.NET Core verhindern](https://learn.microsoft.com/de-de/aspnet/core/security/preventing-open-redirects)
            """);

    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        auth.GetToken(Slug, DemoAuth.Player);
        auth.GetToken(Slug, Riker);
        Reset();

        // VULNERABLE: redirects to any returnUrl, with the user's one-time code attached.
        app.MapGet("/login/continue", (string? returnUrl, HttpRequest request) =>
        {
            if (auth.FindUser(Slug, request) is not { } user) return Results.Unauthorized();
            return Results.Redirect(ContinueLogin(user.Name, returnUrl ?? "/"));
        });

        // Where a normal login lands. Only here so the default request has a real target.
        app.MapGet("/app/home", (string? code, HttpRequest request) =>
        {
            var language = Languages.FromRequest(request);
            return Results.Ok(new { page = "home", message = new Text("Welcome back. Your one-time code has been used.", "Willkommen zurück. Dein Einmal-Code wurde eingelöst.")[language], code });
        });

        // Victim bot: Riker opens links that point at the ship.
        app.MapPost("/support/messages", async (HttpRequest request) =>
        {
            var language = Languages.FromRequest(request);
            string? link;
            try
            {
                using var doc = await JsonDocument.ParseAsync(request.Body);
                link = doc.RootElement.TryGetProperty("link", out var l) ? l.GetString() : null;
            }
            catch (JsonException)
            {
                return Results.BadRequest(new { error = "Body must be JSON: { \"link\": \"...\" }" });
            }
            if (string.IsNullOrWhiteSpace(link))
                return Results.BadRequest(new { error = "Body must be JSON: { \"link\": \"...\" }" });

            return Results.Ok(new { from = "Commander Riker", reply = RikerOpens(link.Trim())[language] });
        });

        app.MapGet("/attacker/inbox", () =>
        {
            lock (_gate) return Results.Ok(_inbox.ToList());
        });
    }

    /// <summary>The vulnerable handler logic, shared by the endpoint and the Riker bot.</summary>
    private string ContinueLogin(string user, string returnUrl)
    {
        string code;
        lock (_gate)
        {
            if (!_codes.TryGetValue(user, out code!))
                _codes[user] = code = user == Riker ? flags.GetFlag(Slug) : Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(8));
        }
        var separator = returnUrl.Contains('?') ? '&' : '?';
        return $"{returnUrl}{separator}code={code}";
    }

    /// <summary>Simulates Riker clicking the link in a browser where he is logged in.</summary>
    private Text RikerOpens(string link)
    {
        if (!Uri.TryCreate(ShipBase, link, out var url) || url.Scheme is not ("http" or "https"))
            return new("That doesn't look like a link. Is Support all right?", "Das sieht nicht nach einem Link aus. Geht's dem Support gut?");

        if (!IsShip(url))
            return new($"{url.Host}? That's not a Starfleet address. Nice try.", $"{url.Host}? Das ist keine Adresse der Sternenflotte. Netter Versuch.");

        if (!url.AbsolutePath.Equals(LoginPath, StringComparison.OrdinalIgnoreCase))
            return new("Opened it. Nothing happened. Is this a prank?", "Hab's geöffnet. Nichts passiert. Soll das ein Scherz sein?");

        var returnUrl = QueryHelpers.ParseQuery(url.Query).TryGetValue("returnUrl", out var r) ? r.ToString() : "/";
        var location = ContinueLogin(Riker, returnUrl);

        // Resolve Location the way a browser does (//host, absolute URLs, relative paths).
        if (!Uri.TryCreate(url, location.Replace('\\', '/'), out var landed) || IsShip(landed))
            return new("Logged in again, like you asked. Thanks, Support.", "Neu angemeldet, wie gewünscht. Danke, Support.");

        if (landed.Host.Equals(AttackerHost, StringComparison.OrdinalIgnoreCase)
            || landed.Host.EndsWith("." + AttackerHost, StringComparison.OrdinalIgnoreCase))
        {
            lock (_gate) _inbox.Add(new InboxEntry(DateTimeOffset.UtcNow, landed.GetComponents(UriComponents.AbsoluteUri, UriFormat.Unescaped)));
        }
        return new("Logged in. Then the page went blank. Odd. Anyway, thanks, Support.",
                   "Angemeldet. Danach war die Seite leer. Komisch. Egal, danke, Support.");
    }

    private static bool IsShip(Uri url) =>
        url.Host.Equals(ShipBase.Host, StringComparison.OrdinalIgnoreCase)
        || url.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
        || url.Host == "127.0.0.1";

    public void Reset()
    {
        lock (_gate)
        {
            _codes.Clear();
            _inbox = [];
        }
    }
}
