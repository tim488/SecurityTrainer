namespace Trainer.Web.Core.Story;

/// <param name="Ending">Shown under the act once all its labs are completed. Null until written.</param>
public sealed record Act(int Number, Text Title, Text? Ending = null);

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

    /// <summary>Shown once all labs are completed.</summary>
    public static readonly Text? Finale = new(
        En: """
            The core is quiet for the first time since the refit. In Ten Forward, Guinan pours a drink
            nobody ordered and slides it to the end of the bar.

            "For the one who caught him," she says.

            Riker raises his glass. Worf nods, which for Worf is a speech. Barclay comes out from behind
            his plant. Data stands at the window, looking at the stars, and says nothing for a long time.

            "He is not gone," he says at last. "But he is no longer here."

            Picard puts a hand on Wesley's shoulder. "Mr. Crusher. Would you make the toast?"

            Deep in the core, in a partition nobody has named, a faint signature flickers once. Then it
            is still.
            """,
        De: """
            Zum ersten Mal seit dem Update ist es im Hauptcomputer still. In Zehn Vorne schenkt Guinan
            einen Drink ein, den keiner bestellt hat, und schiebt ihn ans Ende der Bar.

            „Für den, der ihn erwischt hat“, sagt sie.

            Riker hebt sein Glas. Worf nickt, und für Worf ist das eine Rede. Barclay kommt hinter seiner
            Zimmerpalme hervor. Data steht am Fenster, sieht hinaus zu den Sternen und sagt lange nichts.

            „Er ist nicht fort“, sagt er schließlich. „Aber er ist nicht mehr hier.“

            Picard legt Wesley eine Hand auf die Schulter. „Mr. Crusher, würden Sie den Toast ausbringen?“

            Tief im Hauptcomputer, in einer Partition ohne Namen, flackert eine schwache Signatur auf. Dann
            ist sie still.
            """);

    public static readonly IReadOnlyList<Act> All =
    [
        new(1, new("Uninvited Guest", "Ungebetener Gast"),
            Ending: new(
                En: """
                    The bridge lights dim for half a second. Then the main viewscreen switches on by itself.

                    Data's face looks down at them, smiling in a way Data never smiles.

                    "Captain. Commander. And brother, you look well. Wonderfully dull." His eyes move
                    across the bridge and stop at the conn. "And the boy. Of course it's the boy."

                    "Lore," Data says quietly.

                    "You've been tidying up after me, Wesley. Logs, rosters, cargo. Very thorough. But I'm
                    done reading about this crew." He leans closer. "Now I'm going to be one of you."

                    The screen goes dark. Riker looks at Wesley. For the first time, nobody tells him to sit down.
                    """,
                De: """
                    Die Lichter auf der Brücke flackern einen Moment lang. Dann schaltet sich der Hauptschirm
                    von selbst ein.

                    Datas Gesicht blickt auf sie herab, mit einem Lächeln, das Data nie lächeln würde.

                    „Captain. Commander. Und du, Bruder, siehst gut aus. Herrlich langweilig.“ Sein Blick wandert
                    über die Brücke und bleibt am Steuer hängen. „Und der Junge. Natürlich ist es der Junge.“

                    „Lore“, sagt Data leise.

                    „Du räumst mir hinterher, Wesley. Logbücher, Crewlisten, Fracht. Sehr gründlich. Aber vom
                    Lesen über diese Crew hab' ich genug.“ Er beugt sich vor. „Jetzt werd' ich selbst einer von euch.“

                    Der Schirm wird schwarz. Riker sieht Wesley an. Zum ersten Mal sagt ihm keiner, er solle sich setzen.
                    """)),
        new(2, new("Borrowed Identities", "Falsche Identitäten"),
            Ending: new(
                En: """
                    The voice on comms is calm and precise. "Bridge, this is Data. I have isolated the intruder in
                    the computer core. To finish the job, I need the core firewall lowered for ninety seconds."

                    Picard nods to Worf. Worf's hand moves to the console.

                    "Quickly, please, Captain. It won't hold him for long."

                    Wesley looks up. "Sir. Data doesn't say 'won't'."

                    The bridge goes quiet. Worf's hand stops above the panel.

                    On the comm line, someone laughs. It is Data's voice, but not Data's laugh. "Clever boy. Keep
                    your firewall, then. I'll find another way in."

                    The channel closes. At the ops station, the real Data turns around. "Captain, I have been here
                    the entire time."
                    """,
                De: """
                    Die Stimme über Kom klingt ruhig und präzise. „Brücke, hier spricht Data. Ich habe den
                    Eindringling im Hauptcomputer isoliert. Um ihn endgültig zu stoppen, muss die Firewall des
                    Hauptcomputers für neunzig Sekunden abgeschaltet werden.“

                    Picard nickt Worf zu. Worfs Hand wandert zur Konsole.

                    „Bitte beeilen Sie sich, Captain. Lang hält's nicht mehr.“

                    Wesley sieht auf. „Sir, Data würde nie ‚hält's‘ sagen.“

                    Auf der Brücke wird es still. Worfs Hand bleibt über der Konsole stehen.

                    Über die Leitung lacht jemand. Es ist Datas Stimme, aber nicht Datas Lachen. „Kluger Junge.
                    Dann behaltet eure Firewall. Ich komm' auf einem anderen Weg rein.“

                    Die Verbindung bricht ab. An seiner Konsole dreht sich der echte Data um. „Captain, ich war die
                    ganze Zeit hier.“
                    """)),
        new(3, new("Into the Core", "Ins Herz des Schiffs")),
    ];

    public static Act? Find(int number) => All.FirstOrDefault(a => a.Number == number);
}
