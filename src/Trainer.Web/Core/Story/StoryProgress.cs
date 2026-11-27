namespace Trainer.Web.Core.Story;

/// <summary>
/// Story order and story mode. Missions are played in act order; in story mode a mission
/// opens once every mission before it is completed (flag and fix). A mission that is
/// already completed stays open, even if story mode was switched on later.
/// </summary>
public static class StoryProgress
{
    /// <summary>The first mission in story order that is not completed, or null when all are.</summary>
    public static ILab? Next(IReadOnlyList<ILab> labs, Func<string, bool> isCompleted) =>
        labs.FirstOrDefault(l => !isCompleted(l.Slug));

    /// <summary>The mission after <paramref name="lab"/> in story order, or null for the last one.</summary>
    public static ILab? Following(IReadOnlyList<ILab> labs, ILab lab)
    {
        var index = IndexOf(labs, lab);
        return index >= 0 && index < labs.Count - 1 ? labs[index + 1] : null;
    }

    public static bool IsLocked(IReadOnlyList<ILab> labs, Func<string, bool> isCompleted, bool storyMode, ILab lab)
    {
        if (!storyMode || isCompleted(lab.Slug)) return false;
        var next = Next(labs, isCompleted);
        return next is not null && IndexOf(labs, lab) > IndexOf(labs, next);
    }

    private static int IndexOf(IReadOnlyList<ILab> labs, ILab lab)
    {
        for (var i = 0; i < labs.Count; i++)
            if (ReferenceEquals(labs[i], lab)) return i;
        return -1;
    }
}
