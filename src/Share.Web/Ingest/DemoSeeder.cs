using Share.Web.Data;
using Share.Web.Models;
using Share.Web.Services;
using Microsoft.EntityFrameworkCore;

namespace Share.Web.Ingest;

// The only way to reset and seed. Seeding replays the data files through the real ingest
// service in number order, so the demo state is identical every time. It never invents records.
public class DemoSeeder(AppDbContext db, IngestService ingest, DataFiles files, IAnnouncementService announcements)
{
    // Scripted user actions, applied through the same services as the UI after every reset.
    // Announcements are messages to users, not casework records, so they are kept even by "Reset to empty".
    static readonly (string Title, string Body)[] Announcements =
    [
        ("Dev Share", "Built from scratch in one hour"),
    ];

    public async Task ResetAsync()
    {
        await db.Database.EnsureDeletedAsync();
        await db.Database.MigrateAsync();
        db.ChangeTracker.Clear();
        foreach (var (title, body) in Announcements)
            await announcements.CreateAsync(new Announcement { Title = title, Body = body });
    }

    public async Task<int> SeedThroughAsync(int lastFileNumber)
    {
        await ResetAsync();
        var processed = 0;
        foreach (var file in files.All().Where(f => f.Number <= lastFileNumber))
        {
            await ingest.ProcessFileAsync(file);
            processed++;
        }
        return processed;
    }
}
