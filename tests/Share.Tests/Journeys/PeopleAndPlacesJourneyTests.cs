using AngleSharp.Dom;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Share.Tests.Support;
using Share.Web.Data;

namespace Share.Tests.Journeys;

public class PeopleAndPlacesJourneyTests : IClassFixture<AppFactory>
{
    readonly AppFactory _factory;
    readonly HttpClient _client;

    public PeopleAndPlacesJourneyTests(AppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    static string Text(IElement? e) => e?.TextContent.Trim() ?? "";
    static IElement Row(IDocument doc, string key) =>
        doc.QuerySelectorAll(".govuk-summary-list__row").First(r => Text(r.QuerySelector("dt")) == key);

    [Fact]
    public async Task Sponsors_hosts_and_accommodations_are_de_duplicated()
    {
        await _factory.SeedThroughAsync(1);
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Clare Osborne sponsors two applications at one address: one person, one accommodation.
        Assert.Equal(7, await db.People.CountAsync(p => p.SponsoredApplications.Any()));
        Assert.Equal(8, await db.People.CountAsync(p => p.HostedApplications.Any()));
        Assert.Equal(8, await db.People.CountAsync());
        Assert.Equal(7, await db.Accommodations.CountAsync());
        Assert.Equal(12, await db.Guests.CountAsync());
    }

    [Fact]
    public async Task A_person_who_sponsors_and_hosts_is_one_record_with_both_roles()
    {
        await _factory.SeedThroughAsync(1);
        var sponsors = await _client.GetPageAsync("/Sponsors");
        Html.AssertAccessible(sponsors);
        var clareLink = sponsors.QuerySelectorAll("tbody a").First(a => Text(a) == "Clare Osborne");

        var clare = await _client.GetPageAsync(clareLink.GetAttribute("href")!);
        Html.AssertAccessible(clare);
        Assert.Equal(["Sponsor", "Host"], clare.QuerySelectorAll("main p .govuk-tag").Select(Text));
        Assert.Contains("Guests they sponsor (2)", Text(clare.QuerySelector("main")));
        Assert.Contains("Guests they host (1)", Text(clare.QuerySelector("main")));
        Assert.Single(clare.QuerySelectorAll("main a[href^='/Accommodations/']").Select(a => a.GetAttribute("href")).Distinct());
    }

    [Fact]
    public async Task A_named_host_who_is_not_the_sponsor_is_a_host_only()
    {
        await _factory.SeedThroughAsync(1);
        var hosts = await _client.GetPageAsync("/Hosts");
        Html.AssertAccessible(hosts);
        Assert.Equal(8, hosts.QuerySelectorAll("tbody tr").Length);
        var daniel = await _client.GetPageAsync(hosts.QuerySelectorAll("tbody a").First(a => Text(a) == "Daniel Park").GetAttribute("href")!);

        Html.AssertAccessible(daniel);
        Assert.Equal(["Host"], daniel.QuerySelectorAll("main p .govuk-tag").Select(Text));
        Assert.Contains("Guests they host (1)", Text(daniel.QuerySelector("main")));
        Assert.DoesNotContain("Guests they sponsor", Text(daniel.QuerySelector("main")));
    }

    [Fact]
    public async Task Guest_links_to_application_sponsor_accommodation_and_host_and_back()
    {
        await _factory.SeedThroughAsync(1);
        var guests = await _client.GetPageAsync("/Guests");
        Html.AssertAccessible(guests);
        Assert.Equal(12, guests.QuerySelectorAll("tbody tr").Length);
        var guestHref = guests.QuerySelectorAll("tbody a").First(a => Text(a) == "Olena Kovalenko").GetAttribute("href")!;

        var guest = await _client.GetPageAsync(guestHref);
        Html.AssertAccessible(guest);
        Assert.Equal("1313-2026-0001-7919", Text(Row(guest, "Visa application").QuerySelector("a")));
        Assert.Equal("Clare Osborne", Text(Row(guest, "Sponsor").QuerySelector("a")));
        Assert.Equal("Clare Osborne", Text(Row(guest, "Host").QuerySelector("a")));
        Assert.Equal("12 Copper Hill Lane, Exeter", Text(Row(guest, "Accommodation").QuerySelector("a")));

        // ...and each linked record links back to the guest.
        foreach (var key in new[] { "Visa application", "Sponsor", "Accommodation" })
        {
            var linked = await _client.GetPageAsync(Row(guest, key).QuerySelector("a")!.GetAttribute("href")!);
            Html.AssertAccessible(linked);
            Assert.Contains(linked.QuerySelectorAll("main a"), a => a.GetAttribute("href") == guestHref);
        }
    }

    [Fact]
    public async Task Accommodation_shows_its_council_sponsors_hosts_and_guests()
    {
        await _factory.SeedThroughAsync(1);
        var list = await _client.GetPageAsync("/Accommodations");
        Html.AssertAccessible(list);
        Assert.Equal(7, list.QuerySelectorAll("tbody tr").Length);
        var copperHill = await _client.GetPageAsync(list.QuerySelectorAll("tbody a").First(a => Text(a) == "12 Copper Hill Lane, Exeter").GetAttribute("href")!);

        Html.AssertAccessible(copperHill);
        Assert.Equal("Exeter", Text(Row(copperHill, "Council").QuerySelector("dd")));
        Assert.Equal(["Clare Osborne"], Row(copperHill, "Sponsor").QuerySelectorAll("a").Select(Text));
        Assert.Equal(["Clare Osborne", "Daniel Park"], Row(copperHill, "Hosts named on applications").QuerySelectorAll("a").Select(Text));
        Assert.Equal(2, copperHill.QuerySelectorAll("table tbody tr").Length);
    }
}
