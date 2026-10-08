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
        else if (file.Name.EndsWith("-arrivals.csv"))
            await IngestArrivalsAsync(run, await File.ReadAllTextAsync(file.Path));
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

    // Arrivals and decisions: match each row to an application by GWF first, UAN as fallback,
    // then re-derive the application's visa status from all its updates. Unmatched rows are skipped.
    // Idempotent: a row already stored for this file is counted, not applied twice.
    async Task IngestArrivalsAsync(IngestRun run, string csv)
    {
        var rows = ArrivalsParser.Parse(csv);
        run.RecordsInFile = rows.Count;
        run.Added = 0;
        run.AlreadyPresent = 0;
        run.StatusesChanged = 0;
        run.Arrivals = 0;
        run.SkippedRows.Clear();

        var apps = await db.VisaApplications.IgnoreQueryFilters()
            .Include(a => a.Guests).Include(a => a.DecisionUpdates)
            .ToListAsync();
        var byGwf = new Dictionary<string, VisaApplication>(StringComparer.OrdinalIgnoreCase);
        foreach (var a in apps)
            foreach (var gwf in a.Guests.Select(g => g.Gwf).Append(a.Gwf).Where(g => !string.IsNullOrWhiteSpace(g)).Distinct())
                byGwf.TryAdd(gwf!, a);
        var byUan = apps.GroupBy(a => a.Uan, StringComparer.OrdinalIgnoreCase).ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
        var alreadyStored = run.Id == 0 ? [] : (await db.DecisionUpdates.IgnoreQueryFilters()
            .Where(u => u.IngestRunId == run.Id).Select(u => u.RowNumber).ToListAsync()).ToHashSet();

        foreach (var row in rows)
        {
            var reference = string.Join(" / ", new[] { row.Gwf, row.Uan }.Where(x => x != null));
            if (row.Problem is not null)
            {
                run.SkippedRows.Add(new SkippedRow { RowNumber = row.RowNumber, Reference = reference, Reason = row.Problem });
                continue;
            }
            if (row.Gwf is null && row.Uan is null)
            {
                run.SkippedRows.Add(new SkippedRow { RowNumber = row.RowNumber, Reference = null, Reason = "No GWF or UAN to match on" });
                continue;
            }
            var app = row.Gwf is not null && byGwf.TryGetValue(row.Gwf, out var byG) ? byG
                : row.Uan is not null && byUan.TryGetValue(row.Uan, out var byU) ? byU
                : null;
            if (app is null)
            {
                run.SkippedRows.Add(new SkippedRow { RowNumber = row.RowNumber, Reference = reference, Reason = "No application matches this GWF or UAN" });
                continue;
            }
            if (alreadyStored.Contains(row.RowNumber))
            {
                run.AlreadyPresent++;
                continue;
            }

            var hadArrived = app.DecisionUpdates.Any(u => u.ArrivedAt != null);
            var before = app.Status;
            var update = new DecisionUpdate
            {
                VisaApplication = app, IngestRun = run, RowNumber = row.RowNumber, Gwf = row.Gwf, Uan = row.Uan,
                DecisionDate = row.DecisionDate, Decision = row.Decision, VoyageCode = row.VoyageCode,
                ArrivalPort = row.ArrivalPort, ArrivedAt = row.ArrivedAtUtc, PersonIdentifier = row.PersonIdentifier,
            };
            app.DecisionUpdates.Add(update);
            run.Added++;

            app.Status = VisaStatusRules.Derive(app.DecisionUpdates.Select(u => VisaStatusRules.FromUpdate(u.Decision, u.ArrivedAt)));
            var name = app.Lead?.FullName ?? app.Uan;
            var when = row.ArrivedAtUtc ?? row.DecisionDate?.ToDateTime(TimeOnly.MinValue) ?? clock.GetUtcNow().UtcDateTime;
            if (app.Status != before)
            {
                run.StatusesChanged++;
                run.Events.Add(new TimelineEvent
                {
                    Kind = TimelineEventKind.VisaStatusChanged,
                    OccurredAt = when,
                    Title = "Visa status changed",
                    Description = $"{name}'s visa application ({app.Uan}): {Display.VisaStatusTag(before).Text} to {Display.VisaStatusTag(app.Status).Text}"
                        + (row.Decision is null ? "" : $". Home Office decision: {row.Decision}" + (row.DecisionDate is { } d ? $" on {Display.Date(d)}" : "")),
                    VisaApplication = app,
                    CaseId = app.CaseId,
                });
            }
            if (row.ArrivedAtUtc is { } arrivedAt && !hadArrived && app.Status == VisaStatus.Arrived)
            {
                run.Arrivals++;
                var others = app.Guests.Count - 1;
                run.Events.Add(new TimelineEvent
                {
                    Kind = TimelineEventKind.GuestsArrived,
                    OccurredAt = arrivedAt,
                    Title = "Guests arrived in the UK",
                    Description = $"{name}{(others > 0 ? $" and {others} family member{(others == 1 ? "" : "s")}" : "")} arrived at {Ports.Name(row.ArrivalPort)}"
                        + (row.VoyageCode is null ? "" : $" on {row.VoyageCode}") + ". Check 4 can now be completed",
                    VisaApplication = app,
                    CaseId = app.CaseId,
                });
            }
        }
    }
}
