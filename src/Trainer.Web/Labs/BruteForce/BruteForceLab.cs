using System.Text.Json;
using Trainer.Web.Core;

namespace Trainer.Web.Labs.BruteForce;

/// <summary>
/// 2.3 Brute force: <c>POST /password-reset/request</c> generates a 4-digit
/// code for a user. <c>POST /password-reset/confirm</c> accepts user + code with no
/// attempt limit, so the code can be brute-forced over 0000–9999. The flag is returned
/// on a successful confirm.
/// </summary>
public sealed partial class BruteForceLab(FlagService flags, DemoAuth auth) : ILab
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly Lock _gate = new();
    private Dictionary<string, string> _codes = [];

    public string Slug => "brute-force";
    public string Category => "API";
    public Difficulty Difficulty => Difficulty.Medium;

    public IReadOnlyList<LabTool> Tools => [LabTool.Repeater];

    public RequestTemplate DefaultRequest => new(
        "POST",
        $"{LabRegistry.ApiPrefix}{Slug}/password-reset/confirm",
        new Dictionary<string, string> { ["Authorization"] = $"Bearer {auth.GetToken(Slug, DemoAuth.Player)}" },
        """{ "user": "riker", "code": "§NUM§" }""");

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
