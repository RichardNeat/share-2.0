using Share.Web.Models;

namespace Share.Web.Pages.Shared;

public enum PersonRole { Sponsor, Host }

public record PeopleTable(IReadOnlyList<Person> People, PersonRole Role)
{
    public IEnumerable<VisaApplication> ApplicationsFor(Person p) =>
        Role == PersonRole.Sponsor ? p.SponsoredApplications : p.HostedApplications;
}
