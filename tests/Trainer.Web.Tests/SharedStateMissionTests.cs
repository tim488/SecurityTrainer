using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Trainer.Web.Missions.SharedState;

namespace Trainer.Web.Tests;

public class SharedStateMissionTests : MissionTestBase
{
    // The bot is stopped so it can't overwrite an order between a test's requests.
    public SharedStateMissionTests(WebApplicationFactory<Program> factory) : base(factory, "shared-state") =>
        Shared.BotInterval = Timeout.InfiniteTimeSpan;

    private SharedStateMission Shared => (SharedStateMission)Mission;

    [Fact]
    public async Task Exploit_wesley_sees_rikers_order_and_command_code()
    {
        Shared.SimulateRikerOrder();

        var order = await Client().GetFromJsonAsync<JsonElement>($"{Api}/replicator/order");

        Assert.Equal("riker", order.GetProperty("orderedBy").GetString());
        Assert.Equal(Flag, order.GetProperty("commandCode").GetString());
    }

    [Fact]
    public async Task Wesleys_own_order_has_no_command_code()
    {
        var client = Client();
        var post = await client.PostAsJsonAsync($"{Api}/replicator/order", new { item = "Tea, Earl Grey, hot" });
        post.EnsureSuccessStatusCode();

        var order = await client.GetFromJsonAsync<JsonElement>($"{Api}/replicator/order");
        // The bot may have ordered in between; either way the state is shared.
        Assert.Contains(order.GetProperty("orderedBy").GetString(), new[] { "wesley", "riker" });
    }

    [Fact]
    public async Task Bad_order_is_rejected()
    {
        var response = await Client().PostAsync($"{Api}/replicator/order", new StringContent("{}", System.Text.Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Anonymous_callers_are_rejected()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await Client(null).GetAsync($"{Api}/replicator/order")).StatusCode);
    }

    [Fact]
    public async Task Bot_orders_on_its_own()
    {
        Mission.Reset();
        Shared.BotInterval = TimeSpan.FromMilliseconds(50);

        var client = Client(language: "de");
        string? orderedBy = null;
        for (var i = 0; i < 40 && orderedBy is null; i++)
        {
            await Task.Delay(50);
            var order = await client.GetFromJsonAsync<JsonElement>($"{Api}/replicator/order");
            orderedBy = order.TryGetProperty("orderedBy", out var by) ? by.GetString() : null;
        }

        Assert.Equal("riker", orderedBy);
    }
}
