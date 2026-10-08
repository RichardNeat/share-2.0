using Share.Web;
using Share.Web.Ingest;
using Share.Web.Models;

namespace Share.Tests.Unit;

public class CaseFormationTests
{
    [Fact]
    public void Same_sponsor_and_same_accommodation_is_the_same_case() =>
        Assert.Equal(RecordKeys.Case("email|a@x", "1 high st|exeter", "UAN-1"), RecordKeys.Case("email|a@x", "1 high st|exeter", "UAN-2"));

    [Fact]
    public void Same_sponsor_at_a_different_address_is_a_different_case() =>
        Assert.NotEqual(RecordKeys.Case("email|a@x", "1 high st|exeter", "UAN-1"), RecordKeys.Case("email|a@x", "2 high st|exeter", "UAN-2"));

    [Fact]
    public void Same_address_with_a_different_sponsor_is_a_different_case() =>
        Assert.NotEqual(RecordKeys.Case("email|a@x", "1 high st|exeter", "UAN-1"), RecordKeys.Case("email|b@x", "1 high st|exeter", "UAN-2"));

    [Theory]
    [InlineData(null, "1 high st|exeter")]
    [InlineData("email|a@x", null)]
    public void Missing_sponsor_or_accommodation_gives_the_application_its_own_case(string? sponsor, string? accommodation) =>
        Assert.NotEqual(RecordKeys.Case(sponsor, accommodation, "UAN-1"), RecordKeys.Case(sponsor, accommodation, "UAN-2"));

    [Theory]
    [InlineData("2016-08-30", "2026-10-01", 10)]
    [InlineData("1958-10-11", "2026-10-01", 67)]   // birthday later in the year
    [InlineData("1958-10-11", "2026-10-11", 68)]   // birthday counts on the day
    [InlineData("2008-02-29", "2026-02-28", 17)]   // leap-day birthday not yet reached
    [InlineData("2008-02-29", "2026-03-01", 18)]
    public void Age_is_whole_years_on_the_day(string dob, string today, int expected) =>
        Assert.Equal(expected, Ages.On(DateOnly.Parse(dob), DateOnly.Parse(today)));

    static VisaApplication App(int id, string family, int minute) => new()
    {
        Id = id, SubmissionGuid = $"g{id}", Uan = $"U{id}", SubmittedAt = new DateTime(2026, 2, 9, 9, minute, 0),
        Guests = [new Guest { Position = 1, FamilyName = family }],
    };

    [Fact]
    public void Title_names_the_household_and_its_reference_is_stable()
    {
        var c = new Case { Id = 3, MatchKey = "k", Applications = [App(1, "Melnyk", 0)] };
        Assert.Equal("Melnyk household", c.Title);
        Assert.Equal("CASE-0003", c.Reference);
    }

    [Fact]
    public void Title_names_every_family_in_submission_order()
    {
        var c = new Case { MatchKey = "k", Applications = [App(7, "Lysenko", 30), App(1, "Kovalenko", 0)] };
        Assert.Equal("Kovalenko and Lysenko households", c.Title);
    }
}
