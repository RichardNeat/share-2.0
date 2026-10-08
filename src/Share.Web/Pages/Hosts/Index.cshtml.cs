using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Share.Web.Data;
using Share.Web.Models;

namespace Share.Web.Pages.Hosts;

public class IndexModel(AppDbContext db) : PageModel
{
    public List<Person> People { get; private set; } = [];

    public async Task OnGetAsync() =>
        People = await db.People
            .Where(p => p.HostedApplications.Any())
            .Include(p => p.SponsoredApplications).ThenInclude(a => a.Guests)
            .Include(p => p.HostedApplications).ThenInclude(a => a.Guests)
            .AsSplitQuery()
            .OrderBy(p => p.FamilyName).ThenBy(p => p.GivenName).ThenBy(p => p.Id).ToListAsync();
}
