namespace Share.Web.Users;

// Someone you can "sign in as" from the demo controls. No real authentication (AGENTS.md).
// A council user sees only records in their council; the central admin (no council) sees everything.
public record DemoUser(string Key, string Name, string? Council)
{
    public bool IsAdmin => Council is null;

    public string Sees => IsAdmin ? "Sees every council's records" : $"Sees {Display.Council(Council)} records only";

    // Council values match the data exactly ("Local authority of UK address").
    public static readonly IReadOnlyList<DemoUser> All =
    [
        new("admin", "Central admin", null),
        new("birmingham", "Birmingham council user", "BIRMINGHAM"),
        new("exeter", "Exeter council user", "EXETER"),
    ];

    public static DemoUser Default => All[0];

    public static DemoUser? Find(string? key) => All.FirstOrDefault(u => u.Key == key);
}
