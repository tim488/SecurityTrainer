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
        var html = await factory.CreateClient().GetStringAsync("/labs/jwt");

        Assert.Contains("JWT editor", html);
    }
}
