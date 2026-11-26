using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Trainer.Web.Core;

namespace Trainer.Web.Tests;

/// <summary>Checks every registered lab against the lab contract (SPEC §7, §8).</summary>
public class LabContractTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private LabRegistry Registry => factory.Services.GetRequiredService<LabRegistry>();

    public static TheoryData<string> Slugs()
    {
        var registry = new LabRegistry(new FlagService(), new DemoAuth());
        return new TheoryData<string>(registry.Labs.Select(l => l.Slug));
    }

    [Theory, MemberData(nameof(Slugs))]
    public void Exactly_one_fix_option_is_correct(string slug)
    {
        var lab = Registry.Find(slug)!;

        Assert.True(lab.FixOptions.Count >= 3, "At least 3 fix options.");
        Assert.Single(lab.FixOptions, o => o.IsCorrect);
        Assert.Equal(lab.FixOptions.Count, lab.FixOptions.Select(o => o.Id).Distinct().Count());
    }

    [Theory, MemberData(nameof(Slugs))]
    public void Story_is_complete(string slug)
    {
        var story = Registry.Find(slug)!.Story;

        Assert.False(string.IsNullOrWhiteSpace(story.Briefing));
        Assert.False(string.IsNullOrWhiteSpace(story.CallingCard));
        Assert.False(string.IsNullOrWhiteSpace(story.Debrief));
    }

    [Theory, MemberData(nameof(Slugs))]
    public void Every_wrong_option_has_a_barclay_line(string slug)
    {
        var lab = Registry.Find(slug)!;

        Assert.All(lab.FixOptions.Where(o => !o.IsCorrect), o => Assert.False(string.IsNullOrWhiteSpace(o.BarclayLine), o.Id));
        Assert.Equal(3, lab.Hints.Count);
    }

    [Fact]
    public void Slugs_and_act_order_are_unique()
    {
        var labs = Registry.Labs;

        Assert.Equal(labs.Count, labs.Select(l => l.Slug).Distinct().Count());
        Assert.Equal(labs.Count, labs.Select(l => (l.Story.Act, l.Story.Order)).Distinct().Count());
    }

    [Theory, MemberData(nameof(Slugs))]
    public void Default_request_targets_the_labs_api(string slug)
    {
        Assert.StartsWith($"/api/labs/{slug}/", Registry.Find(slug)!.DefaultRequest.Path);
    }
}
