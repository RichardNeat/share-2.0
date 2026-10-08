using AngleSharp.Dom;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Share.Tests.Support;
using Share.Web.Data;

namespace Share.Tests.Journeys;

public class CasesJourneyTests : IClassFixture<AppFactory>
{
    readonly AppFactory _factory;
    readonly HttpClient _client;

    public CasesJourneyTests(AppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    static string Text(IElement? e) => e?.TextContent.Trim() ?? "";
    static IElement Row(IDocument doc, string key) =>
        doc.QuerySelectorAll(".govuk-summary-list__row").First(r => Text(r.QuerySelector("dt")) == key);

    [Fact]
    public async Task Every_application_belongs_to_exactly_one_case()
    {
        await _factory.SeedThroughAsync(1);
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Assert.Equal(0, await db.VisaApplications.CountAsync(a => a.CaseId == null));
        // Clare Osborne's two applications share sponsor and address: one case. The other six stand alone.
        Assert.Equal(7, await db.Cases.CountAsync());
        var shared = await db.Cases.Include(c => c.Applications).SingleAsync(c => c.Applications.Count == 2);
        Assert.Equal(["1313-2026-0001-7919", "1313-2026-0007-5433"], shared.Applications.Select(a => a.Uan).Order());
        // The case's council comes from the accommodation address.
        Assert.All(await db.Cases.Include(c => c.Accommodation).ToListAsync(), c => Assert.Equal(c.Accommodation!.Council, c.Council));
    }

    [Fact]
    public async Task Case_list_shows_title_people_council_and_status()
    {
        await _factory.SeedThroughAsync(1);
        var list = await _client.GetPageAsync("/Cases");
        Html.AssertAccessible(list);

        var rows = list.QuerySelectorAll("tbody tr");
        Assert.Equal(7, rows.Length);
        var melnyk = rows.First(r => Text(r.QuerySelector("a")) == "Melnyk household");
        var cells = melnyk.QuerySelectorAll("td").Select(Text).ToArray();
        Assert.Equal("3", cells[1]);
        Assert.Equal("10", cells[2]);
        Assert.Equal("Exeter", cells[3]);
        Assert.Equal("Checks required", melnyk.QuerySelectorAll("td")[4].QuerySelector(".govuk-tag")!.TextContent.Trim());
        Assert.Contains(rows, r => Text(r.QuerySelector("a")) == "Kovalenko and Lysenko households");
    }

    [Fact]
    public async Task Case_detail_shows_guests_sponsor_accommodation_applications_and_youngest_age()
    {
        await _factory.SeedThroughAsync(1);
        var list = await _client.GetPageAsync("/Cases");
        var href = list.QuerySelectorAll("tbody a").First(a => Text(a) == "Melnyk household").GetAttribute("href")!;

        var detail = await _client.GetPageAsync(href);
        Html.AssertAccessible(detail);
        Assert.Equal("Donna Temple", Text(Row(detail, "Sponsor").QuerySelector("a")));
        Assert.Equal("29 Chestnut Avenue, Exeter", Text(Row(detail, "Accommodation").QuerySelector("a")));
        Assert.Equal("Exeter", Text(Row(detail, "Council").QuerySelector("dd")));
        Assert.Contains("Sofia Melnyk, aged 10", Text(Row(detail, "Youngest guest").QuerySelector("dd")).Replace("\n", " "));
        Assert.Equal(["Kateryna Melnyk", "Andriy Melnyk", "Sofia Melnyk"], detail.QuerySelectorAll("table tbody tr td:first-child a").Select(Text));
        Assert.Single(detail.QuerySelectorAll("main a[href^='/Applications/']"));
    }

    [Fact]
    public async Task Application_and_guest_link_to_their_case_and_the_ingest_summary_lists_cases_formed()
    {
        await _factory.SeedThroughAsync(0);
        var admin = await _client.GetPageAsync("/Admin");
        var run = await _client.SubmitAsync(admin, "form[action*='ProcessNext']");
        Assert.Equal("7", Text(Row(run, "Cases formed").QuerySelector("dd")));
        Assert.Equal(7, run.QuerySelectorAll("main ul.govuk-list a[href^='/Cases/']").Length);

        var app = await _client.GetPageAsync("/Applications");
        var appPage = await _client.GetPageAsync(app.QuerySelector("tbody a")!.GetAttribute("href")!);
        var caseHref = Row(appPage, "Case").QuerySelector("a")!.GetAttribute("href")!;
        var guestPage = await _client.GetPageAsync("/Guests/1");
        Assert.Equal(caseHref, Row(guestPage, "Case").QuerySelector("a")!.GetAttribute("href"));
    }
}
