using AngleSharp.Dom;
using Microsoft.Extensions.DependencyInjection;
using Share.Tests.Support;
using Share.Web.Ingest;

namespace Share.Tests.Journeys;

public class DuplicatesJourneyTests : IClassFixture<AppFactory>
{
    readonly AppFactory _factory;
    readonly HttpClient _client;

    public DuplicatesJourneyTests(AppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    static string Text(IElement? e) => e?.TextContent.Trim().Replace("\n", " ") ?? "";

    [Fact]
    public async Task Before_file_05_there_is_nothing_to_suggest()
    {
        await _factory.SeedThroughAsync(4);
        var page = await _client.GetPageAsync("/Duplicates");
        Html.AssertAccessible(page);
        Assert.Contains("There are no suggested duplicates", Text(page.QuerySelector("main")));
    }

    [Fact]
    public async Task File_05_brings_two_pairs_ranked_by_confidence()
    {
        await _factory.SeedThroughAsync(5);
        var page = await _client.GetPageAsync("/Duplicates");
        Html.AssertAccessible(page);

        var pairs = page.QuerySelectorAll("section.app-duplicate");
        Assert.Equal(2, pairs.Length);
        Assert.Equal("Olena Kovalenko and Olena Kovalenco", Text(pairs[0].QuerySelector("h2")));
        Assert.Equal("High confidence", Text(pairs[0].QuerySelector(".govuk-tag")));
        Assert.Contains("Names 1 letter apart", Text(pairs[0].QuerySelector("ul")));
        Assert.Equal("Dmytro Shevchenko and Dmytro Shevchenko", Text(pairs[1].QuerySelector("h2")));
        Assert.Contains("Passport numbers 1 character apart", Text(pairs[1].QuerySelector("ul")));
    }

    [Fact]
    public async Task A_dismissed_pair_never_resurfaces_even_after_more_files()
    {
        await _factory.SeedThroughAsync(5);
        var page = await _client.GetPageAsync("/Duplicates");
        var shevchenkoDismiss = page.QuerySelectorAll("section.app-duplicate")[1].QuerySelector("form[action*='Dismiss']")!;
        shevchenkoDismiss.Id = "dismiss-target";
        page = await _client.SubmitAsync(page, "#dismiss-target");
        Assert.Contains("will not be suggested again", Text(page.QuerySelector(".govuk-notification-banner")));
        Assert.Single(page.QuerySelectorAll("section.app-duplicate"));

        using (var scope = _factory.Services.CreateScope())
        {
            var files = scope.ServiceProvider.GetRequiredService<DataFiles>().All();
            await scope.ServiceProvider.GetRequiredService<IngestService>().ProcessFileAsync(files.Single(f => f.Number == 6));
        }
        page = await _client.GetPageAsync("/Duplicates");
        Assert.DoesNotContain(page.QuerySelectorAll("section.app-duplicate h2"), h => Text(h).Contains("Shevchenko"));
    }

    [Fact]
    public async Task Marking_a_duplicate_needs_a_choice_then_links_the_records()
    {
        await _factory.SeedThroughAsync(5);
        var page = await _client.GetPageAsync("/Duplicates");
        var keepForm = page.QuerySelectorAll("section.app-duplicate")[0].QuerySelector("form[action*='Keep']")!;
        keepForm.Id = "keep-target";

        var error = await _client.SubmitAsync(page, "#keep-target");
        Html.AssertAccessible(error);
        Assert.StartsWith("Error:", error.Title);
        var link = error.QuerySelector(".govuk-error-summary a")!;
        Assert.Equal("Select which record to keep", Text(link));
        Assert.NotNull(error.QuerySelector(link.GetAttribute("href")!));

        var keepKovalenko = keepForm.QuerySelectorAll("input[type=radio]")[0].GetAttribute("value")!;
        var done = await _client.SubmitAsync(page, "#keep-target", new() { ["keep"] = keepKovalenko });
        Assert.Contains("Olena Kovalenco", Text(done.QuerySelector(".govuk-notification-banner")));
        Assert.Single(done.QuerySelectorAll("section.app-duplicate"));
        var marked = done.QuerySelectorAll("main ul.govuk-list--spaced li").Single();
        Assert.Contains("Olena Kovalenco", Text(marked));
        Assert.Contains("is a duplicate of Olena Kovalenko", Text(marked));

        var duplicateGuest = await _client.GetPageAsync(marked.QuerySelector("a")!.GetAttribute("href")!);
        Html.AssertAccessible(duplicateGuest);
        Assert.Contains("This record is a duplicate of Olena Kovalenko", Text(duplicateGuest.QuerySelector(".govuk-inset-text")));
    }
}
