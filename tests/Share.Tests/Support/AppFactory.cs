using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Share.Web.Ingest;

namespace Share.Tests.Support;

// Each factory gets its own temp SQLite file, deleted afterwards.
public class AppFactory : WebApplicationFactory<Program>
{
    readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"share-tests-{Guid.NewGuid():N}.db");

    public static string RepoRoot { get; } = FindRepoRoot();
    public static string DataDirectory => Path.Combine(RepoRoot, "data");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Default", $"Data Source={_dbPath}");
        builder.UseSetting("DataDirectory", DataDirectory);
        builder.UseSetting("Clock:Today", "2026-10-01");
    }

    public async Task SeedThroughAsync(int fileNumber)
    {
        using var scope = Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<DemoSeeder>().SeedThroughAsync(fileNumber);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        SqliteConnection.ClearAllPools();
        foreach (var f in new[] { _dbPath, _dbPath + "-shm", _dbPath + "-wal" })
            if (File.Exists(f)) File.Delete(f);
    }

    static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Share.slnx"))) dir = dir.Parent;
        return dir?.FullName ?? throw new InvalidOperationException("Could not find Share.slnx");
    }
}
