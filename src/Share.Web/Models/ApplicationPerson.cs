namespace Share.Web.Models;

// One person on a submission. Position 1 is the lead applicant.
// Name parts, date of birth and passport are separate columns (groundwork for M14 duplicate matching).
public class ApplicationPerson
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

    public bool IsLead => Position == 1;
    public string FullName => $"{GivenName} {FamilyName}".Trim();
}

public class ApplicationAnswer
{
    public int Id { get; set; }
    public int ApplicationPersonId { get; set; }
    public int Position { get; set; }
    public required string Title { get; set; }
    public string? Answer { get; set; }
}
