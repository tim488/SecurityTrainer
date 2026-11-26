using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Trainer.Web.Core;

namespace Trainer.Web.Tests;

public class AppTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task App_starts_and_home_page_renders_case_board()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/");

        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Case board", html);
        Assert.Contains("labs/sample", html);
    }

    [Fact]
    public async Task Lab_page_renders()
    {
        var response = await factory.CreateClient().GetAsync("/labs/sample");

        response.EnsureSuccessStatusCode();
        Assert.Contains("Placeholder", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Sample_lab_returns_flag_for_wesley()
    {
        var (auth, flags) = (Get<DemoAuth>(), Get<FlagService>());
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.GetToken("sample", DemoAuth.Player));

        var body = await client.GetFromJsonAsync<Dictionary<string, string>>("/api/labs/sample/flag");

        Assert.Equal(flags.GetFlag("sample"), body!["flag"]);
    }

    [Fact]
    public async Task Sample_lab_rejects_missing_and_other_tokens()
    {
        var auth = Get<DemoAuth>();
        var client = factory.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/labs/sample/flag")).StatusCode);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.GetToken("sample", "riker"));
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/labs/sample/flag")).StatusCode);
    }

    [Fact]
    public void Default_request_carries_wesleys_token()
    {
        var lab = Get<LabRegistry>().Find("sample")!;

        Assert.Equal($"Bearer {Get<DemoAuth>().GetToken("sample", DemoAuth.Player)}", lab.DefaultRequest.Headers["Authorization"]);
    }

    private T Get<T>() where T : notnull => factory.Services.GetRequiredService<T>();
}
