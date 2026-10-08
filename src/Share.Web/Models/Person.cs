namespace Share.Web.Models;

// Someone in the UK named on applications: a sponsor, a host, or both.
// Sponsor and host are different roles: the sponsor backs the visa, the host is whoever the guests live with.
// One record per person however many applications name them. Identified by MatchKey (see RecordKeys).
public class Person
{
    public int Id { get; set; }
    public required string MatchKey { get; set; }
    public string? GivenName { get; set; }
    public string? FamilyName { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? Email { get; set; }
    public string? Telephone { get; set; }
    public string? Address { get; set; }
    public string? Postcode { get; set; }
    public string? Council { get; set; }
    public List<VisaApplication> SponsoredApplications { get; set; } = [];
    public List<VisaApplication> HostedApplications { get; set; } = [];

    public string FullName => $"{GivenName} {FamilyName}".Trim();
    public bool IsSponsor => SponsoredApplications.Count > 0;
    public bool IsHost => HostedApplications.Count > 0;
}
