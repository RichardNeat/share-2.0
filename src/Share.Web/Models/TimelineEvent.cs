namespace Share.Web.Models;

public enum TimelineEventKind
{
    ApplicationReceived,
    CaseFormed,
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
    public int? IngestRunId { get; set; }
}
