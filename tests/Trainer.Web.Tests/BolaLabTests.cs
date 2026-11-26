using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Trainer.Web.Core;

namespace Trainer.Web.Tests;

public class BolaLabTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private const string Slug = "bola";

    [Fact]
    public async Task Exploit_reads_rikers_log_and_returns_the_flag()
    {
        var log = await Client(DemoAuth.Player).GetFromJsonAsync<JsonElement>($"/api/labs/{Slug}/logs/1003");

        Assert.Equal("riker", log.GetProperty("ownerId").GetString());
        Assert.Contains(Get<FlagService>().GetFlag(Slug), log.GetProperty("notes").GetString());
    }

    [Fact]
    public async Task List_only_shows_own_logs_without_notes()
    {
        var logs = await Client(DemoAuth.Player).GetFromJsonAsync<JsonElement[]>($"/api/labs/{Slug}/logs");

        Assert.Equal([1001, 1002], logs!.Select(l => l.GetProperty("id").GetInt32()));
        Assert.All(logs!, l => Assert.False(l.TryGetProperty("notes", out _)));
    }

    [Fact]
    public async Task Normal_use_still_works()
    {
        var log = await Client(DemoAuth.Player).GetFromJsonAsync<JsonElement>($"/api/labs/{Slug}/logs/1001");

        Assert.Equal("wesley", log.GetProperty("ownerId").GetString());
    }

    [Fact]
    public async Task Unknown_ids_return_a_crew_joke_log_that_stays_the_same_per_id()
    {
        var client = Client(DemoAuth.Player);
        var flag = Get<FlagService>().GetFlag(Slug);

        var owners = new HashSet<string>();
        foreach (var id in new[] { 1, 999, 1008, 1050, 4711, 9999, 123456 })
        {
            var log = await client.GetFromJsonAsync<JsonElement>($"/api/labs/{Slug}/logs/{id}");
            var again = await client.GetFromJsonAsync<JsonElement>($"/api/labs/{Slug}/logs/{id}");

            Assert.Equal(id, log.GetProperty("id").GetInt32());
            Assert.Equal(log.GetProperty("notes").GetString(), again.GetProperty("notes").GetString());
            Assert.DoesNotContain(flag, log.GetProperty("notes").GetString());
            owners.Add(log.GetProperty("ownerId").GetString()!);
        }

        Assert.DoesNotContain("wesley", owners);
        Assert.DoesNotContain("riker", owners);
        Assert.True(owners.Count > 1, "Different IDs should give different crew logs.");
    }

    [Fact]
    public async Task Logs_follow_accept_language()
    {
        var client = Client(DemoAuth.Player);
        client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("de");

        var log = await client.GetFromJsonAsync<JsonElement>($"/api/labs/{Slug}/logs/1004");

        Assert.Equal("Pokerabend", log.GetProperty("title").GetString());
    }

    [Fact]
    public async Task Anonymous_callers_are_rejected()
    {
        var client = factory.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync($"/api/labs/{Slug}/logs")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync($"/api/labs/{Slug}/logs/1003")).StatusCode);
    }

    [Fact]
    public async Task Reset_restores_initial_data()
    {
        Get<LabRegistry>().Find(Slug)!.Reset();

        var logs = await Client("riker").GetFromJsonAsync<JsonElement[]>($"/api/labs/{Slug}/logs");
        Assert.Equal([1003, 1004, 1005, 1006, 1007], logs!.Select(l => l.GetProperty("id").GetInt32()));
    }

    [Fact]
    public void Default_request_lists_wesleys_logs_with_his_token()
    {
        var request = Get<LabRegistry>().Find(Slug)!.DefaultRequest;

        Assert.Equal(("GET", $"/api/labs/{Slug}/logs"), (request.Method, request.Path));
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
