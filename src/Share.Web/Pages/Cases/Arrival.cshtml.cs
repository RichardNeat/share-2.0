using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Share.Web.Models;
using Share.Web.Safeguarding;

namespace Share.Web.Pages.Cases;

public class ArrivalModel(CheckService checks) : PageModel
{
    [BindProperty] public string? Day { get; set; }
    [BindProperty] public string? Month { get; set; }
    [BindProperty] public string? Year { get; set; }

    public Case Case { get; private set; } = null!;
    public string? Error { get; private set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var @case = await checks.LoadCaseAsync(id);
        if (@case is null) return NotFound();
        Case = @case;
        if (@case.ArrivalRecordedOn is { } d)
        {
            Day = d.Day.ToString();
            Month = d.Month.ToString();
            Year = d.Year.ToString();
        }
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        var @case = await checks.LoadCaseAsync(id);
        if (@case is null) return NotFound();
        Case = @case;
        Error = await checks.RecordArrivalAsync(@case, Day, Month, Year);
        if (Error is not null) return Page();
        TempData["Message"] = "Arrival recorded. Check 4 can now be completed";
        return Redirect($"/Cases/{id}#checks");
    }
}
