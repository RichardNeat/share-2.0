using Share.Web.Models;

namespace Share.Web.Safeguarding;

public record CheckState(
    CheckKind Kind,
    CheckStatus Recorded,
    CheckStatus Effective,
    string? FailureReason,
    DbsType? DbsType,
    bool Locked,
    string? Note);

public record CaseAssessment(
    CaseStatus Status,
    bool EnhancedDbsRequired,
    IReadOnlyList<Guest> Children,
    IReadOnlyList<CheckState> Checks);

// The safeguarding rules, lifted from the brief. Pure functions: easy to test, never stale.
public static class SafeguardingRules
{
    public static readonly CheckKind[] AllChecks = [CheckKind.AccommodationExists, CheckKind.AccommodationSuitable, CheckKind.DbsAndSponsorSuitable, CheckKind.GuestsArrived];
    public static readonly CheckKind[] PreArrivalChecks = [CheckKind.AccommodationExists, CheckKind.AccommodationSuitable, CheckKind.DbsAndSponsorSuitable];

    static bool Done(CheckStatus s) => s is CheckStatus.Passed or CheckStatus.NoLongerRequired;

    // Derived case status, in the brief's order of precedence.
    public static CaseStatus Derive(IReadOnlyDictionary<CheckKind, CheckStatus> checks)
    {
        CheckStatus Of(CheckKind k) => checks.TryGetValue(k, out var s) ? s : CheckStatus.NotStarted;
        if (AllChecks.Any(k => Of(k) == CheckStatus.Failed)) return CaseStatus.SomeChecksFailed;
        if (AllChecks.All(k => Done(Of(k)))) return CaseStatus.ChecksCompleted;
        if (PreArrivalChecks.All(k => Done(Of(k)))) return CaseStatus.PreArrivalChecksComplete;
        if (AllChecks.Any(k => Of(k) == CheckStatus.Passed)) return CaseStatus.ChecksPartiallyCompleted;
        return CaseStatus.ChecksRequired;
    }

    // Enhanced DBS rule: any guest under 18 on the case.
    public static IReadOnlyList<Guest> Children(IEnumerable<Guest> guests, DateOnly today) =>
        guests.Where(g => g.DateOfBirth is { } dob && Ages.On(dob, today) < 18).ToList();

    // A standard DBS does not complete check 3 on a case that needs an Enhanced DBS.
    public static (CheckStatus Effective, string? Note) Effective(CheckKind kind, CheckStatus recorded, DbsType? dbs, bool enhancedRequired)
    {
        if (kind == CheckKind.DbsAndSponsorSuitable && recorded == CheckStatus.Passed && enhancedRequired && dbs != DbsType.Enhanced)
            return (CheckStatus.InProgress, "Recorded as passed with a standard DBS. This case needs an Enhanced DBS, so the check is not complete");
        return (recorded, null);
    }

    // Check 4 opens once guests have arrived: recorded by a caseworker, or shown by the arrivals feed (M5).
    public static bool GuestsHaveArrived(Case c) =>
        c.ArrivalRecordedOn is not null || c.Applications.Any(a => a.Status == VisaStatus.Arrived);

    // Validates the arrival date a caseworker enters (GOV.UK date input: day, month, year).
    public static (DateOnly? Date, string? Error) ValidateArrivalDate(string? day, string? month, string? year, DateOnly today)
    {
        if (string.IsNullOrWhiteSpace(day) && string.IsNullOrWhiteSpace(month) && string.IsNullOrWhiteSpace(year))
            return (null, "Enter the date the guests arrived");
        if (string.IsNullOrWhiteSpace(day)) return (null, "Date the guests arrived must include a day");
        if (string.IsNullOrWhiteSpace(month)) return (null, "Date the guests arrived must include a month");
        if (string.IsNullOrWhiteSpace(year)) return (null, "Date the guests arrived must include a year");
        if (!int.TryParse(day.Trim(), out var d) || !int.TryParse(month.Trim(), out var m) || !int.TryParse(year.Trim(), out var y)
            || year.Trim().Length != 4 || m is < 1 or > 12 || d < 1 || d > DateTime.DaysInMonth(y is >= 1 and <= 9999 ? y : 2000, m))
            return (null, "Date the guests arrived must be a real date");
        var date = new DateOnly(y, m, d);
        if (date > today) return (null, "Date the guests arrived must be today or in the past");
        return (date, null);
    }

    public static CaseAssessment Assess(Case c, DateOnly today)
    {
        var children = Children(c.Guests, today);
        var enhanced = children.Count > 0;
        var states = AllChecks.Select(kind =>
        {
            var row = c.Checks.FirstOrDefault(x => x.Kind == kind);
            var recorded = row?.Status ?? CheckStatus.NotStarted;
            var (effective, note) = Effective(kind, recorded, row?.DbsType, enhanced);
            var locked = kind == CheckKind.GuestsArrived && !GuestsHaveArrived(c);
            return new CheckState(kind, recorded, effective, row?.FailureReason, row?.DbsType, locked, note);
        }).ToList();
        var status = Derive(states.ToDictionary(s => s.Kind, s => s.Effective));
        return new CaseAssessment(status, enhanced, children, states);
    }

    public record ValidationError(string Field, string Message);

    // Validates an update to a check from the case page.
    public static List<ValidationError> Validate(CheckKind kind, CheckStatus? status, string? reason, DbsType? dbs, bool enhancedRequired, bool locked)
    {
        var errors = new List<ValidationError>();
        if (locked)
        {
            errors.Add(new("Status", "This check becomes available when guests arrive"));
            return errors;
        }
        if (status is null)
        {
            errors.Add(new("Status", "Select the status of the check"));
            return errors;
        }
        if (status == CheckStatus.Failed && string.IsNullOrWhiteSpace(reason))
            errors.Add(new("FailureReason", "Enter why the check failed"));
        if (kind == CheckKind.DbsAndSponsorSuitable && status == CheckStatus.Passed)
        {
            if (dbs is null)
                errors.Add(new("DbsType", "Select the type of DBS check"));
            else if (enhancedRequired && dbs == DbsType.Standard)
                errors.Add(new("DbsType", "This case has a guest under 18, so the check needs an Enhanced DBS. A standard DBS does not complete it"));
        }
        return errors;
    }
}
