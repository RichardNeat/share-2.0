using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Share.Web.Matching;

public static partial class NameNormaliser
{
    // "  Ol'ena  KOVALENKO-Bilyk " -> "olena kovalenko bilyk": lower case, no accents, no apostrophes,
    // hyphens as spaces, single spaces. So only real spelling differences count towards the distance.
    public static string Normalise(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "";
        var decomposed = name.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var c in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark) continue;
            if (c is '\'' or '’' or '`') continue;
            sb.Append(c is '-' or '.' ? ' ' : c);
        }
        return Spaces().Replace(sb.ToString().Normalize(NormalizationForm.FormC), " ").Trim();
    }

    public static string Passport(string? passport) =>
        string.IsNullOrWhiteSpace(passport) ? "" : Spaces().Replace(passport, "").ToUpperInvariant();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Spaces();
}
