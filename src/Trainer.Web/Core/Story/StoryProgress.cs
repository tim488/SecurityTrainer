namespace Trainer.Web.Core.Story;

/// <summary>
/// Story order and story mode. Missions are played in act order; in story mode a mission
/// opens once every mission before it is completed (flag and fix). A mission that is
/// already completed stays open, even if story mode was switched on later.
/// </summary>
public static class StoryProgress
{
    /// <summary>The first mission in story order that is not completed, or null when all are.</summary>
    public static IMission? Next(IReadOnlyList<IMission> missions, Func<string, bool> isCompleted) =>
        missions.FirstOrDefault(m => !isCompleted(m.Slug));

    /// <summary>The mission after <paramref name="mission"/> in story order, or null for the last one.</summary>
    public static IMission? Following(IReadOnlyList<IMission> missions, IMission mission)
    {
        var index = IndexOf(missions, mission);
        return index >= 0 && index < missions.Count - 1 ? missions[index + 1] : null;
    }

    public static bool IsLocked(IReadOnlyList<IMission> missions, Func<string, bool> isCompleted, bool storyMode, IMission mission)
    {
        if (!storyMode || isCompleted(mission.Slug)) return false;
        var next = Next(missions, isCompleted);
        return next is not null && IndexOf(missions, mission) > IndexOf(missions, next);
    }

    private static int IndexOf(IReadOnlyList<IMission> missions, IMission mission)
    {
        for (var i = 0; i < missions.Count; i++)
            if (ReferenceEquals(missions[i], mission)) return i;
        return -1;
    }
}
