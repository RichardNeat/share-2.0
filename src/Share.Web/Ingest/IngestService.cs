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
        run.PeopleAdded = 0;
        run.AccommodationsAdded = 0;
        run.CasesAdded = 0;
        run.SkippedRows.Clear();

        // Matching must see every council's records, whoever is signed in.
        var known = (await db.VisaApplications.IgnoreQueryFilters().Select(a => a.SubmissionGuid).ToListAsync()).ToHashSet();
        var people = await db.People.IgnoreQueryFilters().ToDictionaryAsync(x => x.MatchKey);
        var accommodations = await db.Accommodations.IgnoreQueryFilters().ToDictionaryAsync(x => x.MatchKey);
        var cases = await db.Cases.IgnoreQueryFilters().ToDictionaryAsync(x => x.MatchKey);

        Person? Resolve(Person? person)
        {
            if (person is null) return null;
            if (people.TryGetValue(person.MatchKey, out var existing)) return existing;
            people[person.MatchKey] = person;
            run.PeopleAdded++;
            return person;
        }

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

            // De-duplicate: link to the people and accommodation we already hold, or add new ones.
            var hostIsSponsor = ReferenceEquals(app.Host, app.Sponsor);
            app.Sponsor = Resolve(app.Sponsor);
            app.Host = hostIsSponsor ? app.Sponsor : Resolve(app.Host);
            if (app.Accommodation is { } accommodation)
            {
                if (accommodations.TryGetValue(accommodation.MatchKey, out var existing)) app.Accommodation = existing;
                else { accommodations[accommodation.MatchKey] = accommodation; run.AccommodationsAdded++; }
            }
            db.VisaApplications.Add(app);
            run.Added++;

            // Form the case: same sponsor and same accommodation means the same household case.
            var caseKey = RecordKeys.Case(app.Sponsor?.MatchKey, app.Accommodation?.MatchKey, app.Uan);
            var caseIsNew = false;
            if (!cases.TryGetValue(caseKey, out var @case))
            {
                @case = new Case { MatchKey = caseKey, Sponsor = app.Sponsor, Accommodation = app.Accommodation, Council = app.Accommodation?.Council ?? app.Council };
                cases[caseKey] = @case;
                run.CasesAdded++;
                caseIsNew = true;
            }
            app.Case = @case;

            var lead = app.Lead!;
            var others = app.Guests.Count - 1;
            run.Events.Add(new TimelineEvent
            {
                Kind = TimelineEventKind.ApplicationReceived,
                OccurredAt = app.SubmittedAt,
                Title = "Application received",
                Description = others == 0
                    ? $"{lead.FullName} applied ({app.Uan})"
                    : $"{lead.FullName} applied with {others} family member{(others == 1 ? "" : "s")} ({app.Uan})",
                VisaApplication = app,
                Case = @case,
            });
            if (caseIsNew)
            {
                run.Events.Add(new TimelineEvent
                {
                    Kind = TimelineEventKind.CaseFormed,
                    OccurredAt = app.SubmittedAt,
                    Title = "Case formed",
                    Description = app.Accommodation is null
                        ? $"New case for {app.Lead?.FullName}"
                        : $"New case for {app.Accommodation.Address}",
                    Case = @case,
                });
            }
        }
    }
}
