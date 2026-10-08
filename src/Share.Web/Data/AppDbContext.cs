using Microsoft.EntityFrameworkCore;
using Share.Web.Models;

namespace Share.Web.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<VisaApplication> VisaApplications => Set<VisaApplication>();
    public DbSet<Guest> Guests => Set<Guest>();
    public DbSet<Person> People => Set<Person>();
    public DbSet<Accommodation> Accommodations => Set<Accommodation>();
    public DbSet<Case> Cases => Set<Case>();
    public DbSet<IngestRun> IngestRuns => Set<IngestRun>();
    public DbSet<TimelineEvent> TimelineEvents => Set<TimelineEvent>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<VisaApplication>(e =>
        {
            e.HasIndex(a => a.SubmissionGuid).IsUnique();
            e.HasIndex(a => a.Uan);
            e.HasIndex(a => a.Gwf);
            e.Property(a => a.Status).HasConversion<string>();
            e.Ignore(a => a.Lead);
            e.HasOne(a => a.Sponsor).WithMany(p => p.SponsoredApplications).HasForeignKey(a => a.SponsorId);
            e.HasOne(a => a.Host).WithMany(p => p.HostedApplications).HasForeignKey(a => a.HostId);
        });
        b.Entity<Guest>().HasIndex(p => p.Gwf);
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
        });
    }
}
