using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Trainer.Web.Tests;

public class MassAssignmentLabTests(WebApplicationFactory<Program> factory) : LabTestBase(factory, "mass-assignment")
{
    [Fact]
    public async Task Exploit_promotes_wesley_and_returns_the_flag()
    {
        Lab.Reset();
        var client = Client();
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync($"{Api}/admin/secret")).StatusCode);

        var put = await client.PutAsJsonAsync($"{Api}/profile",
            new { id = 7, displayName = "Wesley Crusher", email = "wesley@enterprise.starfleet", isAdmin = true, credits = 120 });
        put.EnsureSuccessStatusCode();

        var secret = await client.GetFromJsonAsync<JsonElement>($"{Api}/admin/secret");
        Assert.Equal(Flag, secret.GetProperty("authCode").GetString());
    }

    [Fact]
    public async Task Normal_profile_update_works_and_keeps_the_id()
    {
        Lab.Reset();
        var client = Client();

        var put = await client.PutAsJsonAsync($"{Api}/profile",
            new { id = 999, displayName = "Wes", email = "wes@enterprise.starfleet", isAdmin = false, credits = 120 });
        var profile = await put.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(7, profile.GetProperty("id").GetInt32());
        Assert.Equal("Wes", profile.GetProperty("displayName").GetString());
    }

    [Fact]
    public async Task Crew_list_shows_the_fake_admiral()
    {
        var crew = await Client().GetFromJsonAsync<JsonElement[]>($"{Api}/crew");

        Assert.Contains(crew!, c => c.GetProperty("displayName").GetString() == "Admiral L. Soong" && c.GetProperty("isAdmin").GetBoolean());
    }

    [Fact]
    public async Task Invalid_json_is_a_bad_request()
    {
        var response = await Client().PutAsync($"{Api}/profile", new StringContent("{ nope", System.Text.Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Anonymous_callers_are_rejected()
    {
        foreach (var path in new[] { "profile", "crew", "admin/secret" })
            Assert.Equal(HttpStatusCode.Unauthorized, (await Client(null).GetAsync($"{Api}/{path}")).StatusCode);
    }

    [Fact]
    public async Task Reset_restores_initial_data()
    {
        var client = Client();
        await client.PutAsJsonAsync($"{Api}/profile", new { isAdmin = true });
        Lab.Reset();

        var profile = await client.GetFromJsonAsync<JsonElement>($"{Api}/profile");
        Assert.False(profile.GetProperty("isAdmin").GetBoolean());
        Assert.Equal(120, profile.GetProperty("credits").GetInt32());
    }
}
