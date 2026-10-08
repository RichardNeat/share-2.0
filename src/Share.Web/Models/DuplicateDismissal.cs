namespace Share.Web.Models;

// A reviewer said this pair is not a duplicate. Remembered so it never resurfaces.
public class DuplicateDismissal
{
    public int Id { get; set; }
    public required string PairKey { get; set; }
    public DateTime DismissedAt { get; set; }
}
