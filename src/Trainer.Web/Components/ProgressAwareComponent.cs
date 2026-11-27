using Microsoft.AspNetCore.Components;
using Trainer.Web.Core;
using Trainer.Web.Core.Story;

namespace Trainer.Web.Components;

/// <summary>
/// Base for components that show progress: loads it from localStorage after the
/// first interactive render and re-renders whenever it changes.
/// </summary>
public abstract class ProgressAwareComponent : ComponentBase, IDisposable
{
    [Inject] protected ProgressStore Progress { get; set; } = default!;
    [Inject] protected MissionRegistry Registry { get; set; } = default!;

    protected Language Lang => Progress.Settings.Language;

    /// <summary>Picks the current language's version of an inline UI string.</summary>
    protected string L(string en, string de) => Lang == Language.De ? de : en;

    /// <summary>Picks the current language's version of mission or story text (null stays null).</summary>
    protected string? L(Text? text) => text?[Lang];

    protected string L(Difficulty difficulty) => difficulty switch
    {
        Difficulty.Easy => L("Easy", "Leicht"),
        Difficulty.Medium => L("Medium", "Mittel"),
        _ => L("Hard", "Schwer"),
    };

    protected string L(MissionStatus status) => status switch
    {
        MissionStatus.New => L("new", "neu"),
        MissionStatus.Exploited => L("exploited", "geknackt"),
        _ => L("solved", "gelöst"),
    };

    private bool IsCompleted(string slug) => Progress.Get(slug).IsCompleted;

    /// <summary>The first mission in story order that is not completed yet.</summary>
    protected IMission? NextMission => StoryProgress.Next(Registry.Missions, IsCompleted);

    /// <summary>True when story mode keeps this mission closed. Before progress has loaded, only the first mission is open.</summary>
    protected bool IsLocked(IMission mission) => StoryProgress.IsLocked(Registry.Missions, IsCompleted, Progress.Settings.StoryMode, mission);

    protected override void OnInitialized() => Progress.Changed += OnProgressChanged;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
            await Progress.LoadAsync(Registry.Missions.Select(m => m.Slug));
    }

    private void OnProgressChanged() => _ = InvokeAsync(StateHasChanged);

    public virtual void Dispose()
    {
        Progress.Changed -= OnProgressChanged;
        GC.SuppressFinalize(this);
    }
}
