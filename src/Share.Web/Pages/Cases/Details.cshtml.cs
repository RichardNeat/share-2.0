using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Share.Web.Data;
using Share.Web.Models;

namespace Share.Web.Pages.Cases;

public class DetailsModel(AppDbContext db, TimeProvider clock) : PageModel
{
    public Case Case { get; private set; } = null!;
    public DateOnly Today { get; private set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var @case = await db.Cases
            .Include(c => c.Sponsor)
            .Include(c => c.Accommodation)
            .Include(c => c.Applications).ThenInclude(a => a.Guests)
            .Include(c => c.Applications).ThenInclude(a => a.Host)
            .AsSplitQuery()
            .FirstOrDefaultAsync(c => c.Id == id);
        if (@case is null) return NotFound();
        Case = @case;
        Today = Ages.Today(clock);
        return Page();
    }
}
