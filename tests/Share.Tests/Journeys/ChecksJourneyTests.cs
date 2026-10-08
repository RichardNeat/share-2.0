using AngleSharp.Dom;
using Share.Tests.Support;

namespace Share.Tests.Journeys;

public class ChecksJourneyTests : IClassFixture<AppFactory>
{
    readonly AppFactory _factory;
    readonly HttpClient _client;

    public ChecksJourneyTests(AppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    static string Text(IElement? e) => e?.TextContent.Trim().Replace("\n", " ") ?? "";
    static IElement Row(IDocument doc, string keyStartsWith) =>
        doc.QuerySelectorAll(".govuk-summary-list__row").First(r => Text(r.QuerySelector("dt")).StartsWith(keyStartsWith));
    static string Status(IDocument casePage) => Text(Row(casePage, "Status").QuerySelector(".govuk-tag"));

    async Task<string> CaseHref(string title)
    {
        var list = await _client.GetPageAsync("/Cases");
        return list.QuerySelectorAll("tbody a").First(a => Text(a) == title).GetAttribute("href")!;
    }

    async Task<IDocument> UpdateCheck(string caseHref, int check, Dictionary<string, string> values)
    {
        var form = await _client.GetPageAsync($"{caseHref}/Checks/{check}");
        Html.AssertAccessible(form);
        return await _client.SubmitAsync(form, "main form", values);
    }

    [Fact]
    public async Task Case_shows_four_checks_with_check_4_locked_until_arrival()
    {
        await _factory.SeedThroughAsync(1);
        var page = await _client.GetPageAsync(await CaseHref("Shevchenko household"));
        Html.AssertAccessible(page);

        Assert.Equal("Checks required", Status(page));
        for (var i = 1; i <= 3; i++)
            Assert.NotNull(Row(page, $"{i}.").QuerySelector(".govuk-summary-list__actions a"));
        var check4 = Row(page, "4.");
        Assert.Null(check4.QuerySelector(".govuk-summary-list__actions"));
        Assert.Contains("Available when guests arrive", Text(check4));
    }

    [Fact]
    public async Task Status_recalculates_as_checks_change()
    {
        await _factory.SeedThroughAsync(1);
        var href = await CaseHref("Shevchenko household");

        var page = await UpdateCheck(href, 1, new() { ["Status"] = "Passed" });
        Html.AssertAccessible(page);
        Assert.Contains("Accommodation exists updated to passed", Text(page.QuerySelector(".govuk-notification-banner")));
        Assert.Equal("Checks partially completed", Status(page));

        page = await UpdateCheck(href, 2, new() { ["Status"] = "NoLongerRequired" });
        page = await UpdateCheck(href, 3, new() { ["Status"] = "Passed", ["DbsType"] = "Standard" });
        Assert.Equal("Pre-arrival checks complete", Status(page));
        Assert.Contains("Standard DBS", Text(Row(page, "3.")));

        page = await UpdateCheck(href, 2, new() { ["Status"] = "Failed", ["FailureReason"] = "Damp in the bedroom" });
        Assert.Equal("Some checks failed", Status(page));
        Assert.Contains("Reason: Damp in the bedroom", Text(Row(page, "2.")));
    }

    [Fact]
    public async Task Failed_without_a_reason_shows_an_error_summary_linked_to_the_field()
    {
        await _factory.SeedThroughAsync(1);
        var page = await UpdateCheck(await CaseHref("Shevchenko household"), 1, new() { ["Status"] = "Failed", ["FailureReason"] = "" });

        Html.AssertAccessible(page);
        Assert.StartsWith("Error:", page.Title);
        var link = page.QuerySelector(".govuk-error-summary a")!;
        Assert.Equal("Enter why the check failed", Text(link));
        Assert.NotNull(page.QuerySelector(link.GetAttribute("href")!));
        Assert.NotNull(page.QuerySelector(".govuk-form-group--error textarea#FailureReason"));
    }

    [Fact]
    public async Task A_case_with_a_child_shows_enhanced_dbs_and_refuses_a_standard_dbs()
    {
        await _factory.SeedThroughAsync(1);
        var href = await CaseHref("Melnyk household");
        var page = await _client.GetPageAsync(href);

        Assert.Contains("Enhanced DBS required", Text(Row(page, "Status")));
        Assert.Contains("Sofia Melnyk (10)", Text(page.QuerySelector(".govuk-warning-text")));
        Assert.Contains("Andriy Melnyk (12)", Text(page.QuerySelector(".govuk-warning-text")));

        var refused = await UpdateCheck(href, 3, new() { ["Status"] = "Passed", ["DbsType"] = "Standard" });
        Assert.StartsWith("Error:", refused.Title);
        Assert.Contains("needs an Enhanced DBS", Text(refused.QuerySelector(".govuk-error-summary")));

        var accepted = await UpdateCheck(href, 3, new() { ["Status"] = "Passed", ["DbsType"] = "Enhanced" });
        Assert.Contains("Enhanced DBS", Text(Row(accepted, "3.")));
        Assert.Equal("Checks partially completed", Status(accepted));
    }

    [Fact]
    public async Task The_case_list_shows_the_derived_status_and_enhanced_dbs_flag()
    {
        await _factory.SeedThroughAsync(1);
        await UpdateCheck(await CaseHref("Kravets household"), 1, new() { ["Status"] = "Passed" });

        var list = await _client.GetPageAsync("/Cases");
        string Cell(string title) => Text(list.QuerySelectorAll("tbody tr").First(r => Text(r.QuerySelector("a")) == title).QuerySelectorAll("td")[4]);
        Assert.Equal("Checks partially completed", Cell("Kravets household"));
        Assert.Contains("Enhanced DBS required", Cell("Melnyk household"));
        Assert.DoesNotContain("Enhanced DBS required", Cell("Kravets household"));
    }

    [Fact]
    public async Task Check_4_cannot_be_updated_before_arrival()
    {
        await _factory.SeedThroughAsync(1);
        var href = await CaseHref("Shevchenko household");
        var response = await _client.GetAsync($"{href}/Checks/4");
        Assert.Equal(href, response.RequestMessage!.RequestUri!.AbsolutePath);
    }
}
