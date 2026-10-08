using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Share.Web.Data;
using Share.Web.Models;

namespace Share.Web.Pages;

public class DashboardModel(AppDbContext db, TimeProvider clock) : PageModel
{
    public int CasesNeedingChecks { get; set; }
    public int RecentArrivals { get; set; }
    public int TotalCases { get; set; }
    public int CasesWithAllChecksPassed { get; set; }
    public Dictionary<string, int> CasesByCouncil { get; set; } = [];
    public Dictionary<VisaStatus, int> ApplicationsByStatus { get; set; } = [];

    public async Task OnGetAsync()
    {
        var today = DateOnly.FromDateTime(clock.GetUtcNow().DateTime);
        var sevenDaysAgo = today.AddDays(-7);

        TotalCases = await db.Cases.CountAsync();
        CasesNeedingChecks = await db.Cases.CountAsync(c => c.Checks.Count(ch => ch.Status == CheckStatus.Passed) < 4);
        RecentArrivals = await db.Guests.CountAsync(g => g.VisaApplication!.Case!.ArrivalRecordedOn >= sevenDaysAgo);
        CasesWithAllChecksPassed = await db.Cases.CountAsync(c => c.Checks.Count(ch => ch.Status == CheckStatus.Passed) == 4);

        var byCouncil = await db.Cases
            .GroupBy(c => c.Council ?? "Unknown")
            .Select(g => new { Council = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToListAsync();
        CasesByCouncil = byCouncil.ToDictionary(x => x.Council, x => x.Count);

        var byStatus = await db.VisaApplications
            .GroupBy(a => a.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();
        ApplicationsByStatus = byStatus.ToDictionary(x => x.Status, x => x.Count);
    }
}
