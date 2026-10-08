using Share.Web.Ingest;

namespace Share.Tests.Unit;

public class HostParsingTests
{
    static Share.Web.Models.VisaApplication Parse(string staying, string hostGiven = "", string hostFamily = "") =>
        VisaApplicationParser.Parse($$"""
        [{ "submissionGUID": "g", "application": { "uniqueApplicationNumber": "1" }, "person": [{ "questions": [
          { "title": "Sponsor given name", "answer": "Clare" },
          { "title": "Sponsor family name", "answer": "Osborne" },
          { "title": "Sponsor email address", "answer": "clare.osborne@example.com" },
          { "title": "Will you be staying at your sponsor's address?", "answer": "{{staying}}" },
          { "title": "Host given name", "answer": "{{hostGiven}}" },
          { "title": "Host family name", "answer": "{{hostFamily}}" }
        ]}]}]
        """)[0].Application!;

    [Fact]
    public void Staying_at_the_sponsors_address_makes_the_sponsor_the_host()
    {
        var app = Parse("Yes");
        Assert.Same(app.Sponsor, app.Host);
    }

    [Fact]
    public void Staying_elsewhere_makes_the_named_host_a_different_person()
    {
        var app = Parse("No", "Daniel", "Park");
        Assert.NotSame(app.Sponsor, app.Host);
        Assert.Equal("Daniel Park", app.Host!.FullName);
        Assert.Equal("Clare Osborne", app.Sponsor!.FullName);
    }

    [Fact]
    public void Sponsor_is_keyed_on_their_email_address() =>
        Assert.Equal("email|clare.osborne@example.com", Parse("Yes").Sponsor!.MatchKey);

    [Fact]
    public void Unanswered_question_does_not_assume_the_sponsor_is_the_host() =>
        Assert.Null(Parse("").Host);
}
