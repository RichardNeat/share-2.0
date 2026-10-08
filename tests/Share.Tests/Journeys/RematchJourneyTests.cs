using AngleSharp.Dom;
using Share.Tests.Support;

namespace Share.Tests.Journeys;

public class RematchJourneyTests : IClassFixture<AppFactory>
{
    readonly AppFactory _factory;
    readonly HttpClient _client;

    public RematchJourneyTests(AppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    static string Text(IElement? e) => e?.TextContent.Trim().Replace("\n", " ") ?? "";
    static IElement Row(IDocument doc, string keyStartsWith) =>
        doc.QuerySelectorAll(".govuk-summary-list__row").First(r => Text(r.QuerySelector("dt")).StartsWith(keyStartsWith));

    async Task<string> CaseHref(string title)
    {
        var list = await _client.GetPageAsync("/Cases");
        // Titles grow as families join a case (by file 08 the Melnyk case is "Melnyk and Romanyuk households").
        return list.QuerySelectorAll("tbody a").First(a => Text(a).StartsWith(title)).GetAttribute("href")!;
    }

    async Task<IDocument> FailCheck2(string caseHref)
    {
        var form = await _client.GetPageAsync($"{caseHref}/Checks/2");
        return await _client.SubmitAsync(form, "main form", new() { ["Status"] = "Failed", ["FailureReason"] = "Damp in both bedrooms" });
    }

    [Fact]
    public async Task Offers_files_are_ingested_and_listed()
    {
        await _factory.SeedThroughAsync(6);
        var admin = await _client.GetPageAsync("/Admin");
        var run = await _client.SubmitAsync(admin, "form[action*='ProcessNext']");
        Assert.Contains("07-eoi-offers.json processed", Text(run.QuerySelector(".govuk-notification-banner")));
        Assert.Equal("5", Text(Row(run, "Offers added").QuerySelector("dd")));

        var offers = await _client.GetPageAsync("/Offers");
        Html.AssertAccessible(offers);
        Assert.Equal(5, offers.QuerySelectorAll("tbody tr").Length);
        var detail = await _client.GetPageAsync(offers.QuerySelector("tbody a")!.GetAttribute("href")!);
        Html.AssertAccessible(detail);
        Assert.Equal("Open", Text(Row(detail, "Status").QuerySelector(".govuk-tag")));
    }

    [Fact]
    public async Task A_failed_check_flags_the_case_for_rematch()
    {
        await _factory.SeedThroughAsync(12);
        var href = await CaseHref("Melnyk");
        Assert.Equal(href, (await _client.GetAsync($"{href}/Rematch")).RequestMessage!.RequestUri!.AbsolutePath); // not needed yet

        var page = await FailCheck2(href);
        Assert.Contains("Needs rematch", Text(Row(page, "Status")));
        Assert.Contains("needs a new home", Text(page.QuerySelector(".govuk-warning-text")));
        Assert.Contains(page.QuerySelectorAll("nav[aria-label='Case'] a"), a => Text(a) == "Rematch to an open offer");
        var list = await _client.GetPageAsync("/Cases");
        Assert.Contains("Needs rematch", Text(list.QuerySelectorAll("tbody tr").First(r => Text(r.QuerySelector("a")).StartsWith("Melnyk"))));
    }

    [Fact]
    public async Task Rematch_moves_the_household_resets_checks_and_takes_the_offer()
    {
        await _factory.SeedThroughAsync(12);
        var href = await CaseHref("Melnyk");
        await FailCheck2(href);

        var choose = await _client.GetPageAsync($"{href}/Rematch");
        Html.AssertAccessible(choose);
        var first = choose.QuerySelector(".govuk-radios__item")!;
        var address = Text(first.QuerySelector("label"));
        Assert.Contains("Room for this household", Text(first.QuerySelector(".govuk-hint")));
        Assert.Contains("Same council (Exeter)", Text(first.QuerySelector(".govuk-hint")));

        var confirm = await _client.SubmitAsync(choose, "main form", new() { ["OfferId"] = first.QuerySelector("input")!.GetAttribute("value")! });
        Html.AssertAccessible(confirm);
        var newHost = Text(Row(confirm, "New host").QuerySelector("dd"));
        Assert.Equal(address, Text(Row(confirm, "Moving to").QuerySelector("dd")).Split(", EX")[0]);
        Assert.Contains("resets all four safeguarding checks", Text(confirm.QuerySelector(".govuk-warning-text")));

        var page = await _client.SubmitAsync(confirm, "main form");
        Html.AssertAccessible(page);
        Assert.Contains($"Rematched to {address}", Text(page.QuerySelector(".govuk-notification-banner")));
        Assert.StartsWith(address, Text(Row(page, "Accommodation").QuerySelector("dd")));
        Assert.Equal(newHost, Text(Row(page, "Host").QuerySelector("a")));
        Assert.Equal("Checks required", Text(Row(page, "Status").QuerySelector(".govuk-tag")));
        Assert.DoesNotContain("Needs rematch", Text(Row(page, "Status")));
        for (var i = 1; i <= 3; i++) Assert.Equal("Not started", Text(Row(page, $"{i}.").QuerySelector(".govuk-tag")));
        // Newest first: the status change follows the rematch that caused it.
        Assert.Equal(["Case status changed", "Case rematched"], page.QuerySelectorAll(".app-timeline__item h3").Take(2).Select(Text));

        var offers = await _client.GetPageAsync("/Offers");
        var taken = offers.QuerySelectorAll("tbody tr").First(r => Text(r.QuerySelector("a")) == address);
        Assert.Equal("Taken", Text(taken.QuerySelector(".govuk-tag")));
    }

    [Fact]
    public async Task Continuing_without_choosing_an_offer_shows_an_error()
    {
        await _factory.SeedThroughAsync(12);
        var href = await CaseHref("Melnyk");
        await FailCheck2(href);
        var choose = await _client.GetPageAsync($"{href}/Rematch");
        var page = await _client.SubmitAsync(choose, "main form", new() { ["OfferId"] = "" });

        Html.AssertAccessible(page);
        Assert.StartsWith("Error:", page.Title);
        Assert.Equal("Select an offer", Text(page.QuerySelector(".govuk-error-summary a")));
        Assert.NotNull(page.QuerySelector("#OfferId"));
    }
}
