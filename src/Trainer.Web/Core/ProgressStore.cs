using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.JSInterop;

namespace Trainer.Web.Core;

public sealed class MissionProgress
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

public enum MissionStatus { New, Exploited, Solved }

/// <summary>
/// Per-circuit progress, persisted to <c>localStorage</c> via JS interop.
/// Every interop call is wrapped in try/catch: without storage (prerendering,
/// private mode, blocked storage) the app keeps working and progress is simply
/// lost on reload.
/// </summary>
public sealed class ProgressStore(IJSRuntime js, IConfiguration config)
{
    private const string MissionKeyPrefix = "securityTrainer.mission.";
    private const string OldKeyPrefix = "securityTrainer.lab.";   // used before the lab → mission rename
    private const string SettingsKey = "securityTrainer.settings";
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    private readonly Dictionary<string, MissionProgress> _missions = new(StringComparer.OrdinalIgnoreCase);
    private bool _loaded;

    /// <summary>Story mode is on unless <c>Trainer:StoryMode</c> is false (e.g. for a workshop); the player's own choice wins.</summary>
    private GlobalSettings DefaultSettings() => new() { StoryMode = config.GetValue("Trainer:StoryMode", true) };

    public GlobalSettings Settings => _settings ??= DefaultSettings();
    private GlobalSettings? _settings;

    /// <summary>Raised whenever progress or settings change (or are first loaded).</summary>
    public event Action? Changed;

    public bool IsLoaded => _loaded;

    /// <summary>Loads settings and the given missions' progress. Call from OnAfterRenderAsync.</summary>
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
            if (_missions.ContainsKey(slug)) continue;
            var loaded = await ReadAsync<MissionProgress>(MissionKeyPrefix + slug)
                ?? await MigrateAsync(slug)
                ?? new MissionProgress();
            // A reset or update that happened while this was loading wins over the stored value.
            _missions.TryAdd(slug, loaded);
            changed = true;
        }

        if (changed) Changed?.Invoke();
    }

    /// <summary>
    /// The mission's progress, or an empty one if nothing is loaded yet. Doesn't store the empty one:
    /// pages render before <see cref="LoadAsync"/> runs, and a stored placeholder would hide the saved progress.
    /// </summary>
    public MissionProgress Get(string slug) => _missions.GetValueOrDefault(slug) ?? new MissionProgress();

    public MissionStatus StatusOf(string slug)
    {
        var p = Get(slug);
        return p.IsCompleted ? MissionStatus.Solved : p.FlagSubmitted ? MissionStatus.Exploited : MissionStatus.New;
    }

    public async Task UpdateAsync(string slug, Action<MissionProgress> change)
    {
        var progress = _missions[slug] = Get(slug);
        change(progress);
        await WriteAsync(MissionKeyPrefix + slug, progress);
        Changed?.Invoke();
    }

    public async Task ClearAsync(string slug)
    {
        _missions[slug] = new MissionProgress();
        try { await js.InvokeVoidAsync("localStorage.removeItem", MissionKeyPrefix + slug); }
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

    /// <summary>Moves progress saved under the old key prefix to the new one, so nobody loses it.</summary>
    private async Task<MissionProgress?> MigrateAsync(string slug)
    {
        var old = await ReadAsync<MissionProgress>(OldKeyPrefix + slug);
        if (old is null) return null;
        await WriteAsync(MissionKeyPrefix + slug, old);
        try { await js.InvokeVoidAsync("localStorage.removeItem", OldKeyPrefix + slug); }
        catch (Exception ex) when (IsInteropFailure(ex)) { }
        return old;
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
