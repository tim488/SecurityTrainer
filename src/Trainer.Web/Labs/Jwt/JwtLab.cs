using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Trainer.Web.Core;

namespace Trainer.Web.Labs.Jwt;

/// <summary>
/// 3.3 Forged JWT (SPEC §6). Wesley gets an HS256-signed JWT with <c>"role": "crew"</c>.
/// Token validation mimics JwtBearer with <c>RequireSignedTokens = false</c>: a token with
/// an empty signature is accepted unchecked, a signed one must verify. <c>GET /admin/bridge</c>
/// returns the flag for <c>"role": "admin"</c>. This lab uses JWTs instead of DemoAuth tokens.
/// </summary>
public sealed class JwtLab(FlagService flags) : ILab
{
    private const string Issuer = "https://enterprise.local";
    private const string Audience = "bridge-console";

    private readonly Lock _gate = new();
    private byte[] _signingKey = [];
    private string _wesleyToken = "";

    public string Slug => "jwt";
    public Text Title => new("Forged Clearance", "Gefälschte Freigabe");
    public string Category => "API";
    public Difficulty Difficulty => Difficulty.Hard;
    public IReadOnlyList<LabTool> Tools => [LabTool.JwtEditor];

    public LabStory Story => new(
        Act: 3,
        Order: 3,
        Briefing: new(
            En: """
                Captain's log, stardate 47331.8. At 0412 hours an Admiral Lore logged into the bridge
                console with full clearance and tried to take the helm. Starfleet has no Admiral Lore.
                The console checks a clearance badge, a signed token every officer receives at login.
                Lore's badge was accepted without question. I have asked Mr. Crusher to find out what
                exactly the console is checking.
                """,
            De: """
                Logbuch des Captains, Sternzeit 47331.8. Um 4:12 Uhr hat sich ein Admiral Lore mit voller
                Freigabe an der Brückenkonsole angemeldet und versucht, das Steuer zu übernehmen. Einen Admiral
                Lore gibt es in der Sternenflotte nicht. Die Konsole prüft einen Freigabe-Ausweis, ein signiertes
                Token, das jeder Offizier bei der Anmeldung bekommt. Lores Ausweis wurde ohne Rückfrage
                akzeptiert. Ich habe Mr. Crusher gebeten herauszufinden, was die Konsole eigentlich prüft.
                """),
        CallingCard: new(
            En: """
                An admiral at last, and the chair is even more comfortable than it looks. You can have
                the console back, Wesley. I've found something better. The core fetches link previews for
                every subspace message, from anywhere I ask. Anywhere at all. Even from inside itself. — L.
                """,
            De: """
                Endlich Admiral, und der Sessel ist noch bequemer, als er aussieht. Die Konsole kannst du
                wiederhaben, Wesley. Ich hab' was Besseres gefunden. Der Bordcomputer holt für jede
                Subraumnachricht eine Link-Vorschau, von überall, wo ich will. Wirklich überall. Sogar aus
                sich selbst. — L.
                """),
        Debrief: new(
            En: """
                Good work, Mr. Crusher. The bridge console now accepts only badges signed with our key,
                issued by us, for this ship. Admiral Lore's clearance has been revoked. I am told your own
                forged badge still said "acting ensign". Modest. I approve.
                """,
            De: """
                Gute Arbeit, Mr. Crusher. Die Brückenkonsole akzeptiert jetzt nur noch Ausweise, die mit unserem
                Schlüssel signiert und von uns für dieses Schiff ausgestellt wurden. Admiral Lores Freigabe ist
                entzogen. Man sagt mir, auf Ihrem gefälschten Ausweis stand weiterhin „Fähnrich auf Probe“.
                Bescheiden. Das gefällt mir.
                """));

    private const string VulnerableCode = """
        ```csharp
        builder.Services.AddAuthentication().AddJwtBearer(o =>
        {
            o.TokenValidationParameters = new TokenValidationParameters
            {
                RequireSignedTokens = false,        // "the old consoles send unsigned tokens"
                ValidateIssuerSigningKey = false,
                IssuerSigningKey = new SymmetricSecurityKey(signingKey),
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
            };
        });

        app.MapGet("/me", (ClaimsPrincipal user) => user.Claims.Select(c => new { c.Type, c.Value }))
           .RequireAuthorization();

        app.MapGet("/admin/bridge", () => Results.Ok(BridgeConsole.Open()))
           .RequireAuthorization(p => p.RequireRole("admin"));
        ```
        """;

    public Text ScenarioMarkdown => new(
        En: $"""
            Every officer gets a **JWT** at login: a signed token whose payload says who they are and
            which role they have. The bridge console reads the role from it.

            | Endpoint | What it does |
            |---|---|
            | `GET /api/labs/jwt/me` | The claims in your token, as the console sees them |
            | `GET /api/labs/jwt/admin/bridge` | The bridge console (role `admin` only) |

            Your JWT (role `crew`) is already in the request console. Below the console there is a
            **JWT editor** that decodes a token, lets you edit header and payload, and encodes it again.
            It cannot sign anything, because it does not know the key. The token validation is set up
            like this:

            {VulnerableCode}

            **Goal:** open the bridge console. The flag is its `bridgeCode`.
            """,
        De: $"""
            Jeder Offizier bekommt bei der Anmeldung ein **JWT**: ein signiertes Token, in dessen Payload
            steht, wer er ist und welche Rolle er hat. Die Brückenkonsole liest die Rolle daraus.

            | Endpunkt | Funktion |
            |---|---|
            | `GET /api/labs/jwt/me` | Die Claims in deinem Token, so wie die Konsole sie sieht |
            | `GET /api/labs/jwt/admin/bridge` | Die Brückenkonsole (nur Rolle `admin`) |

            Dein JWT (Rolle `crew`) steht schon in der Request-Konsole. Unter der Konsole findest du einen
            **JWT-Editor**. Er decodiert ein Token, lässt dich Header und Payload bearbeiten und codiert es
            wieder. Signieren kann er nicht, denn den Schlüssel kennt er nicht. Die Prüfung der Tokens ist so
            eingerichtet:

            {VulnerableCode}

            **Ziel:** Öffne die Brückenkonsole. Die Flag ist ihr `bridgeCode`.
            """);

    public IReadOnlyList<Text> Hints =>
    [
        new("Your token has three parts separated by dots: a header, a payload and a signature. The first two are merely Base64url-encoded JSON. The JWT editor below the console decodes them.",
            "Ihr Token besteht aus drei Teilen, getrennt durch Punkte: Header, Payload und Signatur. Die ersten beiden sind nur Base64url-codiertes JSON. Der JWT-Editor unter der Konsole decodiert sie."),
        new("Changing the payload invalidates the signature. However, the configuration sets `RequireSignedTokens = false`. A token without any signature is therefore not checked at all.",
            "Wenn Sie die Payload ändern, passt die Signatur nicht mehr. Die Konfiguration setzt allerdings `RequireSignedTokens = false`. Ein Token ganz ohne Signatur wird also gar nicht erst geprüft."),
        new("Set `\"role\": \"admin\"` in the payload and `\"alg\": \"none\"` in the header, remove the signature, use the token and call `GET /admin/bridge`. I would recommend keeping a modest rank. Lore did not.",
            "Setzen Sie in der Payload `\"role\": \"admin\"` und im Header `\"alg\": \"none\"`, entfernen Sie die Signatur, übernehmen Sie das Token und rufen Sie `GET /admin/bridge` auf. Ich empfehle, beim bescheidenen Rang zu bleiben. Lore hat das nicht getan."),
    ];

    public RequestTemplate DefaultRequest
    {
        get
        {
            string token;
            lock (_gate) token = _wesleyToken;
            return new("GET", $"{LabRegistry.ApiPrefix}{Slug}/me",
                new Dictionary<string, string> { ["Authorization"] = $"Bearer {token}" }, null);
        }
    }

    public IReadOnlyList<FixOption> FixOptions =>
    [
        new("require-signed", """
            builder.Services.AddAuthentication().AddJwtBearer(o =>
            {
                o.TokenValidationParameters = new TokenValidationParameters
                {
                    RequireSignedTokens = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(signingKey),
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                    ValidIssuer = "https://enterprise.local",
                    ValidAudience = "bridge-console",
                    ValidateLifetime = true,
                };
            });
            """, IsCorrect: true,
            new("Correct. Every token now needs a valid HS256 signature made with the ship's key, and it must come from " +
                "our issuer for our audience. An edited payload no longer matches its signature, and an unsigned token is rejected outright. " +
                "These are also the defaults, so the safest change is to delete the overrides.",
                "Richtig. Jedes Token braucht jetzt eine gültige HS256-Signatur mit dem Schlüssel des Schiffs und muss von unserem Aussteller für unsere Zielgruppe stammen. Eine geänderte Payload passt nicht mehr zur Signatur, und ein Token ohne Signatur wird sofort abgelehnt. Das sind übrigens die Standardwerte. Am sichersten ist es also, die abweichenden Einstellungen einfach zu löschen."),
            BarclayLine: new(
                "W-we could tell the console to insist on a signature. Our key, our algorithm, our issuer. That's a lot of settings. I-I'd better double-check them all.",
                "W-wir könnten der Konsole sagen, dass sie auf einer Signatur besteht. Unser Schlüssel, unser Algorithmus, unser Aussteller. D-das sind viele Einstellungen. Die prüfe ich lieber alle zweimal.")),

        new("reject-alg-none", """
            o.Events = new JwtBearerEvents
            {
                OnMessageReceived = ctx =>
                {
                    var token = ctx.Request.Headers.Authorization.ToString()["Bearer ".Length..];
                    var header = Base64UrlEncoder.Decode(token.Split('.')[0]);

                    if (header.Contains("\"alg\":\"none\""))
                        ctx.Fail("alg none is not allowed");

                    return Task.CompletedTask;
                }
            };
            // RequireSignedTokens is still false.
            """, IsCorrect: false,
            new("A blocklist, and a leaky one. Keep `\"alg\": \"HS256\"` and just drop the signature: with " +
                "`RequireSignedTokens = false` an empty signature still counts as \"unsigned, fine\". " +
                "`\"alg\": \"None\"` or a space after the colon also slips past the string check.",
                "Eine Sperrliste, und eine undichte noch dazu. Lass einfach `\"alg\": \"HS256\"` stehen und entferne nur die Signatur: Mit `RequireSignedTokens = false` gilt eine leere Signatur weiterhin als „unsigniert, passt schon“. Auch `\"alg\": \"None\"` oder ein Leerzeichen nach dem Doppelpunkt rutscht an der String-Prüfung vorbei."),
            BarclayLine: new(
                "The trick is `alg: none`, sir. So we block `none`. N-no more none.",
                "Der Trick ist `alg: none`, Sir. A-also sperren wir `none`. K-kein none mehr.")),

        new("shorter-lifetime", """
            var token = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(5),   // was: 8 hours
                SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256),
            };
            """, IsCorrect: false,
            new("The lifetime is a claim in the payload, and the attacker writes the payload. A forged token simply sets " +
                "its own `exp`, or a fresh one every five minutes. Short lifetimes limit stolen tokens, not forged ones.",
                "Die Gültigkeit ist ein Claim in der Payload, und die Payload schreibt der Angreifer selbst. Ein gefälschtes Token setzt einfach sein eigenes `exp`, oder alle fünf Minuten ein neues. Kurze Laufzeiten helfen gegen gestohlene Tokens, nicht gegen gefälschte."),
            BarclayLine: new(
                "If badges only last five minutes, a fake one is useless after five minutes. R-right? That's how it works?",
                "Wenn Ausweise nur fünf Minuten gelten, ist ein gefälschter nach fünf Minuten nutzlos. O-oder? So funktioniert das doch?")),

        new("role-from-db", """
            o.Events = new JwtBearerEvents
            {
                OnTokenValidated = async ctx =>
                {
                    var db = ctx.HttpContext.RequestServices.GetRequiredService<CrewDb>();
                    var sub = ctx.Principal!.FindFirstValue("sub");
                    var officer = await db.Officers.FindAsync(sub);

                    var identity = (ClaimsIdentity)ctx.Principal.Identity!;
                    foreach (var role in identity.FindAll(ClaimTypes.Role).ToList())
                        identity.RemoveClaim(role);
                    if (officer is not null)
                        identity.AddClaim(new Claim(ClaimTypes.Role, officer.Role));
                }
            };
            """, IsCorrect: false,
            new("The role is now trustworthy, but the identity is not. The `sub` claim is forged just as easily: set " +
                "`\"sub\": \"picard\"` and the database hands you the Captain's admin role.",
                "Die Rolle ist jetzt vertrauenswürdig, die Identität aber nicht. Der Claim `sub` lässt sich genauso leicht fälschen: Setz `\"sub\": \"picard\"`, und die Datenbank gibt dir die Admin-Rolle des Captains."),
            BarclayLine: new(
                "We don't trust the role in the badge. We l-look it up in the crew database instead. By the name on the badge.",
                "W-wir glauben der Rolle im Ausweis einfach nicht und schlagen sie in der Crew-Datenbank nach. Über den Namen auf dem Ausweis.")),
    ];

    public Text ExplanationMarkdown => new(
        En: """
            A **JWT** is only as trustworthy as its signature. Header and payload are plain
            Base64url-encoded JSON that anyone can read and change. The signature is what proves
            that the ship issued this exact token. With `RequireSignedTokens = false`, a token
            without a signature was simply believed, so you could write your own role.

            Don't weaken token validation. The JwtBearer defaults already require a signed token
            and check the key, issuer, audience and lifetime. Pin the algorithm with
            `ValidAlgorithms`, keep the signing key secret (user secrets, Key Vault), and never
            accept unsigned tokens "for old clients".

            Why the other options fail:
            - **Blocking `alg: none`** misses unsigned tokens with another `alg`, and spelling variants.
            - **Shorter lifetimes** don't matter when the attacker writes `exp` himself.
            - **Looking up the role by `sub`** still trusts a forged identity.

            Read more:
            - [OWASP: JSON Web Token Cheat Sheet for Java (concepts apply to .NET)](https://cheatsheetseries.owasp.org/cheatsheets/JSON_Web_Token_for_Java_Cheat_Sheet.html)
            - [Microsoft Docs: Configure JWT bearer authentication in ASP.NET Core](https://learn.microsoft.com/aspnet/core/security/authentication/configure-jwt-bearer-authentication)
            """,
        De: """
            Ein **JWT** ist nur so vertrauenswürdig wie seine Signatur. Header und Payload sind einfaches
            Base64url-codiertes JSON, das jeder lesen und ändern kann. Erst die Signatur beweist, dass das
            Schiff genau dieses Token ausgestellt hat. Mit `RequireSignedTokens = false` wurde einem Token
            ohne Signatur einfach geglaubt, und so konntest du dir deine Rolle selbst aussuchen.

            Schwäche die Prüfung der Tokens nicht ab. Die Standardwerte von JwtBearer verlangen schon ein
            signiertes Token und prüfen Schlüssel, Aussteller, Zielgruppe und Gültigkeit. Leg den Algorithmus
            mit `ValidAlgorithms` fest, halte den Signaturschlüssel geheim (User Secrets, Key Vault) und
            akzeptiere nie unsignierte Tokens, auch nicht „für alte Clients“.

            Warum die anderen Optionen nicht helfen:
            - **`alg: none` sperren** übersieht unsignierte Tokens mit einem anderen `alg` und andere Schreibweisen.
            - **Kürzere Laufzeiten** spielen keine Rolle, wenn der Angreifer `exp` selbst schreibt.
            - **Die Rolle über `sub` nachschlagen** vertraut weiterhin einer gefälschten Identität.

            Mehr dazu:
            - [OWASP: JSON Web Token Cheat Sheet (die Konzepte gelten auch für .NET)](https://cheatsheetseries.owasp.org/cheatsheets/JSON_Web_Token_for_Java_Cheat_Sheet.html)
            - [Microsoft Docs: JWT-Bearer-Authentifizierung in ASP.NET Core konfigurieren](https://learn.microsoft.com/de-de/aspnet/core/security/authentication/configure-jwt-bearer-authentication)
            """);

    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        Reset();

        app.MapGet("/me", (HttpRequest request) =>
            Validate(request, out var claims, out var error)
                ? Results.Ok(claims)
                : Results.Json(new { error }, statusCode: StatusCodes.Status401Unauthorized));

        app.MapGet("/admin/bridge", (HttpRequest request) =>
        {
            if (!Validate(request, out var claims, out var error))
                return Results.Json(new { error }, statusCode: StatusCodes.Status401Unauthorized);

            var language = Languages.FromRequest(request);
            if (!HasRole(claims, "admin"))
                return Results.Json(new { error = new Text("Role 'admin' required.", "Rolle 'admin' erforderlich.")[language] },
                    statusCode: StatusCodes.Status403Forbidden);

            var name = claims.TryGetValue("name", out var n) ? n.ToString() : "?";
            var rank = claims.TryGetValue("rank", out var r) ? r.ToString() : "";
            return Results.Ok(new
            {
                Console = new Text("Bridge: helm, tactical, ops", "Brücke: Steuer, Taktik, Einsatz")[language],
                Welcome = new Text($"Welcome to the bridge, {rank} {name}.", $"Willkommen auf der Brücke, {rank} {name}.")[language].Replace("  ", " "),
                BridgeCode = flags.GetFlag(Slug),
            });
        });
    }

    /// <summary>
    /// VULNERABLE validation, as with <c>RequireSignedTokens = false</c>: an empty signature
    /// means "unsigned" and is accepted without any check. Signed tokens must verify (HS256).
    /// </summary>
    private bool Validate(HttpRequest request, out Dictionary<string, JsonElement> claims, out string error)
    {
        claims = [];
        var header = request.Headers.Authorization.ToString();
        if (!header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            error = "Bearer token required.";
            return false;
        }

        var parts = header["Bearer ".Length..].Trim().Split('.');
        if (parts.Length != 3 || Base64Url.DecodeString(parts[0]) is not { } headerJson || Base64Url.DecodeString(parts[1]) is not { } payloadJson)
        {
            error = "IDX14100: JWT is not well formed.";
            return false;
        }

        try
        {
            using var headerDoc = JsonDocument.Parse(headerJson);
            using var payloadDoc = JsonDocument.Parse(payloadJson);
            var alg = headerDoc.RootElement.TryGetProperty("alg", out var a) ? a.GetString() : null;

            if (parts[2].Length > 0)
            {
                if (alg != "HS256")
                {
                    error = $"IDX10511: Signature validation failed. Unsupported algorithm '{alg}'.";
                    return false;
                }
                byte[] key;
                lock (_gate) key = _signingKey;
                var expected = HMACSHA256.HashData(key, Encoding.ASCII.GetBytes($"{parts[0]}.{parts[1]}"));
                if (!Base64Url.TryDecode(parts[2], out var actual) || !CryptographicOperations.FixedTimeEquals(expected, actual))
                {
                    error = "IDX10511: Signature validation failed.";
                    return false;
                }
            }
            // else: unsigned token, accepted because RequireSignedTokens = false.

            foreach (var property in payloadDoc.RootElement.EnumerateObject())
                claims[property.Name] = property.Value.Clone();
        }
        catch (JsonException)
        {
            error = "IDX14100: JWT is not well formed.";
            return false;
        }

        if (claims.TryGetValue("exp", out var exp) && exp.TryGetInt64(out var seconds)
            && DateTimeOffset.FromUnixTimeSeconds(seconds) < DateTimeOffset.UtcNow)
        {
            error = "IDX10223: Lifetime validation failed. The token is expired.";
            return false;
        }

        error = "";
        return true;
    }

    private static bool HasRole(Dictionary<string, JsonElement> claims, string role) =>
        claims.TryGetValue("role", out var value) && value.ValueKind switch
        {
            JsonValueKind.String => value.GetString() == role,
            JsonValueKind.Array => value.EnumerateArray().Any(v => v.ValueKind == JsonValueKind.String && v.GetString() == role),
            _ => false,
        };

    /// <summary>Issues a signed HS256 token, as the ship's login would.</summary>
    public string IssueToken(object payload)
    {
        var header = Base64Url.Encode("""{"alg":"HS256","typ":"JWT"}""");
        var body = Base64Url.Encode(JsonSerializer.Serialize(payload));
        byte[] key;
        lock (_gate) key = _signingKey;
        var signature = Base64Url.Encode(HMACSHA256.HashData(key, Encoding.ASCII.GetBytes($"{header}.{body}")));
        return $"{header}.{body}.{signature}";
    }

    public void Reset()
    {
        lock (_gate) _signingKey = RandomNumberGenerator.GetBytes(32);
        var now = DateTimeOffset.UtcNow;
        var token = IssueToken(new Dictionary<string, object>
        {
            ["sub"] = DemoAuth.Player,
            ["name"] = "Wesley Crusher",
            ["rank"] = "Acting Ensign",
            ["role"] = "crew",
            ["iss"] = Issuer,
            ["aud"] = Audience,
            ["iat"] = now.ToUnixTimeSeconds(),
            ["exp"] = now.AddDays(30).ToUnixTimeSeconds(),
        });
        lock (_gate) _wesleyToken = token;
    }
}
