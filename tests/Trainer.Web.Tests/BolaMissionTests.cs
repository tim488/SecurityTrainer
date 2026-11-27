using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Trainer.Web.Core;

namespace Trainer.Web.Tests;

public class BolaMissionTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private const string Slug = "bola";

    [Fact]
    public async Task Exploit_reads_rikers_log_and_returns_the_flag()
    {
        var log = await Client(DemoAuth.Player).GetFromJsonAsync<JsonElement>($"/api/missions/{Slug}/logs/1003");

        Assert.Equal("riker", log.GetProperty("ownerId").GetString());
        Assert.Contains(Get<FlagService>().GetFlag(Slug), log.GetProperty("notes").GetString());
    }

    [Fact]
    public async Task List_only_shows_own_logs_without_notes()
    {
        var logs = await Client(DemoAuth.Player).GetFromJsonAsync<JsonElement[]>($"/api/missions/{Slug}/logs");

        Assert.Equal([1001, 1002], logs!.Select(l => l.GetProperty("id").GetInt32()));
        Assert.All(logs!, l => Assert.False(l.TryGetProperty("notes", out _)));
    }

    [Fact]
    public async Task Normal_use_still_works()
    {
        var log = await Client(DemoAuth.Player).GetFromJsonAsync<JsonElement>($"/api/missions/{Slug}/logs/1001");

        Assert.Equal("wesley", log.GetProperty("ownerId").GetString());
    }

    [Fact]
    public async Task Unknown_ids_loop_through_the_crew_joke_logs()
    {
        Get<MissionRegistry>().Find(Slug)!.Reset();
        var client = Client(DemoAuth.Player);
        var flag = Get<FlagService>().GetFlag(Slug);

        var titles = new List<string>();
        for (var i = 0; i < 11; i++)
        {
            var id = 5000 + i * 37;
            var log = await client.GetFromJsonAsync<JsonElement>($"/api/missions/{Slug}/logs/{id}");

            Assert.Equal(id, log.GetProperty("id").GetInt32());
            Assert.DoesNotContain(log.GetProperty("ownerId").GetString(), new[] { "wesley", "riker" });
            Assert.DoesNotContain(flag, log.GetProperty("notes").GetString());
            titles.Add(log.GetProperty("title").GetString()!);
        }

        Assert.Equal(10, titles.Take(10).Distinct().Count()); // all ten, no repeats
        Assert.Equal(titles[0], titles[10]);                  // then it starts over
    }

    [Fact]
    public async Task Logs_follow_accept_language()
    {
        var client = Client(DemoAuth.Player);
        client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("de");

        var log = await client.GetFromJsonAsync<JsonElement>($"/api/missions/{Slug}/logs/1004");

        Assert.Equal("Pokerabend", log.GetProperty("title").GetString());
    }

    [Fact]
    public async Task Anonymous_callers_are_rejected()
    {
        var client = factory.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync($"/api/missions/{Slug}/logs")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync($"/api/missions/{Slug}/logs/1003")).StatusCode);
    }

    [Fact]
    public async Task Reset_restores_initial_data()
    {
        Get<MissionRegistry>().Find(Slug)!.Reset();

        var logs = await Client("riker").GetFromJsonAsync<JsonElement[]>($"/api/missions/{Slug}/logs");
        Assert.Equal([1003, 1004, 1005, 1006, 1007], logs!.Select(l => l.GetProperty("id").GetInt32()));
    }

    [Fact]
    public void Default_request_lists_wesleys_logs_with_his_token()
    {
        var request = Get<MissionRegistry>().Find(Slug)!.DefaultRequest;

        Assert.Equal(("GET", $"/api/missions/{Slug}/logs"), (request.Method, request.Path));
        Assert.Equal($"Bearer {Get<DemoAuth>().GetToken(Slug, DemoAuth.Player)}", request.Headers["Authorization"]);
    }

    private HttpClient Client(string user)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Get<DemoAuth>().GetToken(Slug, user));
        return client;
    }

    private T Get<T>() where T : notnull => factory.Services.GetRequiredService<T>();
}
