using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Share.Web.Data;
using Share.Web.Models;
using Share.Web.Users;

namespace Share.Web.Pages.Admin.Ingests;

public class DetailsModel(AppDbContext db, CurrentUser currentUser) : PageModel
{
    [BindProperty(SupportsGet = true)] public bool Processed { get; set; }
    public IngestRun Run { get; private set; } = null!;
    public List<TimelineEvent> Events { get; private set; } = [];
    public DemoUser SignedInAs => currentUser.User;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var run = await db.IngestRuns.Include(r => r.SkippedRows).FirstOrDefaultAsync(r => r.Id == id);
        if (run is null) return NotFound();
        Run = run;
        Events = (await db.TimelineEvents.Where(e => e.IngestRunId == id).ToListAsync()).OldestFirst().ToList();
        return Page();
    }
}
