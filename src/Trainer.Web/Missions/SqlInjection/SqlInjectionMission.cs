using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Trainer.Web.Core;

namespace Trainer.Web.Missions.SqlInjection;

public sealed class CargoItem
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int Quantity { get; set; }
    public string Hold { get; set; } = "";
    public string Description { get; set; } = "";
    public bool IsClassified { get; set; }
}

public sealed class CargoDb(DbContextOptions<CargoDb> options) : DbContext(options)
{
    public DbSet<CargoItem> CargoItems => Set<CargoItem>();
}

/// <summary>
/// 1.3 SQL injection: <c>GET /cargo?search=</c> concatenates the search term
/// into <c>FromSqlRaw</c>. The classified item's description holds the flag.
/// There is one in-memory SQLite database per language with the same schema, picked
/// by the request's <c>Accept-Language</c>, so the injection works the same in both.
/// </summary>
public sealed partial class SqlInjectionMission(FlagService flags, DemoAuth auth) : IMission, IDisposable
{
    private const int FlagItemId = 1013;

    private readonly Lock _gate = new();
    private readonly Dictionary<Language, SqliteConnection> _connections = [];

    public string Slug => "sql-injection";
    public string Category => "Injection";
    public Difficulty Difficulty => Difficulty.Medium;

    public RequestTemplate DefaultRequest => new(
        "GET",
        $"{MissionRegistry.ApiPrefix}{Slug}/cargo?search=phaser",
        new Dictionary<string, string> { ["Authorization"] = $"Bearer {auth.GetToken(Slug, DemoAuth.Player)}" },
        null);

    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        auth.GetToken(Slug, DemoAuth.Player);
        Reset();

        // VULNERABLE: the search term is concatenated into raw SQL.
        app.MapGet("/cargo", (string? search, HttpRequest request) =>
        {
            if (auth.FindUser(Slug, request) is null) return Results.Unauthorized();
            var language = Languages.FromRequest(request);
            lock (_gate)
            {
                using var db = CreateDb(language);
                try
                {
                    // Not composed any further (no Select/Where after it), so EF Core sends the SQL exactly as built.
                    // EF1003 warns about exactly this concatenation. It is the mission's vulnerability, so it stays.
#pragma warning disable EF1003
                    var items = db.CargoItems
                        .FromSqlRaw("SELECT * FROM CargoItems WHERE Name LIKE '%" + search + "%' AND IsClassified = 0")
                        .AsNoTracking()
                        .ToList();
#pragma warning restore EF1003
                    return Results.Ok(items.Select(c => new { c.Id, c.Name, c.Quantity, c.Hold, c.Description }));
                }
                catch (Exception ex) when (ex is DbException or InvalidOperationException)
                {
                    // Verbose on purpose: a real dev environment would show this too, and it helps the player.
                    return Results.Json(new { error = ex.Message }, statusCode: StatusCodes.Status500InternalServerError);
                }
            }
        });
    }

    private CargoDb CreateDb(Language language) =>
        new(new DbContextOptionsBuilder<CargoDb>().UseSqlite(_connections[language]).Options);

    public void Reset()
    {
        lock (_gate)
        {
            foreach (var connection in _connections.Values) connection.Dispose();
            _connections.Clear();

            foreach (var language in Enum.GetValues<Language>())
            {
                var connection = new SqliteConnection("Data Source=:memory:");
                connection.Open();
                _connections[language] = connection;

                using var db = CreateDb(language);
                db.Database.EnsureCreated();
                db.CargoItems.AddRange(Seed(language));
                db.SaveChanges();
            }
        }
    }

    public void Dispose()
    {
        lock (_gate)
        {
            foreach (var connection in _connections.Values) connection.Dispose();
            _connections.Clear();
        }
    }

    private IEnumerable<CargoItem> Seed(Language language)
    {
        CargoItem Item(int id, Text name, int quantity, string hold, Text description, bool classified = false) =>
            new() { Id = id, Name = name[language], Quantity = quantity, Hold = hold, Description = description[language], IsClassified = classified };

        return
        [
            Item(1001, new("Phaser rifles, type 3", "Phasergewehre, Typ 3"), 40, "Cargo bay 2",
                new("Standard issue. Mr. Worf counts them every morning.", "Standardausrüstung. Mr. Worf zählt sie jeden Morgen nach.")),
            Item(1002, new("Phaser power cells", "Phaser-Energiezellen"), 600, "Cargo bay 2",
                new("Rechargeable. Please return empty cells to security.", "Wiederaufladbar. Leere Zellen bitte bei der Sicherheit abgeben.")),
            Item(1003, new("Dilithium ore", "Dilithium-Erz"), 12, "Cargo bay 1",
                new("Unrefined. Do not lick. (Ensign Ro, this means you.)", "Unraffiniert. Nicht ablecken. (Fähnrich Ro, das gilt auch für Sie.)")),
            Item(1004, new("Medical supplies, general", "Medizinischer Bedarf, allgemein"), 85, "Cargo bay 3",
                new("Hyposprays, dermal regenerators, bandages.", "Injektoren, Hautregeneratoren, Verbandszeug.")),
            Item(1005, new("Self-sealing stem bolts", "Selbstdichtende Schaftbolzen"), 5000, "Cargo bay 4",
                new("Nobody remembers ordering these. Nobody knows what they are for.", "Keiner erinnert sich, sie bestellt zu haben. Keiner weiß, wofür sie sind.")),
            Item(1006, new("Trombone reeds and valve oil", "Posaunen-Ventilöl und Zubehör"), 3, "Cargo bay 3",
                new("Private shipment for Cmdr. Riker. Fragile, apparently.", "Private Lieferung für Cmdr. Riker. Angeblich zerbrechlich.")),
            Item(1007, new("Klingon opera recordings", "Klingonische Opernaufnahmen"), 1, "Cargo bay 3",
                new("Property of Lt. Worf. Do not play near the medical supplies.", "Eigentum von Lt. Worf. Nicht in der Nähe der Medikamente abspielen.")),
            Item(1008, new("Chocolate, assorted", "Schokolade, gemischt"), 200, "Cargo bay 3",
                new("Requested by Counselor Troi \"for therapeutic purposes\".", "Von Counselor Troi angefordert, „zu therapeutischen Zwecken“.")),
            Item(1009, new("Cat food, 40 varieties", "Katzenfutter, 40 Sorten"), 40, "Cargo bay 3",
                new("For Spot. Lt. Cmdr. Data is still testing which one she likes.", "Für Spot. Lt. Cmdr. Data testet noch, welche Sorte sie mag.")),
            Item(1010, new("Holodeck emitter, spare", "Holodeck-Emitter, Ersatzteil"), 6, "Cargo bay 4",
                new("Lt. Barclay asked for six. Lt. Cmdr. La Forge asked why.", "Lt. Barclay hat sechs angefordert. Lt. Cmdr. La Forge hat gefragt, wozu.")),
            Item(1011, new("Isolinear chips", "Isolineare Chips"), 900, "Cargo bay 1",
                new("Left over from the computer refit. Some of them blink.", "Übrig vom Software-Update. Ein paar davon blinken.")),
            Item(1014, new("Photon torpedo casings", "Photonentorpedos, leere Hüllen"), 25, "Cargo bay 2",
                new("Empty. Mr. Worf would like that noted as a complaint.", "Leer. Mr. Worf möchte das als Beschwerde vermerkt wissen.")),
            Item(1012, new("Tea, Earl Grey, reserve", "Tee, Earl Grey, Reserve"), 1, "Cargo bay 1",
                new("Real leaves, not replicated. For the Captain, in case the replicator fails again.", "Echte Blätter, nicht repliziert. Für den Captain, falls der Replikator wieder versagt."), classified: true),
            Item(FlagItemId, new("Prototype, Soong laboratory", "Prototyp aus dem Soong-Labor"), 1, "Cargo bay 1, vault",
                new($"Positronic emotion chip. Built by Dr. Noonien Soong for Lt. Cmdr. Data. Handle with care. Vault code: {flags.GetFlag(Slug)}",
                    $"Positronischer Emotionschip, von Dr. Noonien Soong für Lt. Cmdr. Data gebaut. Vorsichtig behandeln. Tresorcode: {flags.GetFlag(Slug)}"),
                classified: true),
        ];
    }
}
