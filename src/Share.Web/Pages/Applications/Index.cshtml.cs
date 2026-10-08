using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Share.Web.Data;
using Share.Web.Models;

namespace Share.Web.Pages.Applications;

public class IndexModel(AppDbContext db) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public VisaStatus? Status { get; set; }

    public List<VisaApplication> Applications { get; private set; } = [];

    public async Task OnGetAsync()
    {
        var query = db.VisaApplications.Include(a => a.Guests).Include(a => a.Sponsor).AsQueryable();
        if (Status is not null) query = query.Where(a => a.Status == Status);
        Applications = await query.OrderBy(a => a.SubmittedAt).ThenBy(a => a.Uan).ToListAsync();
    }
}
