namespace Trainer.Web.Core;

public enum Language
{
    En,
    De,
}

/// <summary>
/// A piece of player-facing text in every supported language. Mission content
/// (story, scenario, hints, feedback, explanation) is written as <see cref="Text"/>;
/// code snippets and HTTP requests stay English.
/// </summary>
public sealed record Text(string En, string De)
{
    public string this[Language language] => language == Language.De ? De : En;

    public bool IsComplete => !string.IsNullOrWhiteSpace(En) && !string.IsNullOrWhiteSpace(De);
}

public static class Languages
{
    public static string Code(Language language) => language == Language.De ? "de" : "en";

    /// <summary>Picks the language from the request's <c>Accept-Language</c> header (English by default).</summary>
    public static Language FromRequest(HttpRequest request)
    {
        var preferred = request.GetTypedHeaders().AcceptLanguage
            .OrderByDescending(l => l.Quality ?? 1)
            .Select(l => l.Value.Value ?? "");
        foreach (var tag in preferred)
        {
            if (tag.StartsWith("de", StringComparison.OrdinalIgnoreCase)) return Language.De;
            if (tag.StartsWith("en", StringComparison.OrdinalIgnoreCase)) return Language.En;
        }
        return Language.En;
    }
}
