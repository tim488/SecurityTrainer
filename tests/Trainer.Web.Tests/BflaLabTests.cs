using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Trainer.Web.Core;

namespace Trainer.Web.Tests;

public class BflaLabTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private const string Slug = "bfla";

    [Fact]
    public async Task Exploit_exports_the_roster_as_crew_and_returns_the_flag()
    {
        var export = await Client(DemoAuth.Player).GetFromJsonAsync<JsonElement>($"/api/labs/{Slug}/admin/crew/export");

        Assert.Equal(Get<FlagService>().GetFlag(Slug), export.GetProperty("verificationCode").GetString());
        Assert.Equal("wesley", export.GetProperty("exportedBy").GetString());
        Assert.True(export.GetProperty("crew").GetArrayLength() >= 10);
    }

    [Fact]
    public async Task Menu_hides_the_admin_entry_from_crew_only()
    {
        var crew = await Client(DemoAuth.Player).GetFromJsonAsync<JsonElement[]>($"/api/labs/{Slug}/menu");
        var admin = await Client("riker").GetFromJsonAsync<JsonElement[]>($"/api/labs/{Slug}/menu");

        Assert.Equal(["/me"], crew!.Select(i => i.GetProperty("href").GetString()));
        Assert.Equal(["/me", "/admin/crew/export"], admin!.Select(i => i.GetProperty("href").GetString()));
    }

    [Fact]
    public async Task Normal_use_still_works()
    {
        var me = await Client(DemoAuth.Player).GetFromJsonAsync<JsonElement>($"/api/labs/{Slug}/me");
        var export = await Client("riker").GetFromJsonAsync<JsonElement>($"/api/labs/{Slug}/admin/crew/export");

        Assert.Equal("crew", me.GetProperty("role").GetString());
        Assert.Equal("Wesley Crusher", me.GetProperty("name").GetString());
        Assert.Equal("riker", export.GetProperty("exportedBy").GetString());
    }

    [Fact]
    public async Task Roster_follows_accept_language()
    {
        var client = Client(DemoAuth.Player);
        client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("de");

        var me = await client.GetFromJsonAsync<JsonElement>($"/api/labs/{Slug}/me");

        Assert.Equal("Fähnrich auf Probe", me.GetProperty("rank").GetString());
    }

    [Fact]
    public async Task Anonymous_callers_are_rejected()
    {
        var client = factory.CreateClient();

        foreach (var path in new[] { "me", "menu", "admin/crew/export" })
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync($"/api/labs/{Slug}/{path}")).StatusCode);
    }

    [Fact]
    public async Task Reset_restores_initial_data()
    {
        var lab = Get<LabRegistry>().Find(Slug)!;
        await Client("riker").GetAsync($"/api/labs/{Slug}/admin/crew/export");
        lab.Reset();

        var export = await Client("riker").GetFromJsonAsync<JsonElement>($"/api/labs/{Slug}/admin/crew/export");
        Assert.Equal(1, export.GetProperty("exportNumber").GetInt32());
    }

    [Fact]
    public void Default_request_loads_wesleys_menu_with_his_token()
    {
        var request = Get<LabRegistry>().Find(Slug)!.DefaultRequest;

        Assert.Equal(("GET", $"/api/labs/{Slug}/menu"), (request.Method, request.Path));
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
