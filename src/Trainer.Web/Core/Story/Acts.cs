namespace Trainer.Web.Core.Story;

/// <param name="Ending">Shown under the act once all its labs are completed. Empty until written.</param>
public sealed record Act(int Number, string Title, string Tagline, string Ending = "");

/// <summary>Act titles, opening scene, act endings and finale (SPEC §3.3). Text comes from docs/story.md.</summary>
public static class Acts
{
    public const string OpeningScene = """
        Ten Forward, the ship's lounge, is loud tonight. The computer core refit is finished, three days
        early, and Lt. Barclay has been toasted so often he is hiding behind a plant.
        Acting Ensign Wesley Crusher stands at the edge of the party. Nobody has
        asked him to make a toast.

        Across the room, Data stops in the middle of a sentence and tilts his head.

        "Captain, there is a positronic signature inside the computer core. It
        matches my own."

        "Then it is yours, Mr. Data."

        "No, sir. I am here."

        The music keeps playing. Nobody else has noticed. Wesley puts down his glass.
        """;

    /// <summary>Shown once all labs are completed. Empty until written.</summary>
    public const string Finale = "";

    public static readonly IReadOnlyList<Act> All =
    [
        new(1, "Uninvited Guest", "Lore looks around: reading data he shouldn't see."),
        new(2, "Borrowed Identities", "Lore stops reading and starts becoming someone else."),
        new(3, "Into the Core", "Lore goes for the core of the ship."),
    ];

    public static Act? Find(int number) => All.FirstOrDefault(a => a.Number == number);
}
