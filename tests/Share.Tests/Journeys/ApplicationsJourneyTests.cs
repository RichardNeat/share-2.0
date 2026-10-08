using Share.Tests.Support;

namespace Share.Tests.Journeys;

public class ApplicationsJourneyTests : IClassFixture<AppFactory>
{
    readonly AppFactory _factory;
    readonly HttpClient _client;

    public ApplicationsJourneyTests(AppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Process_next_file_shows_what_just_happened_then_applications_appear()
    {
        await _factory.SeedThroughAsync(0);

        var admin = await _client.GetPageAsync("/Admin");
        Html.AssertAccessible(admin);
        Assert.Contains("01-visa-applications.json", admin.QuerySelector(".govuk-summary-list")!.TextContent);

        var result = await _client.SubmitAsync(admin, "form[action*='ProcessNext']");
        Html.AssertAccessible(result);
        Assert.Contains("01-visa-applications.json processed", result.QuerySelector(".govuk-notification-banner")!.TextContent);
        Assert.Equal(8, result.QuerySelectorAll("main ul.govuk-list li a[href^='/Applications/']").Length);
        Assert.Contains("No rows were skipped", result.QuerySelector("main")!.TextContent);

        var list = await _client.GetPageAsync("/Applications");
        Html.AssertAccessible(list);
        var rows = list.QuerySelectorAll("tbody tr");
        Assert.Equal(8, rows.Length);
        var first = rows[0].QuerySelectorAll("td").Select(td => td.TextContent.Trim()).ToArray();
        Assert.Equal(["Olena Kovalenko", "1313-2026-0001-7919", "1", "Clare Osborne", "Exeter", "Pending"], first);
        Assert.All(rows, r => Assert.Equal("Pending", r.QuerySelector(".govuk-tag")!.TextContent.Trim()));
    }

    [Fact]
    public async Task Applications_can_be_filtered_by_visa_status()
    {
        await _factory.SeedThroughAsync(1);

        var pending = await _client.GetPageAsync("/Applications?status=Pending");
        Html.AssertAccessible(pending);
        Assert.Equal(8, pending.QuerySelectorAll("tbody tr").Length);
        Assert.Equal("Pending", ((AngleSharp.Html.Dom.IHtmlSelectElement)pending.QuerySelector("select#status")!).Value);

        var arrived = await _client.GetPageAsync("/Applications?status=Arrived");
        Html.AssertAccessible(arrived);
        Assert.Empty(arrived.QuerySelectorAll("tbody tr"));
        Assert.Contains("No applications with status Arrived", arrived.QuerySelector("main")!.TextContent);
    }

    [Fact]
    public async Task Detail_shows_every_answer_including_family_members()
    {
        await _factory.SeedThroughAsync(1);
        var list = await _client.GetPageAsync("/Applications");
        var familyRow = list.QuerySelectorAll("tbody tr").First(r => r.QuerySelectorAll("td")[2].TextContent.Trim() == "3");
        var href = familyRow.QuerySelector("a")!.GetAttribute("href")!;

        var detail = await _client.GetPageAsync(href);
        Html.AssertAccessible(detail);
        var cards = detail.QuerySelectorAll(".govuk-summary-card");
        Assert.Equal(3, cards.Length);
        Assert.Contains("lead applicant", cards[0].QuerySelector("h3")!.TextContent);
        Assert.Contains("family member", cards[1].QuerySelector("h3")!.TextContent);
        Assert.Contains(cards[1].QuerySelectorAll("dt"), dt => dt.TextContent == "Relationship to lead applicant");
    }

    [Fact]
    public async Task Reset_buttons_clear_and_reload_the_demo()
    {
        await _factory.SeedThroughAsync(1);
        var admin = await _client.GetPageAsync("/Admin");

        var emptied = await _client.SubmitAsync(admin, "form[action*='handler=Reset']");
        Html.AssertAccessible(emptied);
        Assert.Contains("Demo reset to empty", emptied.QuerySelector(".govuk-notification-banner")!.TextContent);
        Assert.Contains("0 of", emptied.QuerySelector(".govuk-summary-list")!.TextContent);

        var reloaded = await _client.SubmitAsync(emptied, "form[action*='handler=Seed']", new() { ["through"] = "1" });
        Assert.Contains("1 file loaded", reloaded.QuerySelector(".govuk-notification-banner")!.TextContent);
        Assert.Equal(8, (await _client.GetPageAsync("/Applications")).QuerySelectorAll("tbody tr").Length);
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/Applications")]
    [InlineData("/Admin")]
    [InlineData("/Accessibility")]
    [InlineData("/Offers")]
    [InlineData("/Duplicates")]
    [InlineData("/do-not-press")]
    [InlineData("/Notifications")]
    [InlineData("/Admin/Announcements")]
    [InlineData("/Admin/Announcements/Create")]
    [InlineData("/Cases")]
    [InlineData("/Guests")]
    [InlineData("/Sponsors")]
    [InlineData("/Hosts")]
    [InlineData("/Accommodations")]
    public async Task Every_page_passes_the_accessibility_checks(string url)
    {
        await _factory.SeedThroughAsync(1);
        Html.AssertAccessible(await _client.GetPageAsync(url));
    }
}
