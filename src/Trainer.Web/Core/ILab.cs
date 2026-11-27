namespace Trainer.Web.Core;

/// <summary>Contract every lab implements (SPEC §5.1). All player-facing text is a <see cref="Text"/> (English + German).</summary>
public interface ILab
{
    string Slug { get; }                     // "bola"
    Text Title { get; }                      // "Someone Else's Personal Log"
    string Category { get; }                 // "API", "Injection", "Blazor", ...
    Difficulty Difficulty { get; }           // Easy, Medium, Hard
    LabStory Story { get; }
    Text ScenarioMarkdown { get; }           // technical scenario + goal
    IReadOnlyList<Text> Hints { get; }
    RequestTemplate DefaultRequest { get; }
    IReadOnlyList<FixOption> FixOptions { get; }
    Text ExplanationMarkdown { get; }

    /// <summary>Extra tools on the Exploit step (SPEC §5.2). Most labs need none.</summary>
    IReadOnlyList<LabTool> Tools => [];

    void MapEndpoints(IEndpointRouteBuilder app);
    void Reset();
}

public enum LabTool
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

public record LabStory(int Act, int Order, Text Briefing, Text CallingCard, Text Debrief);

/// <param name="Code">The snippet; code is not translated.</param>
/// <param name="BarclayLine">Barclay suggesting this option; every option has one, so it gives nothing away.</param>
/// <param name="Language">highlight.js language of <paramref name="Code"/> (csharp, razor, json, sql, …).</param>
public record FixOption(string Id, string Code, bool IsCorrect, Text Feedback, Text? BarclayLine = null, string Language = "csharp");

public record RequestTemplate(string Method, string Path, Dictionary<string, string> Headers, string? Body);
