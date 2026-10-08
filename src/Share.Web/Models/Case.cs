namespace Share.Web.Models;

// A case: applications sharing a sponsor and an accommodation address, worked as one household.
public class Case
{
    public int Id { get; set; }
    public required string MatchKey { get; set; }
    public int? SponsorId { get; set; }
    public Person? Sponsor { get; set; }
    public int? AccommodationId { get; set; }
    public Accommodation? Accommodation { get; set; }
    public string? Council { get; set; }
    public List<VisaApplication> Applications { get; set; } = [];

    // Deterministic: Ids are assigned in ingest order on an empty database.
    public string Reference => $"CASE-{Id:D4}";

    public IEnumerable<Guest> Guests => Applications.OrderBy(a => a.SubmittedAt).ThenBy(a => a.Id).SelectMany(a => a.Guests.OrderBy(g => g.Position));

    public string Title
    {
        get
        {
            var families = Applications.OrderBy(a => a.SubmittedAt).ThenBy(a => a.Id)
                .Select(a => a.Lead?.FamilyName).Where(f => !string.IsNullOrWhiteSpace(f)).Distinct().ToList();
            return families.Count switch
            {
                0 => Reference,
                1 => $"{families[0]} household",
                _ => $"{string.Join(", ", families[..^1])} and {families[^1]} households",
            };
        }
    }

    // M4 derives this from the four safeguarding checks. With no checks recorded, the rule gives Checks Required.
    public CaseStatus Status => CaseStatus.ChecksRequired;
}

public enum CaseStatus
{
    ChecksRequired,
    ChecksPartiallyCompleted,
    PreArrivalChecksComplete,
    ChecksCompleted,
    SomeChecksFailed,
}
