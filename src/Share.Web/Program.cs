using Microsoft.EntityFrameworkCore;
using Share.Web;
using Share.Web.Data;
using Share.Web.Ingest;
using Share.Web.Users;
using Share.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddSession();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<CurrentUser>();
builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseSqlite(builder.Configuration.GetConnectionString("Default") ?? "Data Source=app.db"));

var dataDirectory = builder.Configuration["DataDirectory"]
    ?? Path.Combine(builder.Environment.ContentRootPath, "..", "..", "data");
builder.Services.AddSingleton(new DataFiles(Path.GetFullPath(dataDirectory)));

builder.Services.AddSingleton<TimeProvider>(
    DateOnly.TryParse(builder.Configuration["Clock:Today"], out var today)
        ? new FixedDateTimeProvider(today)
        : TimeProvider.System);
builder.Services.AddScoped<IngestService>();
builder.Services.AddScoped<DemoSeeder>();
builder.Services.AddScoped<Share.Web.Safeguarding.CheckService>();
builder.Services.AddScoped<IAnnouncementService, AnnouncementService>();
builder.Services.AddScoped<INotificationService, NotificationService>();

var app = builder.Build();

// Console commands run before the startup migration, so they can recover any old database.
// Console commands: `dotnet run --project src/Share.Web -- reset` or `-- seed --through 6`
if (args.Length > 0 && args[0] is "reset" or "seed")
{
    using var scope = app.Services.CreateScope();
    var seeder = scope.ServiceProvider.GetRequiredService<DemoSeeder>();
    if (args[0] == "reset")
    {
        await seeder.ResetAsync();
        Console.WriteLine("Database reset to empty.");
    }
    else
    {
        var i = Array.IndexOf(args, "--through");
        var through = i >= 0 && i + 1 < args.Length && int.TryParse(args[i + 1], out var n) ? n : int.MaxValue;
        var count = await seeder.SeedThroughAsync(through);
        Console.WriteLine($"Database reset and seeded with {count} file(s).");
    }
    return;
}

using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.Migrate();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}
app.UseStatusCodePagesWithReExecute("/NotFound");

app.UseSession();
app.UseRouting();
app.UseAuthorization();
app.MapStaticAssets();
app.MapRazorPages().WithStaticAssets();

app.Run();

public partial class Program;
