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
        Assert.Contains("labs/bola", html);
    }

    [Fact]
    public async Task Lab_page_renders()
    {
        var response = await factory.CreateClient().GetAsync("/labs/bola");

        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Someone Else&#x27;s Personal Log", html);
        Assert.Contains("<table>", html); // Markdown tables in the scenario render as tables
    }

    [Theory, MemberData(nameof(LabContractTests.Slugs), MemberType = typeof(LabContractTests))]
    public async Task Every_lab_page_renders(string slug)
    {
        var response = await factory.CreateClient().GetAsync($"/labs/{slug}");

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Jwt_page_shows_the_jwt_editor()
    {
        var html = await factory.WithFreePlay().CreateClient().GetStringAsync("/labs/jwt");

        Assert.Contains("JWT editor", html);
    }

    [Fact]
    public async Task Story_mode_hides_locked_missions_until_progress_is_known()
    {
        // Progress lives in the browser, so the server renders a later mission as neither open nor locked.
        var html = await factory.CreateClient().GetStringAsync("/labs/jwt");

        Assert.DoesNotContain("JWT editor", html);
        Assert.DoesNotContain("Mission locked", html);
    }

    [Fact]
    public async Task Story_mode_locks_later_missions_on_the_overview()
    {
        var html = await factory.CreateClient().GetStringAsync("/");

        Assert.Contains("Someone Else&#x27;s Personal Log", html);
        Assert.Contains("card locked", html);
        Assert.DoesNotContain("href=\"labs/jwt\"", html);

        var freePlay = await factory.WithFreePlay().CreateClient().GetStringAsync("/");
        Assert.DoesNotContain("card locked", freePlay);
        Assert.Contains("href=\"labs/jwt\"", freePlay);
    }
}

public static class FactoryExtensions
{
    /// <summary>Starts the app with story mode off, so every mission page renders on the server.</summary>
    public static WebApplicationFactory<Program> WithFreePlay(this WebApplicationFactory<Program> factory) =>
        factory.WithWebHostBuilder(b => b.UseSetting("Trainer:StoryMode", "false"));
}
