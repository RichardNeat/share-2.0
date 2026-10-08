using Share.Web.Models;
using Share.Web.Safeguarding;
using static Share.Web.Models.CheckKind;
using static Share.Web.Models.CheckStatus;

namespace Share.Tests.Unit;

public class SafeguardingRulesTests
{
    static CaseStatus Derive(CheckStatus c1, CheckStatus c2, CheckStatus c3, CheckStatus c4) =>
        SafeguardingRules.Derive(new Dictionary<CheckKind, CheckStatus>
        {
            [AccommodationExists] = c1, [AccommodationSuitable] = c2, [DbsAndSponsorSuitable] = c3, [GuestsArrived] = c4,
        });

    [Fact]
    public void No_checks_started_means_checks_required() =>
        Assert.Equal(CaseStatus.ChecksRequired, Derive(NotStarted, NotStarted, NotStarted, NotStarted));

    [Fact]
    public void In_progress_alone_is_still_checks_required() =>
        Assert.Equal(CaseStatus.ChecksRequired, Derive(InProgress, InProgress, NotStarted, NotStarted));

    [Fact]
    public void No_longer_required_alone_is_not_a_pass() =>
        Assert.Equal(CaseStatus.ChecksRequired, Derive(NoLongerRequired, NotStarted, NotStarted, NotStarted));

    [Fact]
    public void One_pass_means_partially_completed() =>
        Assert.Equal(CaseStatus.ChecksPartiallyCompleted, Derive(Passed, NotStarted, InProgress, NotStarted));

    [Fact]
    public void Checks_one_to_three_done_means_pre_arrival_complete() =>
        Assert.Equal(CaseStatus.PreArrivalChecksComplete, Derive(Passed, NoLongerRequired, Passed, NotStarted));

    [Fact]
    public void All_four_done_means_completed() =>
        Assert.Equal(CaseStatus.ChecksCompleted, Derive(Passed, Passed, NoLongerRequired, Passed));

    [Theory]
    [InlineData(Failed, Passed, Passed, Passed)]
    [InlineData(Passed, Passed, Passed, Failed)]
    [InlineData(NotStarted, NotStarted, Failed, NotStarted)]
    public void Any_failure_wins_over_everything(CheckStatus c1, CheckStatus c2, CheckStatus c3, CheckStatus c4) =>
        Assert.Equal(CaseStatus.SomeChecksFailed, Derive(c1, c2, c3, c4));

    [Fact]
    public void Missing_checks_count_as_not_started() =>
        Assert.Equal(CaseStatus.ChecksRequired, SafeguardingRules.Derive(new Dictionary<CheckKind, CheckStatus>()));

    static readonly DateOnly Today = new(2026, 10, 1);

    [Fact]
    public void A_guest_under_18_makes_the_case_need_an_enhanced_dbs()
    {
        var guests = new[]
        {
            new Guest { GivenName = "Adult", DateOfBirth = new(1990, 1, 1) },
            new Guest { GivenName = "Seventeen", DateOfBirth = new(2008, 10, 2) },
            new Guest { GivenName = "Eighteen today", DateOfBirth = new(2008, 10, 1) },
            new Guest { GivenName = "Unknown" },
        };
        Assert.Equal(["Seventeen"], SafeguardingRules.Children(guests, Today).Select(g => g.GivenName));
    }

    [Fact]
    public void A_standard_dbs_does_not_complete_check_3_when_enhanced_is_required()
    {
        var (effective, note) = SafeguardingRules.Effective(DbsAndSponsorSuitable, Passed, DbsType.Standard, enhancedRequired: true);
        Assert.Equal(InProgress, effective);
        Assert.NotNull(note);
    }

    [Theory]
    [InlineData(DbsType.Enhanced, true)]
    [InlineData(DbsType.Standard, false)]
    public void Otherwise_a_passed_dbs_check_stands(DbsType dbs, bool enhancedRequired) =>
        Assert.Equal(Passed, SafeguardingRules.Effective(DbsAndSponsorSuitable, Passed, dbs, enhancedRequired).Effective);

    [Fact]
    public void The_dbs_rule_only_affects_check_3() =>
        Assert.Equal(Passed, SafeguardingRules.Effective(AccommodationExists, Passed, null, enhancedRequired: true).Effective);

    static Case CaseWith(params Guest[] guests) => new()
    {
        MatchKey = "k",
        Applications = [new VisaApplication { SubmissionGuid = "g", Uan = "U", Guests = [.. guests] }],
    };

    [Fact]
    public void Assess_derives_status_from_effective_check_statuses()
    {
        var c = CaseWith(new Guest { Position = 1, DateOfBirth = new(2016, 8, 30) });
        c.Checks = [
            new CaseCheck { Kind = AccommodationExists, Status = Passed },
            new CaseCheck { Kind = AccommodationSuitable, Status = Passed },
            new CaseCheck { Kind = DbsAndSponsorSuitable, Status = Passed, DbsType = DbsType.Standard },
        ];
        var a = SafeguardingRules.Assess(c, Today);
        Assert.True(a.EnhancedDbsRequired);
        // Standard DBS on a case with a child: checks 1-3 are not all complete.
        Assert.Equal(CaseStatus.ChecksPartiallyCompleted, a.Status);

        c.Checks[2].DbsType = DbsType.Enhanced;
        Assert.Equal(CaseStatus.PreArrivalChecksComplete, SafeguardingRules.Assess(c, Today).Status);
    }

    [Fact]
    public void Check_4_is_locked_until_a_guest_has_arrived()
    {
        var c = CaseWith(new Guest { Position = 1 });
        Assert.True(SafeguardingRules.Assess(c, Today).Checks.Single(x => x.Kind == GuestsArrived).Locked);
        c.Applications[0].Status = VisaStatus.Arrived;
        Assert.False(SafeguardingRules.Assess(c, Today).Checks.Single(x => x.Kind == GuestsArrived).Locked);
    }

    [Fact]
    public void Validation_requires_a_status() =>
        Assert.Equal("Status", Assert.Single(SafeguardingRules.Validate(AccommodationExists, null, null, null, false, false)).Field);

    [Fact]
    public void Validation_requires_a_reason_when_failed() =>
        Assert.Equal("FailureReason", Assert.Single(SafeguardingRules.Validate(AccommodationExists, Failed, " ", null, false, false)).Field);

    [Fact]
    public void Validation_requires_a_dbs_type_when_check_3_passes() =>
        Assert.Equal("DbsType", Assert.Single(SafeguardingRules.Validate(DbsAndSponsorSuitable, Passed, null, null, false, false)).Field);

    [Fact]
    public void Validation_rejects_a_standard_dbs_when_a_child_is_on_the_case()
    {
        var error = Assert.Single(SafeguardingRules.Validate(DbsAndSponsorSuitable, Passed, null, DbsType.Standard, true, false));
        Assert.Contains("Enhanced DBS", error.Message);
        Assert.Empty(SafeguardingRules.Validate(DbsAndSponsorSuitable, Passed, null, DbsType.Enhanced, true, false));
    }

    [Fact]
    public void Validation_refuses_a_locked_check() =>
        Assert.Single(SafeguardingRules.Validate(GuestsArrived, Passed, null, null, false, locked: true));
}
