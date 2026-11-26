namespace Trainer.Web.Core;

public enum Language
{
    En,
    De,
}

/// <summary>
/// A piece of player-facing text in every supported language. Lab content
/// (story, scenario, hints, feedback, explanation) is written as <see cref="Text"/>;
/// code snippets and HTTP requests stay English.
/// </summary>
public sealed record Text(string En, string De)
{
    public string this[Language language] => language == Language.De ? De : En;

    public bool IsComplete => !string.IsNullOrWhiteSpace(En) && !string.IsNullOrWhiteSpace(De);
}
