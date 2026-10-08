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
    public int IngestRunId { get; set; }
    public IngestRun? IngestRun { get; set; }
    public List<ApplicationPerson> People { get; set; } = [];

    public ApplicationPerson? Lead => People.OrderBy(p => p.Position).FirstOrDefault();
    public string SponsorName => $"{SponsorGivenName} {SponsorFamilyName}".Trim();
}
