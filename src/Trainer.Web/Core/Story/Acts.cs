namespace Trainer.Web.Core.Story;

/// <param name="Ending">Shown under the act once all its labs are completed. Null until written.</param>
public sealed record Act(int Number, Text Title, Text Tagline, Text? Ending = null);

/// <summary>
/// Act titles, opening scene, act endings and finale (SPEC §3.3).
/// Text comes from docs/story.md (English) and docs/story.de.md (German).
/// </summary>
public static class Acts
{
    public static readonly Text OpeningScene = new(
        En: """
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
            """,
        De: """
            In Zehn Vorne, der Bar der Enterprise, wird heute gefeiert. Die neue Software für den
            Bordcomputer ist drei Tage zu früh fertig geworden. Lt. Barclay wurde so oft zugeprostet,
            dass er sich inzwischen hinter einer Zimmerpalme versteckt.

            Wesley Crusher, der jüngste Fähnrich an Bord, steht etwas verloren am Rand. Mit ihm
            stößt keiner an.

            Auf der anderen Seite des Raums bricht Data mitten im Satz ab und legt den Kopf schief.

            „Captain, im Bordcomputer befindet sich eine positronische Signatur. Sie ist mit meiner
            identisch.“

            „Dann ist es wohl Ihre, Mr. Data.“

            „Nein, Sir. Ich bin hier.“

            Die Musik spielt weiter, und sonst hat niemand etwas bemerkt. Wesley stellt sein Glas ab.
            """);

    /// <summary>Shown once all labs are completed. Null until written.</summary>
    public static readonly Text? Finale = null;

    public static readonly IReadOnlyList<Act> All =
    [
        new(1, new("Uninvited Guest", "Ungebetener Gast"),
            new("Lore looks around: reading data he shouldn't see.",
                "Lore schnüffelt in fremden Daten herum.")),
        new(2, new("Borrowed Identities", "Falsche Identitäten"),
            new("Lore stops reading and starts becoming someone else.",
                "Lore gibt sich als jemand anderes aus.")),
        new(3, new("Into the Core", "Ins Herz des Schiffs"),
            new("Lore goes for the core of the ship.",
                "Lore greift nach dem Hauptcomputer.")),
    ];

    public static Act? Find(int number) => All.FirstOrDefault(a => a.Number == number);
}
