namespace Trainer.Web.Core;

/// <summary>Contract every lab implements (SPEC §5.1).</summary>
public interface ILab
{
    string Slug { get; }                     // "bola"
    string Title { get; }                    // "Someone Else's Personal Log"
    string Category { get; }                 // "API", "Injection", "Blazor", ...
    Difficulty Difficulty { get; }           // Easy, Medium, Hard
    LabStory Story { get; }
    string ScenarioMarkdown { get; }         // technical scenario + goal
    IReadOnlyList<string> Hints { get; }
    RequestTemplate DefaultRequest { get; }
    IReadOnlyList<FixOption> FixOptions { get; }
    string ExplanationMarkdown { get; }

    void MapEndpoints(IEndpointRouteBuilder app);
    void Reset();
}

public enum Difficulty
{
    Easy,
    Medium,
    Hard,
}

public record LabStory(int Act, int Order, string Briefing, string CallingCard, string Debrief);

public record FixOption(string Id, string Code, bool IsCorrect, string Feedback);

public record RequestTemplate(string Method, string Path, Dictionary<string, string> Headers, string? Body);
