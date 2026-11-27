using Markdig.Syntax;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Trainer.Web.Core;

namespace Trainer.Web.Tests;

/// <summary>Checks every registered mission against the mission contract.</summary>
public class MissionContractTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private MissionRegistry Registry => factory.Services.GetRequiredService<MissionRegistry>();

    public static TheoryData<string> Slugs()
    {
        var registry = new MissionRegistry(new FlagService(), new DemoAuth());
        return new TheoryData<string>(registry.Missions.Select(l => l.Slug));
    }

    [Theory, MemberData(nameof(Slugs))]
    public void Exactly_one_fix_option_is_correct(string slug)
    {
        var mission = Registry.Find(slug)!;

        Assert.True(mission.FixOptions.Count >= 3, "At least 3 fix options.");
        Assert.Single(mission.FixOptions, o => o.IsCorrect);
        Assert.Equal(mission.FixOptions.Count, mission.FixOptions.Select(o => o.Id).Distinct().Count());
    }

    [Theory, MemberData(nameof(Slugs))]
    public void Story_is_complete_in_every_language(string slug)
    {
        var story = Registry.Find(slug)!.Story;

        Assert.True(story.Briefing.IsComplete, "Briefing");
        Assert.True(story.CallingCard.IsComplete, "Calling card");
        Assert.True(story.Debrief.IsComplete, "Debrief");
    }

    [Theory, MemberData(nameof(Slugs))]
    public void Every_option_has_a_barclay_line(string slug)
    {
        var mission = Registry.Find(slug)!;

        // Including the correct one, otherwise the missing line gives the answer away.
        Assert.All(mission.FixOptions, o => Assert.True(o.BarclayLine?.IsComplete, o.Id));
        Assert.Equal(3, mission.Hints.Count);
    }

    [Theory, MemberData(nameof(Slugs))]
    public void All_mission_text_exists_in_english_and_german(string slug)
    {
        var mission = Registry.Find(slug)!;

        Assert.True(mission.Title.IsComplete, "Title");
        Assert.True(mission.ScenarioMarkdown.IsComplete, "Scenario");
        Assert.True(mission.ExplanationMarkdown.IsComplete, "Explanation");
        Assert.All(mission.Hints, h => Assert.True(h.IsComplete, "Hint"));
        Assert.All(mission.FixOptions, o => Assert.True(o.Feedback.IsComplete, o.Id));
    }

    [Theory, MemberData(nameof(Slugs))]
    public void Endpoint_code_exists_in_both_code_styles(string slug)
    {
        var mission = Registry.Find(slug)!;

        // Fix options that map an endpoint need the controller version too.
        Assert.All(mission.FixOptions.Where(o => MapsEndpoint(o.Code)),
            o => Assert.False(string.IsNullOrWhiteSpace(o.ControllerCode), o.Id));
        Assert.All(mission.FixOptions.Where(o => o.ControllerCode is not null),
            o => Assert.Contains("Controller", o.ControllerCode!));

        foreach (var markdown in CodeStyles.MarkdownOf(mission))
        {
            var blocks = Markdig.Markdown.Parse(markdown).Descendants<FencedCodeBlock>().ToList();
            // An endpoint snippet is always tagged, and every tagged snippet has its counterpart.
            Assert.DoesNotContain(blocks, b => CodeStyles.Of(b) is null && MapsEndpoint(b.Lines.ToString()));
            Assert.Equal(blocks.Count(b => CodeStyles.Of(b) == CodeStyle.MinimalApi), blocks.Count(b => CodeStyles.Of(b) == CodeStyle.Controller));
        }
    }

    // app.MapGet, admin.MapPost, group.MapGroup, …
    private static bool MapsEndpoint(string code) =>
        System.Text.RegularExpressions.Regex.IsMatch(code, @"\.Map(Get|Post|Put|Patch|Delete|Methods|Group)\(");

    [Fact]
    public void Slugs_and_act_order_are_unique()
    {
        var missions = Registry.Missions;

        Assert.Equal(missions.Count, missions.Select(l => l.Slug).Distinct().Count());
        Assert.Equal(missions.Count, missions.Select(l => (l.Story.Act, l.Story.Order)).Distinct().Count());
    }

    [Theory, MemberData(nameof(Slugs))]
    public void Default_request_targets_the_missions_api(string slug)
    {
        Assert.StartsWith($"/api/missions/{slug}/", Registry.Find(slug)!.DefaultRequest.Path);
    }
}
