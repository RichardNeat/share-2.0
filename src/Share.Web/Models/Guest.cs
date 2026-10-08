namespace Share.Web.Models;

// One person on a visa application. Position 1 is the lead applicant.
// Guests are not merged automatically: spotting duplicate people is M13/M14's job, with a human confirming.
// Name parts, date of birth and passport are separate columns (groundwork for M14 duplicate matching).
public class Guest
{
    public int Id { get; set; }
    public int VisaApplicationId { get; set; }
    public VisaApplication? VisaApplication { get; set; }
    public int Position { get; set; }
    public string? Role { get; set; }
    public string? GivenName { get; set; }
    public string? FamilyName { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? Nationality { get; set; }
    public string? PassportNumber { get; set; }
    public string? Gwf { get; set; }
    public List<ApplicationAnswer> Answers { get; set; } = [];

    // Set when a reviewer accepts a suggested duplicate: this record is the same person as that one.
    public int? DuplicateOfGuestId { get; set; }
    public Guest? DuplicateOf { get; set; }
    public DateTime? MarkedDuplicateAt { get; set; }

    public bool IsLead => Position == 1;
    public string FullName => $"{GivenName} {FamilyName}".Trim();
}

public class ApplicationAnswer
{
    public int Id { get; set; }
    public int GuestId { get; set; }
    public int Position { get; set; }
    public required string Title { get; set; }
    public string? Answer { get; set; }
}
