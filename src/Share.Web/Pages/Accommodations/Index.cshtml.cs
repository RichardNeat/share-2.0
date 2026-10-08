using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Share.Web.Data;
using Share.Web.Models;

namespace Share.Web.Pages.Accommodations;

public class IndexModel(AppDbContext db) : PageModel
{
    public List<Accommodation> Accommodations { get; private set; } = [];

    public async Task OnGetAsync() =>
        Accommodations = await db.Accommodations
            .Include(a => a.Applications).ThenInclude(x => x.Guests)
            .Include(a => a.Applications).ThenInclude(x => x.Sponsor)
            .AsSplitQuery()
            .OrderBy(a => a.Council).ThenBy(a => a.Address).ThenBy(a => a.Id).ToListAsync();
}
