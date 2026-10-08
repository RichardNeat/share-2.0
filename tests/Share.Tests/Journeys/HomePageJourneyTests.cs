using AngleSharp.Dom;
using Microsoft.Extensions.DependencyInjection;
using Share.Tests.Support;
using Share.Web.Models;
using Share.Web.Services;

namespace Share.Tests.Journeys;

public class HomePageJourneyTests : IClassFixture<AppFactory>
{
    readonly AppFactory _factory;

    public HomePageJourneyTests(AppFactory factory) => _factory = factory;

    static string Text(IElement? e) => e?.TextContent.Trim() ?? "";

    [Fact]
    public async Task Home_page_shows_welcome_then_announcements_then_tiles()
    {
        await _factory.SeedThroughAsync(1);
        var home = await _factory.CreateClient().GetPageAsync("/");
        Html.AssertAccessible(home);

        var welcome = home.QuerySelector("main .app-welcome")!;
        Assert.Equal("Welcome to The Share", Text(welcome.QuerySelector("h1")));
        Assert.Equal("Use this service to record and manage sponsor and guest data relating to the Homes for Ukraine scheme.", Text(welcome.QuerySelector("p")));

        var announcements = home.QuerySelector("main .govuk-notification-banner")!;
        Assert.Equal("Announcements", Text(announcements.QuerySelector(".govuk-notification-banner__title")));
        Assert.Equal(["Dev Share"], announcements.QuerySelectorAll("h3").Select(Text));
        Assert.Contains("Built from scratch in one hour", Text(announcements));
        Assert.Null(home.QuerySelector("main h2.govuk-heading-l"));

        // Order on the page: welcome, announcements, tiles.
        var sections = home.QuerySelectorAll("main .app-welcome, main .govuk-notification-banner, main .app-tiles")
            .Select(e => e.ClassName!.Split(' ').First(c => c.StartsWith("app-") || c == "govuk-notification-banner")).ToArray();
        Assert.Equal(["app-welcome", "govuk-notification-banner", "app-tiles"], sections);

        var tiles = home.QuerySelectorAll(".app-tile").ToList();
        Assert.Equal(["/Cases", "/Applications", "/Guests", "/Sponsors", "/Hosts", "/Accommodations", "/Admin"],
            tiles.Select(t => t.QuerySelector("a")!.GetAttribute("href")));
        Assert.Equal("7 cases", Text(tiles[0].QuerySelector(".app-tile__count")));
        Assert.Equal("8 applications", Text(tiles[1].QuerySelector(".app-tile__count")));
        Assert.Null(tiles[^1].QuerySelector(".app-tile__count"));
    }

    [Fact]
    public async Task Published_announcements_appear_in_the_banner()
    {
        await _factory.SeedThroughAsync(1);
        using (var scope = _factory.Services.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<IAnnouncementService>().CreateAsync(new Announcement
            {
                Title = "New arrivals file tonight",
                Body = "Arrivals will update after 6pm.",
                Link = "/Cases",
            });
        }

        var banner = (await _factory.CreateClient().GetPageAsync("/")).QuerySelector("main .govuk-notification-banner")!;
        Assert.Equal(["Dev Share", "New arrivals file tonight"], banner.QuerySelectorAll("h3").Select(Text).Order());
        Assert.Contains("Arrivals will update after 6pm.", Text(banner));
        Assert.Equal("/Cases", banner.QuerySelector("a.govuk-notification-banner__link")!.GetAttribute("href"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public async Task The_dev_share_announcement_survives_every_reset(int through)
    {
        await _factory.SeedThroughAsync(through);
        await _factory.SeedThroughAsync(through);
        var banner = (await _factory.CreateClient().GetPageAsync("/")).QuerySelector("main .govuk-notification-banner")!;
        Assert.Equal(["Dev Share"], banner.QuerySelectorAll("h3").Select(Text));
    }

    [Fact]
    public async Task Tile_counts_follow_the_signed_in_council()
    {
        await _factory.SeedThroughAsync(1);
        var client = _factory.CreateClient();
        await client.SubmitAsync(await client.GetPageAsync("/Admin"), "form[action*='SwitchUser']", new() { ["user"] = "exeter" });

        var home = await client.GetPageAsync("/");
        Assert.Contains("Counts show Exeter records only", Text(home.QuerySelector("main")));
        Assert.Equal("3 cases", Text(home.QuerySelector(".app-tile .app-tile__count")));
    }
}
