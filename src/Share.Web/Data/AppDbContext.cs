using Microsoft.EntityFrameworkCore;
using Share.Web.Models;
using Share.Web.Users;

namespace Share.Web.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options, CurrentUser user) : DbContext(options)
{
    // Row-level scoping (M6): when set, every query sees only records in this council.
    // Applied as global query filters, so lists, detail pages and counts are all scoped and a
    // guessed URL for another council's record finds nothing. Null (central admin) sees everything.
    // Ingest reads with IgnoreQueryFilters, because matching must see every record.
    public string? CouncilScope { get; set; } = user.User.Council;

    public DbSet<VisaApplication> VisaApplications => Set<VisaApplication>();
    public DbSet<Guest> Guests => Set<Guest>();
    public DbSet<Person> People => Set<Person>();
    public DbSet<Accommodation> Accommodations => Set<Accommodation>();
    public DbSet<Case> Cases => Set<Case>();
    public DbSet<IngestRun> IngestRuns => Set<IngestRun>();
    public DbSet<TimelineEvent> TimelineEvents => Set<TimelineEvent>();
    public DbSet<DecisionUpdate> DecisionUpdates => Set<DecisionUpdate>();
    public DbSet<Offer> Offers => Set<Offer>();
    public DbSet<Announcement> Announcements => Set<Announcement>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        // Every scoped entity is filtered by the same council, so a required end (a guest's
        // application, say) is never filtered out for a row that passes its own filter.
        // An application's council is the accommodation address's, the same as its case's.
        b.Entity<VisaApplication>().HasQueryFilter(a => CouncilScope == null || a.Council == CouncilScope);
        b.Entity<Guest>().HasQueryFilter(g => CouncilScope == null || g.VisaApplication!.Council == CouncilScope);
        b.Entity<Accommodation>().HasQueryFilter(x => CouncilScope == null || x.Council == CouncilScope);
        // A sponsor or host belongs to every council they have an application in.
        b.Entity<Person>().HasQueryFilter(p => CouncilScope == null
            || p.SponsoredApplications.Any(a => a.Council == CouncilScope)
            || p.HostedApplications.Any(a => a.Council == CouncilScope));
        b.Entity<Case>().HasQueryFilter(c => CouncilScope == null || c.Council == CouncilScope);
        b.Entity<Offer>().HasQueryFilter(o => CouncilScope == null || o.Council == CouncilScope);
        b.Entity<DecisionUpdate>().HasQueryFilter(u => CouncilScope == null || u.VisaApplication!.Council == CouncilScope);
        b.Entity<TimelineEvent>().HasQueryFilter(e => CouncilScope == null
            || ((e.VisaApplicationId == null || e.VisaApplication!.Council == CouncilScope)
                && (e.CaseId == null || e.Case!.Council == CouncilScope)
                && (e.OfferId == null || e.Offer!.Council == CouncilScope)));

        b.Entity<VisaApplication>(e =>
        {
            e.HasIndex(a => a.SubmissionGuid).IsUnique();
            e.HasIndex(a => a.Uan);
            e.HasIndex(a => a.Gwf);
            e.Property(a => a.Status).HasConversion<string>();
            e.Ignore(a => a.Lead);
            e.Ignore(a => a.LatestArrival);
            e.HasOne(a => a.Sponsor).WithMany(p => p.SponsoredApplications).HasForeignKey(a => a.SponsorId);
            e.HasOne(a => a.Host).WithMany(p => p.HostedApplications).HasForeignKey(a => a.HostId);
        });
        b.Entity<Guest>().HasIndex(p => p.Gwf);
        b.Entity<Offer>(e =>
        {
            e.HasIndex(o => o.SubmissionReference).IsUnique();
            e.Property(o => o.Status).HasConversion<string>();
            e.Ignore(o => o.Address);
        });
        b.Entity<DecisionUpdate>(e =>
        {
            e.HasIndex(u => new { u.IngestRunId, u.RowNumber }).IsUnique();
            e.HasOne(u => u.VisaApplication).WithMany(a => a.DecisionUpdates).HasForeignKey(u => u.VisaApplicationId);
        });
        b.Entity<TimelineEvent>().Property(t => t.Kind).HasConversion<string>();
        b.Entity<CaseCheck>(e =>
        {
            e.HasIndex(x => new { x.CaseId, x.Kind }).IsUnique();
            e.Property(x => x.Kind).HasConversion<string>();
            e.Property(x => x.Status).HasConversion<string>();
            e.Property(x => x.DbsType).HasConversion<string>();
        });
        b.Entity<IngestRun>().HasIndex(r => r.FileName).IsUnique();
        b.Entity<Person>().HasIndex(x => x.MatchKey).IsUnique();
        b.Entity<Accommodation>().HasIndex(x => x.MatchKey).IsUnique();
        b.Entity<Case>(e =>
        {
            e.HasIndex(c => c.MatchKey).IsUnique();
            e.Ignore(c => c.Guests);
            e.HasMany(c => c.Checks).WithOne().HasForeignKey(x => x.CaseId);
            e.HasOne(c => c.Host).WithMany().HasForeignKey(c => c.HostId);
        });
        b.Entity<Announcement>(e =>
        {
            e.HasIndex(a => new { a.IsHidden, a.PublishAt }).IsDescending(false, true);
            e.HasIndex(a => a.CreatedAt);
        });
        b.Entity<Notification>(e =>
        {
            e.HasIndex(n => new { n.UserId, n.IsRead });
            e.HasIndex(n => n.CreatedAt);
            e.Property(n => n.EventType).HasConversion<string>();
        });
    }
}
