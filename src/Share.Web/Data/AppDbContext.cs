using Microsoft.EntityFrameworkCore;
using Share.Web.Models;

namespace Share.Web.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<VisaApplication> VisaApplications => Set<VisaApplication>();
    public DbSet<ApplicationPerson> ApplicationPeople => Set<ApplicationPerson>();
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
        });
        b.Entity<ApplicationPerson>().HasIndex(p => p.Gwf);
        b.Entity<TimelineEvent>().Property(t => t.Kind).HasConversion<string>();
        b.Entity<IngestRun>().HasIndex(r => r.FileName).IsUnique();
    }
}
