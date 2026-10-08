using Share.Web.Ingest;
using Share.Web.Models;
using Share.Web.Safeguarding;

namespace Share.Tests.Unit;

public class OfferParserTests
{
    const string Json = """
    [{
      "submission_reference": "4G8NR92T",
      "submitted_at": "2026-04-12T08:01:00.000Z",
      "answers": [
        { "question_text": "Full name", "answer_text": "Grace Bennett" },
        { "question_text": "Email address", "answer_text": "grace.bennett@example.com" },
        { "question_text": "Address of the accommodation you are offering", "address1": "3 Chapel Yard", "address2": "Digbeth", "town_or_city": "Birmingham", "postcode": "B9 4AA" },
        { "question_text": "When is the accommodation available from?", "answer_text": "01/04/2026" },
        { "question_text": "How many adults could the accommodation sleep?", "answer_text": "2" },
        { "question_text": "How many children could the accommodation sleep?", "answer_text": "2" },
        { "question_text": "Does the accommodation have step-free access?", "answer_text": "Yes" },
        { "question_text": "Would you accept a household with pets?", "answer_text": "No" }
      ]
    }, { "answers": [] }]
    """;

    [Fact]
    public void Reads_an_offer_and_takes_the_council_from_the_town()
    {
        var o = OfferParser.Parse(Json)[0].Offer!;
        Assert.Equal("4G8NR92T", o.SubmissionReference);
        Assert.Equal("Grace Bennett", o.HostName);
        Assert.Equal("3 Chapel Yard, Digbeth, Birmingham", o.Address);
        Assert.Equal("BIRMINGHAM", o.Council);
        Assert.Equal(new DateOnly(2026, 4, 1), o.AvailableFrom);
        Assert.Equal((2, 2), (o.Adults, o.Children));
        Assert.Equal((true, false), (o.StepFree, o.Pets));
        Assert.Equal(OfferStatus.Open, o.Status);
    }

    [Fact]
    public void An_offer_without_a_reference_is_skipped_with_a_reason() =>
        Assert.Equal("No submission reference", OfferParser.Parse(Json)[1].SkipReason);
}

public class RematchRulesTests
{
    static readonly DateOnly Today = new(2026, 10, 1);

    static Offer Offer(int id, string council, int adults, int children, string from = "2026-04-01") => new()
    {
        Id = id, SubmissionReference = $"R{id}", Council = council, Adults = adults, Children = children,
        AvailableFrom = DateOnly.Parse(from), Address1 = $"{id} Road",
    };

    // One adult and two children in Exeter, like the Melnyk household.
    static Case Melnyk() => new()
    {
        MatchKey = "k", Council = "EXETER",
        Applications = [new VisaApplication { SubmissionGuid = "g", Uan = "U", Guests = [
            new Guest { Position = 1, DateOfBirth = new(1989, 6, 17) },
            new Guest { Position = 2, DateOfBirth = new(2014, 1, 22) },
            new Guest { Position = 3, DateOfBirth = new(2016, 8, 30) },
        ] }],
    };

    [Fact]
    public void Household_counts_adults_and_children_on_the_day() =>
        Assert.Equal((1, 2), RematchRules.Household(Melnyk().Guests, Today));

    [Theory]
    [InlineData(2, 1, true)]    // children can use an adult's bed
    [InlineData(1, 2, true)]
    [InlineData(1, 1, false)]   // not enough beds
    [InlineData(0, 4, false)]   // an adult cannot take a child's place
    public void Room_for_a_household(int offerAdults, int offerChildren, bool expected) =>
        Assert.Equal(expected, RematchRules.HasRoom(Offer(1, "EXETER", offerAdults, offerChildren), 1, 2));

    [Fact]
    public void Offers_rank_by_room_then_council_then_availability_then_tightest_fit()
    {
        var ranked = RematchRules.Rank(Melnyk(), [
            Offer(1, "EXETER", 1, 1),                   // too small
            Offer(2, "BIRMINGHAM", 2, 2),               // room, other council
            Offer(3, "EXETER", 2, 2, "2026-12-01"),     // room, same council, not yet available
            Offer(4, "EXETER", 2, 2),                   // room, same council, 1 spare bed
            Offer(5, "EXETER", 2, 1),                   // room, same council, exact fit
        ], Today);
        Assert.Equal([5, 4, 3, 2, 1], ranked.Select(f => f.Offer.Id));
        Assert.Contains("Too small for this household of 1 adult and 2 children (sleeps 1 adult and 1 child)", ranked[^1].Reasons);
        Assert.Contains("Available from 1 December 2026", ranked[2].Reasons);
    }

    [Fact]
    public void Taken_offers_are_never_suggested()
    {
        var taken = Offer(1, "EXETER", 2, 2);
        taken.Status = OfferStatus.Taken;
        Assert.Empty(RematchRules.Rank(Melnyk(), [taken], Today));
    }

    [Fact]
    public void A_failed_check_means_the_case_needs_a_rematch()
    {
        var c = Melnyk();
        Assert.False(RematchRules.NeedsRematch(SafeguardingRules.Assess(c, Today)));
        c.Checks = [new CaseCheck { Kind = CheckKind.AccommodationSuitable, Status = CheckStatus.Failed, FailureReason = "Damp" }];
        Assert.True(RematchRules.NeedsRematch(SafeguardingRules.Assess(c, Today)));
    }
}
