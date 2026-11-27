using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Trainer.Web.Core;

namespace Trainer.Web.Missions.Jwt;

/// <summary>
/// 3.3 Forged JWT. Wesley gets an HS256-signed JWT with <c>"role": "crew"</c>.
/// Token validation mimics JwtBearer with <c>RequireSignedTokens = false</c>: a token with
/// an empty signature is accepted unchecked, a signed one must verify. <c>GET /admin/bridge</c>
/// returns the flag for <c>"role": "admin"</c>. This mission uses JWTs instead of DemoAuth tokens.
/// </summary>
public sealed partial class JwtMission(FlagService flags) : IMission
{
    private const string Issuer = "https://enterprise.local";
    private const string Audience = "bridge-console";

    private readonly Lock _gate = new();
    private byte[] _signingKey = [];
    private string _wesleyToken = "";

    public string Slug => "jwt";
    public string Category => "API";
    public Difficulty Difficulty => Difficulty.Hard;
    public IReadOnlyList<MissionTool> Tools => [MissionTool.JwtEditor];

    public RequestTemplate DefaultRequest
    {
        get
        {
            string token;
            lock (_gate) token = _wesleyToken;
            return new("GET", $"{MissionRegistry.ApiPrefix}{Slug}/me",
                new Dictionary<string, string> { ["Authorization"] = $"Bearer {token}" }, null);
        }
    }

    /// <summary>Ranks the ship issues, translated for the welcome. A forged rank is shown as written.</summary>
    private static readonly Dictionary<string, Text> KnownRanks = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Acting Ensign"] = new("Acting Ensign", "Fähnrich auf Probe"),
    };

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
            rank = KnownRanks.TryGetValue(rank, out var translated) ? translated[language] : rank;
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
