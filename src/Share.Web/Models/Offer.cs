namespace Share.Web.Models;

public enum OfferStatus
{
    Open,
    Taken,
}

// An expression of interest: an offer of accommodation from a would-be host, with no guest attached.
public class Offer
{
    public int Id { get; set; }
    public required string SubmissionReference { get; set; }
    public DateTime SubmittedAt { get; set; }
    public string? HostName { get; set; }
    public string? Email { get; set; }
    public string? Telephone { get; set; }
    public string? Address1 { get; set; }
    public string? Address2 { get; set; }
    public string? TownOrCity { get; set; }
    public string? Postcode { get; set; }
    // From the town or city: the offer files carry no council, and both councils are cities.
    public string? Council { get; set; }
    public DateOnly? AvailableFrom { get; set; }
    public int? Adults { get; set; }
    public int? Children { get; set; }
    public int? Bedrooms { get; set; }
    public bool? StepFree { get; set; }
    public bool? Pets { get; set; }
    public OfferStatus Status { get; set; } = OfferStatus.Open;
    public int? TakenByCaseId { get; set; }
    public Case? TakenByCase { get; set; }
    public DateTime? TakenAt { get; set; }

    public string Address => string.Join(", ", new[] { Address1, Address2, TownOrCity }.Where(x => !string.IsNullOrWhiteSpace(x)));
}
