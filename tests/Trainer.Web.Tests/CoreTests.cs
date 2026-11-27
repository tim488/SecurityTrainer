using Trainer.Web.Core;
using Trainer.Web.Core.Story;

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
        var flag = flags.GetFlag("bola");

        Assert.True(flags.Check("bola", flag));
        Assert.True(flags.Check("bola", $"  {flag}\n"));
        Assert.False(flags.Check("bola", flag.ToUpperInvariant()));
        Assert.False(flags.Check("bola", flag[..^1]));
        Assert.False(flags.Check("bola", ""));
        Assert.False(flags.Check("bola", null));
        Assert.False(flags.Check("other", flag));
    }
}

public class DemoAuthTests
{
    [Fact]
    public void Tokens_are_random_and_map_back_to_their_user_and_lab()
    {
        var auth = new DemoAuth();
        var wesley = auth.GetToken("bola", "wesley");
        var riker = auth.GetToken("bola", "riker");

        Assert.NotEqual(wesley, riker);
        Assert.NotEqual(wesley, new DemoAuth().GetToken("bola", "wesley"));
        Assert.Equal(new DemoUser("bola", "wesley"), auth.FindUser("bola", wesley));
        Assert.Equal("riker", auth.FindUser("bola", riker)?.Name);
        Assert.Null(auth.FindUser("other-lab", wesley));
        Assert.Null(auth.FindUser("bola", "not-a-token"));
        Assert.Null(auth.FindUser("bola", (string?)null));
    }
}

public class RequestSenderTests
{
    private static readonly Uri Base = new("http://localhost:5000/");

    [Theory]
    [InlineData("/api/labs/bola/logs")]
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

public class StoryTests
{
    [Fact]
    public void Acts_and_opening_scene_exist_in_english_and_german()
    {
        Assert.True(Trainer.Web.Core.Story.Acts.OpeningScene.IsComplete);
        Assert.All(Trainer.Web.Core.Story.Acts.All, a =>
        {
            Assert.True(a.Title.IsComplete);
            Assert.True(a.Tagline.IsComplete);
            Assert.True(a.Ending?.IsComplete ?? true);
        });
    }

    [Fact]
    public void Text_picks_the_requested_language()
    {
        var text = new Text("Hints", "Hinweise");

        Assert.Equal("Hints", text[Language.En]);
        Assert.Equal("Hinweise", text[Language.De]);
        Assert.False(new Text("Hints", " ").IsComplete);
    }
}

public class StardateTests
{
    [Theory]
    [InlineData(2026, 1, 1, 0, "47000.0")]   // start of the year
    [InlineData(2026, 7, 2, 12, "47500.0")]  // exactly half way through 2026 (182.5 of 365 days)
    [InlineData(2026, 12, 31, 23, "47999.8")] // cut off, never rounded up to the next year
    [InlineData(2027, 1, 1, 0, "48000.0")]
    public void Converts_date_and_time_to_tng_stardate(int year, int month, int day, int hour, string expected) =>
        Assert.Equal(expected, Trainer.Web.Core.Story.Stardate.Format(new DateTime(year, month, day, hour, 0, 0)));
}

public class StoryProgressTests
{
    private static readonly IReadOnlyList<ILab> Labs = new LabRegistry(new FlagService(), new DemoAuth()).Labs;

    private static Func<string, bool> Completed(params int[] indexes)
    {
        var slugs = indexes.Select(i => Labs[i].Slug).ToHashSet();
        return slugs.Contains;
    }

    [Fact]
    public void Story_mode_opens_only_the_first_mission_at_the_start()
    {
        Assert.False(StoryProgress.IsLocked(Labs, Completed(), storyMode: true, Labs[0]));
        Assert.All(Labs.Skip(1), l => Assert.True(StoryProgress.IsLocked(Labs, Completed(), storyMode: true, l), l.Slug));
        Assert.Same(Labs[0], StoryProgress.Next(Labs, Completed()));
    }

    [Fact]
    public void Completing_a_mission_opens_the_next_one()
    {
        var completed = Completed(0, 1);

        Assert.False(StoryProgress.IsLocked(Labs, completed, storyMode: true, Labs[2]));
        Assert.True(StoryProgress.IsLocked(Labs, completed, storyMode: true, Labs[3]));
        Assert.Same(Labs[2], StoryProgress.Next(Labs, completed));
    }

    [Fact]
    public void Completed_missions_stay_open_and_free_play_opens_everything()
    {
        // Mission 5 was completed in free play; story mode still waits for mission 1.
        var completed = Completed(4);

        Assert.False(StoryProgress.IsLocked(Labs, completed, storyMode: true, Labs[4]));
        Assert.True(StoryProgress.IsLocked(Labs, completed, storyMode: true, Labs[1]));
        Assert.All(Labs, l => Assert.False(StoryProgress.IsLocked(Labs, Completed(), storyMode: false, l)));
    }

    [Fact]
    public void Nothing_is_next_when_everything_is_completed()
    {
        var all = Completed(Enumerable.Range(0, Labs.Count).ToArray());

        Assert.Null(StoryProgress.Next(Labs, all));
        Assert.Null(StoryProgress.Following(Labs, Labs[^1]));
        Assert.Same(Labs[1], StoryProgress.Following(Labs, Labs[0]));
    }
}
