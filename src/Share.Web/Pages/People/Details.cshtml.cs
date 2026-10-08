using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Share.Web.Data;
using Share.Web.Models;

namespace Share.Web.Pages.People;

public class DetailsModel(AppDbContext db) : PageModel
{
    public Person Person { get; private set; } = null!;
    public List<VisaApplication> Applications { get; private set; } = [];
    public List<Accommodation> Accommodations { get; private set; } = [];
    public List<Guest> SponsoredGuests { get; private set; } = [];
    public List<Guest> HostedGuests { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var person = await db.People.FirstOrDefaultAsync(p => p.Id == id);
        if (person is null) return NotFound();
        Person = person;

        Applications = await db.VisaApplications
            .Where(a => a.SponsorId == id || a.HostId == id)
            .Include(a => a.Guests).Include(a => a.Sponsor).Include(a => a.Host).Include(a => a.Accommodation)
            .AsSplitQuery()
            .OrderBy(a => a.SubmittedAt).ThenBy(a => a.Id).ToListAsync();

        Accommodations = Applications.Where(a => a.Accommodation != null).Select(a => a.Accommodation!)
            .DistinctBy(a => a.Id).OrderBy(a => a.Address).ToList();
        SponsoredGuests = Applications.Where(a => a.SponsorId == id).SelectMany(a => a.Guests.OrderBy(g => g.Position)).ToList();
        HostedGuests = Applications.Where(a => a.HostId == id).SelectMany(a => a.Guests.OrderBy(g => g.Position)).ToList();
        return Page();
    }
}
