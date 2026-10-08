using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Share.Web.Data;
using Share.Web.Models;

namespace Share.Web.Pages.Cases;

public class IndexModel(AppDbContext db, TimeProvider clock) : PageModel
{
    public List<Case> Cases { get; private set; } = [];
    public DateOnly Today { get; private set; }

    public async Task OnGetAsync()
    {
        Today = Ages.Today(clock);
        Cases = await db.Cases
            .Include(c => c.Applications).ThenInclude(a => a.Guests)
            .Include(c => c.Accommodation)
            .AsSplitQuery()
            .OrderBy(c => c.Id).ToListAsync();
    }
}
