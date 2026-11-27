using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Trainer.Web.Tests;

public class ClientSecretsMissionTests(WebApplicationFactory<Program> factory) : MissionTestBase(factory, "client-secrets")
{
    [Fact]
    public async Task Exploit_reads_the_key_from_appsettings_and_returns_the_flag()
    {
        var client = Client(null);
        var settings = await client.GetFromJsonAsync<JsonElement>($"{Api}/static/appsettings.json");
        var key = settings.GetProperty("Starbase").GetProperty("ApiKey").GetString()!;

        client.DefaultRequestHeaders.Add("X-Api-Key", key);
        var orders = await client.GetFromJsonAsync<JsonElement>($"{Api}/starbase/requisitions");

        Assert.Contains(orders.GetProperty("orders").EnumerateArray(), o => o.GetProperty("trackingCode").GetString() == Flag);
    }

    [Fact]
    public async Task Starbase_rejects_missing_or_wrong_keys()
    {
        var client = Client(null);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync($"{Api}/starbase/requisitions")).StatusCode);

        client.DefaultRequestHeaders.Add("X-Api-Key", "sb74-live-guess");
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync($"{Api}/starbase/requisitions")).StatusCode);
    }

    [Fact]
    public async Task Index_page_is_served()
    {
        var html = await Client(null).GetStringAsync($"{Api}/static/index.html");

        Assert.Contains("appsettings.json", html);
    }

    [Fact]
    public async Task Reset_rotates_the_key()
    {
        var client = Client(null);
        var before = (await client.GetFromJsonAsync<JsonElement>($"{Api}/static/appsettings.json")).GetProperty("Starbase").GetProperty("ApiKey").GetString();
        Mission.Reset();
        var after = (await client.GetFromJsonAsync<JsonElement>($"{Api}/static/appsettings.json")).GetProperty("Starbase").GetProperty("ApiKey").GetString();

        Assert.NotEqual(before, after);
    }
}
