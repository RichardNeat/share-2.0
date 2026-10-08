namespace Share.Web.Models;

public enum TimelineEventKind
{
    ApplicationReceived,
    CaseFormed,
    CheckUpdated,
    CaseStatusChanged,
    GuestsArrived,
    VisaStatusChanged,
    OfferReceived,
    CaseRematched,
    DuplicateMarked,
}

public static class TimelineOrder
{
    // Events that share a timestamp are shown in the order they logically happen.
    public static int Rank(TimelineEventKind kind) => kind switch
    {
        TimelineEventKind.ApplicationReceived => 0,
        TimelineEventKind.CaseFormed => 1,
        TimelineEventKind.VisaStatusChanged => 2,
        TimelineEventKind.GuestsArrived => 3,
        TimelineEventKind.CheckUpdated => 4,
        TimelineEventKind.CaseStatusChanged => 6,
        TimelineEventKind.CaseRematched => 5,
        TimelineEventKind.DuplicateMarked => 4,
        TimelineEventKind.OfferReceived => 0,
        _ => 6,
    };

    public static IEnumerable<TimelineEvent> NewestFirst(this IEnumerable<TimelineEvent> events) =>
        events.OrderByDescending(e => e.OccurredAt).ThenByDescending(e => Rank(e.Kind)).ThenByDescending(e => e.Id);

    public static IEnumerable<TimelineEvent> OldestFirst(this IEnumerable<TimelineEvent> events) =>
        events.OrderBy(e => e.OccurredAt).ThenBy(e => Rank(e.Kind)).ThenBy(e => e.Id);
}

// One log of meaningful changes. Feeds the ingest summary now and the case timeline (M5).
// Data events take their timestamp from the data, never the ingest time.
public class TimelineEvent
{
    public int Id { get; set; }
    public DateTime OccurredAt { get; set; }
    public TimelineEventKind Kind { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public int? VisaApplicationId { get; set; }
    public VisaApplication? VisaApplication { get; set; }
    public int? CaseId { get; set; }
    public Case? Case { get; set; }
    public int? OfferId { get; set; }
    public Offer? Offer { get; set; }
    public int? IngestRunId { get; set; }
}
