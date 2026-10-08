using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Share.Web.Data;
using Share.Web.Ingest;
using Share.Web.Models;

namespace Share.Web.Pages.Admin;

public class IndexModel(AppDbContext db, IngestService ingest, DemoSeeder seeder, DataFiles files) : PageModel
{
    [TempData] public string? Message { get; set; }
    [TempData] public string? Error { get; set; }

    public IReadOnlyList<DataFile> Files { get; private set; } = [];
    public DataFile? NextFile { get; private set; }
    public List<IngestRun> Runs { get; private set; } = [];

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
}
