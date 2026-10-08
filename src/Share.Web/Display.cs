using System.Globalization;
using Share.Web.Models;

namespace Share.Web;

// Shared formatting so every page words things the same way.
public static class Display
{
    static readonly CultureInfo UkCulture = CultureInfo.GetCultureInfo("en-GB");
    static readonly TimeZoneInfo London = TimeZoneInfo.FindSystemTimeZoneById("Europe/London");

    public static string Council(string? council) =>
        string.IsNullOrWhiteSpace(council) ? "Not given" : UkCulture.TextInfo.ToTitleCase(council.Trim().ToLowerInvariant());

    public static string Date(DateOnly? date) => date?.ToString("d MMMM yyyy", UkCulture) ?? "Not given";

    // GOV.UK style: 9 February 2026 at 9:15am
    public static string DateTime(DateTime utc)
    {
        var local = TimeZoneInfo.ConvertTimeFromUtc(System.DateTime.SpecifyKind(utc, DateTimeKind.Utc), London);
        return $"{local.ToString("d MMMM yyyy", UkCulture)} at {local.ToString("h:mm", UkCulture)}{(local.Hour < 12 ? "am" : "pm")}";
    }

    public static string OrNotGiven(string? value) => string.IsNullOrWhiteSpace(value) ? "Not given" : value;

    // One wording and one colour per status.
    public static (string Text, string Css) VisaStatusTag(VisaStatus status) => status switch
    {
        VisaStatus.Pending => ("Pending", "govuk-tag--grey"),
        VisaStatus.Confirmed => ("Confirmed", "govuk-tag--blue"),
        VisaStatus.Issued => ("Issued", "govuk-tag--light-blue"),
        VisaStatus.Arrived => ("Arrived", "govuk-tag--green"),
        VisaStatus.Withdrawn => ("Withdrawn", "govuk-tag--yellow"),
        VisaStatus.Refused => ("Refused", "govuk-tag--red"),
        _ => (status.ToString(), "govuk-tag--grey"),
    };

    public static (string Text, string Css) CaseStatusTag(CaseStatus status) => status switch
    {
        CaseStatus.ChecksRequired => ("Checks required", "govuk-tag--grey"),
        CaseStatus.ChecksPartiallyCompleted => ("Checks partially completed", "govuk-tag--yellow"),
        CaseStatus.PreArrivalChecksComplete => ("Pre-arrival checks complete", "govuk-tag--blue"),
        CaseStatus.ChecksCompleted => ("Checks completed", "govuk-tag--green"),
        CaseStatus.SomeChecksFailed => ("Some checks failed", "govuk-tag--red"),
        _ => (status.ToString(), "govuk-tag--grey"),
    };

    public static string Age(DateOnly? dateOfBirth, DateOnly today) =>
        dateOfBirth is { } dob ? $"{Ages.On(dob, today)}" : "Not given";
}
