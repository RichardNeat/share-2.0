using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Share.Web.Data;
using Share.Web.Models;

namespace Share.Web.Pages.Guests;

public class DetailsModel(AppDbContext db) : PageModel
{
    public Guest Guest { get; private set; } = null!;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var guest = await db.Guests
            .Include(g => g.VisaApplication).ThenInclude(a => a!.Sponsor)
            .Include(g => g.VisaApplication).ThenInclude(a => a!.Accommodation)
            .Include(g => g.VisaApplication).ThenInclude(a => a!.Host)
            .Include(g => g.VisaApplication).ThenInclude(a => a!.Case)
            .Include(g => g.VisaApplication).ThenInclude(a => a!.DecisionUpdates)
            .Include(g => g.VisaApplication).ThenInclude(a => a!.Guests)
            .FirstOrDefaultAsync(g => g.Id == id);
        if (guest is null) return NotFound();
        Guest = guest;
        return Page();
    }
}
