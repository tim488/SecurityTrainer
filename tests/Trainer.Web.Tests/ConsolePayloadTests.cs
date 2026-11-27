using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Trainer.Web.Core;

namespace Trainer.Web.Tests;

/// <summary>Payloads typed into the request console must reach the mission unchanged.</summary>
public class ConsolePayloadTests(WebApplicationFactory<Program> factory) : MissionTestBase(factory, "sql-injection")
{
    [Theory]
    [InlineData("/api/missions/sql-injection/cargo?search=' OR 1=1 --", "search", "' OR 1=1 --")]
    [InlineData("/api/missions/path-traversal/archive/download?file=../restricted/crew-evaluations.pdf", "file", "../restricted/crew-evaluations.pdf")]
    [InlineData("/api/missions/path-traversal/archive/download?file=....//restricted/x.pdf", "file", "....//restricted/x.pdf")]
    public void Console_keeps_query_payloads_intact(string path, string key, string expected)
    {
        Assert.True(RequestSender.TryBuildUri(new Uri("http://localhost:5000/"), path, out var uri));

        var query = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(uri.Query);
        Assert.Equal(expected, query[key].ToString());
    }

    [Fact]
    public async Task Sql_payload_with_spaces_works_through_the_built_uri()
    {
        Assert.True(RequestSender.TryBuildUri(new Uri("http://localhost/"), "/api/missions/sql-injection/cargo?search=' OR 1=1 --", out var uri));

        var items = await Client().GetFromJsonAsync<JsonElement[]>(uri.PathAndQuery);

        Assert.Contains(items!, i => i.GetProperty("description").GetString()!.Contains(Flag));
    }
}
