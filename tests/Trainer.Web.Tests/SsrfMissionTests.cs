using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Trainer.Web.Tests;

public class SsrfMissionTests(WebApplicationFactory<Program> factory) : MissionTestBase(factory, "ssrf")
{
    [Theory]
    [InlineData("http://169.254.169.254/latest/credentials")]
    [InlineData("http://2852039166/latest/credentials")]                                         // decimal IP
    [InlineData("http://metadata.internal/latest/credentials")]                                  // DNS alias
    [InlineData("https://redirect.example/?to=http://169.254.169.254/latest/credentials")]       // redirect
    public async Task Exploit_reads_the_core_credentials(string url)
    {
        var preview = await Preview(url);

        Assert.Contains(Flag, preview.GetProperty("snippet").GetString());
    }

    [Fact]
    public async Task Normal_preview_returns_the_title()
    {
        var preview = await Preview("https://memory-alpha.example/");

        Assert.StartsWith("Memory Alpha", preview.GetProperty("title").GetString());
    }

    [Fact]
    public async Task Internal_hosts_are_reachable_and_unknown_hosts_fail()
    {
        Assert.Contains("inventory", (await Preview("http://inventory.internal/")).GetProperty("title").GetString(), StringComparison.OrdinalIgnoreCase);

        var response = await Client().PostAsJsonAsync($"{Api}/link-preview", new { url = "https://nowhere.example/" });
        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
    }

    [Fact]
    public async Task Bad_input_and_anonymous_callers_are_rejected()
    {
        Assert.Equal(HttpStatusCode.BadRequest, (await Client().PostAsJsonAsync($"{Api}/link-preview", new { url = "file:///etc/passwd" })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Client(null).PostAsJsonAsync($"{Api}/link-preview", new { url = "https://memory-alpha.example/" })).StatusCode);
    }

    private async Task<JsonElement> Preview(string url)
    {
        var response = await Client().PostAsJsonAsync($"{Api}/link-preview", new { url });
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }
}
