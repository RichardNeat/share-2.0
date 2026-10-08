using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;

namespace Share.Tests.Support;

public static class Html
{
    public static async Task<IDocument> GetPageAsync(this HttpClient client, string url)
    {
        var response = await client.GetAsync(url);
        response.EnsureSuccessStatusCode();
        return await ParseAsync(response);
    }

    public static async Task<IDocument> ParseAsync(HttpResponseMessage response)
    {
        var html = await response.Content.ReadAsStringAsync();
        var context = BrowsingContext.New(Configuration.Default);
        return await context.OpenAsync(req => req.Content(html).Address(response.RequestMessage!.RequestUri!));
    }

    // Submits a form found on the page (including its antiforgery token), following redirects.
    public static async Task<IDocument> SubmitAsync(this HttpClient client, IDocument page, string formSelector, Dictionary<string, string>? values = null)
    {
        var form = page.QuerySelector<IHtmlFormElement>(formSelector) ?? throw new InvalidOperationException($"No form {formSelector}");
        var fields = new Dictionary<string, string>();
        foreach (var input in form.QuerySelectorAll<IHtmlInputElement>("input[name]")) fields[input.Name!] = input.Value;
        foreach (var select in form.QuerySelectorAll<IHtmlSelectElement>("select[name]")) fields[select.Name!] = select.Value;
        foreach (var (k, v) in values ?? []) fields[k] = v;
        var response = await client.PostAsync(form.Action, new FormUrlEncodedContent(fields));
        response.EnsureSuccessStatusCode();
        return await ParseAsync(response);
    }

    // The shared accessibility checks every page must pass.
    public static void AssertAccessible(IDocument doc)
    {
        Assert.False(string.IsNullOrWhiteSpace(doc.Title), "Page has no title");
        Assert.Single(doc.QuerySelectorAll("h1"));
        Assert.NotNull(doc.QuerySelector("a.govuk-skip-link[href='#main-content']"));
        Assert.NotNull(doc.QuerySelector("main#main-content"));
        Assert.Single(doc.QuerySelectorAll("main"));
        Assert.DoesNotContain("GOV.UK - The Share", doc.Title);
        foreach (var field in doc.QuerySelectorAll("input:not([type=hidden]), select, textarea"))
        {
            var id = field.Id;
            Assert.False(string.IsNullOrEmpty(id), $"{field.TagName} has no id");
            Assert.NotNull(doc.QuerySelector($"label[for='{id}']"));
        }
    }
}
