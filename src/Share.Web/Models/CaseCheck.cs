namespace Share.Web.Models;

public enum CheckKind
{
    AccommodationExists = 1,
    AccommodationSuitable = 2,
    DbsAndSponsorSuitable = 3,
    GuestsArrived = 4,
}

public enum CheckStatus
{
    NotStarted,
    InProgress,
    Passed,
    Failed,
    NoLongerRequired,
}

public enum DbsType
{
    Standard,
    Enhanced,
}

// One of the four safeguarding checks on a case. A row exists once someone has updated it;
// until then the check is Not Started.
public class CaseCheck
{
    public int Id { get; set; }
    public int CaseId { get; set; }
    public CheckKind Kind { get; set; }
    public CheckStatus Status { get; set; }
    public string? FailureReason { get; set; }
    public DbsType? DbsType { get; set; }
    public DateTime UpdatedAt { get; set; }
}
