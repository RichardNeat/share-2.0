using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Share.Web.Data;
using Share.Web.Models;

namespace Share.Web.Pages.Applications;

public class DetailsModel(AppDbContext db) : PageModel
{
    public VisaApplication Application { get; private set; } = null!;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var app = await db.VisaApplications
            .Include(a => a.Guests).ThenInclude(p => p.Answers)
            .Include(a => a.Sponsor)
            .Include(a => a.Host)
            .Include(a => a.Accommodation)
            .Include(a => a.Case)
            .FirstOrDefaultAsync(a => a.Id == id);
        if (app is null) return NotFound();
        Application = app;
        return Page();
    }
}
