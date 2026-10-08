using Microsoft.EntityFrameworkCore;
using Share.Web.Data;
using Share.Web.Models;

namespace Share.Web.Ingest;

public class IngestService(AppDbContext db, DataFiles files, TimeProvider clock)
{
    public async Task<DataFile?> NextFileAsync()
    {
        var done = await db.IngestRuns.Select(r => r.FileName).ToListAsync();
        return files.All().FirstOrDefault(f => !done.Contains(f.Name));
    }

    // Processes the next unprocessed file in number order. Returns null when every file is done.
    public async Task<IngestRun?> ProcessNextAsync()
    {
        var next = await NextFileAsync();
        return next is null ? null : await ProcessFileAsync(next);
    }

    // Idempotent: a submission already stored (same submission GUID) is counted, not duplicated.
    public async Task<IngestRun> ProcessFileAsync(DataFile file)
    {
        var run = await db.IngestRuns.FirstOrDefaultAsync(r => r.FileName == file.Name);
        if (run is null)
        {
            run = new IngestRun { FileName = file.Name, FileNumber = file.Number };
            db.IngestRuns.Add(run);
        }
        run.ProcessedAt = clock.GetUtcNow().UtcDateTime;

        if (file.Name.EndsWith("-visa-applications.json"))
            await IngestApplicationsAsync(run, await File.ReadAllTextAsync(file.Path));
        else
            throw new NotSupportedException($"{file.Name} is not a file type we can process yet");

        await db.SaveChangesAsync();
        return run;
    }

    async Task IngestApplicationsAsync(IngestRun run, string json)
    {
        var parsed = VisaApplicationParser.Parse(json);
        run.RecordsInFile = parsed.Count;
        run.Added = 0;
        run.AlreadyPresent = 0;
        run.SkippedRows.Clear();

        var known = (await db.VisaApplications.Select(a => a.SubmissionGuid).ToListAsync()).ToHashSet();
        foreach (var p in parsed)
        {
            if (p.Application is null)
            {
                run.SkippedRows.Add(new SkippedRow { RowNumber = p.RowNumber, Reference = p.Reference, Reason = p.SkipReason! });
                continue;
            }
            if (!known.Add(p.Application.SubmissionGuid))
            {
                run.AlreadyPresent++;
                continue;
            }

            var app = p.Application;
            app.IngestRun = run;
            db.VisaApplications.Add(app);
            run.Added++;

            var lead = app.Lead!;
            var others = app.People.Count - 1;
            run.Events.Add(new TimelineEvent
            {
                Kind = TimelineEventKind.ApplicationReceived,
                OccurredAt = app.SubmittedAt,
                Title = "Application received",
                Description = others == 0
                    ? $"{lead.FullName} applied ({app.Uan})"
                    : $"{lead.FullName} applied with {others} family member{(others == 1 ? "" : "s")} ({app.Uan})",
                VisaApplication = app,
            });
        }
    }
}
