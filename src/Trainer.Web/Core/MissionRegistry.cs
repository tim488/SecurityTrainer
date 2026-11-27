using Trainer.Web.Missions.Bfla;
using Trainer.Web.Missions.BlazorXss;
using Trainer.Web.Missions.Bola;
using Trainer.Web.Missions.BruteForce;
using Trainer.Web.Missions.ClientSecrets;
using Trainer.Web.Missions.Jwt;
using Trainer.Web.Missions.MassAssignment;
using Trainer.Web.Missions.OpenRedirect;
using Trainer.Web.Missions.PathTraversal;
using Trainer.Web.Missions.SharedState;
using Trainer.Web.Missions.SqlInjection;
using Trainer.Web.Missions.Ssrf;

namespace Trainer.Web.Core;

/// <summary>
/// Holds all missions. Missions are registered explicitly here (no reflection), and each
/// mission's endpoints are mapped under <c>/api/missions/{slug}</c>.
/// </summary>
public sealed class MissionRegistry : IDisposable
{
    public const string ApiPrefix = "/api/missions/";

    private readonly Dictionary<string, IMission> _bySlug;

    public MissionRegistry(FlagService flags, DemoAuth auth)
    {
        // Register missions here, one line per mission. Act and order come from each mission's Story.
        IMission[] missions =
        [
            new BolaMission(flags, auth),
            new BflaMission(flags, auth),
            new SqlInjectionMission(flags, auth),
            new MassAssignmentMission(flags, auth),
            new ClientSecretsMission(flags, auth),
            new BruteForceMission(flags, auth),
            new OpenRedirectMission(flags, auth),
            new PathTraversalMission(flags, auth),
            new SharedStateMission(flags, auth),
            new JwtMission(flags),
            new SsrfMission(flags, auth),
            new BlazorXssMission(flags, auth),
        ];

        Missions = missions.OrderBy(m => m.Story.Act).ThenBy(m => m.Story.Order).ToList();
        _bySlug = Missions.ToDictionary(m => m.Slug, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<IMission> Missions { get; }

    public IMission? Find(string slug) => _bySlug.GetValueOrDefault(slug);

    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        foreach (var mission in Missions)
        {
            var group = app.MapGroup(ApiPrefix + mission.Slug).WithTags(mission.Slug);
            mission.MapEndpoints(group);
        }
    }

    /// <summary>Stops mission background work (victim bots) and releases mission resources.</summary>
    public void Dispose()
    {
        foreach (var mission in Missions.OfType<IDisposable>()) mission.Dispose();
    }
}
