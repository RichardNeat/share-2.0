using Share.Web.Data;
using Share.Web.Models;
using System.Text;

namespace Share.Web.Services;

public class DownloadService
{
    private readonly AppDbContext _db;

    public DownloadService(AppDbContext db)
    {
        _db = db;
    }

    public (string filename, string csv) ExportGuests(string? userCouncil = null)
    {
        var sb = new StringBuilder();
        sb.AppendLine("GWF,Given Name,Family Name,Date of Birth,Nationality,Passport,Application UAN");

        var guests = _db.Guests.AsQueryable();
        if (!string.IsNullOrEmpty(userCouncil))
        {
            guests = guests.Where(g => g.VisaApplication!.Accommodation!.Council == userCouncil ||
                                       g.VisaApplication!.Council == userCouncil);
        }

        foreach (var g in guests.OrderBy(g => g.Id))
        {
            sb.AppendLine($"\"{g.Gwf}\",\"{g.GivenName}\",\"{g.FamilyName}\",\"{g.DateOfBirth:yyyy-MM-dd}\",\"{g.Nationality}\",\"{g.PassportNumber}\",\"{g.VisaApplication?.Uan}\"");
        }

        return ($"guests-{DateTime.Now:yyyyMMdd-HHmmss}.csv", sb.ToString());
    }

    public (string filename, string csv) ExportPeople(string? userCouncil = null)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Name,Email,Telephone,Address,Postcode,Roles");

        var people = _db.People.AsQueryable();
        if (!string.IsNullOrEmpty(userCouncil))
        {
            people = people.Where(p => p.Council == userCouncil);
        }

        foreach (var p in people.OrderBy(p => p.Id))
        {
            var roles = new List<string>();
            if (p.IsSponsor) roles.Add("Sponsor");
            if (p.IsHost) roles.Add("Host");
            sb.AppendLine($"\"{p.FullName}\",\"{p.Email}\",\"{p.Telephone}\",\"{p.Address}\",\"{p.Postcode}\",\"{string.Join("; ", roles)}\"");
        }

        return ($"people-{DateTime.Now:yyyyMMdd-HHmmss}.csv", sb.ToString());
    }

    public (string filename, string csv) ExportAccommodations(string? userCouncil = null)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Address,Postcode,Council");

        var accs = _db.Accommodations.AsQueryable();
        if (!string.IsNullOrEmpty(userCouncil))
        {
            accs = accs.Where(a => a.Council == userCouncil);
        }

        foreach (var a in accs.OrderBy(a => a.Id))
        {
            sb.AppendLine($"\"{a.Address}\",\"{a.Postcode}\",\"{a.Council}\"");
        }

        return ($"accommodations-{DateTime.Now:yyyyMMdd-HHmmss}.csv", sb.ToString());
    }

    public (string filename, string csv) ExportCases(string? userCouncil = null)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Reference,Title,Sponsor,Accommodation,Council,Status");

        var cases = _db.Cases.AsQueryable();
        if (!string.IsNullOrEmpty(userCouncil))
        {
            cases = cases.Where(c => c.Council == userCouncil);
        }

        foreach (var c in cases.OrderBy(c => c.Id))
        {
            var status = c.Checks.Count == 0 ? "Checks Required" : "In Progress";
            sb.AppendLine($"\"{c.Reference}\",\"{c.Title}\",\"{c.Sponsor?.FullName}\",\"{c.Accommodation?.Address}\",\"{c.Council}\",\"{status}\"");
        }

        return ($"cases-{DateTime.Now:yyyyMMdd-HHmmss}.csv", sb.ToString());
    }
}
