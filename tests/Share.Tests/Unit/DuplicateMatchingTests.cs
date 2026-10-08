using Share.Web.Matching;
using Share.Web.Models;

namespace Share.Tests.Unit;

public class LevenshteinTests
{
    [Theory]
    [InlineData("", "", 0)]
    [InlineData("", "abc", 3)]
    [InlineData("abc", "", 3)]
    [InlineData("olena", "olena", 0)]
    [InlineData("kovalenko", "kovalenco", 1)]      // substitution
    [InlineData("oleksandr", "oleksander", 1)]    // insertion (transliteration)
    [InlineData("iryna", "irna", 1)]              // deletion
    [InlineData("kitten", "sitting", 3)]
    [InlineData("andriy", "andrii", 1)]
    public void Counts_single_character_edits(string a, string b, int expected)
    {
        Assert.Equal(expected, Levenshtein.Distance(a, b));
        Assert.Equal(expected, Levenshtein.Distance(b, a));   // symmetric
    }
}

public class NameNormaliserTests
{
    [Theory]
    [InlineData("  Olena   KOVALENKO ", "olena kovalenko")]
    [InlineData("Ol'ena", "olena")]
    [InlineData("Kovalenko-Bilyk", "kovalenko bilyk")]
    [InlineData("Zoë Müller", "zoe muller")]
    [InlineData(null, "")]
    public void Ignores_case_accents_apostrophes_hyphens_and_spacing(string? input, string expected) =>
        Assert.Equal(expected, NameNormaliser.Normalise(input));

    [Fact]
    public void Passports_ignore_spaces_and_case() =>
        Assert.Equal("FB201117", NameNormaliser.Passport(" fb 201 117 "));
}

public class DuplicateFinderTests
{
    static int _id;
    static Guest G(string given, string family, string? dob, string? passport, string uan, int position = 1, int? appId = null)
    {
        var id = ++_id;
        var app = new VisaApplication { Id = appId ?? id, SubmissionGuid = $"g{id}", Uan = uan, SubmittedAt = new DateTime(2026, 2, 1).AddDays(id) };
        var g = new Guest { Id = id, GivenName = given, FamilyName = family, DateOfBirth = dob is null ? null : DateOnly.Parse(dob), PassportNumber = passport, Position = position, VisaApplication = app, VisaApplicationId = app.Id };
        app.Guests.Add(g);
        return g;
    }

    [Fact]
    public void Same_passport_and_names_one_letter_apart_is_high_confidence()
    {
        var s = DuplicateFinder.Compare(G("Olena", "Kovalenko", "1991-03-12", "FB201117", "U1"), G("Olena", "Kovalenco", "1991-03-12", "FB201117", "U2"))!;
        Assert.Equal(DuplicateConfidence.High, s.Confidence);
        Assert.Equal(["Same passport number (FB201117)", "Names 1 letter apart (Olena Kovalenko and Olena Kovalenco), same date of birth"], s.Reasons);
    }

    [Fact]
    public void Same_name_and_birth_date_with_a_passport_typo_is_high_confidence()
    {
        var s = DuplicateFinder.Compare(G("Dmytro", "Shevchenko", "1987-09-29", "FB335290", "U1"), G("Dmytro", "Shevchenko", "1987-09-29", "FB335291", "U2"))!;
        Assert.Equal(DuplicateConfidence.High, s.Confidence);
        Assert.Contains("Passport numbers 1 character apart (FB335290 and FB335291)", s.Reasons);
        Assert.Contains("Same name and date of birth", s.Reasons);
    }

    [Fact]
    public void Transliterated_name_with_the_same_birth_date_is_medium_confidence() =>
        Assert.Equal(DuplicateConfidence.Medium, DuplicateFinder.Compare(G("Oleksandr", "Bondar", "1980-01-01", null, "U1"), G("Oleksander", "Bondar", "1980-01-01", null, "U2"))!.Confidence);

    [Fact]
    public void Similar_names_with_different_birth_dates_are_low_confidence() =>
        Assert.Equal(DuplicateConfidence.Low, DuplicateFinder.Compare(G("Iryna", "Bondarenko", "1994-12-04", null, "U1"), G("Irina", "Bondarenko", "1995-01-01", null, "U2"))!.Confidence);

    [Fact]
    public void Different_people_are_not_suggested()
    {
        Assert.Null(DuplicateFinder.Compare(G("Roman", "Polishchuk", "1993-02-17", "FB546633", "U1"), G("Oleksandra", "Dovzhenko", "1993-10-30", "FB786633", "U2")));
        Assert.Null(DuplicateFinder.Compare(G("Olena", "Kovalenko", "1991-03-12", null, "U1"), G("Olena", "Kovalenko", null, null, "U2")));
    }

    [Fact]
    public void Family_members_on_one_application_are_never_paired()
    {
        var a = G("Ivan", "Melnyk", "2015-05-05", null, "U1", 2, appId: 999);
        var b = G("Ivan", "Melnyk", "2015-05-05", null, "U1", 3, appId: 999);
        Assert.Empty(DuplicateFinder.Find([a, b], new HashSet<string>()));
    }

    [Fact]
    public void Dismissed_pairs_and_marked_duplicates_are_left_out_and_the_rest_ranked()
    {
        var k1 = G("Olena", "Kovalenko", "1991-03-12", "FB201117", "U1");
        var k2 = G("Olena", "Kovalenco", "1991-03-12", "FB201117", "U2");
        var s1 = G("Dmytro", "Shevchenko", "1987-09-29", "FB335290", "U3");
        var s2 = G("Dmytro", "Shevchenko", "1987-09-29", "FB335291", "U4");
        var low1 = G("Iryna", "Bondarenko", "1994-12-04", null, "U5");
        var low2 = G("Irina", "Bondarenko", "1995-01-01", null, "U6");

        var all = DuplicateFinder.Find([low2, s2, k1, low1, s1, k2], new HashSet<string>());
        Assert.Equal(["U1#1|U2#1", "U3#1|U4#1", "U5#1|U6#1"], all.Select(s => s.PairKey));   // 170, 150, then low

        var dismissed = new HashSet<string> { "U3#1|U4#1" };
        k2.DuplicateOfGuestId = k1.Id;
        Assert.Equal(["U5#1|U6#1"], DuplicateFinder.Find([low2, s2, k1, low1, s1, k2], dismissed).Select(s => s.PairKey));
    }
}
