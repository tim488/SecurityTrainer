# Story guide

The big picture of the story and the rules for writing it. The text players read
lives in the code: each mission's `Missions/<Name>/<Name>Mission.Text.cs`, plus `Core/Story/Acts.cs`
for the opening scene, act titles, act endings and finale.

## Premise

- Setting: USS Enterprise-D, right after a rushed software refit of the computer core.
- Hero: **Wesley Crusher**, acting ensign and teenage prodigy, played by the player.
  Nobody on the bridge takes him fully seriously. He grows into the job mission by mission.
- Villain: **Lore**, Data's android brother. He has slipped into the ship's computer,
  holds a grudge against Wesley and wants the ship. He is the only antagonist.
- Lore's path follows a real attack: **look around → take over identities → reach the core**.
- Tone: real tension, with comic relief from Barclay's nerves, Data's precision and Lore's wit.

### Background (for readers who don't know the show)

- Lore and Data were built by the same scientist and look identical. Data is curious
  and kind; Lore is vain, charming and ruthless.
- Wesley once helped expose Lore and stop him. Lore has not forgiven "the boy".
- Data can't use contractions ("I do not", never "I don't"). Lore can. That's how
  Wesley unmasks him at the end of Act 2.
- Ten Forward is the ship's lounge. Its comment board is where the finale happens (3.5).

## Cast and voices

| Character | Role in the app | Voice |
|---|---|---|
| **Wesley Crusher** | the player (user `wesley`) | eager, underestimated, clever |
| **Captain Picard** | briefings (Captain's log) and debriefs | calm, brief, formal; calls him "Mr. Crusher" |
| **Lt. Cmdr. Data** | hints | precise, formal, **never a contraction**; quietly conflicted about his brother |
| **Lt. Barclay** | suggests every fix option, right or wrong | nervous, stutters ("I-I …", "M-maybe …"); wrote much of the refit and feels guilty |
| **Lore** | calling cards | charming, cruel, uses contractions, mocks "the boy" |
| **Commander Riker** | first officer, Lore's favourite target (user `riker`) | |

## How a mission tells its part

Each mission has the same pieces, in this order:

1. **Briefing**: a Captain's log from Picard, at most 100 words.
2. **Hints**: three from Data, each a bit more concrete.
3. **Calling card**: Lore's note next to the flag. It points at the system he attacks
   next, which is the next mission.
4. **Barclay lines**: one per fix option, the correct one too. They all sound equally
   unsure, so they give nothing away.
5. **Debrief**: Picard again, at most 60 words.

The opening scene (at most 120 words) shows on the missions page from the start. Each
act ending shows once all its missions are done; the finale follows 3.5 directly. Every
scene reached stays on the page so players can re-read it: the newest one is open, the
older ones are folded up. **The story never gives away the exploit.**

## The arc

| Mission | What happens | Calling card points to |
|---|---|---|
| Opening | The refit is done and the crew celebrates. Data notices a signature in the core that matches his own, but it isn't him. | |
| **Act 1: Uninvited Guest** | *Lore looks around: reading data he shouldn't see.* | |
| 1.1 `bola` | Lore reads Riker's personal log. | the crew roster |
| 1.2 `bfla` | Lore exports the crew roster and broadcasts it on all frequencies. | the cargo manifest |
| 1.3 `sql-injection` | Lore finds a classified prototype in the cargo manifest: the emotion chip Dr. Soong built for Data. He wants it, but not yet. | becoming someone else |
| Act 1 ending | Lore appears on the viewscreen and recognises Wesley. It's personal now. | |
| **Act 2: Borrowed Identities** | *Lore stops reading and starts becoming someone else.* | |
| 2.1 `mass-assignment` | "Admiral L. Soong" appears in the crew database; Lore promoted himself through the profile page. | the quartermaster's app |
| 2.2 `client-secrets` | Starbase supply reports requisitions nobody placed. Lore used the API key shipped inside the quartermaster's app. | Riker's four-digit code |
| 2.3 `brute-force` | Lore resets Riker's access code without touching his communicator. | stealing Riker's session |
| 2.4 `open-redirect` | Riker follows a "Starfleet Support" link to the ship's login, which forwards his one-time code to Lore. | the ship's archive |
| Act 2 ending | Lore impersonates Data on comms and asks for the core firewall to be lowered. Wesley notices a contraction. | |
| **Act 3: Into the Core** | *Lore goes for the core of the ship.* | |
| 3.1 `path-traversal` | Lore quotes Picard's confidential crew evaluations from the restricted archive. | crew data getting mixed up |
| 3.2 `shared-state` | Crew members receive each other's replicator orders, and Riker's comes with his command code. | a clearance check that skips signatures |
| 3.3 `jwt` | Lore logs into the bridge console as "Admiral Lore". | the core fetching anything |
| 3.4 `ssrf` | DaiMon Tarr offers to sell back the core credentials Lore pulled through the link preview. | the Ten Forward comment board |
| 3.5 `blazor-xss` | The sting: Wesley's comment makes Lore's own screen send his `modToken`. Barclay recognises Lore's partition and Data locks him out. | a parting threat |
| Finale | The ship is secure. Wesley is asked to make the toast nobody asked him for in the opening. Lore escapes; a faint signature remains in the core. | |

## Writing in English

- Short, plain sentences. Picard and Data are formal; Lore is casual and sharp.
- Technical texts (scenario, goal, feedback, explanation) speak to the player directly.

## Writing in German

- Not translated German: write it as if it were the original. Restructure sentences
  and swap images freely.
- Names as in the German dub: **Zehn Vorne**, „Logbuch des Captains, Sternzeit …",
  Mr. Crusher, Mr. Data, Fähnrich.
- **Du** in technical texts (scenario, goal, feedback, explanation). The characters
  say **Sie** to Wesley; only Lore says du.
- Voices: Data never uses contractions („gibt es", never „gibt's"); the end of Act 2
  depends on it. Lore uses them („gibt's", „nehm' ich"). Barclay stutters („I-ich …").
- Easy to read: normal, flowing sentences. No nested clauses, but no telegram style
  either. „und", „aber" and one simple subordinate clause are fine.
- Common IT terms stay English: Token, Flag, Policy, Request, Header.
- Everyday German instead of dub tech jargon: **Bordcomputer** / **Hauptcomputer**
  (not „Computerkern"), **Software-Update** (not „Überholung").

## Open threads

- **The emotion chip** (1.3): Lore says he'll "collect it later". Nothing picks this
  up yet. It could pay off in the finale or stay a sequel hook.
- **German rank names**: "acting ensign" is „Fähnrich auf Probe" and Data's position is
  „Zweiter Offizier, Einsatzoffizier". Neither has been checked against the dub.
