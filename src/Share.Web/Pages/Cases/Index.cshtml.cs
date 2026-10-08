using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Share.Web.Data;
using Share.Web.Models;
using Share.Web.Safeguarding;

namespace Share.Web.Pages.Cases;

public class IndexModel(AppDbContext db, TimeProvider clock) : PageModel
{
    public List<(Case Case, CaseAssessment Assessment)> Cases { get; private set; } = [];
    public DateOnly Today { get; private set; }

    public async Task OnGetAsync()
    {
        Today = Ages.Today(clock);
        var cases = await db.Cases
            .Include(c => c.Applications).ThenInclude(a => a.Guests)
            .Include(c => c.Accommodation)
            .Include(c => c.Checks)
            .AsSplitQuery()
            .OrderBy(c => c.Id).ToListAsync();
        Cases = cases.Select(c => (c, SafeguardingRules.Assess(c, Today))).ToList();
    }
}
