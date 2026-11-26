using Trainer.Web.Labs.Bfla;
using Trainer.Web.Labs.Bola;
using Trainer.Web.Labs.SqlInjection;

namespace Trainer.Web.Core;

/// <summary>
/// Holds all labs. Labs are registered explicitly here (no reflection), and each
/// lab's endpoints are mapped under <c>/api/labs/{slug}</c>.
/// </summary>
public sealed class LabRegistry
{
    public const string ApiPrefix = "/api/labs/";

    private readonly Dictionary<string, ILab> _bySlug;

    public LabRegistry(FlagService flags, DemoAuth auth)
    {
        // Register labs here, one line per lab. Act and order come from each lab's Story.
        ILab[] labs =
        [
            new BolaLab(flags, auth),
            new BflaLab(flags, auth),
            new SqlInjectionLab(flags, auth),
        ];

        Labs = labs.OrderBy(l => l.Story.Act).ThenBy(l => l.Story.Order).ToList();
        _bySlug = Labs.ToDictionary(l => l.Slug, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<ILab> Labs { get; }

    public ILab? Find(string slug) => _bySlug.GetValueOrDefault(slug);

    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        foreach (var lab in Labs)
        {
            var group = app.MapGroup(ApiPrefix + lab.Slug).WithTags(lab.Slug);
            lab.MapEndpoints(group);
        }
    }
}
