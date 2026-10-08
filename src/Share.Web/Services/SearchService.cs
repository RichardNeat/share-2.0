using Microsoft.EntityFrameworkCore;
using Share.Web.Data;
using Share.Web.Models;

namespace Share.Web.Services;

public class SearchService
{
    private readonly AppDbContext _db;

    public SearchService(AppDbContext db)
    {
        _db = db;
    }

    public SearchResults Search(string query, string? userCouncil = null)
    {
        var results = new SearchResults();
        if (string.IsNullOrWhiteSpace(query)) return results;

        var (isPhrase, terms) = ParseQuery(query);

        if (isPhrase)
        {
            results.Guests = SearchGuests(terms[0], userCouncil, exact: true);
            results.People = SearchPeople(terms[0], userCouncil, exact: true);
            results.Accommodations = SearchAccommodations(terms[0], userCouncil, exact: true);
            results.Cases = SearchCases(terms[0], userCouncil, exact: true);
        }
        else
        {
            results.Guests = SearchGuests(query, userCouncil);
            results.People = SearchPeople(query, userCouncil);
            results.Accommodations = SearchAccommodations(query, userCouncil);
            results.Cases = SearchCases(query, userCouncil);
        }

        return results;
    }

    private (bool isPhrase, string[] terms) ParseQuery(string query)
    {
        var trimmed = query.Trim();
        if (trimmed.StartsWith("\"") && trimmed.EndsWith("\""))
        {
            return (true, new[] { trimmed.Trim('"') });
        }
        return (false, trimmed.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
    }

    private List<Guest> SearchGuests(string query, string? userCouncil, bool exact = false)
    {
        var q = _db.Guests.AsQueryable();

        if (exact)
        {
            q = q.Where(g => g.FullName.ToLower().Contains(query.ToLower()) ||
                             g.Gwf!.ToLower().Contains(query.ToLower()));
        }
        else
        {
            var terms = query.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            foreach (var term in terms)
            {
                q = q.Where(g => g.FullName.ToLower().Contains(term) ||
                                g.GivenName!.ToLower().Contains(term) ||
                                g.FamilyName!.ToLower().Contains(term) ||
                                g.Nationality!.ToLower().Contains(term) ||
                                g.PassportNumber!.ToLower().Contains(term) ||
                                g.Gwf!.ToLower().Contains(term));
            }
        }

        if (!string.IsNullOrEmpty(userCouncil))
        {
            q = q.Where(g => g.VisaApplication!.Accommodation!.Council == userCouncil ||
                             g.VisaApplication!.Council == userCouncil);
        }

        return q.OrderBy(g => g.FullName).Take(20).ToList();
    }

    private List<Person> SearchPeople(string query, string? userCouncil, bool exact = false)
    {
        var q = _db.People.AsQueryable();

        if (exact)
        {
            q = q.Where(p => p.FullName.ToLower().Contains(query.ToLower()) ||
                             p.Email!.ToLower().Contains(query.ToLower()) ||
                             p.Postcode!.ToLower().Contains(query.ToLower()) ||
                             p.Address!.ToLower().Contains(query.ToLower()));
        }
        else
        {
            var terms = query.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            foreach (var term in terms)
            {
                q = q.Where(p => p.FullName.ToLower().Contains(term) ||
                                p.GivenName!.ToLower().Contains(term) ||
                                p.FamilyName!.ToLower().Contains(term) ||
                                p.Email!.ToLower().Contains(term) ||
                                p.Postcode!.ToLower().Contains(term) ||
                                p.Address!.ToLower().Contains(term));
            }
        }

        if (!string.IsNullOrEmpty(userCouncil))
        {
            q = q.Where(p => p.Council == userCouncil);
        }

        return q.OrderBy(p => p.FullName).Take(20).ToList();
    }

    private List<Accommodation> SearchAccommodations(string query, string? userCouncil, bool exact = false)
    {
        var q = _db.Accommodations.AsQueryable();

        if (exact)
        {
            q = q.Where(a => a.Address.ToLower().Contains(query.ToLower()) ||
                             a.Postcode!.ToLower().Contains(query.ToLower()));
        }
        else
        {
            var terms = query.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            foreach (var term in terms)
            {
                q = q.Where(a => a.Address.ToLower().Contains(term) ||
                                a.Postcode!.ToLower().Contains(term));
            }
        }

        if (!string.IsNullOrEmpty(userCouncil))
        {
            q = q.Where(a => a.Council == userCouncil);
        }

        return q.OrderBy(a => a.Address).Take(20).ToList();
    }

    private List<Case> SearchCases(string query, string? userCouncil, bool exact = false)
    {
        var q = _db.Cases.AsQueryable();
        var lowerQuery = query.ToLower();

        if (exact)
        {
            q = q.Where(c => c.Reference.ToLower().Contains(lowerQuery) ||
                             c.Title.ToLower().Contains(lowerQuery) ||
                             c.Sponsor!.FullName.ToLower().Contains(lowerQuery) ||
                             c.Accommodation!.Address.ToLower().Contains(lowerQuery) ||
                             c.Accommodation!.Postcode!.ToLower().Contains(lowerQuery));
        }
        else
        {
            var terms = lowerQuery.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            foreach (var term in terms)
            {
                q = q.Where(c => c.Reference.ToLower().Contains(term) ||
                                c.Title.ToLower().Contains(term) ||
                                c.Sponsor!.FullName.ToLower().Contains(term) ||
                                c.Accommodation!.Address.ToLower().Contains(term) ||
                                c.Accommodation!.Postcode!.ToLower().Contains(term));
            }
        }

        if (!string.IsNullOrEmpty(userCouncil))
        {
            q = q.Where(c => c.Council == userCouncil);
        }

        return q.OrderBy(c => c.Reference).Take(20).ToList();
    }
}

public class SearchResults
{
    public List<Guest> Guests { get; set; } = [];
    public List<Person> People { get; set; } = [];
    public List<Accommodation> Accommodations { get; set; } = [];
    public List<Case> Cases { get; set; } = [];

    public bool HasResults => Guests.Count > 0 || People.Count > 0 || Accommodations.Count > 0 || Cases.Count > 0;
}
