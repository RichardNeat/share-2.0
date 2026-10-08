using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Share.Web.Data;
using Share.Web.Models;

namespace Share.Web.Pages.Offers;

public class IndexModel(AppDbContext db) : PageModel
{
    public List<Offer> Offers { get; private set; } = [];

    public async Task OnGetAsync() =>
        Offers = (await db.Offers.ToListAsync())
            .OrderBy(o => o.Status).ThenBy(o => o.AvailableFrom).ThenBy(o => o.Id).ToList();
}
