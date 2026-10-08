using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Share.Web.Models;
using Share.Web.Safeguarding;

namespace Share.Web.Pages.Cases;

public class CheckModel(CheckService checks, TimeProvider clock) : PageModel
{
    [BindProperty] public CheckStatus? Status { get; set; }
    [BindProperty] public string? FailureReason { get; set; }
    [BindProperty] public DbsType? DbsType { get; set; }

    public Case Case { get; private set; } = null!;
    public CheckKind Kind { get; private set; }
    public bool EnhancedDbsRequired { get; private set; }
    public List<SafeguardingRules.ValidationError> Errors { get; private set; } = [];

    async Task<IActionResult?> LoadAsync(int id, int kind)
    {
        if (!Enum.IsDefined(typeof(CheckKind), kind)) return NotFound();
        var @case = await checks.LoadCaseAsync(id);
        if (@case is null) return NotFound();
        Case = @case;
        Kind = (CheckKind)kind;
        var assessment = SafeguardingRules.Assess(@case, Ages.Today(clock));
        EnhancedDbsRequired = assessment.EnhancedDbsRequired;
        // Check 4 is unlocked by arrivals, not edited here until then.
        if (assessment.Checks.Single(c => c.Kind == Kind).Locked) return RedirectToPage("Details", new { id });
        return null;
    }

    public async Task<IActionResult> OnGetAsync(int id, int kind)
    {
        if (await LoadAsync(id, kind) is { } result) return result;
        var row = Case.Checks.FirstOrDefault(c => c.Kind == Kind);
        Status = row?.Status ?? CheckStatus.NotStarted;
        FailureReason = row?.FailureReason;
        DbsType = row?.DbsType;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id, int kind)
    {
        if (await LoadAsync(id, kind) is { } result) return result;
        Errors = await checks.UpdateAsync(Case, Kind, Status, FailureReason, DbsType);
        if (Errors.Count > 0) return Page();
        TempData["Message"] = $"{Display.CheckName(Kind)} updated to {Display.CheckStatusTag(Status!.Value).Text.ToLowerInvariant()}";
        return Redirect($"/Cases/{id}#checks");
    }

    public string? ErrorFor(string field) => Errors.FirstOrDefault(e => e.Field == field)?.Message;
}
