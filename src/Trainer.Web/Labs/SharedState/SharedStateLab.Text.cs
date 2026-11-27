using System.Text.Json;
using Trainer.Web.Core;

namespace Trainer.Web.Labs.SharedState;

// Player-facing text of SharedStateLab: story, scenario, hints, fix options and explanation.
// The endpoints and data are in SharedStateLab.cs.
public sealed partial class SharedStateLab
{
    public Text Title => new("Crossed Beams", "Gekreuzte Strahlen");

    public LabStory Story => new(
        Act: 3,
        Order: 2,
        Briefing: new(
            En: """
                Captain's log, stardate 47330.5. The replicators have developed a sense of humour.
                Counselor Troi ordered a chocolate sundae and received Mr. Worf's gagh. Mr. Worf did not
                find it funny. Worse, Commander Riker reports that someone placed an order with his
                command code, and it was not him. Since the refit, the replicator panel runs on a new
                system. I have asked Mr. Crusher to find out what it is sharing, and with whom.
                """,
            De: """
                Logbuch des Captains, Sternzeit 47330.5. Die Replikatoren haben Humor entwickelt. Counselor
                Troi hat einen Schokoladen-Eisbecher bestellt und Mr. Worfs Gagh bekommen. Mr. Worf fand das
                nicht witzig. Schlimmer noch: Laut Commander Riker hat jemand mit seinem Kommandocode bestellt,
                und er war es nicht. Seit dem Update läuft das Replikator-Panel auf einem neuen System. Ich
                habe Mr. Crusher gebeten herauszufinden, was es teilt, und mit wem.
                """),
        CallingCard: new(
            En: """
                Commander Riker's command code, Wesley. And his dinner: eggs, extra spicy. The man has
                no shame. Codes are nice, but I've seen how the bridge console checks clearance. It reads
                your badge and believes every word. It never asks who signed it. I think I'll write
                myself a new one. — L.
                """,
            De: """
                Commander Rikers Kommandocode, Wesley. Und sein Abendessen: Eier, extra scharf. Der Mann kennt
                keine Scham. Codes sind ja ganz nett, aber ich hab' mir angesehen, wie die Brückenkonsole die
                Freigabe prüft. Sie liest deinen Ausweis und glaubt jedes Wort. Wer ihn unterschrieben hat,
                fragt sie nie. Ich glaub', ich schreib' mir einen neuen. — L.
                """),
        Debrief: new(
            En: """
                Good work, Mr. Crusher. Every crew member now has their own replicator panel, and
                nobody sees anyone else's order. Commander Riker has a new command code, and Counselor
                Troi has her sundae. Mr. Worf has asked me to find out who received his gagh. I have
                decided not to.
                """,
            De: """
                Gute Arbeit, Mr. Crusher. Jedes Crewmitglied hat jetzt sein eigenes Replikator-Panel, und
                keiner sieht mehr die Bestellung eines anderen. Commander Riker hat einen neuen Kommandocode,
                Counselor Troi hat ihren Eisbecher. Mr. Worf möchte wissen, wer sein Gagh bekommen hat. Ich
                habe beschlossen, es nicht herauszufinden.
                """));

    private const string VulnerableCode = """
        ```csharp
        // Program.cs (Blazor Server)
        builder.Services.AddSingleton<ReplicatorOrderState>();

        public class ReplicatorOrderState
        {
            public string? OrderedBy { get; set; }
            public string? Item { get; set; }
            public string? CommandCode { get; set; }   // needed for off-ration items
        }
        ```

        ```razor
        @* ReplicatorPanel.razor *@
        @inject ReplicatorOrderState Order
        @inject CurrentUser User

        <p>Your order: @Order.Item (@Order.OrderedBy)</p>

        @code {
            void Place(string item, string? commandCode)
            {
                Order.OrderedBy = User.Name;
                Order.Item = item;
                Order.CommandCode = commandCode;
            }
        }
        ```
        """;

    public Text ScenarioMarkdown => new(
        En: $"""
            The replicator panel is a **Blazor Server** component. It keeps the current order in a
            small state service. For this mission, the panel's two actions are available as endpoints:

            | Endpoint | What it does |
            |---|---|
            | `GET /api/missions/shared-state/replicator/order` | What the panel shows you as "your order" |
            | `POST /api/missions/shared-state/replicator/order` | Places an order: `{"{"} "item": "Tea, Earl Grey, hot" {"}"}` |

            You are logged in as `wesley`; your bearer token is already in the request console.
            Commander Riker is using his own panel at the same time. The state service and the panel
            look like this:

            {VulnerableCode}

            **Goal:** find Commander Riker's command code. It is the flag.
            """,
        De: $"""
            Das Replikator-Panel ist eine **Blazor-Server**-Komponente. Die aktuelle Bestellung hält es in
            einem kleinen State-Service. Für diese Mission gibt es die beiden Aktionen des Panels als
            Endpunkte:

            | Endpunkt | Funktion |
            |---|---|
            | `GET /api/missions/shared-state/replicator/order` | Was das Panel dir als „deine Bestellung“ anzeigt |
            | `POST /api/missions/shared-state/replicator/order` | Gibt eine Bestellung auf: `{"{"} "item": "Tee, Earl Grey, heiß" {"}"}` |

            Du bist als `wesley` angemeldet, dein Token steht schon in der Request-Konsole. Commander Riker
            benutzt gleichzeitig sein eigenes Panel. So sehen der State-Service und das Panel aus:

            {VulnerableCode}

            **Ziel:** Finde Commander Rikers Kommandocode. Er ist die Flag.
            """);

    public IReadOnlyList<Text> Hints =>
    [
        new("`GET /replicator/order` should show your own order. Please call it a few times over the next minute. I have observed that the field `orderedBy` does not always contain your name.",
            "`GET /replicator/order` sollte Ihre eigene Bestellung zeigen. Rufen Sie es bitte innerhalb der nächsten Minute ein paar Mal auf. Ich habe beobachtet, dass im Feld `orderedBy` nicht immer Ihr Name steht."),
        new("`ReplicatorOrderState` is registered as a singleton. In Blazor Server, that means a single instance for every user of the application, for as long as the server runs.",
            "`ReplicatorOrderState` ist als Singleton registriert. In Blazor Server heißt das: eine einzige Instanz für alle Benutzer der Anwendung, solange der Server läuft."),
        new("Commander Riker places an off-ration order every few seconds and authorises it with his command code. If you look at your panel just after he does, you will see what he sees.",
            "Commander Riker gibt alle paar Sekunden eine Sonderbestellung auf und bestätigt sie mit seinem Kommandocode. Wenn Sie kurz danach auf Ihr Panel schauen, sehen Sie, was er sieht."),
    ];

    public IReadOnlyList<FixOption> FixOptions =>
    [
        new("scoped-state", """
            // Program.cs (Blazor Server)
            builder.Services.AddScoped<ReplicatorOrderState>();   // one per circuit (browser tab)

            // Anything that must outlive the circuit goes to storage keyed by the user:
            // orders.Save(User.Id, order);
            """, IsCorrect: true,
            new("Correct. In Blazor Server, a scoped service lives as long as one user's circuit, so every crew member " +
                "gets their own `ReplicatorOrderState`. Data that has to outlive the circuit belongs in storage keyed by the user, never in a singleton.",
                "Richtig. In Blazor Server lebt ein Scoped-Service so lange wie die Verbindung (Circuit) eines Benutzers. Jedes Crewmitglied bekommt also seinen eigenen `ReplicatorOrderState`. Daten, die länger leben müssen, gehören in einen Speicher, der nach Benutzer getrennt ist, aber nie in ein Singleton."),
            BarclayLine: new(
                "M-maybe each crew member gets their own copy? Scoped, I think it's called. I always mix up the lifetimes. S-sorry.",
                "V-vielleicht kriegt jedes Crewmitglied eine eigene Kopie? Scoped heißt das, glaube ich. Ich verwechsle die Lebensdauern immer. T-tut mir leid.")),

        new("add-lock", """
            public class ReplicatorOrderState
            {
                private readonly Lock _gate = new();
                private string? _orderedBy, _item, _commandCode;

                public void Place(string user, string item, string? code)
                {
                    lock (_gate) { _orderedBy = user; _item = item; _commandCode = code; }
                }

                public (string? User, string? Item, string? Code) Current
                {
                    get { lock (_gate) return (_orderedBy, _item, _commandCode); }
                }
            }
            """, IsCorrect: false,
            new("Thread safety is not isolation. The lock stops two writes from tangling, but there is still one order " +
                "for the whole ship, and everybody reads Riker's code the moment he places it.",
                "Thread-Sicherheit ist keine Trennung. Das Lock verhindert, dass sich zwei Schreibzugriffe verheddern, aber es gibt weiterhin nur eine Bestellung für das ganze Schiff. Jeder liest Rikers Code, sobald er bestellt."),
            BarclayLine: new(
                "It's a threading problem, sir. Obviously. I'll put a `lock` around it. Locks fix threading problems. U-usually.",
                "Das ist ein Threading-Problem, Sir. Ganz klar. I-ich setze ein `lock` drum. Locks lösen Threading-Probleme. M-meistens.")),

        new("clear-after-order", """
            void Place(string item, string? commandCode)
            {
                Order.OrderedBy = User.Name;
                Order.Item = item;
                Order.CommandCode = commandCode;

                Replicator.Deliver(Order);

                Order.CommandCode = null;   // don't keep the code around
            }
            """, IsCorrect: false,
            new("A race, not a fix. Between setting and clearing, every other user can read the code, and the item and " +
                "name still leak afterwards. Shrinking the window just means Lore has to ask more often.",
                "Das ist ein Wettlauf, keine Lösung. Zwischen Setzen und Löschen kann jeder andere Benutzer den Code lesen, und Gericht und Name sickern danach trotzdem durch. Ein kleineres Zeitfenster heißt nur, dass Lore öfter fragen muss."),
            BarclayLine: new(
                "We c-could wipe the code as soon as the order is delivered. Then there's nothing left to see. Th-the window is tiny.",
                "W-wir könnten den Code löschen, sobald die Bestellung geliefert ist. Dann gibt's nichts mehr zu sehen. D-das Zeitfenster ist winzig.")),

        new("immutable-record", """
            public record ReplicatorOrder(string OrderedBy, string Item, string? CommandCode);

            public class ReplicatorOrderState
            {
                public ReplicatorOrder? Current { get; private set; }
                public void Place(ReplicatorOrder order) => Current = order;
            }

            builder.Services.AddSingleton<ReplicatorOrderState>();
            """, IsCorrect: false,
            new("Each order is now immutable, but the state that points to the current order is still one singleton for " +
                "everybody. Riker replaces it, Wesley reads it. Immutability prevents changes, not sharing.",
                "Jede Bestellung ist jetzt unveränderlich, aber der State, der auf die aktuelle Bestellung zeigt, ist weiterhin ein einziges Singleton für alle. Riker ersetzt sie, Wesley liest sie. Unveränderlichkeit verhindert Änderungen, aber nicht das Teilen."),
            BarclayLine: new(
                "What if the order can't be changed at all? A record. Immutable. Nobody can mess with it. Th-that's the modern way.",
                "Und wenn man die Bestellung gar nicht ändern kann? Ein Record, unveränderlich. D-dann kann keiner dran rumspielen. So macht man das heute.")),
    ];

    public Text ExplanationMarkdown => new(
        En: """
            **Shared state across users** is a classic Blazor Server mistake. Blazor Server runs
            every user's components on the same server, and a service registered as a
            **singleton** exists once for the whole application. Whatever one user writes into it,
            every other user reads. No attack was needed: you simply looked at your own panel.

            Register per-user UI state as **scoped**: in Blazor Server, a scope lasts as long as one
            circuit (one browser tab). Keep data that must last longer in storage keyed by the user
            ID. Use singletons only for things that really are the same for everyone, such as
            configuration or caches of public data.

            Why the other options fail:
            - **A `lock`** makes access thread-safe, but still shared.
            - **Clearing the code afterwards** leaves a race window, and the rest of the order still leaks.
            - **An immutable record** can't be changed, but the singleton holding it is still shared.

            Read more:
            - [Microsoft Docs: Blazor dependency injection, service lifetimes](https://learn.microsoft.com/aspnet/core/blazor/fundamentals/dependency-injection#service-lifetime)
            - [Microsoft Docs: Blazor Server state management](https://learn.microsoft.com/aspnet/core/blazor/state-management)
            """,
        De: """
            **Geteilter Zustand zwischen Benutzern** ist ein klassischer Fehler in Blazor Server. Blazor Server
            führt die Komponenten aller Benutzer auf demselben Server aus, und ein Service, der als **Singleton**
            registriert ist, existiert für die ganze Anwendung nur einmal. Was ein Benutzer hineinschreibt,
            lesen alle anderen. Dafür war kein Angriff nötig: Du hast einfach auf dein eigenes Panel geschaut.

            Registriere Zustand, der zu einem Benutzer gehört, als **Scoped**. In Blazor Server dauert ein Scope
            so lange wie ein Circuit, also ein Browser-Tab. Daten, die länger halten müssen, speicherst du nach
            Benutzer-ID getrennt. Singletons sind nur für Dinge da, die wirklich für alle gleich sind, etwa
            Konfiguration oder ein Cache öffentlicher Daten.

            Warum die anderen Optionen nicht helfen:
            - **Ein `lock`** macht den Zugriff thread-sicher, aber weiterhin geteilt.
            - **Den Code hinterher löschen** lässt ein Zeitfenster offen, und der Rest der Bestellung sickert trotzdem durch.
            - **Ein unveränderlicher Record** lässt sich nicht ändern, aber das Singleton, das ihn hält, wird weiterhin geteilt.

            Mehr dazu:
            - [Microsoft Docs: Dependency Injection in Blazor, Lebensdauer von Services](https://learn.microsoft.com/de-de/aspnet/core/blazor/fundamentals/dependency-injection#service-lifetime)
            - [Microsoft Docs: Zustandsverwaltung in Blazor](https://learn.microsoft.com/de-de/aspnet/core/blazor/state-management)
            """);
}
