using Trainer.Web.Core;

namespace Trainer.Web.Tests;

public class FlagServiceTests
{
    [Fact]
    public void Flag_has_expected_format_and_is_stable_per_start()
    {
        var flags = new FlagService();

        var flag = flags.GetFlag("bola");

        Assert.Matches("^FLAG\\{bola-[0-9a-f]{24}\\}$", flag);
        Assert.Equal(flag, flags.GetFlag("bola"));
    }

    [Fact]
    public void Flags_differ_between_starts_and_labs()
    {
        Assert.NotEqual(new FlagService().GetFlag("bola"), new FlagService().GetFlag("bola"));
        var flags = new FlagService();
        Assert.NotEqual(flags.GetFlag("bola"), flags.GetFlag("bfla"));
    }

    [Fact]
    public void Check_accepts_only_the_right_flag()
    {
        var flags = new FlagService();
        var flag = flags.GetFlag("sample");

        Assert.True(flags.Check("sample", flag));
        Assert.True(flags.Check("sample", $"  {flag}\n"));
        Assert.False(flags.Check("sample", flag.ToUpperInvariant()));
        Assert.False(flags.Check("sample", flag[..^1]));
        Assert.False(flags.Check("sample", ""));
        Assert.False(flags.Check("sample", null));
        Assert.False(flags.Check("other", flag));
    }
}

public class DemoAuthTests
{
    [Fact]
    public void Tokens_are_random_and_map_back_to_their_user_and_lab()
    {
        var auth = new DemoAuth();
        var wesley = auth.GetToken("sample", "wesley");
        var riker = auth.GetToken("sample", "riker");

        Assert.NotEqual(wesley, riker);
        Assert.NotEqual(wesley, new DemoAuth().GetToken("sample", "wesley"));
        Assert.Equal(new DemoUser("sample", "wesley"), auth.FindUser("sample", wesley));
        Assert.Equal("riker", auth.FindUser("sample", riker)?.Name);
        Assert.Null(auth.FindUser("other-lab", wesley));
        Assert.Null(auth.FindUser("sample", "not-a-token"));
        Assert.Null(auth.FindUser("sample", (string?)null));
    }
}

public class RequestSenderTests
{
    private static readonly Uri Base = new("http://localhost:5000/");

    [Theory]
    [InlineData("/api/labs/sample/flag")]
    [InlineData("/api/labs/bola/logs/1003?x=1")]
    public void Allows_lab_api_paths(string path) =>
        Assert.True(RequestSender.TryBuildUri(Base, path, out _));

    [Theory]
    [InlineData("")]
    [InlineData("/")]
    [InlineData("/labs/sample")]
    [InlineData("/api/labs/../../etc")]
    [InlineData("/api/labs/%2e%2e/admin")]
    [InlineData("http://evil.example/api/labs/x")]
    [InlineData("//evil.example/api/labs/x")]
    public void Rejects_other_paths(string path) =>
        Assert.False(RequestSender.TryBuildUri(Base, path, out _));
}
