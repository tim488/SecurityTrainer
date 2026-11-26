using Microsoft.AspNetCore.Components;
using Trainer.Web.Core;

namespace Trainer.Web.Components;

/// <summary>
/// Base for components that show progress: loads it from localStorage after the
/// first interactive render and re-renders whenever it changes.
/// </summary>
public abstract class ProgressAwareComponent : ComponentBase, IDisposable
{
    [Inject] protected ProgressStore Progress { get; set; } = default!;
    [Inject] protected LabRegistry Registry { get; set; } = default!;

    protected bool SkipStory => Progress.Settings.SkipStory;

    protected Language Lang => Progress.Settings.Language;

    /// <summary>Picks the current language's version of an inline UI string.</summary>
    protected string L(string en, string de) => Lang == Language.De ? de : en;

    /// <summary>Picks the current language's version of lab or story text (null stays null).</summary>
    protected string? L(Text? text) => text?[Lang];

    protected string L(Difficulty difficulty) => difficulty switch
    {
        Difficulty.Easy => L("Easy", "Leicht"),
        Difficulty.Medium => L("Medium", "Mittel"),
        _ => L("Hard", "Schwer"),
    };

    protected string L(LabStatus status) => status switch
    {
        LabStatus.New => L("new", "neu"),
        LabStatus.Exploited => L("exploited", "geknackt"),
        _ => L("solved", "gelöst"),
    };

    protected override void OnInitialized() => Progress.Changed += OnProgressChanged;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
            await Progress.LoadAsync(Registry.Labs.Select(l => l.Slug));
    }

    private void OnProgressChanged() => _ = InvokeAsync(StateHasChanged);

    public virtual void Dispose()
    {
        Progress.Changed -= OnProgressChanged;
        GC.SuppressFinalize(this);
    }
}
