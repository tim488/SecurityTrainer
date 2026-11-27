using Microsoft.AspNetCore.Mvc.Testing;

namespace Trainer.Web.Tests;

public class AppTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task App_starts_and_home_page_renders_mission_list()
    {
        var response = await factory.CreateClient().GetAsync("/");

        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Missions", html);
        Assert.Contains("Uninvited Guest", html);
        Assert.Contains("missions/bola", html);
    }

    [Fact]
    public async Task Unknown_pages_show_the_not_found_page()
    {
        var response = await factory.CreateClient().GetAsync("/does-not-exist");

        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("Not found", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Unknown_mission_apis_return_a_plain_404()
    {
        var response = await factory.CreateClient().GetAsync("/api/missions/bola/does-not-exist");

        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
        Assert.DoesNotContain("<html", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Mission_page_renders()
    {
        var response = await factory.CreateClient().GetAsync("/missions/bola");

        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Someone Else&#x27;s Personal Log", html);
        Assert.Contains("<table>", html); // Markdown tables in the scenario render as tables
    }

    [Fact]
    public async Task Old_mission_addresses_redirect_to_missions()
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/labs/bola");

        Assert.Equal(System.Net.HttpStatusCode.MovedPermanently, response.StatusCode);
        Assert.Equal("/missions/bola", response.Headers.Location?.OriginalString);
    }

    [Theory, MemberData(nameof(MissionContractTests.Slugs), MemberType = typeof(MissionContractTests))]
    public async Task Every_mission_page_renders(string slug)
    {
        var response = await factory.CreateClient().GetAsync($"/missions/{slug}");

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Jwt_page_shows_the_jwt_editor()
    {
        var html = await factory.WithFreePlay().CreateClient().GetStringAsync("/missions/jwt");

        Assert.Contains("JWT editor", html);
    }

    [Fact]
    public async Task Story_mode_hides_locked_missions_until_progress_is_known()
    {
        // Progress lives in the browser, so the server renders a later mission as neither open nor locked.
        var html = await factory.CreateClient().GetStringAsync("/missions/jwt");

        Assert.DoesNotContain("JWT editor", html);
        Assert.DoesNotContain("Mission locked", html);
    }

    [Fact]
    public async Task Story_mode_locks_later_missions_on_the_overview()
    {
        var html = await factory.CreateClient().GetStringAsync("/");

        Assert.Contains("Someone Else&#x27;s Personal Log", html);
        Assert.Contains("card locked", html);
        Assert.DoesNotContain("href=\"missions/jwt\"", html);

        var freePlay = await factory.WithFreePlay().CreateClient().GetStringAsync("/");
        Assert.DoesNotContain("card locked", freePlay);
        Assert.Contains("href=\"missions/jwt\"", freePlay);
    }
}

public static class FactoryExtensions
{
    /// <summary>Starts the app with story mode off, so every mission page renders on the server.</summary>
    public static WebApplicationFactory<Program> WithFreePlay(this WebApplicationFactory<Program> factory) =>
        factory.WithWebHostBuilder(b => b.UseSetting("Trainer:StoryMode", "false"));
}
