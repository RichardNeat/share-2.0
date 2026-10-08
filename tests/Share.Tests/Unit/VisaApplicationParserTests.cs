using Share.Web.Ingest;

namespace Share.Tests.Unit;

public class VisaApplicationParserTests
{
    const string OneFamily = """
    [{
      "submissionGUID": "g-1",
      "application": { "eventDateTime": "2026-02-09T09:15:00Z", "uniqueApplicationNumber": "1313-2026-0001-7919" },
      "person": [
        { "id": 1, "role": "Applicant", "questions": [
          { "title": "Given name", "answer": "Olena" },
          { "title": "Family name", "answer": "Kovalenko" },
          { "title": "Date of birth", "answer": "1991-03-12" },
          { "title": "Nationality", "answer": "Ukraine" },
          { "title": "Passport number", "answer": "FB201117" },
          { "title": "GWF number", "answer": "GWF063900037" },
          { "title": "Sponsor given name", "answer": "Clare" },
          { "title": "Sponsor family name", "answer": "Osborne" },
          { "title": "Local authority of UK address", "answer": "EXETER" }
        ]},
        { "id": 2, "role": "Family member", "questions": [
          { "title": "Given name", "answer": "Andriy" },
          { "title": "Family name", "answer": "Kovalenko" },
          { "title": "Date of birth", "answer": "2014-01-22" }
        ]}
      ]
    }]
    """;

    [Fact]
    public void Extracts_lead_applicant_and_application_details()
    {
        var app = Assert.Single(VisaApplicationParser.Parse(OneFamily)).Application!;

        Assert.Equal("1313-2026-0001-7919", app.Uan);
        Assert.Equal("GWF063900037", app.Gwf);
        Assert.Equal(new DateTime(2026, 2, 9, 9, 15, 0, DateTimeKind.Utc), app.SubmittedAt);
        Assert.Equal("Clare Osborne", app.SponsorName);
        Assert.Equal("EXETER", app.Council);
        Assert.Equal(Share.Web.Models.VisaStatus.Pending, app.Status);
        var lead = app.Lead!;
        Assert.Equal("Olena Kovalenko", lead.FullName);
        Assert.Equal(new DateOnly(1991, 3, 12), lead.DateOfBirth);
        Assert.Equal("Ukraine", lead.Nationality);
        Assert.Equal("FB201117", lead.PassportNumber);
    }

    [Fact]
    public void Captures_family_members_with_their_answers_in_order()
    {
        var app = VisaApplicationParser.Parse(OneFamily)[0].Application!;

        Assert.Equal(2, app.People.Count);
        var child = app.People[1];
        Assert.False(child.IsLead);
        Assert.Equal("Andriy", child.GivenName);
        Assert.Equal(new DateOnly(2014, 1, 22), child.DateOfBirth);
        Assert.Equal(["Given name", "Family name", "Date of birth"], child.Answers.Select(a => a.Title));
    }

    [Theory]
    [InlineData("""[{ "application": { "uniqueApplicationNumber": "1" }, "person": [{}] }]""", "No submission GUID")]
    [InlineData("""[{ "submissionGUID": "g", "application": {}, "person": [{}] }]""", "No unique application number (UAN)")]
    [InlineData("""[{ "submissionGUID": "g", "application": { "uniqueApplicationNumber": "1" }, "person": [] }]""", "No people on the submission")]
    public void Skips_submissions_missing_essentials_with_a_reason(string json, string reason)
    {
        var result = Assert.Single(VisaApplicationParser.Parse(json));
        Assert.Null(result.Application);
        Assert.Equal(reason, result.SkipReason);
    }

    [Fact]
    public void Blank_or_malformed_answers_become_null_rather_than_failing()
    {
        var json = """[{ "submissionGUID": "g", "application": { "uniqueApplicationNumber": "1" }, "person": [{ "questions": [ { "title": "Date of birth", "answer": "not a date" }, { "title": "Given name", "answer": "" } ] }] }]""";
        var person = VisaApplicationParser.Parse(json)[0].Application!.Lead!;
        Assert.Null(person.DateOfBirth);
        Assert.Null(person.GivenName);
    }
}
