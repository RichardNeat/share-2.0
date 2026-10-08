namespace Share.Web.Models;

public class VisaApplication
{
    public int Id { get; set; }
    public required string SubmissionGuid { get; set; }
    public required string Uan { get; set; }
    public string? Gwf { get; set; }
    public DateTime SubmittedAt { get; set; }
    public VisaStatus Status { get; set; } = VisaStatus.Pending;
    public string? SponsorGivenName { get; set; }
    public string? SponsorFamilyName { get; set; }
    public string? Council { get; set; }
    public bool? StayingWithSponsor { get; set; }
    public string? HostGivenName { get; set; }
    public string? HostFamilyName { get; set; }
    public int? SponsorId { get; set; }
    public Person? Sponsor { get; set; }
    public int? HostId { get; set; }
    public Person? Host { get; set; }
    public int? AccommodationId { get; set; }
    public Accommodation? Accommodation { get; set; }
    public int? CaseId { get; set; }
    public Case? Case { get; set; }
    public int IngestRunId { get; set; }
    public IngestRun? IngestRun { get; set; }
    public List<Guest> Guests { get; set; } = [];

    public Guest? Lead => Guests.OrderBy(p => p.Position).FirstOrDefault();
    public string SponsorName => $"{SponsorGivenName} {SponsorFamilyName}".Trim();

    public string HostName => Host?.FullName ?? "";
}
