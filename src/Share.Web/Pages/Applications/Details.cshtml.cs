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
            .Include(a => a.People).ThenInclude(p => p.Answers)
            .FirstOrDefaultAsync(a => a.Id == id);
        if (app is null) return NotFound();
        Application = app;
        return Page();
    }
}
