namespace Share.Web.Models;

// Precedence on conflict (DATA.md): Arrived > Issued > Withdrawn > Refused > Confirmed.
public enum VisaStatus
{
    Pending,
    Confirmed,
    Refused,
    Withdrawn,
    Issued,
    Arrived,
}
