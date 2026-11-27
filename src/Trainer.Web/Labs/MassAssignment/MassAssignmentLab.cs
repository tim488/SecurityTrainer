using System.Text.Json;
using Trainer.Web.Core;

namespace Trainer.Web.Labs.MassAssignment;

/// <summary>The stored crew account, bound straight from the request body (that is the bug).</summary>
public sealed record CrewUser(int Id, string? DisplayName, string? Email, bool IsAdmin, int Credits);

/// <summary>
/// 2.1 Mass assignment: <c>PUT /profile</c> binds the body to the stored
/// <see cref="CrewUser"/> and copies every property, so <c>"isAdmin": true</c> sticks.
/// <c>GET /admin/secret</c> returns the flag to admins.
/// </summary>
public sealed partial class MassAssignmentLab(FlagService flags, DemoAuth auth) : ILab
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly Lock _gate = new();
    private Dictionary<string, CrewUser> _users = [];

    public string Slug => "mass-assignment";
    public string Category => "API";
    public Difficulty Difficulty => Difficulty.Easy;

    public RequestTemplate DefaultRequest => new(
        "GET",
        $"{LabRegistry.ApiPrefix}{Slug}/profile",
        new Dictionary<string, string> { ["Authorization"] = $"Bearer {auth.GetToken(Slug, DemoAuth.Player)}" },
        null);

    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        auth.GetToken(Slug, DemoAuth.Player);
        auth.GetToken(Slug, "riker");
        Reset();

        app.MapGet("/profile", (HttpRequest request) =>
        {
            if (auth.FindUser(Slug, request) is not { } user) return Results.Unauthorized();
            lock (_gate) return Results.Ok(_users[user.Name]);
        });

        // VULNERABLE: every property of the body is copied onto the stored user.
        app.MapPut("/profile", async (HttpRequest request) =>
        {
            if (auth.FindUser(Slug, request) is not { } user) return Results.Unauthorized();
            CrewUser? input;
            try
            {
                input = await JsonSerializer.DeserializeAsync<CrewUser>(request.Body, Json);
            }
            catch (JsonException ex)
            {
                return Results.BadRequest(new { error = $"Invalid JSON: {ex.Message}" });
            }
            if (input is null) return Results.BadRequest(new { error = "Body required." });

            lock (_gate)
            {
                var updated = input with { Id = _users[user.Name].Id };   // never change the key
                _users[user.Name] = updated;
                return Results.Ok(updated);
            }
        });

        app.MapGet("/crew", (HttpRequest request) =>
        {
            if (auth.FindUser(Slug, request) is null) return Results.Unauthorized();
            lock (_gate) return Results.Ok(_users.Values.OrderBy(u => u.Id).Select(u => new { u.DisplayName, u.IsAdmin }).ToList());
        });

        app.MapGet("/admin/secret", (HttpRequest request) =>
        {
            if (auth.FindUser(Slug, request) is not { } user) return Results.Unauthorized();
            var language = Languages.FromRequest(request);
            lock (_gate)
            {
                if (!_users[user.Name].IsAdmin)
                    return Results.Json(new { error = new Text("Admins only.", "Nur für Admins.")[language] }, statusCode: StatusCodes.Status403Forbidden);
            }
            return Results.Ok(new
            {
                Channel = new Text("Starfleet Command, priority one", "Sternenflottenkommando, Priorität eins")[language],
                Message = new Text(
                    "Enterprise, confirm receipt of the new command codes. And please explain who Admiral L. Soong is. Nobody here has heard of him.",
                    "Enterprise, bitte bestätigen Sie den Empfang der neuen Kommandocodes. Und erklären Sie uns bitte, wer Admiral L. Soong ist. Hier hat noch keiner von ihm gehört.")[language],
                AuthCode = flags.GetFlag(Slug),
            });
        });
    }

    public void Reset()
    {
        lock (_gate)
        {
            _users = new()
            {
                ["picard"] = new(1, "Jean-Luc Picard", "picard@enterprise.starfleet", true, 0),
                ["riker"] = new(2, "William T. Riker", "riker@enterprise.starfleet", true, 2_300),
                ["lsoong"] = new(3, "Admiral L. Soong", "l.soong@enterprise.starfleet", true, 999_999),
                ["barclay"] = new(4, "Reginald Barclay", "barclay@enterprise.starfleet", false, 85),
                [DemoAuth.Player] = new(7, "Wesley Crusher", "wesley@enterprise.starfleet", false, 120),
            };
        }
    }
}
