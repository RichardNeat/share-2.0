using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Share.Web.Data;
using Share.Web.Models;

namespace Share.Web.Pages.Offers;

public class DetailsModel(AppDbContext db) : PageModel
{
    public Offer Offer { get; private set; } = null!;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var offer = await db.Offers.Include(o => o.TakenByCase).FirstOrDefaultAsync(o => o.Id == id);
        if (offer is null) return NotFound();
        Offer = offer;
        return Page();
    }
}
