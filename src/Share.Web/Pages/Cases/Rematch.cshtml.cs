using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Share.Web.Data;
using Share.Web.Models;
using Share.Web.Safeguarding;

namespace Share.Web.Pages.Cases;

// Step 1 of a rematch: choose an open offer, ranked by fit.
public class RematchModel(CheckService checks, AppDbContext db, TimeProvider clock) : PageModel
{
    [BindProperty] public int? OfferId { get; set; }
    public Case Case { get; private set; } = null!;
    public IReadOnlyList<OfferFit> Offers { get; private set; } = [];
    public string? Error { get; private set; }

    async Task<IActionResult?> LoadAsync(int id)
    {
        var @case = await checks.LoadCaseAsync(id);
        if (@case is null) return NotFound();
        Case = @case;
        var today = Ages.Today(clock);
        if (!RematchRules.NeedsRematch(SafeguardingRules.Assess(@case, today))) return Redirect($"/Cases/{id}");
        // Offers in every council are shown; same-council offers rank first.
        Offers = RematchRules.Rank(@case, await db.Offers.IgnoreQueryFilters().ToListAsync(), today);
        return null;
    }

    public async Task<IActionResult> OnGetAsync(int id) => await LoadAsync(id) ?? Page();

    public async Task<IActionResult> OnPostAsync(int id)
    {
        if (await LoadAsync(id) is { } result) return result;
        if (OfferId is null || Offers.All(o => o.Offer.Id != OfferId))
        {
            Error = "Select an offer";
            return Page();
        }
        return Redirect($"/Cases/{id}/Rematch/{OfferId}");
    }
}
