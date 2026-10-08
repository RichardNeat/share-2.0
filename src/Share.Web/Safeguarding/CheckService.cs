using Microsoft.EntityFrameworkCore;
using Share.Web.Data;
using Share.Web.Models;

namespace Share.Web.Safeguarding;

public class CheckService(AppDbContext db, TimeProvider clock)
{
    public Task<Case?> LoadCaseAsync(int id) =>
        db.Cases
            .Include(c => c.Sponsor)
            .Include(c => c.Accommodation)
            .Include(c => c.Checks)
            .Include(c => c.Applications).ThenInclude(a => a.Guests)
            .Include(c => c.Applications).ThenInclude(a => a.Host)
            .Include(c => c.Applications).ThenInclude(a => a.DecisionUpdates)
            .AsSplitQuery()
            .FirstOrDefaultAsync(c => c.Id == id);

    // Records that the guests have arrived, which unlocks check 4.
    public async Task<string?> RecordArrivalAsync(Case c, string? day, string? month, string? year)
    {
        var (date, error) = SafeguardingRules.ValidateArrivalDate(day, month, year, Ages.Today(clock));
        if (error is not null) return error;
        var now = clock.GetUtcNow().UtcDateTime;
        var changed = c.ArrivalRecordedOn is not null;
        c.ArrivalRecordedOn = date;
        c.ArrivalRecordedAt = now;
        db.TimelineEvents.Add(new TimelineEvent
        {
            Kind = TimelineEventKind.GuestsArrived,
            OccurredAt = now,
            Title = changed ? "Arrival date changed" : "Guests arrived",
            Description = $"A caseworker recorded that the guests arrived at the accommodation on {Display.Date(date)}",
            CaseId = c.Id,
        });
        await db.SaveChangesAsync();
        return null;
    }

    // Records a check update and logs it (and any change of case status) for the timeline.
    public async Task<List<SafeguardingRules.ValidationError>> UpdateAsync(Case c, CheckKind kind, CheckStatus? status, string? reason, DbsType? dbs)
    {
        var today = Ages.Today(clock);
        var before = SafeguardingRules.Assess(c, today);
        var state = before.Checks.Single(x => x.Kind == kind);
        var errors = SafeguardingRules.Validate(kind, status, reason, dbs, before.EnhancedDbsRequired, state.Locked);
        if (errors.Count > 0) return errors;

        var row = c.Checks.FirstOrDefault(x => x.Kind == kind);
        if (row is null)
        {
            row = new CaseCheck { CaseId = c.Id, Kind = kind };
            c.Checks.Add(row);
        }
        var now = clock.GetUtcNow().UtcDateTime;
        row.Status = status!.Value;
        row.FailureReason = status == CheckStatus.Failed ? reason!.Trim() : null;
        row.DbsType = kind == CheckKind.DbsAndSponsorSuitable ? dbs : null;
        row.UpdatedAt = now;

        var description = $"{Display.CheckName(kind)}: {Display.CheckStatusTag(row.Status).Text}"
            + (row.DbsType is { } d ? $" ({Display.DbsTypeName(d)})" : "")
            + (row.FailureReason is { } r ? $". Reason: {r}" : "");
        db.TimelineEvents.Add(new TimelineEvent { Kind = TimelineEventKind.CheckUpdated, OccurredAt = now, Title = "Check updated", Description = description, CaseId = c.Id });

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
        return [];
    }
}
