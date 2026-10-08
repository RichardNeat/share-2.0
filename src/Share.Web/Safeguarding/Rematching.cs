using Microsoft.EntityFrameworkCore;
using Share.Web.Data;
using Share.Web.Ingest;
using Share.Web.Models;

namespace Share.Web.Safeguarding;

public record OfferFit(Offer Offer, bool SameCouncil, bool HasRoom, bool AvailableNow, IReadOnlyList<string> Reasons);

// Rematch: move a household whose sponsorship has broken down onto an open offer of accommodation.
public static class RematchRules
{
    // A case needs a rematch when any of its safeguarding checks has failed.
    public static bool NeedsRematch(CaseAssessment assessment) =>
        assessment.Checks.Any(c => c.Effective == CheckStatus.Failed);

    public static (int Adults, int Children) Household(IEnumerable<Guest> guests, DateOnly today)
    {
        var list = guests.ToList();
        var children = list.Count(g => g.DateOfBirth is { } d && Ages.On(d, today) < 18);
        return (list.Count - children, children);
    }

    // Children can sleep in adults' beds; adults cannot take children's places.
    public static bool HasRoom(Offer offer, int adults, int children) =>
        offer.Adults is { } a && adults <= a && adults + children <= a + (offer.Children ?? 0);

    static string People(int adults, int children) =>
        string.Join(" and ", new[]
        {
            adults > 0 ? $"{adults} adult{(adults == 1 ? "" : "s")}" : null,
            children > 0 ? $"{children} child{(children == 1 ? "" : "ren")}" : null,
        }.Where(x => x != null));

    // Ranks open offers: room for the household first, then same council, then available now,
    // then the tightest fit (fewest spare beds), then soonest available.
    public static IReadOnlyList<OfferFit> Rank(Case c, IEnumerable<Offer> offers, DateOnly today)
    {
        var (adults, children) = Household(c.Guests, today);
        return offers.Where(o => o.Status == OfferStatus.Open).Select(o =>
        {
            var sameCouncil = string.Equals(o.Council, c.Council, StringComparison.OrdinalIgnoreCase);
            var room = HasRoom(o, adults, children);
            var now = o.AvailableFrom is null || o.AvailableFrom <= today;
            var reasons = new List<string>
            {
                room ? $"Room for this household of {People(adults, children)}"
                     : $"Too small for this household of {People(adults, children)} (sleeps {People(o.Adults ?? 0, o.Children ?? 0)})",
                sameCouncil ? $"Same council ({Display.Council(o.Council)})" : $"Different council: {Display.Council(o.Council)}",
                now ? "Available now" : $"Available from {Display.Date(o.AvailableFrom)}",
            };
            return new OfferFit(o, sameCouncil, room, now, reasons);
        })
        .OrderByDescending(f => f.HasRoom).ThenByDescending(f => f.SameCouncil).ThenByDescending(f => f.AvailableNow)
        .ThenBy(f => (f.Offer.Adults ?? 0) + (f.Offer.Children ?? 0) - (adults + children))
        .ThenBy(f => f.Offer.AvailableFrom).ThenBy(f => f.Offer.Id)
        .ToList();
    }
}

public class RematchService(AppDbContext db, TimeProvider clock)
{
    // Moves the case to the offer: new accommodation and host, every check reset, the offer taken.
    public async Task RematchAsync(Case c, Offer offer)
    {
        if (offer.Status != OfferStatus.Open) throw new InvalidOperationException("That offer has already been taken");
        var now = clock.GetUtcNow().UtcDateTime;
        var today = Ages.Today(clock);
        var before = SafeguardingRules.Assess(c, today);
        var oldAddress = c.Accommodation?.Address ?? "no recorded address";

        // Reuse the accommodation and person if we already hold them (same identifiers as ingest).
        var address = string.Join(", ", new[] { offer.Address1, offer.Address2, offer.TownOrCity }.Where(x => !string.IsNullOrWhiteSpace(x)));
        var accommodationKey = RecordKeys.Accommodation(address, offer.Council)!;
        var accommodation = await db.Accommodations.IgnoreQueryFilters().FirstOrDefaultAsync(a => a.MatchKey == accommodationKey)
            ?? new Accommodation { MatchKey = accommodationKey, Address = address, Postcode = offer.Postcode, Council = offer.Council };
        var hostKey = RecordKeys.Sponsor(offer.Email, $"offer-{offer.SubmissionReference}");
        var parts = (offer.HostName ?? "").Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        var host = await db.People.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.MatchKey == hostKey)
            ?? new Person
            {
                MatchKey = hostKey,
                GivenName = parts.ElementAtOrDefault(0),
                FamilyName = parts.ElementAtOrDefault(1),
                Email = offer.Email,
                Telephone = offer.Telephone,
                Address = address,
                Postcode = offer.Postcode,
                Council = offer.Council,
            };

        c.Accommodation = accommodation;
        c.Host = host;
        c.Council = offer.Council ?? c.Council;
        c.RematchedAt = now;
        // New address, new person: check everything again.
        db.RemoveRange(c.Checks);
        c.Checks.Clear();
        c.ArrivalRecordedOn = null;
        c.ArrivalRecordedAt = null;
        offer.Status = OfferStatus.Taken;
        offer.TakenByCase = c;
        offer.TakenAt = now;

        db.TimelineEvents.Add(new TimelineEvent
        {
            Kind = TimelineEventKind.CaseRematched,
            OccurredAt = now,
            Title = "Case rematched",
            Description = $"Moved from {oldAddress} to {address}. New host {host.FullName}. All safeguarding checks reset to not started",
            CaseId = c.Id,
            Offer = offer,
        });
        var after = SafeguardingRules.Assess(c, today);
        if (after.Status != before.Status)
        {
            db.TimelineEvents.Add(new TimelineEvent
            {
                Kind = TimelineEventKind.CaseStatusChanged,
                OccurredAt = now,
                Title = "Case status changed",
                Description = $"{Display.CaseStatusTag(before.Status).Text} to {Display.CaseStatusTag(after.Status).Text}",
                CaseId = c.Id,
            });
        }
        await db.SaveChangesAsync();
    }
}
