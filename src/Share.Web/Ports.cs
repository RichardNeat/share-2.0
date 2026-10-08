namespace Share.Web;

// Readable names for the arrival ports in the feed; unknown codes are shown as they are.
public static class Ports
{
    static readonly Dictionary<string, string> Names = new(StringComparer.OrdinalIgnoreCase)
    {
        ["LHR"] = "Heathrow", ["LGW"] = "Gatwick", ["LTN"] = "Luton", ["STN"] = "Stansted", ["LCY"] = "London City",
        ["MAN"] = "Manchester", ["BHX"] = "Birmingham", ["BRS"] = "Bristol", ["EXT"] = "Exeter", ["EDI"] = "Edinburgh",
        ["GLA"] = "Glasgow", ["NCL"] = "Newcastle", ["LPL"] = "Liverpool", ["EMA"] = "East Midlands", ["LBA"] = "Leeds Bradford",
        ["DOV"] = "Dover", ["STP"] = "St Pancras",
    };

    public static string Name(string? code) =>
        string.IsNullOrWhiteSpace(code) ? "an unknown port" : Names.TryGetValue(code, out var n) ? $"{n} ({code.ToUpperInvariant()})" : code;
}
