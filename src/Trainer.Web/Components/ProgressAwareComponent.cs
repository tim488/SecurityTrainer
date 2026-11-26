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
