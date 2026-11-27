using Trainer.Web.Core;

namespace Trainer.Web.Labs.Bola;

public sealed record PersonalLog(int Id, string OwnerId, string Stardate, Text Title, Text Notes)
{
    public object ToJson(Language language) => new { Id, OwnerId, Stardate, Title = Title[language], Notes = Notes[language] };
}

/// <summary>
/// 1.1 Broken Object Level Authorization: <c>GET /logs/{id}</c> returns
/// any log to any logged-in user. Riker's log 1003 holds the flag. Any other ID
/// returns the next of ten joke logs from the rest of the crew, in turn.
/// Log text follows the request's <c>Accept-Language</c> (English or German).
/// </summary>
public sealed partial class BolaLab(FlagService flags, DemoAuth auth) : ILab
{
    private const string Riker = "riker";
    private const int FlagLogId = 1003;

    private readonly Lock _gate = new();
    private List<PersonalLog> _logs = [];
    private int _nextCrewLog;

    public string Slug => "bola";
    public string Category => "API";
    public Difficulty Difficulty => Difficulty.Easy;

    public RequestTemplate DefaultRequest => new(
        "GET",
        $"{LabRegistry.ApiPrefix}{Slug}/logs",
        new Dictionary<string, string> { ["Authorization"] = $"Bearer {auth.GetToken(Slug, DemoAuth.Player)}" },
        null);

    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        auth.GetToken(Slug, DemoAuth.Player);
        auth.GetToken(Slug, Riker);
        Reset();

        // Correctly scoped: only the caller's own logs.
        app.MapGet("/logs", (HttpRequest request) =>
        {
            if (auth.FindUser(Slug, request) is not { } user) return Results.Unauthorized();
            var language = Languages.FromRequest(request);
            lock (_gate)
            {
                return Results.Ok(_logs
                    .Where(l => l.OwnerId == user.Name)
                    .Select(l => new { l.Id, l.Stardate, Title = l.Title[language] })
                    .ToList());
            }
        });

        // VULNERABLE: authenticated, but no ownership check.
        app.MapGet("/logs/{id:int}", (int id, HttpRequest request) =>
        {
            if (auth.FindUser(Slug, request) is null) return Results.Unauthorized();
            var language = Languages.FromRequest(request);
            lock (_gate)
            {
                var log = _logs.FirstOrDefault(l => l.Id == id) ?? NextCrewLog(id);
                return Results.Ok(log.ToJson(language));
            }
        });
    }

    /// <summary>Loops through the joke logs: each unknown ID gets the next one. Call inside the lock.</summary>
    private PersonalLog NextCrewLog(int id)
    {
        var (owner, stardate, title, notes) = CrewLogs[_nextCrewLog];
        _nextCrewLog = (_nextCrewLog + 1) % CrewLogs.Length;
        return new PersonalLog(id, owner, stardate, title, notes);
    }

    private static readonly (string Owner, string Stardate, Text Title, Text Notes)[] CrewLogs =
    [
        ("data", "47302.6", new("Humour, attempt 4,211", "Humor, Versuch 4.211"),
            new("I told Commander La Forge a joke about a Ferengi, a Vulcan and a tribble. He did not laugh. I have adjusted my timing by 0.3 seconds and will try again tomorrow.",
                "Ich habe Commander La Forge einen Witz über einen Ferengi, einen Vulkanier und einen Tribble erzählt. Er hat nicht gelacht. Ich habe mein Timing um 0,3 Sekunden angepasst und versuche es morgen erneut.")),
        ("barclay", "47309.1", new("Things I did not say", "Was ich nicht gesagt habe"),
            new("Wanted to tell Commander La Forge that the refit was rushed. Practised it eleven times on the holodeck. In the real meeting I said 'sounds great, sir'. The holodeck Geordi was much more understanding.",
                "Ich wollte Commander La Forge sagen, dass das Update überhastet war, und habe es elfmal auf dem Holodeck geübt. Im echten Meeting habe ich dann „klingt super, Sir“ gesagt. Der Holodeck-Geordi war viel verständnisvoller.")),
        ("worf", "47311.4", new("Complaint", "Beschwerde"),
            new("Someone has replaced my prune juice with ordinary apple juice. Again. This is the third time. I will find them.",
                "Jemand hat meinen Pflaumensaft durch gewöhnlichen Apfelsaft ersetzt. Schon wieder, zum dritten Mal. Ich werde ihn finden.")),
        ("troi", "47312.8", new("Chocolate", "Schokolade"),
            new("My session notes are confidential, so this log is only about chocolate. Today's sundae had four kinds. I sense that I regret nothing.",
                "Meine Sitzungsnotizen sind vertraulich, also geht es in diesem Logbuch nur um Schokolade. Der Eisbecher heute hatte vier Sorten. Ich spüre, dass ich nichts bereue.")),
        ("picard", "47315.0", new("Replicator", "Replikator"),
            new("Ordered: tea, Earl Grey, hot. Received: tea, Earl Grey, lukewarm. Third time this week. Mr. Barclay apologised nine times and fled.",
                "Bestellt: Tee, Earl Grey, heiß. Bekommen: Tee, Earl Grey, lauwarm. Zum dritten Mal diese Woche. Mr. Barclay hat sich neunmal entschuldigt und ist geflohen.")),
        ("geordi", "47316.3", new("Lights on deck 8", "Licht auf Deck 8"),
            new("Since the refit, the lights on deck 8 flicker at 61 hertz. Through my VISOR it looks like a disco. Nobody else can see it, so I'm the only one dancing.",
                "Seit dem Update flackert das Licht auf Deck 8 mit 61 Hertz. Durch mein VISOR sieht das aus wie in einer Disco. Außer mir sieht es keiner, also tanze ich allein.")),
        ("guinan", "47319.7", new("Closing time", "Sperrstunde"),
            new("A man who looked exactly like Data ordered a drink and left a generous tip. Data never tips. I've made a note.",
                "Ein Mann, der genau wie Data aussah, hat einen Drink bestellt und großzügig Trinkgeld gegeben. Data gibt nie Trinkgeld. Ich habe es mir gemerkt.")),
        ("beverly", "47320.1", new("Medical log, unofficial", "Ärztliches Logbuch, inoffiziell"),
            new("Treated Lt. Barclay for refit-related stress. Prescription: sleep, fewer holodeck simulations of his colleagues and no coffee after 22:00. He took notes. In triplicate.",
                "Lt. Barclay wegen Update-Stress behandelt. Verordnung: Schlaf, weniger Holodeck-Simulationen seiner Kollegen und kein Kaffee nach 22 Uhr. Er hat mitgeschrieben, in dreifacher Ausfertigung.")),
        ("obrien", "47321.0", new("Transporter room 3", "Transporterraum 3"),
            new("Commander Riker asked again whether the transporter could 'tidy up' his beard during transport. No. Transporters are not barbers. Filed under 'strange requests'. That folder is getting full.",
                "Commander Riker hat wieder gefragt, ob der Transporter beim Beamen seinen Bart „etwas in Form bringen“ könnte. Nein, Transporter sind keine Friseure. Abgelegt unter „Seltsame Anfragen“. Der Ordner wird langsam voll.")),
        ("spot", "47321.2", new("Feeding schedule (dictated)", "Fütterungsplan (diktiert)"),
            new("Mrrrow. Mrow. Mrrrrrow. [Transcribed from audio. Speaker identified as: Spot.]",
                "Mrrrau. Mrau. Mrrrrrau. [Aus einer Audioaufnahme transkribiert. Erkannter Sprecher: Spot.]")),
    ];

    public void Reset()
    {
        lock (_gate)
        {
            _nextCrewLog = 0;
            _logs =
            [
                new(1001, DemoAuth.Player, "47310.2",
                    new("First week on the refit crew", "Erste Woche im Update-Team"),
                    new("Lt. Barclay let me rewrite the log indexer. He says it's the most important job on the ship. I think he was being nice.",
                        "Lt. Barclay hat mich den Logbuch-Index neu schreiben lassen. Er sagt, das sei der wichtigste Job auf dem Schiff. Ich glaube, er wollte nur nett sein.")),
                new(1002, DemoAuth.Player, "47318.9",
                    new("Nobody listens", "Keiner hört zu"),
                    new("Told Geordi the timings of the new computer software looked off. He said 'later, Wes.' It's always later.",
                        "Ich habe Geordi gesagt, dass mit den Timings der neuen Bordcomputer-Software etwas nicht stimmt. Er meinte nur: „Später, Wes.“ Es ist immer später.")),
                new(FlagLogId, Riker, "47320.5",
                    new("Personal — do not share", "Privat – nicht weitergeben"),
                    new($"Forty minutes of trombone practice. The bridge of 'Night Bird' is finally coming together. Note to self: {flags.GetFlag(Slug)}",
                        $"Vierzig Minuten Posaune geübt. Die Bridge von „Night Bird“ sitzt endlich. Notiz an mich: {flags.GetFlag(Slug)}")),
                new(1004, Riker, "47320.9",
                    new("Poker night", "Pokerabend"),
                    new("Won 300 credits from Geordi and lost 400 to Dr. Crusher. Data folded a royal flush because 'the probability suggested you were not bluffing, Commander.' I was bluffing.",
                        "300 Credits von Geordi gewonnen und 400 an Dr. Crusher verloren. Data hat einen Royal Flush weggelegt, weil „die Wahrscheinlichkeit dafür sprach, dass Sie nicht bluffen, Commander“. Ich habe geblufft.")),
                new(1005, Riker, "47321.1",
                    new("Re: the chair", "Betreff: der Stuhl"),
                    new("Counselor Troi asked again why I swing my leg over the chair instead of sitting down like a normal person. I told her it's tactical. It is not tactical. I just like it.",
                        "Counselor Troi hat wieder gefragt, warum ich das Bein über den Stuhl schwinge, statt mich wie ein normaler Mensch hinzusetzen. Ich habe gesagt, das sei taktisch. Ist es nicht, ich mag es einfach.")),
                new(1006, Riker, "47321.3",
                    new("Beard, day 14", "Bart, Tag 14"),
                    new("Still undecided. The Captain hasn't said a word, which is either approval or the most disapproving silence in Starfleet history. Worf says it makes me look 'almost Klingon'. I'm keeping it.",
                        "Immer noch unentschlossen. Der Captain hat kein Wort gesagt, und das ist entweder Zustimmung oder das missbilligendste Schweigen der Sternenflottengeschichte. Worf meint, ich sehe damit „fast klingonisch“ aus. Der Bart bleibt.")),
                new(1007, Riker, "47321.4",
                    new("Cooking for the senior staff", "Kochabend für die Führungsoffiziere"),
                    new("Made my famous eggs again. Worf ate them and called them 'acceptable'. Everyone else suddenly had urgent duties. Next time: gagh omelette.",
                        "Wieder meine berühmten Eier gemacht. Worf hat sie gegessen und „annehmbar“ genannt. Alle anderen hatten plötzlich dringende Termine. Nächstes Mal: Gagh-Omelett.")),
            ];
        }
    }
}
