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
        var client = Client(DemoAuth.Player);

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/labs/{Slug}/logs/1001")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/labs/{Slug}/logs/9999")).StatusCode);
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
        Assert.Equal([1003], logs!.Select(l => l.GetProperty("id").GetInt32()));
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
