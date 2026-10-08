using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Share.Tests.Support;
using Share.Web.Data;
using Share.Web.Ingest;

namespace Share.Tests.Journeys;

public class IngestAndSeedTests : IClassFixture<AppFactory>
{
    readonly AppFactory _factory;
    public IngestAndSeedTests(AppFactory factory) => _factory = factory;

    async Task<T> WithDb<T>(Func<AppDbContext, IServiceProvider, Task<T>> action)
    {
        using var scope = _factory.Services.CreateScope();
        return await action(scope.ServiceProvider.GetRequiredService<AppDbContext>(), scope.ServiceProvider);
    }

    [Fact]
    public async Task File_01_ingests_every_submission_and_person()
    {
        await _factory.SeedThroughAsync(1);
        var (apps, people, family) = await WithDb(async (db, _) => (
            await db.VisaApplications.CountAsync(),
            await db.Guests.CountAsync(),
            await db.Guests.CountAsync(p => p.Position > 1)));

        Assert.Equal(8, apps);
        Assert.Equal(12, people);
        Assert.Equal(4, family);
    }

    [Fact]
    public async Task Reprocessing_a_file_does_not_create_duplicates()
    {
        await _factory.SeedThroughAsync(1);
        var run = await WithDb(async (db, sp) =>
            await sp.GetRequiredService<IngestService>().ProcessFileAsync(sp.GetRequiredService<DataFiles>().All()[0]));

        Assert.Equal(0, run.Added);
        Assert.Equal(8, run.AlreadyPresent);
        Assert.Equal(8, await WithDb((db, _) => db.VisaApplications.CountAsync()));
    }

    [Fact]
    public async Task Seeding_twice_gives_identical_results()
    {
        async Task<string> Snapshot()
        {
            await _factory.SeedThroughAsync(99);
            return await WithDb(async (db, _) => string.Join("\n",
                (await db.VisaApplications.Include(a => a.Guests).OrderBy(a => a.Id).ToListAsync())
                    .Select(a => $"{a.Id}|{a.Uan}|{a.Status}|{string.Join(",", a.Guests.OrderBy(p => p.Id).Select(p => $"{p.Id}:{p.FullName}"))}"))
                + "\n" + string.Join("\n", (await db.TimelineEvents.OrderBy(e => e.Id).ToListAsync())
                    .Select(e => $"{e.Id}|{e.OccurredAt:O}|{e.Description}|{e.VisaApplicationId}")));
        }

        Assert.Equal(await Snapshot(), await Snapshot());
    }
}
