using Microsoft.Extensions.Configuration;
using Microsoft.JSInterop;
using Trainer.Web.Core;

namespace Trainer.Web.Tests;

public class ProgressStoreTests
{
    private const string Solved = """{"flagSubmitted":true,"fixAnsweredCorrectly":true,"attempts":2}""";

    private static ProgressStore Store(FakeLocalStorage storage) => new(storage, new ConfigurationBuilder().Build());

    [Fact]
    public async Task Saved_progress_loads_even_if_the_page_rendered_first()
    {
        var storage = new FakeLocalStorage { ["securityTrainer.mission.bola"] = Solved };
        var store = Store(storage);

        Assert.False(store.Get("bola").IsCompleted);   // first render, before loading
        await store.LoadAsync(["bola"]);

        Assert.True(store.Get("bola").IsCompleted);
    }

    [Fact]
    public async Task Rendering_before_loading_does_not_overwrite_saved_progress()
    {
        var storage = new FakeLocalStorage { ["securityTrainer.mission.bola"] = Solved };
        var store = Store(storage);

        store.Get("bola");
        await store.LoadAsync(["bola"]);
        await store.UpdateAsync("bola", p => p.HintsRevealed++);

        Assert.Contains("\"fixAnsweredCorrectly\":true", storage["securityTrainer.mission.bola"]);
    }

    [Fact]
    public async Task A_reset_while_loading_is_not_undone_by_the_load()
    {
        var storage = new FakeLocalStorage { ["securityTrainer.mission.bola"] = Solved };
        var store = Store(storage);
        // The player clicks Reset just after the saved progress was read, before it is applied.
        storage.AfterGet = key => { if (key == "securityTrainer.mission.bola") store.ClearAsync("bola").GetAwaiter().GetResult(); };

        await store.LoadAsync(["bola"]);

        Assert.False(store.Get("bola").IsCompleted);
        Assert.False(storage.ContainsKey("securityTrainer.mission.bola"));
    }

    [Fact]
    public async Task Clearing_all_forgets_every_mission_but_keeps_the_settings()
    {
        var storage = new FakeLocalStorage
        {
            ["securityTrainer.mission.bola"] = Solved,
            ["securityTrainer.mission.jwt"] = Solved,
            ["securityTrainer.settings"] = """{"language":"De","storyMode":false}""",
        };
        var store = Store(storage);
        await store.LoadAsync(["bola", "jwt"]);

        await store.ClearAllAsync(["bola", "jwt"]);

        Assert.False(store.Get("bola").IsCompleted);
        Assert.False(store.Get("jwt").IsCompleted);
        Assert.Equal(["securityTrainer.settings"], storage.Keys);
        Assert.Equal(Language.De, store.Settings.Language);
    }

    [Fact]
    public async Task Progress_saved_under_the_old_lab_keys_is_moved()
    {
        var storage = new FakeLocalStorage { ["securityTrainer.lab.bola"] = Solved };
        var store = Store(storage);

        await store.LoadAsync(["bola"]);

        Assert.True(store.Get("bola").IsCompleted);
        Assert.True(storage.ContainsKey("securityTrainer.mission.bola"));
        Assert.False(storage.ContainsKey("securityTrainer.lab.bola"));
    }

    /// <summary>The browser's localStorage, in memory: getItem, setItem and removeItem.</summary>
    private sealed class FakeLocalStorage : Dictionary<string, string>, IJSRuntime
    {
        /// <summary>Runs after getItem has read its value, to simulate something happening mid-load.</summary>
        public Action<string>? AfterGet { get; set; }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
            InvokeAsync<TValue>(identifier, CancellationToken.None, args);

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            var key = (string)args![0]!;
            object? result = null;
            switch (identifier)
            {
                case "localStorage.getItem":
                    result = TryGetValue(key, out var value) ? value : null;
                    AfterGet?.Invoke(key);
                    break;
                case "localStorage.setItem": this[key] = (string)args[1]!; break;
                case "localStorage.removeItem": Remove(key); break;
                default: throw new NotSupportedException(identifier);
            }
            return ValueTask.FromResult((TValue)result!);
        }
    }
}
