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

    /// <summary>Missions open one at a time, in story order. Off means free play.</summary>
    public bool StoryMode { get; set; } = true;
}

public enum LabStatus { New, Exploited, Solved }

/// <summary>
/// Per-circuit progress, persisted to <c>localStorage</c> via JS interop.
/// Every interop call is wrapped in try/catch: without storage (prerendering,
/// private mode, blocked storage) the app keeps working and progress is simply
/// lost on reload.
/// </summary>
public sealed class ProgressStore(IJSRuntime js, IConfiguration config)
{
    private const string LabKeyPrefix = "securityTrainer.lab.";
    private const string SettingsKey = "securityTrainer.settings";
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    private readonly Dictionary<string, LabProgress> _labs = new(StringComparer.OrdinalIgnoreCase);
    private bool _loaded;

    /// <summary>Story mode is on unless <c>Trainer:StoryMode</c> is false (e.g. for a workshop); the player's own choice wins.</summary>
    private GlobalSettings DefaultSettings() => new() { StoryMode = config.GetValue("Trainer:StoryMode", true) };

    public GlobalSettings Settings => _settings ??= DefaultSettings();
    private GlobalSettings? _settings;

    /// <summary>Raised whenever progress or settings change (or are first loaded).</summary>
    public event Action? Changed;

    public bool IsLoaded => _loaded;

    /// <summary>Loads settings and the given labs' progress. Call from OnAfterRenderAsync.</summary>
    public async Task LoadAsync(IEnumerable<string> slugs)
    {
        var changed = false;
        if (!_loaded)
        {
            _settings = await ReadAsync<GlobalSettings>(SettingsKey) ?? DefaultSettings();
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

    public async Task SetStoryModeAsync(bool storyMode)
    {
        Settings.StoryMode = storyMode;
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
