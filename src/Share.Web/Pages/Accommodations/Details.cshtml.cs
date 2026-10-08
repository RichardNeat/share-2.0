using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Share.Web.Data;
using Share.Web.Models;

namespace Share.Web.Pages.Accommodations;

public class DetailsModel(AppDbContext db) : PageModel
{
    public Accommodation Accommodation { get; private set; } = null!;
    public List<Person> Sponsors { get; private set; } = [];
    public List<Person> Hosts { get; private set; } = [];
    public List<Guest> Guests { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var accommodation = await db.Accommodations
            .Include(a => a.Applications).ThenInclude(x => x.Guests)
            .Include(a => a.Applications).ThenInclude(x => x.Sponsor)
            .Include(a => a.Applications).ThenInclude(x => x.Host)
            .AsSplitQuery()
            .FirstOrDefaultAsync(a => a.Id == id);
        if (accommodation is null) return NotFound();
        Accommodation = accommodation;
        var apps = accommodation.Applications.OrderBy(x => x.SubmittedAt).ThenBy(x => x.Id).ToList();
        Sponsors = apps.Where(x => x.Sponsor != null).Select(x => x.Sponsor!).DistinctBy(s => s.Id).ToList();
        Hosts = apps.Where(x => x.Host != null).Select(x => x.Host!).DistinctBy(h => h.Id).ToList();
        Guests = apps.SelectMany(x => x.Guests.OrderBy(p => p.Position)).ToList();
        return Page();
    }
}
