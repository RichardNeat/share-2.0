using Microsoft.EntityFrameworkCore;
using Share.Web.Data;
using Share.Web.Models;

namespace Share.Web.Matching;

public class DuplicateService(AppDbContext db, TimeProvider clock)
{
    // Guests the signed-in user can see, so a council reviews only its own pairs.
    async Task<List<Guest>> GuestsAsync() =>
        await db.Guests.Include(g => g.VisaApplication).ThenInclude(a => a!.Case).ToListAsync();

    public async Task<IReadOnlyList<DuplicateSuggestion>> SuggestionsAsync()
    {
        var dismissed = (await db.DuplicateDismissals.Select(d => d.PairKey).ToListAsync()).ToHashSet();
        return DuplicateFinder.Find(await GuestsAsync(), dismissed);
    }

    public async Task<List<Guest>> MarkedAsync() =>
        await db.Guests.Where(g => g.DuplicateOfGuestId != null)
            .Include(g => g.VisaApplication).Include(g => g.DuplicateOf).ThenInclude(k => k!.VisaApplication)
            .OrderBy(g => g.MarkedDuplicateAt).ThenBy(g => g.Id).ToListAsync();

    public async Task<DuplicateSuggestion?> FindAsync(string pairKey) =>
        (await SuggestionsAsync()).FirstOrDefault(s => s.PairKey == pairKey);

    public async Task DismissAsync(DuplicateSuggestion s)
    {
        db.DuplicateDismissals.Add(new DuplicateDismissal { PairKey = s.PairKey, DismissedAt = clock.GetUtcNow().UtcDateTime });
        await db.SaveChangesAsync();
    }

    // Marks one guest as the duplicate of the record the reviewer chose to keep.
    public async Task MarkDuplicateAsync(DuplicateSuggestion s, Guest keep)
    {
        var duplicate = ReferenceEquals(keep, s.A) ? s.B : s.A;
        var now = clock.GetUtcNow().UtcDateTime;
        duplicate.DuplicateOfGuestId = keep.Id;
        duplicate.MarkedDuplicateAt = now;
        var text = $"{duplicate.FullName} ({duplicate.VisaApplication!.Uan}) marked as a duplicate of {keep.FullName} ({keep.VisaApplication!.Uan})";
        foreach (var caseId in new[] { duplicate.VisaApplication.CaseId, keep.VisaApplication.CaseId }.Where(id => id != null).Distinct())
            db.TimelineEvents.Add(new TimelineEvent { Kind = TimelineEventKind.DuplicateMarked, OccurredAt = now, Title = "Duplicate marked", Description = text, CaseId = caseId });
        await db.SaveChangesAsync();
    }
}
