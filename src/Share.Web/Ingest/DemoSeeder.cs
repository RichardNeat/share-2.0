using Share.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace Share.Web.Ingest;

// The only way to reset and seed. Seeding replays the data files through the real ingest
// service in number order, so the demo state is identical every time. It never invents records.
public class DemoSeeder(AppDbContext db, IngestService ingest, DataFiles files)
{
    public async Task ResetAsync()
    {
        await db.Database.EnsureDeletedAsync();
        await db.Database.MigrateAsync();
        db.ChangeTracker.Clear();
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
