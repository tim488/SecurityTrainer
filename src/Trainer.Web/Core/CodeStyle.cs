using Markdig.Syntax;

namespace Trainer.Web.Core;

/// <summary>How endpoint code is shown to the player. Display only: the missions' real endpoints never change.</summary>
public enum CodeStyle
{
    MinimalApi,
    Controller,
}

/// <summary>
/// Endpoint snippets in mission Markdown come in pairs of fenced blocks tagged with the style
/// after the language: <c>```csharp minimal</c> and <c>```csharp controller</c>.
/// Untagged blocks are shown in both styles. Fix options use <see cref="FixOption.ControllerCode"/>.
/// </summary>
public static class CodeStyles
{
    public const string MinimalTag = "minimal";
    public const string ControllerTag = "controller";

    public static string Tag(CodeStyle style) => style == CodeStyle.Controller ? ControllerTag : MinimalTag;

    /// <summary>The style a fenced block is tagged with, or null for an untagged block.</summary>
    public static CodeStyle? Of(FencedCodeBlock block) => block.Arguments?.Trim() switch
    {
        MinimalTag => CodeStyle.MinimalApi,
        ControllerTag => CodeStyle.Controller,
        _ => null,
    };

    /// <summary>True when the mission shows endpoint code in both styles, so the page offers the switch.</summary>
    public static bool HasVariants(IMission mission) =>
        mission.FixOptions.Any(o => o.ControllerCode is not null)
        || MarkdownOf(mission).Any(m => m.Contains("```csharp " + ControllerTag, StringComparison.Ordinal));

    /// <summary>All of a mission's Markdown in every language (scenario, explanation, hints).</summary>
    public static IEnumerable<string> MarkdownOf(IMission mission) =>
        new[] { mission.ScenarioMarkdown, mission.ExplanationMarkdown }.Concat(mission.Hints).SelectMany(t => new[] { t.En, t.De });
}
