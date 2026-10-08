namespace Share.Web;

// Pins the date when Clock:Today is set, so ages and the demo are deterministic.
// The time of day is real, so actions taken during a demo get sensible times on the timeline.
public class FixedDateTimeProvider(DateOnly today) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() =>
        new(today.ToDateTime(TimeOnly.FromDateTime(DateTime.UtcNow)), TimeSpan.Zero);
}
