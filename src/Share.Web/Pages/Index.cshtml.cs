using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Share.Web.Data;
using Share.Web.Models;
using Share.Web.Services;

namespace Share.Web.Pages;

public class IndexModel(AppDbContext db, IAnnouncementService announcementService) : PageModel
{
    private readonly IAnnouncementService _announcementService = announcementService;

    public int CaseCount { get; private set; }
    public int ApplicationCount { get; private set; }
    public int GuestCount { get; private set; }
    public int SponsorCount { get; private set; }
    public int HostCount { get; private set; }
    public int AccommodationCount { get; private set; }
    public List<Announcement> Announcements { get; private set; } = [];

    public async Task OnGetAsync()
    {
        CaseCount = await db.Cases.CountAsync();
        ApplicationCount = await db.VisaApplications.CountAsync();
        GuestCount = await db.Guests.CountAsync();
        SponsorCount = await db.People.CountAsync(p => p.SponsoredApplications.Any());
        HostCount = await db.People.CountAsync(p => p.HostedApplications.Any());
        AccommodationCount = await db.Accommodations.CountAsync();
        Announcements = await _announcementService.GetPublishedAnnouncementsAsync();
    }
}
