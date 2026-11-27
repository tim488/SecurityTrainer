using Trainer.Web.Labs.Bfla;
using Trainer.Web.Labs.BlazorXss;
using Trainer.Web.Labs.Bola;
using Trainer.Web.Labs.BruteForce;
using Trainer.Web.Labs.ClientSecrets;
using Trainer.Web.Labs.Jwt;
using Trainer.Web.Labs.MassAssignment;
using Trainer.Web.Labs.OpenRedirect;
using Trainer.Web.Labs.PathTraversal;
using Trainer.Web.Labs.SharedState;
using Trainer.Web.Labs.SqlInjection;
using Trainer.Web.Labs.Ssrf;

namespace Trainer.Web.Core;

/// <summary>
/// Holds all labs. Labs are registered explicitly here (no reflection), and each
/// lab's endpoints are mapped under <c>/api/missions/{slug}</c>.
/// </summary>
public sealed class LabRegistry : IDisposable
{
    public const string ApiPrefix = "/api/missions/";

    private readonly Dictionary<string, ILab> _bySlug;

    public LabRegistry(FlagService flags, DemoAuth auth)
    {
        // Register labs here, one line per lab. Act and order come from each lab's Story.
        ILab[] labs =
        [
            new BolaLab(flags, auth),
            new BflaLab(flags, auth),
            new SqlInjectionLab(flags, auth),
            new MassAssignmentLab(flags, auth),
            new ClientSecretsLab(flags, auth),
            new BruteForceLab(flags, auth),
            new OpenRedirectLab(flags, auth),
            new PathTraversalLab(flags, auth),
            new SharedStateLab(flags, auth),
            new JwtLab(flags),
            new SsrfLab(flags, auth),
            new BlazorXssLab(flags, auth),
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

    /// <summary>Stops lab background work (victim bots) and releases lab resources.</summary>
    public void Dispose()
    {
        foreach (var lab in Labs.OfType<IDisposable>()) lab.Dispose();
    }
}
