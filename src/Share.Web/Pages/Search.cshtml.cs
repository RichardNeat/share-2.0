using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Share.Web.Services;

namespace Share.Web.Pages;

public class SearchModel : PageModel
{
    private readonly SearchService _searchService;

    [BindProperty(SupportsGet = true)]
    public string? Query { get; set; }

    public SearchResults? Results { get; set; }

    public SearchModel(SearchService searchService)
    {
        _searchService = searchService;
    }

    public void OnGet()
    {
        if (!string.IsNullOrWhiteSpace(Query))
        {
            var userCouncil = User.FindFirst("council")?.Value;
            Results = _searchService.Search(Query, userCouncil);
        }
    }
}
