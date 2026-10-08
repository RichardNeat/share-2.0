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
    public List<Tile> Tiles { get; private set; } = [];

    // A link to one part of the service. Count is null for tiles that aren't a list of records.
    public record Tile(string Title, string Href, string Description, int? Count = null, string Singular = "", string Plural = "");

    public async Task OnGetAsync()
    {
        CaseCount = await db.Cases.CountAsync();
        ApplicationCount = await db.VisaApplications.CountAsync();
        GuestCount = await db.Guests.CountAsync();
        SponsorCount = await db.People.CountAsync(p => p.SponsoredApplications.Any());
        HostCount = await db.People.CountAsync(p => p.HostedApplications.Any());
        AccommodationCount = await db.Accommodations.CountAsync();
        Announcements = await _announcementService.GetPublishedAnnouncementsAsync();

        Tiles =
        [
            new("Cases", "/Cases", "Work through safeguarding checks for each household.", CaseCount, "case", "cases"),
            new("Visa applications", "/Applications", "See every application received and its visa status.", ApplicationCount, "application", "applications"),
            new("Guests", "/Guests", "Find a guest and the application and case they belong to.", GuestCount, "guest", "guests"),
            new("Sponsors", "/Sponsors", "See who is sponsoring guests and which applications they back.", SponsorCount, "sponsor", "sponsors"),
            new("Hosts", "/Hosts", "See who guests are living with.", HostCount, "host", "hosts"),
            new("Accommodations", "/Accommodations", "See the properties guests are staying in.", AccommodationCount, "accommodation", "accommodations"),
            new("Demo controls", "/Admin", "Change user, process the next data file or reset the demo."),
        ];
    }
}
