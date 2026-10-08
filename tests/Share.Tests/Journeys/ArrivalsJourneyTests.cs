using AngleSharp.Dom;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Share.Tests.Support;
using Share.Web.Data;
using Share.Web.Ingest;
using Share.Web.Models;

namespace Share.Tests.Journeys;

public class ArrivalsJourneyTests : IClassFixture<AppFactory>
{
    readonly AppFactory _factory;
    readonly HttpClient _client;

    public ArrivalsJourneyTests(AppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    static string Text(IElement? e) => e?.TextContent.Trim().Replace("\n", " ") ?? "";
    static IElement Row(IDocument doc, string keyStartsWith) =>
        doc.QuerySelectorAll(".govuk-summary-list__row").First(r => Text(r.QuerySelector("dt")).StartsWith(keyStartsWith));

    async Task<Dictionary<string, VisaStatus>> Statuses()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.VisaApplications.ToDictionaryAsync(a => a.Uan, a => a.Status);
    }

    async Task<string> CaseHref(string title)
    {
        var list = await _client.GetPageAsync("/Cases");
        return list.QuerySelectorAll("tbody a").First(a => Text(a) == title).GetAttribute("href")!;
    }

    [Fact]
    public async Task File_02_matches_by_gwf_then_uan_and_updates_visa_statuses()
    {
        await _factory.SeedThroughAsync(2);
        var s = await Statuses();
        Assert.Equal(VisaStatus.Arrived, s["1313-2026-0001-7919"]);  // Kovalenko, GWF
        Assert.Equal(VisaStatus.Issued, s["1313-2026-0002-5838"]);   // Shevchenko, GWF
        Assert.Equal(VisaStatus.Arrived, s["1313-2026-0003-3757"]);  // Melnyk, GWF
        Assert.Equal(VisaStatus.Issued, s["1313-2026-0005-9595"]);   // Tkachenko, UAN only
        Assert.Equal(VisaStatus.Pending, s["1313-2026-0004-1676"]);  // not in the file
    }

    [Fact]
    public async Task Process_next_file_shows_arrivals_and_the_unmatched_row()
    {
        await _factory.SeedThroughAsync(1);
        var admin = await _client.GetPageAsync("/Admin");
        var run = await _client.SubmitAsync(admin, "form[action*='ProcessNext']");

        Html.AssertAccessible(run);
        Assert.Contains("02-arrivals.csv processed", Text(run.QuerySelector(".govuk-notification-banner")));
        Assert.Equal("4", Text(Row(run, "Updates matched").QuerySelector("dd")));
        Assert.Equal("4", Text(Row(run, "Visa statuses changed").QuerySelector("dd")));
        Assert.Equal("2", Text(Row(run, "Guests arrived in the UK").QuerySelector("dd")));
        var skipped = run.QuerySelectorAll("table tbody tr");
        Assert.Contains("GWF000000001", Text(Assert.Single(skipped)));
        Assert.Contains("No application matches", Text(skipped[0]));
        Assert.Contains(run.QuerySelectorAll("main ul.govuk-list li"), li => Text(li).Contains("arrived at Gatwick (LGW) on FR1234"));
    }

    [Fact]
    public async Task Reprocessing_the_arrivals_file_applies_nothing_twice()
    {
        await _factory.SeedThroughAsync(2);
        using var scope = _factory.Services.CreateScope();
        var sp = scope.ServiceProvider;
        var file = sp.GetRequiredService<DataFiles>().All().Single(f => f.Number == 2);
        var run = await sp.GetRequiredService<IngestService>().ProcessFileAsync(file);

        Assert.Equal(0, run.Added);
        Assert.Equal(4, run.AlreadyPresent);
        Assert.Equal(4, await sp.GetRequiredService<AppDbContext>().DecisionUpdates.CountAsync());
    }

    [Fact]
    public async Task An_arrival_in_the_feed_unlocks_check_4_on_the_case()
    {
        await _factory.SeedThroughAsync(2);
        var melnyk = await _client.GetPageAsync(await CaseHref("Melnyk household"));
        var check4 = Row(melnyk, "4.");
        Assert.Contains("arrived in the UK at Luton (LTN) on 27 February 2026 at 2:40pm", Text(check4));
        Assert.Contains("Update", Text(check4.QuerySelector(".govuk-summary-list__actions")));

        var shevchenko = await _client.GetPageAsync(await CaseHref("Shevchenko household"));
        Assert.Contains("Record arrival", Text(Row(shevchenko, "4.").QuerySelector(".govuk-summary-list__actions")));
    }

    [Fact]
    public async Task Case_history_shows_received_status_changed_arrived_and_check_updated_newest_first()
    {
        await _factory.SeedThroughAsync(2);
        var href = await CaseHref("Melnyk household");
        var form = await _client.GetPageAsync($"{href}/Checks/1");
        var page = await _client.SubmitAsync(form, "main form", new() { ["Status"] = "Passed" });

        Html.AssertAccessible(page);
        var items = page.QuerySelectorAll(".app-timeline__item");
        var titles = items.Select(i => Text(i.QuerySelector("h3"))).ToList();
        Assert.Equal(["Case status changed", "Check updated", "Guests arrived in the UK", "Visa status changed", "Case formed", "Application received"], titles);
        Assert.All(items, i => Assert.False(string.IsNullOrWhiteSpace(i.QuerySelector("time")?.GetAttribute("datetime"))));
        Assert.Contains("Pending to Arrived", Text(items[3]));
        Assert.Equal("27 February 2026 at 2:40pm", Text(items[2].QuerySelector("time")));
    }

    [Fact]
    public async Task Application_and_guest_pages_show_the_uk_arrival()
    {
        await _factory.SeedThroughAsync(2);
        var app = await _client.GetPageAsync("/Applications?status=Arrived");
        Assert.Equal(2, app.QuerySelectorAll("tbody tr").Length);
        var detail = await _client.GetPageAsync(app.QuerySelectorAll("tbody a").First(a => Text(a) == "Olena Kovalenko").GetAttribute("href")!);
        Assert.Equal("26 February 2026 at 10:15am at Gatwick (LGW) on FR1234", Text(Row(detail, "Arrived in the UK").QuerySelector("dd")));
    }
}
