using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.JSInterop;

namespace Trainer.Web.Core;

public sealed class LabProgress
{
    public int HintsRevealed { get; set; }
    public bool FlagSubmitted { get; set; }
    public bool FixAnsweredCorrectly { get; set; }
    public int Attempts { get; set; }

    public bool IsCompleted => FlagSubmitted && FixAnsweredCorrectly;
}

public sealed class GlobalSettings
{
    public Language Language { get; set; } = Language.En;
    public CodeStyle CodeStyle { get; set; } = CodeStyle.MinimalApi;
}

public enum LabStatus { New, Exploited, Solved }

/// <summary>
/// Per-circuit progress (SPEC §5.6), persisted to <c>localStorage</c> via JS interop.
/// Every interop call is wrapped in try/catch: without storage (prerendering,
/// private mode, blocked storage) the app keeps working and progress is simply
/// lost on reload.
/// </summary>
public sealed class ProgressStore(IJSRuntime js)
{
    private const string LabKeyPrefix = "securityTrainer.lab.";
    private const string SettingsKey = "securityTrainer.settings";
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    private readonly Dictionary<string, LabProgress> _labs = new(StringComparer.OrdinalIgnoreCase);
    private bool _loaded;

    public GlobalSettings Settings { get; private set; } = new();

    /// <summary>Raised whenever progress or settings change (or are first loaded).</summary>
    public event Action? Changed;

    public bool IsLoaded => _loaded;

    /// <summary>Loads settings and the given labs' progress. Call from OnAfterRenderAsync.</summary>
    public async Task LoadAsync(IEnumerable<string> slugs)
    {
        var changed = false;
        if (!_loaded)
        {
            Settings = await ReadAsync<GlobalSettings>(SettingsKey) ?? new GlobalSettings();
            _loaded = true;
            changed = true;
        }

        foreach (var slug in slugs)
        {
            if (_labs.ContainsKey(slug)) continue;
            _labs[slug] = await ReadAsync<LabProgress>(LabKeyPrefix + slug) ?? new LabProgress();
            changed = true;
        }

        if (changed) Changed?.Invoke();
    }

    public LabProgress Get(string slug)
    {
        if (!_labs.TryGetValue(slug, out var progress))
            _labs[slug] = progress = new LabProgress();
        return progress;
    }

    public LabStatus StatusOf(string slug)
    {
        var p = Get(slug);
        return p.IsCompleted ? LabStatus.Solved : p.FlagSubmitted ? LabStatus.Exploited : LabStatus.New;
    }

    public async Task UpdateAsync(string slug, Action<LabProgress> change)
    {
        var progress = Get(slug);
        change(progress);
        await WriteAsync(LabKeyPrefix + slug, progress);
        Changed?.Invoke();
    }

    public async Task ClearAsync(string slug)
    {
        _labs[slug] = new LabProgress();
        try { await js.InvokeVoidAsync("localStorage.removeItem", LabKeyPrefix + slug); }
        catch (Exception ex) when (IsInteropFailure(ex)) { }
        Changed?.Invoke();
    }

    public async Task SetLanguageAsync(Language language)
    {
        Settings.Language = language;
        await WriteAsync(SettingsKey, Settings);
        Changed?.Invoke();
    }

    public async Task SetCodeStyleAsync(CodeStyle style)
    {
        Settings.CodeStyle = style;
        await WriteAsync(SettingsKey, Settings);
        Changed?.Invoke();
    }

    private async Task<T?> ReadAsync<T>(string key) where T : class
    {
        try
        {
            var json = await js.InvokeAsync<string?>("localStorage.getItem", key);
            return string.IsNullOrEmpty(json) ? null : JsonSerializer.Deserialize<T>(json, Json);
        }
        catch (Exception ex) when (IsInteropFailure(ex) || ex is JsonException)
        {
            return null;
        }
    }

    private async Task WriteAsync<T>(string key, T value)
    {
        try { await js.InvokeVoidAsync("localStorage.setItem", key, JsonSerializer.Serialize(value, Json)); }
        catch (Exception ex) when (IsInteropFailure(ex)) { }
    }

    private static bool IsInteropFailure(Exception ex) =>
        ex is JSException or JSDisconnectedException or InvalidOperationException or TaskCanceledException;
}
