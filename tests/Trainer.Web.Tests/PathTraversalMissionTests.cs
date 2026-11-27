using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Trainer.Web.Core.Simulation;

namespace Trainer.Web.Tests;

public class PathTraversalMissionTests(WebApplicationFactory<Program> factory) : MissionTestBase(factory, "path-traversal")
{
    [Theory]
    [InlineData("../restricted/crew-evaluations.pdf")]
    [InlineData("/data/restricted/crew-evaluations.pdf")]
    [InlineData("./../../data/restricted/crew-evaluations.pdf")]
    public async Task Exploit_reads_the_restricted_evaluations(string file)
    {
        var body = await Client().GetStringAsync($"{Api}/archive/download?file={Uri.EscapeDataString(file)}");

        Assert.Contains(Flag, body);
    }

    [Fact]
    public async Task Normal_download_and_listing_work()
    {
        var client = Client();
        var list = await client.GetFromJsonAsync<JsonElement[]>($"{Api}/archive");
        var report = await client.GetStringAsync($"{Api}/archive/download?file=1001.pdf");

        Assert.Equal(4, list!.Length);
        Assert.Contains("Farpoint", report);
        Assert.DoesNotContain(list!, f => f.GetProperty("file").GetString()!.Contains("restricted"));
    }

    [Fact]
    public async Task Missing_files_are_404_and_follow_language()
    {
        Assert.Equal(HttpStatusCode.NotFound, (await Client().GetAsync($"{Api}/archive/download?file=9999.pdf")).StatusCode);
        Assert.Contains("Farpoint-Station", await Client(language: "de").GetStringAsync($"{Api}/archive/download?file=1001.pdf"));
    }

    [Fact]
    public async Task Anonymous_callers_are_rejected()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await Client(null).GetAsync($"{Api}/archive/download?file=1001.pdf")).StatusCode);
    }

    [Theory]
    [InlineData("/data/archive", "x.pdf", "/data/archive/x.pdf")]
    [InlineData("/data/archive", "/etc/passwd", "/etc/passwd")]
    [InlineData("/data/archive/", "../restricted/a", "/data/archive/../restricted/a")]
    public void Combine_behaves_like_path_combine(string a, string b, string expected) =>
        Assert.Equal(expected, VirtualFileSystem.Combine(a, b));

    [Theory]
    [InlineData("/data/archive/../restricted/a", "/data/restricted/a")]
    [InlineData("/../../a/./b", "/a/b")]
    public void GetFullPath_resolves_dots(string path, string expected) =>
        Assert.Equal(expected, VirtualFileSystem.GetFullPath(path));
}
