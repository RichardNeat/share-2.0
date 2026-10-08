using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Share.Web.Data;
using Share.Web.Models;
using Share.Web.Safeguarding;

namespace Share.Web.Pages.Cases;

public class DetailsModel(CheckService checks, AppDbContext db, TimeProvider clock) : PageModel
{
    [TempData] public string? Message { get; set; }
    public Case Case { get; private set; } = null!;
    public CaseAssessment Assessment { get; private set; } = null!;
    public DateOnly Today { get; private set; }
    public List<TimelineEvent> Timeline { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var @case = await checks.LoadCaseAsync(id);
        if (@case is null) return NotFound();
        Case = @case;
        Today = Ages.Today(clock);
        Assessment = SafeguardingRules.Assess(@case, Today);
        Timeline = (await db.TimelineEvents.Where(e => e.CaseId == id).ToListAsync()).NewestFirst().ToList();
        return Page();
    }
}
