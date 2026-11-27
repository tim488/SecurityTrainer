using System.Text.Json;
using Trainer.Web.Core;

namespace Trainer.Web.Labs.BruteForce;

/// <summary>
/// 2.3 Brute force: <c>POST /password-reset/request</c> generates a 4-digit
/// code for a user. <c>POST /password-reset/confirm</c> accepts user + code with no
/// attempt limit, so the code can be brute-forced over 0000–9999. The flag is returned
/// on a successful confirm.
/// </summary>
public sealed class BruteForceLab(FlagService flags, DemoAuth auth) : ILab
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly Lock _gate = new();
    private Dictionary<string, string> _codes = [];

    public string Slug => "brute-force";
    public Text Title => new("Four-Digit Airlock", "Vierstellige Luftschleuse");
    public string Category => "API";
    public Difficulty Difficulty => Difficulty.Medium;

    public LabStory Story => new(
        Act: 2,
        Order: 3,
        Briefing: new(
            En: """
                Captain's log, stardate 47326.8. Commander Riker's access code has been changed
                without his knowledge. He never touched his communicator; nobody did. The ship's
                password reset system sent a four-digit confirmation code, and someone confirmed
                it before the Commander even saw the message. The code has only ten thousand
                possible values and the system does not limit how often you may guess. I have asked
                Mr. Crusher to find out how quickly that code can be found.
                """,
            De: """
                Logbuch des Captains, Sternzeit 47326.8. Commander Rikers Zugangscode wurde ohne
                sein Wissen geändert. Er hat sein Kommunikationsgerät nicht angerührt, und sonst
                hat es auch niemand getan. Das Passwort-Reset-System des Schiffs hat einen
                vierstelligen Bestätigungscode verschickt, und jemand hat ihn bestätigt, bevor der
                Commander die Nachricht überhaupt gesehen hat. Der Code hat nur zehntausend mögliche
                Werte, und das System begrenzt die Versuche nicht. Ich habe Mr. Crusher gebeten
                herauszufinden, wie schnell sich so ein Code finden lässt.
                """),
        CallingCard: new(
            En: """
                Four digits, Wesley. That's all it took. Ten thousand guesses and not a single
                alarm. The Commander didn't even notice until his door wouldn't open. I've got his
                code now, but I want more than a door. There's a link from Starfleet Support
                waiting in his inbox. He trusts Starfleet Support. He shouldn't. — L.
                """,
            De: """
                Vier Ziffern, Wesley. Mehr war's nicht. Zehntausend Versuche und kein einziger
                Alarm. Der Commander hat's erst gemerkt, als seine Tür nicht mehr aufging. Seinen
                Code hab' ich jetzt, aber mir reicht keine Tür. In seinem Posteingang wartet ein
                Link vom Sternenflotten-Support. Er vertraut dem Support. Sollte er nicht. — L.
                """),
        Debrief: new(
            En: """
                Well done, Mr. Crusher. The reset code is now longer, random, and expires after
                three wrong attempts. Commander Riker has a new access code. He has asked that it
                have more than four digits. I have approved this request with some enthusiasm.
                """,
            De: """
                Gut gemacht, Mr. Crusher. Der Reset-Code ist jetzt länger, zufällig und verfällt
                nach drei Fehlversuchen. Commander Riker hat einen neuen Zugangscode. Er hat darum
                gebeten, dass er mehr als vier Ziffern hat. Ich habe das mit einer gewissen
                Begeisterung genehmigt.
                """));

    private const string VulnerableCode = """
        ```csharp minimal
        app.MapPost("/password-reset/request", (ResetRequest body) =>
        {
            var code = Random.Shared.Next(0, 10_000).ToString("D4");
            pendingCodes[body.User] = code;
            // In a real system this would be sent by email or SMS.
            return Results.Ok(new { message = $"A 4-digit code was sent to {body.User}." });
        });

        app.MapPost("/password-reset/confirm", (ConfirmRequest body) =>
        {
            if (pendingCodes.TryGetValue(body.User, out var expected) && body.Code == expected)
            {
                pendingCodes.Remove(body.User);
                return Results.Ok(new { message = "Access code has been reset." });
            }
            return Results.Json(new { error = "Invalid code." }, statusCode: 400);
        });
        ```

        ```csharp controller
        [ApiController]
        [Route("password-reset")]
        public class PasswordResetController(ResetCodeStore pendingCodes) : ControllerBase
        {
            [HttpPost("request")]
            public IActionResult RequestReset(ResetRequest body)
            {
                var code = Random.Shared.Next(0, 10_000).ToString("D4");
                pendingCodes[body.User] = code;
                // In a real system this would be sent by email or SMS.
                return Ok(new { message = $"A 4-digit code was sent to {body.User}." });
            }

            [HttpPost("confirm")]
            public IActionResult Confirm(ConfirmRequest body)
            {
                if (pendingCodes.TryGetValue(body.User, out var expected) && body.Code == expected)
                {
                    pendingCodes.Remove(body.User);
                    return Ok(new { message = "Access code has been reset." });
                }
                return BadRequest(new { error = "Invalid code." });
            }
        }
        ```
        """;

    public Text ScenarioMarkdown => new(
        En: $$"""
            The ship's password reset works in two steps. First, request a reset for a user.
            The system generates a **4-digit code** (0000–9999). Then confirm the reset by
            sending the user and the code. There is no limit on how many codes you can try.

            | Endpoint | What it does |
            |---|---|
            | `POST /api/labs/brute-force/password-reset/request` | Request a reset (body: `{"user": "riker"}`) |
            | `POST /api/labs/brute-force/password-reset/confirm` | Confirm the reset (body: `{"user": "riker", "code": "1234"}`) |

            The reset endpoints look like this:

            {{VulnerableCode}}

            **Goal:** request a reset for `riker`, then find the code. The flag is returned
            when you confirm with the correct code. The Repeater tool above the console can
            try all ten thousand codes for you.
            """,
        De: $$"""
            Das Passwort-Reset-System des Schiffs funktioniert in zwei Schritten. Zuerst einen
            Reset für einen Benutzer anfordern. Das System erzeugt einen **vierstelligen Code**
            (0000–9999). Dann den Reset bestätigen, indem du den Benutzer und den Code schickst.
            Es gibt keine Begrenzung der Versuche.

            | Endpunkt | Funktion |
            |---|---|
            | `POST /api/labs/brute-force/password-reset/request` | Reset anfordern (Body: `{"user": "riker"}`) |
            | `POST /api/labs/brute-force/password-reset/confirm` | Reset bestätigen (Body: `{"user": "riker", "code": "1234"}`) |

            Die Reset-Endpunkte sehen so aus:

            {{VulnerableCode}}

            **Ziel:** Fordere einen Reset für `riker` an und finde den Code. Die Flag wird
            zurückgegeben, wenn du mit dem richtigen Code bestätigst. Das Repeater-Tool über
            der Konsole kann alle zehntausend Codes für dich durchprobieren.
            """);

    public IReadOnlyList<Text> Hints =>
    [
        new("`POST /password-reset/request` with `\"user\": \"riker\"` generates a code. I observe that the response does not reveal the code itself, only that one was created. The code has four decimal digits, which means exactly ten thousand possibilities.",
            "`POST /password-reset/request` mit `\"user\": \"riker\"` erzeugt einen Code. Ich stelle fest, dass die Antwort den Code selbst nicht verrät, nur dass einer erzeugt wurde. Der Code hat vier Dezimalziffern, also genau zehntausend Möglichkeiten."),
        new("`POST /password-reset/confirm` returns 400 for every wrong code but does not invalidate it. You may try as many codes as you wish; the correct one will still be accepted on the ten-thousandth attempt.",
            "`POST /password-reset/confirm` liefert bei jedem falschen Code eine 400-Antwort, macht ihn aber nicht ungültig. Sie können beliebig viele Codes probieren. Der richtige wird auch beim zehntausendsten Versuch noch akzeptiert."),
        new("The Repeater sends `POST /password-reset/confirm` for every number from 0000 to 9999 and stops when it receives a 200 response. I recommend setting it to the confirm endpoint with the body template `§NUM§` as the code.",
            "Der Repeater schickt `POST /password-reset/confirm` für jede Nummer von 0000 bis 9999 und stoppt bei einer 200-Antwort. Ich empfehle, ihn auf den Confirm-Endpunkt mit dem Body-Template `§NUM§` als Code einzustellen."),
    ];

    public IReadOnlyList<LabTool> Tools => [LabTool.Repeater];

    public RequestTemplate DefaultRequest => new(
        "POST",
        $"{LabRegistry.ApiPrefix}{Slug}/password-reset/confirm",
        new Dictionary<string, string> { ["Authorization"] = $"Bearer {auth.GetToken(Slug, DemoAuth.Player)}" },
        """{ "user": "riker", "code": "§NUM§" }""");

    public IReadOnlyList<FixOption> FixOptions =>
    [
        new("attempt-limit", """
            app.MapPost("/password-reset/confirm", (ConfirmRequest body) =>
            {
                if (!pendingCodes.TryGetValue(body.User, out var entry))
                    return Results.Json(new { error = "No reset pending." }, statusCode: 400);

                if (entry.Attempts >= 3)
                {
                    pendingCodes.Remove(body.User);
                    return Results.Json(new { error = "Too many attempts. Request a new code." }, statusCode: 429);
                }

                entry.Attempts++;

                if (body.Code != entry.Code)
                    return Results.Json(new { error = "Invalid code." }, statusCode: 400);

                pendingCodes.Remove(body.User);
                return Results.Ok(new { message = "Access code has been reset." });
            });
            """, IsCorrect: true,
            new("Correct. After three wrong attempts the code is invalidated. Combined with a longer, random " +
                "code and a rate limit per account, brute force becomes impractical. The attacker would need to " +
                "request a new code each time, and each one expires in three tries.",
                "Richtig. Nach drei Fehlversuchen wird der Code ungültig. In Kombination mit einem längeren, " +
                "zufälligen Code und einem Rate-Limit pro Konto wird Brute Force unpraktikabel. Der Angreifer " +
                "müsste jedes Mal einen neuen Code anfordern, und jeder verfällt nach drei Versuchen."),
            BarclayLine: new(
                "W-what if the code just stops working after a few wrong tries? And maybe a longer code, too. S-six digits? Eight? I always forget mine anyway.",
                "U-und wenn der Code nach ein paar Fehlversuchen einfach nicht mehr geht? Und v-vielleicht ein längerer Code. S-sechs Stellen? Acht? Ich vergesse meinen sowieso immer."),
            ControllerCode: """
            [ApiController]
            [Route("password-reset")]
            public class PasswordResetController(ResetCodeStore pendingCodes) : ControllerBase
            {
                [HttpPost("confirm")]
                public IActionResult Confirm(ConfirmRequest body)
                {
                    if (!pendingCodes.TryGetValue(body.User, out var entry))
                        return BadRequest(new { error = "No reset pending." });

                    if (entry.Attempts >= 3)
                    {
                        pendingCodes.Remove(body.User);
                        return StatusCode(429, new { error = "Too many attempts. Request a new code." });
                    }

                    entry.Attempts++;

                    if (body.Code != entry.Code)
                        return BadRequest(new { error = "Invalid code." });

                    pendingCodes.Remove(body.User);
                    return Ok(new { message = "Access code has been reset." });
                }
            }
            """),

        new("forwarded-for-limit", """
            app.MapPost("/password-reset/confirm", (ConfirmRequest body, HttpContext ctx) =>
            {
                var ip = ctx.Request.Headers["X-Forwarded-For"].FirstOrDefault()
                         ?? ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                if (rateLimiter.IsBlocked(ip))
                    return Results.Json(new { error = "Rate limit exceeded." }, statusCode: 429);

                rateLimiter.Record(ip);

                if (pendingCodes.TryGetValue(body.User, out var code) && body.Code == code)
                {
                    pendingCodes.Remove(body.User);
                    return Results.Ok(new { message = "Access code has been reset." });
                }
                return Results.Json(new { error = "Invalid code." }, statusCode: 400);
            });
            """, IsCorrect: false,
            new("The rate limiter counts by IP address taken from the `X-Forwarded-For` header. That header is " +
                "set by the client. An attacker sends a different fake IP with every request and never hits the limit.",
                "Das Rate-Limit zählt nach IP-Adresse, und die kommt aus dem `X-Forwarded-For`-Header. Diesen " +
                "Header setzt der Client. Ein Angreifer schickt bei jedem Request eine andere gefälschte IP mit " +
                "und erreicht das Limit nie."),
            BarclayLine: new(
                "I-I'll add a rate limiter. By IP address. From the `X-Forwarded-For` header, that's what it's for. R-right? Nobody would fake their own address.",
                "I-ich baue ein Rate-Limit ein. Nach IP-Adresse. Die steht ja im `X-Forwarded-For`-Header, d-dafür ist der da. Oder? Die eigene Adresse fälscht doch keiner."),
            ControllerCode: """
            [ApiController]
            [Route("password-reset")]
            public class PasswordResetController(ResetCodeStore pendingCodes, IpRateLimiter rateLimiter) : ControllerBase
            {
                [HttpPost("confirm")]
                public IActionResult Confirm(ConfirmRequest body)
                {
                    var ip = Request.Headers["X-Forwarded-For"].FirstOrDefault()
                             ?? HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                    if (rateLimiter.IsBlocked(ip))
                        return StatusCode(429, new { error = "Rate limit exceeded." });

                    rateLimiter.Record(ip);

                    if (pendingCodes.TryGetValue(body.User, out var code) && body.Code == code)
                    {
                        pendingCodes.Remove(body.User);
                        return Ok(new { message = "Access code has been reset." });
                    }
                    return BadRequest(new { error = "Invalid code." });
                }
            }
            """),

        new("form-captcha", """
            @* PasswordReset.razor *@
            <EditForm Model="model" OnValidSubmit="ConfirmAsync">
                <InputText @bind-Value="model.Code" placeholder="4-digit code" />

                <div class="captcha-box">
                    <img src="/captcha/image" alt="CAPTCHA" />
                    <InputText @bind-Value="model.Captcha" placeholder="Enter the text above" />
                </div>

                <button type="submit">Confirm reset</button>
            </EditForm>
            """, IsCorrect: false,
            new("The CAPTCHA is on the Blazor form, but the API has no CAPTCHA check. You never used the form " +
                "in this mission: you sent requests straight to the endpoint, and it still accepts every guess.",
                "Das CAPTCHA ist im Blazor-Formular, aber die API prüft kein CAPTCHA. Du hast das Formular in " +
                "dieser Mission nie benutzt. Du hast die Requests direkt an den Endpunkt geschickt, und der " +
                "nimmt weiterhin jeden Versuch an."),
            BarclayLine: new(
                "C-could we add a CAPTCHA? To the form. Those squiggly letters. N-nobody can read those. Well, I can't.",
                "K-könnten wir ein CAPTCHA einbauen? Ins Formular. Diese krakeligen Buchstaben. D-die kann doch keiner lesen. Also ich jedenfalls nicht."),
            Language: "razor"),

        new("delay-on-failure", """
            app.MapPost("/password-reset/confirm", async (ConfirmRequest body) =>
            {
                if (pendingCodes.TryGetValue(body.User, out var code) && body.Code == code)
                {
                    pendingCodes.Remove(body.User);
                    return Results.Ok(new { message = "Access code has been reset." });
                }

                await Task.Delay(500); // slow down brute force
                return Results.Json(new { error = "Invalid code." }, statusCode: 400);
            });
            """, IsCorrect: false,
            new("The delay runs on the server, one request at a time. An attacker sends hundreds of requests in " +
                "parallel, and each one waits its own 500 milliseconds independently. Ten thousand parallel " +
                "requests finish in half a second.",
                "Die Verzögerung läuft auf dem Server, ein Request nach dem anderen. Ein Angreifer schickt " +
                "hunderte Requests parallel, und jeder wartet seine eigenen 500 Millisekunden unabhängig " +
                "voneinander. Zehntausend parallele Requests sind in einer halben Sekunde durch."),
            BarclayLine: new(
                "I-I'll add a half-second delay when the code is wrong. That slows them down. F-five thousand seconds for ten thousand tries. That's… a lot. Right?",
                "I-ich baue eine halbe Sekunde Verzögerung ein, wenn der Code falsch ist. D-das bremst die aus. Fünftausend Sekunden für zehntausend Versuche. Das ist … viel. Oder?"),
            ControllerCode: """
            [ApiController]
            [Route("password-reset")]
            public class PasswordResetController(ResetCodeStore pendingCodes) : ControllerBase
            {
                [HttpPost("confirm")]
                public async Task<IActionResult> Confirm(ConfirmRequest body)
                {
                    if (pendingCodes.TryGetValue(body.User, out var code) && body.Code == code)
                    {
                        pendingCodes.Remove(body.User);
                        return Ok(new { message = "Access code has been reset." });
                    }

                    await Task.Delay(500); // slow down brute force
                    return BadRequest(new { error = "Invalid code." });
                }
            }
            """),
    ];

    public Text ExplanationMarkdown => new(
        En: """
            **Brute force** works when a secret has few possible values and nothing limits
            how many times you can guess. A 4-digit code has 10,000 possibilities. Without an
            attempt limit, an attacker tries them all in seconds.

            The fix combines three things:
            1. **Invalidate the code after a few wrong attempts** so the attacker must request
               a new one each time (and cannot just resume where they left off).
            2. **Use a longer, random code** (6–8 digits, or a random string) so the search
               space is too large to exhaust within the attempt limit.
            3. **Rate-limit per account**, not per IP, so the attacker cannot split the work
               across addresses.

            Why the other options fail:
            - **`X-Forwarded-For` rate limit** trusts a header the client controls. Every request gets a fresh IP.
            - **CAPTCHA in the form only** does not protect the API, which is the actual target.
            - **`Task.Delay` on failure** only slows sequential requests. Parallel requests each wait independently.

            Read more:
            - [OWASP: Brute Force Attack](https://owasp.org/www-community/attacks/Brute_force_attack)
            - [OWASP: Credential Stuffing Prevention Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Credential_Stuffing_Prevention_Cheat_Sheet.html)
            """,
        De: """
            **Brute Force** funktioniert, wenn ein Geheimnis nur wenige mögliche Werte hat und
            nichts die Anzahl der Versuche begrenzt. Ein vierstelliger Code hat 10.000 Möglichkeiten.
            Ohne Versuchslimit probiert ein Angreifer alle in Sekunden durch.

            Die Lösung kombiniert drei Dinge:
            1. **Den Code nach wenigen Fehlversuchen ungültig machen**, damit der Angreifer jedes
               Mal einen neuen anfordern muss und nicht einfach weitermachen kann.
            2. **Einen längeren, zufälligen Code verwenden** (6–8 Stellen oder einen zufälligen
               String), damit der Suchraum zu groß ist, um ihn innerhalb des Versuchslimits
               durchzuprobieren.
            3. **Rate-Limit pro Konto**, nicht pro IP, damit der Angreifer die Arbeit nicht auf
               verschiedene Adressen verteilen kann.

            Warum die anderen Optionen nicht helfen:
            - **Rate-Limit per `X-Forwarded-For`** vertraut einem Header, den der Client selbst setzt. Jeder Request bekommt eine neue IP.
            - **CAPTCHA nur im Formular** schützt die API nicht, und die ist das eigentliche Ziel.
            - **`Task.Delay` bei Fehlern** bremst nur sequenzielle Requests. Parallele Requests warten jeweils unabhängig.

            Mehr dazu:
            - [OWASP: Brute Force Attack](https://owasp.org/www-community/attacks/Brute_force_attack)
            - [OWASP: Credential Stuffing Prevention Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Credential_Stuffing_Prevention_Cheat_Sheet.html)
            """);

    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        auth.GetToken(Slug, DemoAuth.Player);
        auth.GetToken(Slug, "riker");
        Reset();

        app.MapPost("/password-reset/request", async (HttpRequest request) =>
        {
            if (auth.FindUser(Slug, request) is null) return Results.Unauthorized();
            var language = Languages.FromRequest(request);

            ResetRequestBody? body;
            try
            {
                body = await JsonSerializer.DeserializeAsync<ResetRequestBody>(request.Body, Json);
            }
            catch (JsonException ex)
            {
                return Results.BadRequest(new { error = $"Invalid JSON: {ex.Message}" });
            }
            if (body is null || string.IsNullOrWhiteSpace(body.User))
                return Results.BadRequest(new { error = new Text("Body must contain \"user\".", "Body muss \"user\" enthalten.")[language] });

            var code = Random.Shared.Next(0, 10_000).ToString("D4");
            lock (_gate) _codes[body.User.ToLowerInvariant()] = code;

            return Results.Ok(new
            {
                message = new Text(
                    $"A 4-digit reset code was sent to {body.User}.",
                    $"Ein vierstelliger Reset-Code wurde an {body.User} gesendet.")[language],
            });
        });

        // VULNERABLE: no attempt limit, so the 4-digit code can be brute-forced.
        app.MapPost("/password-reset/confirm", async (HttpRequest request) =>
        {
            if (auth.FindUser(Slug, request) is null) return Results.Unauthorized();
            var language = Languages.FromRequest(request);

            ConfirmRequestBody? body;
            try
            {
                body = await JsonSerializer.DeserializeAsync<ConfirmRequestBody>(request.Body, Json);
            }
            catch (JsonException ex)
            {
                return Results.BadRequest(new { error = $"Invalid JSON: {ex.Message}" });
            }
            if (body is null || string.IsNullOrWhiteSpace(body.User) || string.IsNullOrWhiteSpace(body.Code))
                return Results.BadRequest(new { error = new Text("Body must contain \"user\" and \"code\".", "Body muss \"user\" und \"code\" enthalten.")[language] });

            var user = body.User.ToLowerInvariant();
            lock (_gate)
            {
                if (!_codes.TryGetValue(user, out var expected))
                    return Results.Json(
                        new { error = new Text("No reset pending for this user.", "Kein Reset für diesen Benutzer ausstehend.")[language] },
                        statusCode: StatusCodes.Status400BadRequest);

                if (body.Code != expected)
                    return Results.Json(
                        new { error = new Text("Invalid code.", "Ungültiger Code.")[language] },
                        statusCode: StatusCodes.Status400BadRequest);

                _codes.Remove(user);
            }

            return Results.Ok(new
            {
                message = new Text(
                    "Access code has been reset successfully.",
                    "Zugangscode wurde erfolgreich zurückgesetzt.")[language],
                flag = flags.GetFlag(Slug),
            });
        });
    }

    public void Reset()
    {
        lock (_gate) _codes = [];
    }

    private sealed record ResetRequestBody(string? User);
    private sealed record ConfirmRequestBody(string? User, string? Code);
}
