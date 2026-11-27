namespace Trainer.Web.Core;

/// <summary>Contract every mission implements. All player-facing text is a <see cref="Text"/> (English + German).</summary>
public interface IMission
{
    string Slug { get; }                     // "bola"
    Text Title { get; }                      // "Someone Else's Personal Log"
    string Category { get; }                 // "API", "Injection", "Blazor", ...
    Difficulty Difficulty { get; }           // Easy, Medium, Hard
    MissionStory Story { get; }
    Text ScenarioMarkdown { get; }           // technical scenario + goal
    IReadOnlyList<Text> Hints { get; }
    RequestTemplate DefaultRequest { get; }
    IReadOnlyList<FixOption> FixOptions { get; }
    Text ExplanationMarkdown { get; }

    /// <summary>Extra tools on the Exploit step. Most missions need none.</summary>
    IReadOnlyList<MissionTool> Tools => [];

    void MapEndpoints(IEndpointRouteBuilder app);
    void Reset();
}

public enum MissionTool
{
    /// <summary>Decode, edit and re-encode a JWT, then use it in the request console.</summary>
    JwtEditor,

    /// <summary>A sandboxed iframe showing the comment moderator's view (blazor-xss).</summary>
    ModeratorView,

    /// <summary>Sends a request for every number in a range, replacing a placeholder with the current number (brute-force).</summary>
    Repeater,
}

public enum Difficulty
{
    Easy,
    Medium,
    Hard,
}

public record MissionStory(int Act, int Order, Text Briefing, Text CallingCard, Text Debrief);

/// <param name="Code">The snippet; code is not translated.</param>
/// <param name="BarclayLine">Barclay suggesting this option; every option has one, so it gives nothing away.</param>
/// <param name="Language">highlight.js language of <paramref name="Code"/> (csharp, razor, json, sql, …).</param>
/// <param name="ControllerCode">The same fix as a classic MVC controller, for snippets that define endpoints
/// (<paramref name="Code"/> is the Minimal API version). Shown when the player picks <see cref="CodeStyle.Controller"/>.</param>
public record FixOption(string Id, string Code, bool IsCorrect, Text Feedback, Text? BarclayLine = null, string Language = "csharp",
    string? ControllerCode = null)
{
    public string CodeFor(CodeStyle style) => style == CodeStyle.Controller && ControllerCode is not null ? ControllerCode : Code;
}

public record RequestTemplate(string Method, string Path, Dictionary<string, string> Headers, string? Body);
