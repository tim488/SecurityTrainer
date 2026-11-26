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

    void MapEndpoints(IEndpointRouteBuilder app);
    void Reset();
}

public enum Difficulty
{
    Easy,
    Medium,
    Hard,
}

public record LabStory(int Act, int Order, Text Briefing, Text CallingCard, Text Debrief);

/// <param name="Code">The snippet; code is not translated.</param>
/// <param name="BarclayLine">Barclay suggesting this option; every option has one, so it gives nothing away. Hidden by "Skip story".</param>
/// <param name="Language">highlight.js language of <paramref name="Code"/> (csharp, razor, json, sql, …).</param>
public record FixOption(string Id, string Code, bool IsCorrect, Text Feedback, Text? BarclayLine = null, string Language = "csharp");

public record RequestTemplate(string Method, string Path, Dictionary<string, string> Headers, string? Body);
