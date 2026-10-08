using Share.Web.Ingest;

namespace Share.Tests.Unit;

public class RecordKeysTests
{
    [Theory]
    [InlineData("12 Copper Hill Lane, Exeter", "12 copper hill lane exeter")]
    [InlineData("  12  Copper Hill Lane ,Exeter ", "12 copper hill lane exeter")]
    [InlineData(null, "")]
    public void Normalise_ignores_case_punctuation_and_spacing(string? input, string expected) =>
        Assert.Equal(expected, RecordKeys.Normalise(input));

    [Fact]
    public void Same_email_is_the_same_sponsor_whatever_the_case_or_spacing() =>
        Assert.Equal(RecordKeys.Sponsor("clare.osborne@example.com", "A"), RecordKeys.Sponsor(" Clare.Osborne@Example.com ", "B"));

    [Fact]
    public void Different_emails_are_different_sponsors() =>
        Assert.NotEqual(RecordKeys.Sponsor("clare@example.com", "A"), RecordKeys.Sponsor("clare.o@example.com", "A"));

    [Fact]
    public void Sponsor_key_ignores_name_and_date_of_birth_entirely()
    {
        // The key is built from email alone, so two people sharing a name and date of birth stay apart.
        Assert.NotEqual(RecordKeys.Sponsor("one@example.com", "A"), RecordKeys.Sponsor("two@example.com", "A"));
    }

    [Fact]
    public void Sponsor_without_an_email_is_never_merged_across_applications() =>
        Assert.NotEqual(RecordKeys.Sponsor(null, "1313-0001"), RecordKeys.Sponsor(" ", "1313-0002"));

    [Fact]
    public void Named_hosts_are_never_merged_across_applications()
    {
        Assert.NotEqual(RecordKeys.Host("Daniel", "Park", "1313-0001"), RecordKeys.Host("Daniel", "Park", "1313-0002"));
        Assert.Null(RecordKeys.Host("", null, "1313-0001"));
    }

    [Fact]
    public void A_named_host_never_merges_into_a_sponsor_record() =>
        Assert.NotEqual(RecordKeys.Host("Clare", "Osborne", "1313-0001"), RecordKeys.Sponsor(null, "1313-0001"));

    [Fact]
    public void Accommodation_is_identified_by_address_and_council()
    {
        Assert.Equal(RecordKeys.Accommodation("12 Copper Hill Lane, Exeter", "EXETER"), RecordKeys.Accommodation("12 copper hill lane exeter", "Exeter"));
        Assert.NotEqual(RecordKeys.Accommodation("1 High Street", "EXETER"), RecordKeys.Accommodation("1 High Street", "BIRMINGHAM"));
        Assert.Null(RecordKeys.Accommodation(" ", "EXETER"));
    }
}
