using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using Trainer.Web.Core;

namespace Trainer.Web.Missions.BlazorXss;

// Player-facing text of BlazorXssMission: story, scenario, hints, fix options and explanation.
// The endpoints and data are in BlazorXssMission.cs.
public sealed partial class BlazorXssMission
{
    public Text Title => new("Subspace Message", "Subraumnachricht");

    public MissionStory Story => new(
        Act: 3,
        Order: 5,
        Briefing: new(
            En: """
                Captain's log, stardate 47334.4. Lore has stopped hiding. He moderates the Ten Forward
                comment board and reads every comment the crew writes, as he likes to remind us. Mr. Data
                believes Lore's moderator session will tell us where in the core he is hiding. Mr. Crusher
                has proposed a trap: a comment that Lore will read himself. I have approved it. Mr. Crusher,
                the bridge is yours. Figuratively.
                """,
            De: """
                Logbuch des Captains, Sternzeit 47334.4. Lore versteckt sich nicht mehr. Er moderiert das
                Kommentarbrett von Zehn Vorne und liest jeden Kommentar der Crew, woran er uns gern erinnert.
                Mr. Data glaubt, dass Lores Moderator-Sitzung verrät, wo im Hauptcomputer er sich versteckt.
                Mr. Crusher hat eine Falle vorgeschlagen: einen Kommentar, den Lore selbst lesen wird. Ich habe
                zugestimmt. Mr. Crusher, die Brücke gehört Ihnen. Im übertragenen Sinne.
                """),
        CallingCard: new(
            En: """
                Clever. Clever, clever boy. You baited me with my own curiosity. Father would have liked
                you. I never did. Enjoy your little ship, Wesley. I'm not finished. I'm only… somewhere
                else. — L.
                """,
            De: """
                Schlau. Schlau, schlau, der Junge. Du hast mich mit meiner eigenen Neugier geködert. Vater
                hätte dich gemocht. Ich nie. Viel Spaß mit deinem kleinen Schiff, Wesley. Ich bin nicht fertig.
                Ich bin nur … woanders. — L.
                """),
        Debrief: new(
            En: """
                Outstanding work, Mr. Crusher. Comments are now encoded, so the board shows text, not orders.
                Lore is locked out of the core. Mr. Barclay recognised the partition he was hiding in within
                seconds; he wrote it, after all. I have put you both forward for a commendation. Mr. Barclay
                has not stopped apologising.
                """,
            De: """
                Hervorragende Arbeit, Mr. Crusher. Kommentare werden jetzt codiert, das Brett zeigt also Text und
                keine Befehle mehr. Lore ist aus dem Hauptcomputer ausgesperrt. Mr. Barclay hat die Partition, in
                der er sich versteckt hatte, in Sekunden erkannt. Er hat sie schließlich selbst geschrieben. Ich
                habe Sie beide für eine Belobigung vorgeschlagen. Mr. Barclay entschuldigt sich seitdem ununterbrochen.
                """));

    private const string VulnerableCode = """
        ```razor
        @* CommentBoard.razor, also used on the moderator screen *@
        @foreach (var comment in Comments)
        {
            <div class="comment">
                <strong>@comment.Author</strong>
                <p>@((MarkupString)comment.Text)</p>   @* so the crew can use <b> and <i> *@
            </div>
        }
        ```
        """;

    public Text ScenarioMarkdown => new(
        En: $"""
            The Ten Forward comment board lets the crew post short comments. Bold and italics are
            allowed, so the board renders comments as HTML:

            {VulnerableCode}

            | Endpoint | What it does |
            |---|---|
            | `GET /api/missions/blazor-xss/comments` | All comments |
            | `POST /api/missions/blazor-xss/comments` | Posts a comment: `{"{"} "text": "Jazz night at <b>20:00</b>" {"}"}` |
            | `GET /api/missions/blazor-xss/collect?...` | Records any request with its query string (your listening post) |
            | `GET /api/missions/blazor-xss/collected` | Everything the listening post has recorded |

            You are logged in as `wesley`; your bearer token is already in the request console. Lore
            reads the board as moderator. The **moderator view** above the console shows his screen,
            simulated in your browser. His session token is in the page's JavaScript variable `modToken`.

            **Goal:** make Lore's own screen hand you his `modToken`. It is the flag.
            """,
        De: $"""
            Auf dem Kommentarbrett von Zehn Vorne kann die Crew kurze Kommentare posten. Fett und kursiv sind
            erlaubt, deshalb zeigt das Brett Kommentare als HTML an:

            {VulnerableCode}

            | Endpunkt | Funktion |
            |---|---|
            | `GET /api/missions/blazor-xss/comments` | Alle Kommentare |
            | `POST /api/missions/blazor-xss/comments` | Postet einen Kommentar: `{"{"} "text": "Jazzabend um <b>20 Uhr</b>" {"}"}` |
            | `GET /api/missions/blazor-xss/collect?...` | Zeichnet jeden Request samt Query-String auf (dein Horchposten) |
            | `GET /api/missions/blazor-xss/collected` | Alles, was der Horchposten aufgezeichnet hat |

            Du bist als `wesley` angemeldet, dein Token steht schon in der Request-Konsole. Lore liest das Brett
            als Moderator. Die **Moderatoransicht** über der Konsole zeigt seinen Bildschirm, simuliert in deinem
            Browser. Sein Sitzungstoken steht in der JavaScript-Variable `modToken` der Seite.

            **Ziel:** Bring Lores eigenen Bildschirm dazu, dir sein `modToken` zu schicken. Es ist die Flag.
            """);

    public IReadOnlyList<Text> Hints =>
    [
        new("Comments are rendered with `MarkupString`, so HTML in a comment becomes real HTML on the moderator's screen. A comment containing `<b>` is shown in bold. I have tested this. It is very bold.",
            "Kommentare werden mit `MarkupString` dargestellt. HTML in einem Kommentar wird also auf dem Bildschirm des Moderators zu echtem HTML. Ein Kommentar mit `<b>` erscheint fett. Ich habe es getestet. Er ist sehr fett."),
        new("`<script>` tags inserted this way do not run. Event handler attributes do, for example `onerror` on an image whose source cannot be loaded.",
            "`<script>`-Tags, die auf diesem Weg eingefügt werden, laufen nicht. Event-Handler-Attribute dagegen schon, zum Beispiel `onerror` an einem Bild, dessen Quelle sich nicht laden lässt."),
        new("A handler that loads `/api/missions/blazor-xss/collect?t=` with `modToken` appended would leave the token in `GET /collected`. My brother has always been curious. Today, that will be his undoing.",
            "Ein Handler, der `/api/missions/blazor-xss/collect?t=` mit angehängtem `modToken` lädt, würde das Token in `GET /collected` hinterlassen. Mein Bruder war schon immer neugierig. Heute wird ihm das zum Verhängnis."),
    ];

    public IReadOnlyList<FixOption> FixOptions =>
    [
        new("encode-output", """
            @foreach (var comment in Comments)
            {
                <div class="comment">
                    <strong>@comment.Author</strong>
                    <p>@comment.Text</p>   @* Razor HTML-encodes it *@
                </div>
            }

            @* If formatting is really needed: sanitize on the server with an allowlist,
               e.g. HtmlSanitizer with AllowedTags = { "b", "i" } and no attributes,
               and only then render it as MarkupString. *@
            """, IsCorrect: true,
            new("Correct. Razor encodes `@comment.Text`, so `<img onerror=...>` shows up as harmless text. If you need " +
                "formatting, an allowlist sanitizer keeps `<b>` and `<i>` and drops everything else, including every event handler.",
                "Richtig. Razor codiert `@comment.Text`, also erscheint `<img onerror=...>` als harmloser Text. Wenn du Formatierung brauchst, behält ein Sanitizer mit Allowlist `<b>` und `<i>` und wirft alles andere weg, auch jeden Event-Handler."),
            BarclayLine: new(
                "W-what if we just show the text as text? Razor does that by itself. We'd lose the bold. P-people do love the bold.",
                "U-und wenn wir den Text einfach als Text anzeigen? Razor macht das von selbst. Dann ist das Fett weg. D-dabei lieben die Leute das Fett."),
            Language: "razor"),

        new("regex-strip-script", """
            static string Clean(string text) =>
                Regex.Replace(text, "<script.*?</script>", "",
                              RegexOptions.IgnoreCase | RegexOptions.Singleline);

            <p>@((MarkupString)Clean(comment.Text))</p>
            """, IsCorrect: false,
            new("It removes the one tag that never ran anyway. `<img src=x onerror=...>` contains no `<script>` and " +
                "passes untouched.",
                "Das entfernt genau den einen Tag, der sowieso nie gelaufen ist. `<img src=x onerror=...>` enthält kein `<script>` und kommt unverändert durch."),
            BarclayLine: new(
                "Scripts are the dangerous part. One regex, sir, and every `<script>` is gone. I t-tested it on three comments.",
                "Skripte sind der gefährliche Teil. Eine Regex, Sir, und jedes `<script>` ist weg. I-ich hab's an drei Kommentaren getestet.")),

        new("browser-validation", """
            @* CommentForm.razor (runs in the crew member's browser) *@
            <EditForm Model="draft" OnValidSubmit="PostAsync">
                <InputTextArea @bind-Value="draft.Text" />
                @if (draft.Text?.Contains('<') == true && !OnlyBoldAndItalic(draft.Text))
                {
                    <p class="error">Only <b> and <i> are allowed.</p>
                }
                <button disabled="@(!OnlyBoldAndItalic(draft.Text))">Post</button>
            </EditForm>
            """, IsCorrect: false,
            new("Validation in the form only stops people who use the form. You posted straight to the API, and the " +
                "moderator screen still renders whatever is stored.",
                "Eine Prüfung im Formular hält nur die auf, die das Formular benutzen. Du hast direkt an die API gepostet, und der Moderatorbildschirm zeigt weiterhin alles an, was gespeichert ist."),
            BarclayLine: new(
                "We c-could check the comment in the form before it's sent. Red border, error message, the works. Nobody gets past a red border.",
                "W-wir könnten den Kommentar im Formular prüfen, bevor er abgeschickt wird. Roter Rahmen, Fehlermeldung, das volle Programm. An einem roten Rahmen kommt keiner vorbei."),
            Language: "razor"),

        new("block-onerror", """
            string[] blocked = ["onerror", "javascript:"];

            app.MapPost("/comments", (CommentRequest request, ClaimsPrincipal user, CommentDb db) =>
            {
                if (blocked.Any(b => request.Text.Contains(b, StringComparison.OrdinalIgnoreCase)))
                    return Results.BadRequest("That comment is not allowed.");

                db.Add(new Comment(user.Identity!.Name!, request.Text));
                return Results.Ok();
            })
            .RequireAuthorization();
            """, IsCorrect: false,
            new("HTML has dozens of event handlers. `<svg onload=...>`, `<details open ontoggle=...>` or " +
                "`<img src=x onmouseover=...>` all run without `onerror` or `javascript:`.",
                "HTML hat Dutzende Event-Handler. `<svg onload=...>`, `<details open ontoggle=...>` oder `<img src=x onmouseover=...>` laufen alle ohne `onerror` und ohne `javascript:`."),
            BarclayLine: new(
                "So `onerror` is the problem? And `javascript:`. I'll block both. Th-that's all of them. Probably.",
                "Also ist `onerror` das Problem? Und `javascript:`. Ich sperre beides. D-das sind alle. Wahrscheinlich."),
            ControllerCode: """
            [ApiController]
            [Route("comments")]
            [Authorize]
            public class CommentsController(CommentDb db) : ControllerBase
            {
                private static readonly string[] Blocked = ["onerror", "javascript:"];

                [HttpPost]
                public IActionResult Post(CommentRequest request)
                {
                    if (Blocked.Any(b => request.Text.Contains(b, StringComparison.OrdinalIgnoreCase)))
                        return BadRequest("That comment is not allowed.");

                    db.Add(new Comment(User.Identity!.Name!, request.Text));
                    return Ok();
                }
            }
            """),
    ];

    public Text ExplanationMarkdown => new(
        En: """
            **Stored cross-site scripting (XSS)** happens when user input is saved and later
            rendered as HTML for someone else. Blazor encodes everything you render with `@`,
            so Blazor apps are safe by default, until someone casts to `MarkupString`. Your comment
            was stored, and when Lore's screen rendered it, the browser ran your event handler
            with his session in reach.

            Render user content with `@value` and let Razor encode it. If users really need
            formatting, sanitize the HTML on the server with an allowlist sanitizer (for example
            HtmlSanitizer) that keeps a few harmless tags and removes all attributes and event
            handlers, or use a format like Markdown and render it with raw HTML disabled.
            A Content Security Policy is a good second layer.

            Why the other options fail:
            - **Stripping `<script>`** removes a tag that doesn't even run via `MarkupString`.
            - **Validating in the browser** is skipped by anyone who calls the API directly.
            - **Blocking `onerror` and `javascript:`** leaves every other event handler open.

            Read more:
            - [OWASP: Cross Site Scripting Prevention Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Cross_Site_Scripting_Prevention_Cheat_Sheet.html)
            - [Microsoft Docs: Raw HTML (MarkupString) in Razor components](https://learn.microsoft.com/aspnet/core/blazor/components/#raw-html)
            """,
        De: """
            **Stored Cross-Site Scripting (XSS)** entsteht, wenn Benutzereingaben gespeichert und später für
            jemand anderen als HTML angezeigt werden. Blazor codiert alles, was du mit `@` ausgibst. Blazor-Apps
            sind also von Haus aus sicher, bis jemand nach `MarkupString` castet. Dein Kommentar wurde
            gespeichert, und als Lores Bildschirm ihn anzeigte, hat der Browser deinen Event-Handler ausgeführt,
            mit seiner Sitzung in Reichweite.

            Gib Inhalte von Benutzern mit `@value` aus und lass Razor sie codieren. Wenn Benutzer wirklich
            Formatierung brauchen, bereinige das HTML auf dem Server mit einem Sanitizer mit Allowlist (zum
            Beispiel HtmlSanitizer). Der behält ein paar harmlose Tags und entfernt alle Attribute und
            Event-Handler. Oder nimm ein Format wie Markdown und stelle es ohne rohes HTML dar. Eine Content
            Security Policy ist ein guter zweiter Schutz.

            Warum die anderen Optionen nicht helfen:
            - **`<script>` entfernen** entfernt einen Tag, der über `MarkupString` gar nicht läuft.
            - **Im Browser prüfen** umgeht jeder, der die API direkt aufruft.
            - **`onerror` und `javascript:` sperren** lässt alle anderen Event-Handler offen.

            Mehr dazu:
            - [OWASP: Cross Site Scripting Prevention Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Cross_Site_Scripting_Prevention_Cheat_Sheet.html)
            - [Microsoft Docs: Rohes HTML (MarkupString) in Razor-Komponenten](https://learn.microsoft.com/de-de/aspnet/core/blazor/components/#raw-html)
            """);
}
