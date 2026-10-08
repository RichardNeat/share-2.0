using System.Globalization;
using System.Text.Json;
using Share.Web.Models;

namespace Share.Web.Ingest;

public record ParsedSubmission(int RowNumber, VisaApplication? Application, string? Reference, string? SkipReason);

// Turns a visa applications file (JSON array of submissions) into unsaved records.
// Details arrive as questions; we match on the question title.
public static class VisaApplicationParser
{
    public static IReadOnlyList<ParsedSubmission> Parse(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var results = new List<ParsedSubmission>();
        var row = 0;
        foreach (var submission in doc.RootElement.EnumerateArray())
        {
            row++;
            results.Add(ParseSubmission(row, submission));
        }
        return results;
    }

    static ParsedSubmission ParseSubmission(int row, JsonElement s)
    {
        var guid = Str(s, "submissionGUID");
        var app = s.TryGetProperty("application", out var a) ? a : default;
        var uan = app.ValueKind == JsonValueKind.Object ? Str(app, "uniqueApplicationNumber") : null;
        var reference = uan ?? guid;

        if (string.IsNullOrWhiteSpace(guid)) return new(row, null, reference, "No submission GUID");
        if (string.IsNullOrWhiteSpace(uan)) return new(row, null, reference, "No unique application number (UAN)");
        if (!s.TryGetProperty("person", out var people) || people.ValueKind != JsonValueKind.Array || people.GetArrayLength() == 0)
            return new(row, null, reference, "No people on the submission");

        var submittedAt = DateTime.TryParse(Str(app, "eventDateTime"), CultureInfo.InvariantCulture,
            DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var dt) ? dt : DateTime.MinValue;

        var application = new VisaApplication { SubmissionGuid = guid, Uan = uan, SubmittedAt = submittedAt };
        var position = 0;
        foreach (var p in people.EnumerateArray())
        {
            position++;
            var answers = new List<ApplicationAnswer>();
            if (p.TryGetProperty("questions", out var qs) && qs.ValueKind == JsonValueKind.Array)
            {
                var i = 0;
                foreach (var q in qs.EnumerateArray())
                {
                    var title = Str(q, "title");
                    if (string.IsNullOrWhiteSpace(title)) continue;
                    answers.Add(new ApplicationAnswer { Title = title, Answer = Str(q, "answer"), Position = ++i });
                }
            }
            string? Ans(string title) => answers.FirstOrDefault(x => x.Title == title)?.Answer is { Length: > 0 } v ? v.Trim() : null;

            application.Guests.Add(new Guest
            {
                Position = position,
                Role = Str(p, "role"),
                GivenName = Ans("Given name"),
                FamilyName = Ans("Family name"),
                DateOfBirth = DateOnly.TryParseExact(Ans("Date of birth"), "yyyy-MM-dd", out var dob) ? dob : null,
                Nationality = Ans("Nationality"),
                PassportNumber = Ans("Passport number"),
                Gwf = Ans("GWF number"),
                Answers = answers,
            });

            if (position == 1)
            {
                application.Gwf = Ans("GWF number");
                application.SponsorGivenName = Ans("Sponsor given name");
                application.SponsorFamilyName = Ans("Sponsor family name");
                application.Council = Ans("Local authority of UK address");
                application.StayingWithSponsor = Ans("Will you be staying at your sponsor's address?") switch
                {
                    "Yes" => true,
                    "No" => false,
                    _ => null,
                };
                application.HostGivenName = Ans("Host given name");
                application.HostFamilyName = Ans("Host family name");

                var sponsorDob = DateOnly.TryParseExact(Ans("Sponsor date of birth"), "yyyy-MM-dd", out var sd) ? sd : (DateOnly?)null;
                if (!string.IsNullOrWhiteSpace($"{application.SponsorGivenName}{application.SponsorFamilyName}{Ans("Sponsor email address")}"))
                {
                    application.Sponsor = new Person
                    {
                        MatchKey = RecordKeys.Sponsor(Ans("Sponsor email address"), uan),
                        GivenName = application.SponsorGivenName,
                        FamilyName = application.SponsorFamilyName,
                        DateOfBirth = sponsorDob,
                        Email = Ans("Sponsor email address"),
                        Telephone = Ans("Sponsor telephone number"),
                        Address = Ans("Sponsor address"),
                        Postcode = Ans("Sponsor postcode"),
                        Council = Ans("Sponsor local authority"),
                    };
                }

                // The host is the sponsor when the guests stay at the sponsor's address; otherwise the named host.
                // If the question is unanswered we do not assume the sponsor is the host.
                if (application.StayingWithSponsor == true)
                    application.Host = application.Sponsor;
                else if (RecordKeys.Host(application.HostGivenName, application.HostFamilyName, uan) is { } hostKey)
                    application.Host = new Person { MatchKey = hostKey, GivenName = application.HostGivenName, FamilyName = application.HostFamilyName };

                var address = Ans("UK address where you will be staying");
                if (RecordKeys.Accommodation(address, application.Council) is { } accommodationKey)
                {
                    application.Accommodation = new Accommodation
                    {
                        MatchKey = accommodationKey,
                        Address = address!,
                        Postcode = Ans("Postcode of UK address"),
                        Council = application.Council,
                    };
                }
            }
        }
        return new(row, application, reference, null);
    }

    static string? Str(JsonElement e, string name) =>
        e.ValueKind == JsonValueKind.Object && e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
}
