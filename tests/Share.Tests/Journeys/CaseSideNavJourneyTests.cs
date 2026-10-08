using AngleSharp.Dom;
using Share.Tests.Support;

namespace Share.Tests.Journeys;

public class CaseSideNavJourneyTests : IClassFixture<AppFactory>
{
    readonly AppFactory _factory;
    readonly HttpClient _client;

    public CaseSideNavJourneyTests(AppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    static string Text(IElement? e) => e?.TextContent.Trim() ?? "";

    async Task<IDocument> Case(string title)
    {
        var list = await _client.GetPageAsync("/Cases");
        return await _client.GetPageAsync(list.QuerySelectorAll("tbody a").First(a => Text(a) == title).GetAttribute("href")!);
    }

    static List<string> Links(IDocument page, int listIndex) =>
        page.QuerySelectorAll("nav[aria-label='Case'] ul")[listIndex].QuerySelectorAll("a").Select(Text).ToList();

    [Fact]
    public async Task Sections_link_to_headings_on_the_page()
    {
        await _factory.SeedThroughAsync(1);
        var page = await Case("Melnyk household");
        Html.AssertAccessible(page);

        var nav = page.QuerySelector("nav[aria-label='Case']");
        Assert.NotNull(nav);
        Assert.Equal(["Case summary", "Safeguarding checks", "Guests (3)", "Visa applications (1)", "Case history"], Links(page, 0));
        foreach (var a in nav!.QuerySelectorAll("ul")[0].QuerySelectorAll("a"))
        {
            var target = page.QuerySelector(a.GetAttribute("href")!);
            Assert.NotNull(target);
            Assert.Equal("H2", target!.TagName);
        }
    }

    [Fact]
    public async Task Actions_offer_record_arrival_until_guests_arrive()
    {
        await _factory.SeedThroughAsync(1);
        var page = await Case("Melnyk household");
        Assert.Equal([
            "Update check 1: accommodation exists",
            "Update check 2: accommodation suitable",
            "Update check 3: DBS check and sponsor suitable",
            "Record arrival",
        ], Links(page, 1));

        // Every action leads to a working, accessible page.
        foreach (var href in page.QuerySelectorAll("nav[aria-label='Case'] ul")[1].QuerySelectorAll("a").Select(a => a.GetAttribute("href")!))
            Html.AssertAccessible(await _client.GetPageAsync(href));
    }

    [Fact]
    public async Task Once_the_feed_shows_an_arrival_check_4_can_be_updated_instead()
    {
        await _factory.SeedThroughAsync(2);
        var actions = Links(await Case("Melnyk household"), 1);
        Assert.Contains("Update check 4: guests have arrived in their accommodation", actions);
        Assert.DoesNotContain("Record arrival", actions);
    }
}
