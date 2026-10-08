namespace Share.Web;

// Pins "today" when Clock:Today is set, so ages and the demo are deterministic.
public class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}
