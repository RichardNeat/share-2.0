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
    public List<CaseCheck> Checks { get; set; } = [];

    // Set by a rematch: the case's new host, replacing the hosts named on its applications.
    public int? HostId { get; set; }
    public Person? Host { get; set; }
    public DateTime? RematchedAt { get; set; }

    // Recorded by a caseworker on the case page. The arrivals feed (M5) can also show guests as arrived.
    public DateOnly? ArrivalRecordedOn { get; set; }
    public DateTime? ArrivalRecordedAt { get; set; }

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
}

public enum CaseStatus
{
    ChecksRequired,
    ChecksPartiallyCompleted,
    PreArrivalChecksComplete,
    ChecksCompleted,
    SomeChecksFailed,
}
