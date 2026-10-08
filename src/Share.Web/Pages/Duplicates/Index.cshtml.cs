using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Share.Web.Matching;
using Share.Web.Models;

namespace Share.Web.Pages.Duplicates;

public class IndexModel(DuplicateService duplicates) : PageModel
{
    [TempData] public string? Message { get; set; }
    public IReadOnlyList<DuplicateSuggestion> Suggestions { get; private set; } = [];
    public List<Guest> Marked { get; private set; } = [];
    public string? ErrorPairKey { get; private set; }

    async Task LoadAsync()
    {
        Suggestions = await duplicates.SuggestionsAsync();
        Marked = await duplicates.MarkedAsync();
    }

    public async Task OnGetAsync() => await LoadAsync();

    public async Task<IActionResult> OnPostDismissAsync(string pairKey)
    {
        if (await duplicates.FindAsync(pairKey) is { } s)
        {
            await duplicates.DismissAsync(s);
            Message = $"{s.A.FullName} and {s.B.FullName} marked as not a duplicate. This pair will not be suggested again";
        }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostKeepAsync(string pairKey, int? keep)
    {
        var s = await duplicates.FindAsync(pairKey);
        if (s is null) return RedirectToPage();
        var kept = keep == s.A.Id ? s.A : keep == s.B.Id ? s.B : null;
        if (kept is null)
        {
            await LoadAsync();
            ErrorPairKey = pairKey;
            return Page();
        }
        await duplicates.MarkDuplicateAsync(s, kept);
        var other = ReferenceEquals(kept, s.A) ? s.B : s.A;
        Message = $"{other.FullName} ({other.VisaApplication!.Uan}) marked as a duplicate of {kept.FullName}";
        return RedirectToPage();
    }
}
