using Share.Web.Models;

namespace Share.Web.Ingest;

// Visa status rules from the brief (DATA.md).
public static class VisaStatusRules
{
    // One update on its own.
    public static VisaStatus FromUpdate(string? decision, DateTime? arrivedAt)
    {
        switch (decision?.Trim().ToUpperInvariant())
        {
            case "ISSUED":
            case "GRANT":
                return arrivedAt is null ? VisaStatus.Issued : VisaStatus.Arrived;
            case "WITHDRAWN": return VisaStatus.Withdrawn;
            case "REFUSED": return VisaStatus.Refused;
            case "VOIDED": return VisaStatus.Confirmed;
            default: return VisaStatus.Confirmed; // anything else counts as Confirmed
        }
    }

    static int Rank(VisaStatus s) => s switch
    {
        VisaStatus.Arrived => 5,
        VisaStatus.Issued => 4,
        VisaStatus.Withdrawn => 3,
        VisaStatus.Refused => 2,
        VisaStatus.Confirmed => 1,
        _ => 0,
    };

    // All updates for an application: never updated = Pending; on conflict Arrived > Issued > Withdrawn > Refused > Confirmed.
    public static VisaStatus Derive(IEnumerable<VisaStatus> updates) =>
        updates.DefaultIfEmpty(VisaStatus.Pending).MaxBy(Rank);
}
