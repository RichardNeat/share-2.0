using System.Text;
using System.Text.RegularExpressions;

namespace Share.Web.Ingest;

// How records are recognised as the same across applications.
public static partial class RecordKeys
{
    // Lowercase, letters and digits only, single spaces: "12 Copper Hill Lane, Exeter" -> "12 copper hill lane exeter"
    public static string Normalise(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "";
        var sb = new StringBuilder();
        foreach (var c in value.Trim().ToLowerInvariant())
            sb.Append(char.IsLetterOrDigit(c) ? c : ' ');
        return Spaces().Replace(sb.ToString(), " ").Trim();
    }

    // People are only matched on a unique identifier, never on name or date of birth:
    // two different people can share both.

    // A sponsor is identified by their email address. Without one, the sponsor is never merged
    // with anyone: the record is keyed to the application that names them.
    public static string Sponsor(string? email, string applicationUan) =>
        string.IsNullOrWhiteSpace(email) ? $"sponsor-on|{applicationUan}" : $"email|{email.Trim().ToLowerInvariant()}";

    // A host named on an application (not the sponsor) comes with a name only, so is never merged:
    // one record per application. Spotting the same host twice is for duplicate suggestions (M14).
    public static string? Host(string? givenName, string? familyName, string applicationUan) =>
        string.IsNullOrWhiteSpace($"{givenName}{familyName}") ? null : $"host-on|{applicationUan}";

    // A case groups applications with the same sponsor and the same accommodation.
    // If either is missing we cannot know who else belongs, so the application gets a case of its own.
    public static string Case(string? sponsorKey, string? accommodationKey, string applicationUan) =>
        sponsorKey is null || accommodationKey is null ? $"application|{applicationUan}" : $"{sponsorKey}||{accommodationKey}";

    // An accommodation is identified by its address and council.
    public static string? Accommodation(string? address, string? council)
    {
        var a = Normalise(address);
        return a.Length == 0 ? null : $"{a}|{Normalise(council)}";
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex Spaces();
}
