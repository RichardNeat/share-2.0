using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Share.Web.Data;
using Share.Web.Models;
using Share.Web.Safeguarding;

namespace Share.Web.Pages.Cases;

// Step 2 of a rematch: check the move, then confirm.
public class RematchConfirmModel(CheckService checks, RematchService rematch, AppDbContext db, TimeProvider clock) : PageModel
{
    public Case Case { get; private set; } = null!;
    public OfferFit Fit { get; private set; } = null!;

    async Task<IActionResult?> LoadAsync(int id, int offerId)
    {
        var @case = await checks.LoadCaseAsync(id);
        if (@case is null) return NotFound();
        Case = @case;
        var today = Ages.Today(clock);
        if (!RematchRules.NeedsRematch(SafeguardingRules.Assess(@case, today))) return Redirect($"/Cases/{id}");
        var offer = await db.Offers.IgnoreQueryFilters().FirstOrDefaultAsync(o => o.Id == offerId && o.Status == OfferStatus.Open);
        if (offer is null) return Redirect($"/Cases/{id}/Rematch");
        Fit = RematchRules.Rank(@case, [offer], today).Single();
        return null;
    }

    public async Task<IActionResult> OnGetAsync(int id, int offerId) => await LoadAsync(id, offerId) ?? Page();

    public async Task<IActionResult> OnPostAsync(int id, int offerId)
    {
        if (await LoadAsync(id, offerId) is { } result) return result;
        await rematch.RematchAsync(Case, Fit.Offer);
        TempData["Message"] = $"Rematched to {Fit.Offer.Address}. All safeguarding checks have been reset";
        return Redirect($"/Cases/{id}");
    }
}
