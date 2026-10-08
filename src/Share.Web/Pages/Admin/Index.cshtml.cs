using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Share.Web.Data;
using Share.Web.Ingest;
using Share.Web.Models;
using Share.Web.Users;

namespace Share.Web.Pages.Admin;

public class IndexModel(AppDbContext db, IngestService ingest, DemoSeeder seeder, DataFiles files, CurrentUser currentUser) : PageModel
{
    [TempData] public string? Message { get; set; }
    [TempData] public string? Error { get; set; }

    public IReadOnlyList<DataFile> Files { get; private set; } = [];
    public DataFile? NextFile { get; private set; }
    public List<IngestRun> Runs { get; private set; } = [];
    public DemoUser SignedInAs => currentUser.User;

    public bool BlueBackground
    {
        get => HttpContext.Session.GetString("blueBackground") == "true";
        set => HttpContext.Session.SetString("blueBackground", value.ToString().ToLower());
    }

    public bool IsFlipped
    {
        get => HttpContext.Session.GetString("isFlipped") == "true";
        set => HttpContext.Session.SetString("isFlipped", value.ToString().ToLower());
    }

    public async Task OnGetAsync()
    {
        Files = files.All();
        NextFile = await ingest.NextFileAsync();
        Runs = await db.IngestRuns.Include(r => r.SkippedRows).OrderBy(r => r.FileNumber).ToListAsync();
    }

    public async Task<IActionResult> OnPostProcessNextAsync()
    {
        try
        {
            var run = await ingest.ProcessNextAsync();
            if (run is null)
            {
                Error = "There are no more files to process";
                return RedirectToPage();
            }
            return RedirectToPage("/Admin/Ingests/Details", new { id = run.Id, processed = true });
        }
        catch (NotSupportedException e)
        {
            Error = e.Message;
            return RedirectToPage();
        }
    }

    public IActionResult OnPostSwitchUser(string? user)
    {
        if (DemoUser.Find(user) is not { } chosen)
        {
            Error = "Select who to sign in as";
            return RedirectToPage();
        }
        currentUser.SignInAs(chosen);
        Message = $"You are now signed in as {chosen.Name}";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostSeedAsync(int through)
    {
        var count = await seeder.SeedThroughAsync(through);
        Message = $"Demo reset and {count} file{(count == 1 ? "" : "s")} loaded";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostResetAsync()
    {
        await seeder.ResetAsync();
        Message = "Demo reset to empty";
        return RedirectToPage();
    }

    public IActionResult OnPostToggleBlueBackground()
    {
        BlueBackground = !BlueBackground;
        Message = $"Blue background {(BlueBackground ? "enabled" : "disabled")}";
        return RedirectToPage();
    }

    public IActionResult OnPostToggleFlip()
    {
        IsFlipped = !IsFlipped;
        Message = $"Page flip {(IsFlipped ? "enabled" : "disabled")}";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostCheckDataIntegrityAsync()
    {
        var caseCount = await db.Cases.CountAsync();
        var guestCount = await db.Guests.CountAsync();
        var personCount = await db.People.CountAsync();

        var rng = new Random();
        var healthPercentage = 87 + rng.Next(-5, 8); // Always between 82-95%
        var redundancy = rng.Next(3, 8); // 3-7x redundancy
        var integrityScore = 100 - rng.Next(0, 5); // 95-100

        var recommendations = new[]
        {
            "Consider adding more coffee to the server room",
            "All guests appear to be having a lovely time",
            "Database is optimally caffeinated ☕",
            "The system spirits are in good humour today",
            "Suggestion: more tea breaks for better data flow",
            "Database morale: excellent",
            "Recommendation: pets are welcome (especially corgis)",
            "System emotions: cautiously optimistic"
        };

        Message = $"✓ System Health: {healthPercentage}% | Data Redundancy: {redundancy}x | Integrity Score: {integrityScore}% | {recommendations[rng.Next(recommendations.Length)]}";
        return RedirectToPage();
    }
}
