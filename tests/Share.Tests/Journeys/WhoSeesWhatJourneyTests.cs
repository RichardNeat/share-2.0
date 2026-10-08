using System.Net;
using AngleSharp.Dom;
using Share.Tests.Support;

namespace Share.Tests.Journeys;

// M6: users and row-level scoping. A new client per test, so each starts as the central admin.
public class WhoSeesWhatJourneyTests : IClassFixture<AppFactory>
{
    readonly AppFactory _factory;

    public WhoSeesWhatJourneyTests(AppFactory factory) => _factory = factory;

    static string Text(IElement? e) => e?.TextContent.Trim() ?? "";

    // Signs in through the demo controls, as a person would.
    async Task<HttpClient> ClientAsync(string userKey)
    {
        var client = _factory.CreateClient();
        await client.SubmitAsync(await client.GetPageAsync("/Admin"), "form[action*='SwitchUser']", new() { ["user"] = userKey });
        return client;
    }

    static async Task<string[]> HrefsAsync(HttpClient client, string url, string prefix) =>
        (await client.GetPageAsync(url)).QuerySelectorAll($"tbody a[href^='{prefix}']")
            .Select(a => a.GetAttribute("href")!).Distinct().ToArray();

    [Fact]
    public async Task There_are_three_users_and_switching_from_demo_controls_shows_the_role_on_every_page()
    {
        await _factory.SeedThroughAsync(1);
        var client = _factory.CreateClient();

        var admin = await client.GetPageAsync("/Admin");
        Html.AssertAccessible(admin);
        Assert.Contains("Central admin", Text(admin.QuerySelector("#signed-in-as")));
        Assert.Equal(["Central admin", "Birmingham council user", "Exeter council user"],
            admin.QuerySelectorAll("#user ~ form .govuk-radios__label").Select(Text));

        var switched = await client.SubmitAsync(admin, "form[action*='SwitchUser']", new() { ["user"] = "birmingham" });
        Html.AssertAccessible(switched);
        Assert.Contains("You are now signed in as Birmingham council user", Text(switched.QuerySelector(".govuk-notification-banner")));
        Assert.NotNull(switched.QuerySelector("#user-birmingham[checked]"));

        foreach (var url in new[] { "/", "/Cases", "/Applications", "/Guests", "/Sponsors", "/Hosts", "/Accommodations", "/Admin" })
        {
            var page = await client.GetPageAsync(url);
            Html.AssertAccessible(page);
            Assert.Contains("Signed in as Birmingham council user", Text(page.QuerySelector("#signed-in-as")));
        }
    }

    [Fact]
    public async Task Council_users_see_only_their_council_and_the_admin_sees_everything()
    {
        await _factory.SeedThroughAsync(1);

        var admin = await ClientAsync("admin");
        Assert.Equal(8, (await HrefsAsync(admin, "/Applications", "/Applications/")).Length);
        Assert.Equal(7, (await HrefsAsync(admin, "/Cases", "/Cases/")).Length);
        Assert.Equal(12, (await HrefsAsync(admin, "/Guests", "/Guests/")).Length);
        Assert.Equal(7, (await HrefsAsync(admin, "/Accommodations", "/Accommodations/")).Length);

        var birmingham = await ClientAsync("birmingham");
        var apps = await birmingham.GetPageAsync("/Applications");
        Assert.Equal(4, apps.QuerySelectorAll("tbody tr").Length);
        Assert.All(apps.QuerySelectorAll("tbody tr"), r => Assert.Contains("Birmingham", Text(r)));
        Assert.Equal(6, (await HrefsAsync(birmingham, "/Guests", "/Guests/")).Length);
        Assert.Equal(4, (await HrefsAsync(birmingham, "/Cases", "/Cases/")).Length);
        Assert.Equal(4, (await HrefsAsync(birmingham, "/Sponsors", "/People/")).Length);
        var accommodations = await birmingham.GetPageAsync("/Accommodations");
        Assert.Equal(4, accommodations.QuerySelectorAll("tbody tr").Length);
        Assert.DoesNotContain("Exeter", Text(accommodations.QuerySelector("tbody")));

        var exeter = await ClientAsync("exeter");
        Assert.Equal(4, (await HrefsAsync(exeter, "/Applications", "/Applications/")).Length);
        Assert.Equal(6, (await HrefsAsync(exeter, "/Guests", "/Guests/")).Length);
        // Clare Osborne's two applications share one case.
        Assert.Equal(3, (await HrefsAsync(exeter, "/Cases", "/Cases/")).Length);
        Assert.Equal(3, (await HrefsAsync(exeter, "/Sponsors", "/People/")).Length);
        Assert.Contains("Daniel Park", Text((await exeter.GetPageAsync("/Hosts")).QuerySelector("tbody")));
        Assert.DoesNotContain("Daniel Park", Text((await birmingham.GetPageAsync("/Hosts")).QuerySelector("tbody")));
    }

    [Theory]
    [InlineData("/Cases", "/Cases/")]
    [InlineData("/Applications", "/Applications/")]
    [InlineData("/Guests", "/Guests/")]
    [InlineData("/Sponsors", "/People/")]
    [InlineData("/Hosts", "/People/")]
    [InlineData("/Accommodations", "/Accommodations/")]
    public async Task Guessing_the_url_of_another_councils_record_does_not_work(string list, string prefix)
    {
        await _factory.SeedThroughAsync(1);
        var birmingham = await ClientAsync("birmingham");
        var admin = await ClientAsync("admin");
        var exeterOnly = (await HrefsAsync(await ClientAsync("exeter"), list, prefix))
            .Except(await HrefsAsync(birmingham, list, prefix)).ToArray();
        Assert.NotEmpty(exeterOnly);

        foreach (var href in exeterOnly)
        {
            var response = await birmingham.GetAsync(href);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            var page = await Html.ParseAsync(response);
            Html.AssertAccessible(page);
            Assert.Equal("Page not found", Text(page.QuerySelector("h1")));

            Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync(href)).StatusCode);
        }
    }

    [Fact]
    public async Task A_council_user_cannot_view_or_update_a_check_on_another_councils_case()
    {
        await _factory.SeedThroughAsync(1);
        var birmingham = await ClientAsync("birmingham");
        var exeterCase = (await HrefsAsync(await ClientAsync("exeter"), "/Cases", "/Cases/"))[0];
        var checkUrl = $"{exeterCase}/Checks/1";

        var admin = await ClientAsync("admin");
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync(checkUrl)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await birmingham.GetAsync(checkUrl)).StatusCode);

        // Even with a valid form token, the update finds no case.
        var form = await birmingham.GetPageAsync("/Admin");
        var token = form.QuerySelector("input[name='__RequestVerificationToken']")!.GetAttribute("value")!;
        var post = await birmingham.PostAsync(checkUrl, new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Status"] = "Passed",
        }));
        Assert.Equal(HttpStatusCode.NotFound, post.StatusCode);
    }

    [Fact]
    public async Task Detail_pages_list_only_related_records_in_the_users_council()
    {
        await _factory.SeedThroughAsync(1);
        var exeter = await ClientAsync("exeter");
        var birminghamApps = await HrefsAsync(await ClientAsync("birmingham"), "/Applications", "/Applications/");

        foreach (var href in await HrefsAsync(exeter, "/Sponsors", "/People/"))
        {
            var person = await exeter.GetPageAsync(href);
            var linked = person.QuerySelectorAll("main a[href^='/Applications/']").Select(a => a.GetAttribute("href")!).ToList();
            Assert.NotEmpty(linked);
            Assert.All(linked, href => Assert.DoesNotContain(href, birminghamApps));
        }
    }

    [Fact]
    public async Task A_council_user_can_still_process_files_and_sees_only_their_changes()
    {
        await _factory.SeedThroughAsync(0);
        var client = await ClientAsync("birmingham");
        var admin = await client.GetPageAsync("/Admin");

        var result = await client.SubmitAsync(admin, "form[action*='ProcessNext']");
        Html.AssertAccessible(result);
        Assert.Equal(4, result.QuerySelectorAll("main ul.govuk-list li a[href^='/Applications/']").Length);
        Assert.Equal(4, result.QuerySelectorAll("main ul.govuk-list li a[href^='/Cases/']").Select(a => a.GetAttribute("href")).Distinct().Count());
        Assert.Contains("only changes to Birmingham records are listed", Text(result.QuerySelector(".govuk-inset-text")));

        // Ingest matched against every council's records, not just Birmingham's.
        Assert.Equal(8, (await HrefsAsync(await ClientAsync("admin"), "/Applications", "/Applications/")).Length);
        Assert.Equal(7, (await HrefsAsync(await ClientAsync("admin"), "/Cases", "/Cases/")).Length);
        Assert.Equal(7, (await HrefsAsync(await ClientAsync("admin"), "/Accommodations", "/Accommodations/")).Length);
    }
}
