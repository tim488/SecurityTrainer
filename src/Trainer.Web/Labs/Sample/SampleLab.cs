using Trainer.Web.Core;

namespace Trainer.Web.Labs.Sample;

/// <summary>
/// PLACEHOLDER LAB. It only exists to exercise the read → exploit → flag → fix
/// flow end-to-end while the real labs (SPEC §6) are being written. There is no
/// real vulnerability here: calling the endpoint with Wesley's token returns the
/// flag. Delete this folder and its line in <see cref="LabRegistry"/> once real
/// labs exist.
/// </summary>
public sealed class SampleLab(FlagService flags, DemoAuth auth) : ILab
{
    public string Slug => "sample";
    public string Title => "Placeholder: Hello, Request Console";
    public string Category => "Placeholder";
    public Difficulty Difficulty => Difficulty.Easy;

    // Act 0 keeps the placeholder out of the way of the real acts 1–3.
    public LabStory Story => new(
        Act: 0,
        Order: 1,
        Briefing: "(Placeholder briefing. Story text is being rewritten.)",
        CallingCard: "(Placeholder calling card.)",
        Debrief: "(Placeholder debrief.)");

    public string ScenarioMarkdown => """
        **This is a placeholder lab** used to test the trainer's flow. It will be replaced by real labs.

        The endpoint `GET /api/labs/sample/flag` returns the flag to an authenticated crew member.

        **Goal:** send the prefilled request with Wesley's token, copy the flag and submit it.
        """;

    public IReadOnlyList<string> Hints =>
    [
        "The request console is already prefilled. Press Send.",
        "The flag is in the `flag` field of the JSON response.",
    ];

    public RequestTemplate DefaultRequest => new(
        "GET",
        $"{LabRegistry.ApiPrefix}{Slug}/flag",
        new Dictionary<string, string> { ["Authorization"] = $"Bearer {auth.GetToken(Slug, DemoAuth.Player)}" },
        null);

    public IReadOnlyList<FixOption> FixOptions =>
    [
        new("a", """
            group.MapGet("/flag", (HttpRequest req) =>
                auth.FindUser(Slug, req) is { Name: "wesley" }
                    ? Results.Ok(new { flag = flags.GetFlag(Slug) })
                    : Results.Unauthorized());
            """, IsCorrect: true,
            "Correct (for this placeholder): the server checks the token before returning anything."),
        new("b", """
            // Hide the Send button for users who shouldn't see the flag
            <button hidden="@(!isWesley)">Send</button>
            """, IsCorrect: false,
            "Hiding UI does nothing: anyone can still call the endpoint directly."),
        new("c", """
            group.MapGet("/flag-" + Guid.NewGuid(), ...); // unguessable route
            """, IsCorrect: false,
            "Obscurity is not access control: routes leak through logs, links and client code."),
    ];

    public string ExplanationMarkdown => """
        Placeholder explanation. Real labs explain the vulnerability and link to
        [OWASP](https://owasp.org/) and [Microsoft Docs](https://learn.microsoft.com/aspnet/core/security/).
        """;

    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        // Seed the users so their tokens exist from startup.
        auth.GetToken(Slug, DemoAuth.Player);
        auth.GetToken(Slug, "riker");

        app.MapGet("/flag", (HttpRequest request) =>
            auth.FindUser(Slug, request) switch
            {
                null => Results.Unauthorized(),
                { Name: DemoAuth.Player } => Results.Ok(new { message = "Welcome aboard, Wesley.", flag = flags.GetFlag(Slug) }),
                var user => Results.Json(new { error = $"{user.Name} is not allowed here." }, statusCode: StatusCodes.Status403Forbidden),
            });
    }

    public void Reset()
    {
        // Nothing to reseed: the placeholder lab has no data.
    }
}
