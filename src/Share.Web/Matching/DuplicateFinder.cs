using Share.Web.Models;

namespace Share.Web.Matching;

public enum DuplicateConfidence
{
    Low,
    Medium,
    High,
}

public record DuplicateSuggestion(Guest A, Guest B, DuplicateConfidence Confidence, int Score, IReadOnlyList<string> Reasons, string PairKey);

// Finds pairs of guests who are likely the same person. Pure: give it guests (with their applications)
// and the pairs already dismissed, and it returns suggestions ranked by confidence.
public static class DuplicateFinder
{
    // Stable across reseeds of the same files: the application's UAN and the person's position on it.
    public static string GuestKey(Guest g) => $"{g.VisaApplication!.Uan}#{g.Position}";

    public static string PairKey(Guest a, Guest b) =>
        string.Join("|", new[] { GuestKey(a), GuestKey(b) }.Order(StringComparer.Ordinal));

    public static IReadOnlyList<DuplicateSuggestion> Find(IEnumerable<Guest> guests, ISet<string> dismissed)
    {
        var list = guests.Where(g => g.DuplicateOfGuestId is null && g.VisaApplication is not null)
            .OrderBy(g => g.VisaApplication!.SubmittedAt).ThenBy(g => g.Id).ToList();
        var results = new List<DuplicateSuggestion>();
        for (var i = 0; i < list.Count; i++)
        for (var j = i + 1; j < list.Count; j++)
        {
            var (a, b) = (list[i], list[j]);
            if (a.VisaApplicationId == b.VisaApplicationId) continue;   // family members on one form
            var key = PairKey(a, b);
            if (dismissed.Contains(key)) continue;
            if (Compare(a, b) is { } s) results.Add(s with { PairKey = key });
        }
        return results.OrderByDescending(r => r.Confidence).ThenByDescending(r => r.Score).ThenBy(r => r.PairKey, StringComparer.Ordinal).ToList();
    }

    public static DuplicateSuggestion? Compare(Guest a, Guest b)
    {
        var reasons = new List<(DuplicateConfidence Confidence, int Score, string Text)>();
        var nameA = NameNormaliser.Normalise($"{a.GivenName} {a.FamilyName}");
        var nameB = NameNormaliser.Normalise($"{b.GivenName} {b.FamilyName}");
        var passA = NameNormaliser.Passport(a.PassportNumber);
        var passB = NameNormaliser.Passport(b.PassportNumber);
        var sameDob = a.DateOfBirth is not null && a.DateOfBirth == b.DateOfBirth;
        var nameDistance = nameA.Length > 0 && nameB.Length > 0 ? Levenshtein.Distance(nameA, nameB) : int.MaxValue;

        if (passA.Length > 0 && passA == passB)
            reasons.Add((DuplicateConfidence.High, 100, $"Same passport number ({passA})"));
        else if (passA.Length >= 6 && passB.Length >= 6 && Levenshtein.Distance(passA, passB) == 1)
            reasons.Add((DuplicateConfidence.Medium, 60, $"Passport numbers 1 character apart ({passA} and {passB})"));

        if (nameDistance == 0 && sameDob)
            reasons.Add((DuplicateConfidence.High, 90, "Same name and date of birth"));
        else if (nameDistance is 1 or 2 && sameDob)
            reasons.Add((DuplicateConfidence.Medium, 70, $"Names {nameDistance} letter{(nameDistance == 1 ? "" : "s")} apart ({a.FullName} and {b.FullName}), same date of birth"));
        else if (!sameDob && a.DateOfBirth is not null && b.DateOfBirth is not null
                 && NameNormaliser.Normalise(a.FamilyName) is { Length: > 0 } fa && fa == NameNormaliser.Normalise(b.FamilyName)
                 && Levenshtein.Distance(NameNormaliser.Normalise(a.GivenName), NameNormaliser.Normalise(b.GivenName)) <= 1)
            reasons.Add((DuplicateConfidence.Low, 30, "Same family name and given names at most 1 letter apart, but different dates of birth"));

        if (reasons.Count == 0) return null;
        return new DuplicateSuggestion(a, b, reasons.Max(r => r.Confidence), reasons.Sum(r => r.Score), reasons.Select(r => r.Text).ToList(), "");
    }
}
