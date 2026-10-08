using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Share.Web.Data;

namespace Share.Web.Pages;

public class IndexModel(AppDbContext db) : PageModel
{
    public int ApplicationCount { get; private set; }

    public async Task OnGetAsync() => ApplicationCount = await db.VisaApplications.CountAsync();
}
