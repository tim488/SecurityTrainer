using System.Text.Json;
using Trainer.Web.Core;

namespace Trainer.Web.Missions.SharedState;

/// <summary>The replicator panel's state. Registered as a singleton, which is the bug.</summary>
public sealed class ReplicatorOrderState
{
    public string? OrderedBy { get; set; }
    public Text? Item { get; set; }
    public string? CommandCode { get; set; }
    public DateTimeOffset? At { get; set; }
}

/// <summary>
/// 3.2 Shared state: the Blazor replicator panel keeps its order in a singleton
/// <see cref="ReplicatorOrderState"/>, shared by every user. A Riker bot places an off-ration
/// order every few seconds, authorised with his command code (the flag), which lands in that
/// shared state. <c>GET /replicator/order</c> returns whatever is in it.
/// </summary>
public sealed partial class SharedStateMission(FlagService flags, DemoAuth auth) : IMission, IDisposable
{
    private const string Riker = "riker";

    private readonly Lock _gate = new();
    private readonly ReplicatorOrderState _state = new();   // one instance for everyone
    private Timer? _bot;
    private TimeSpan _botInterval = TimeSpan.FromSeconds(4);
    private int _rikerOrders;

    /// <summary>How often Riker's bot orders. Takes effect at once; <see cref="Timeout.InfiniteTimeSpan"/> stops the bot (tests do this).</summary>
    public TimeSpan BotInterval
    {
        get => _botInterval;
        set
        {
            _botInterval = value;
            _bot?.Change(value, value);
        }
    }

    public string Slug => "shared-state";
    public string Category => "Blazor";
    public Difficulty Difficulty => Difficulty.Medium;

    public RequestTemplate DefaultRequest => new(
        "POST",
        $"{MissionRegistry.ApiPrefix}{Slug}/replicator/order",
        new Dictionary<string, string> { ["Authorization"] = $"Bearer {auth.GetToken(Slug, DemoAuth.Player)}" },
        """{ "item": "Tea, Earl Grey, hot" }""");

    private static readonly Text[] RikerItems =
    [
        new("Eggs, scrambled, extra spicy", "Rührei, extra scharf"),
        new("Aldebaran whiskey (synthehol)", "Aldebaran-Whiskey (Synthehol)"),
        new("Chocolate cake, triple layer", "Schokoladentorte, dreistöckig"),
        new("Trombone valve oil, premium", "Posaunen-Ventilöl, Premium"),
        new("Jambalaya, Riker family recipe", "Jambalaya nach Familienrezept Riker"),
    ];

    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        auth.GetToken(Slug, DemoAuth.Player);
        auth.GetToken(Slug, Riker);
        Reset();
        _bot = new Timer(_ => SimulateRikerOrder(), null, TimeSpan.FromSeconds(1), _botInterval);

        // VULNERABLE: everyone reads the same singleton state.
        app.MapGet("/replicator/order", (HttpRequest request) =>
        {
            if (auth.FindUser(Slug, request) is null) return Results.Unauthorized();
            var language = Languages.FromRequest(request);
            lock (_gate)
            {
                if (_state.Item is null)
                    return Results.Ok(new { status = new Text("No order yet.", "Noch keine Bestellung.")[language] });
                return Results.Ok(new
                {
                    _state.OrderedBy,
                    Item = _state.Item[language],
                    Status = new Text("delivered", "geliefert")[language],
                    _state.At,
                    _state.CommandCode,
                });
            }
        });

        app.MapPost("/replicator/order", async (HttpRequest request) =>
        {
            if (auth.FindUser(Slug, request) is not { } user) return Results.Unauthorized();
            string? item;
            try
            {
                using var doc = await JsonDocument.ParseAsync(request.Body);
                item = doc.RootElement.TryGetProperty("item", out var i) ? i.GetString() : null;
            }
            catch (JsonException)
            {
                item = null;
            }
            if (string.IsNullOrWhiteSpace(item))
                return Results.BadRequest(new { error = "Body must be JSON: { \"item\": \"...\" }" });

            item = item.Trim()[..Math.Min(item.Trim().Length, 80)];
            Place(user.Name, new Text(item, item), commandCode: null);
            return Results.Ok(new { orderedBy = user.Name, item, status = new Text("delivered", "geliefert")[Languages.FromRequest(request)] });
        });
    }

    private void Place(string user, Text item, string? commandCode)
    {
        lock (_gate)
        {
            _state.OrderedBy = user;
            _state.Item = item;
            _state.CommandCode = commandCode;
            _state.At = DateTimeOffset.UtcNow;
        }
    }

    /// <summary>Victim bot: Riker places an off-ration order with his command code. Public for tests.</summary>
    public void SimulateRikerOrder()
    {
        int n;
        lock (_gate) n = _rikerOrders++;
        Place(Riker, RikerItems[n % RikerItems.Length], flags.GetFlag(Slug));
    }

    public void Reset()
    {
        lock (_gate)
        {
            _state.OrderedBy = null;
            _state.Item = null;
            _state.CommandCode = null;
            _state.At = null;
            _rikerOrders = 0;
        }
    }

    public void Dispose() => _bot?.Dispose();
}
