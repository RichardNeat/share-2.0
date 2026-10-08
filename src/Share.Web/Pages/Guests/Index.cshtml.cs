using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Share.Web.Data;
using Share.Web.Models;

namespace Share.Web.Pages.Guests;

public class IndexModel(AppDbContext db) : PageModel
{
    public List<Guest> Guests { get; private set; } = [];

    public async Task OnGetAsync() =>
        Guests = await db.Guests.Include(g => g.VisaApplication).ThenInclude(a => a!.Sponsor)
            .OrderBy(g => g.FamilyName).ThenBy(g => g.GivenName).ThenBy(g => g.Id).ToListAsync();
}
