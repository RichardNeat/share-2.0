# HFU Hackathon 2026: Rebuild Share

We are a team in a timed (~75 minute) hackathon, rebuilding a casework service for the Homes for Ukraine programme from an empty repo. This repo is the team's build repo. The brief lives in a separate read-only clone at `~/Dev/hfu-hackathon-2026`; read from it, never write to it.

## The brief (imported from the brief repo)

@~/Dev/hfu-hackathon-2026/AGENTS.md
@~/Dev/hfu-hackathon-2026/RULES.md
@~/Dev/hfu-hackathon-2026/DATA.md
@~/Dev/hfu-hackathon-2026/GLOSSARY.md

Milestones are in `~/Dev/hfu-hackathon-2026/MILESTONES.md` (M1 to M5, in order) and `~/Dev/hfu-hackathon-2026/milestones/` (M6 to M24, any order after M5). Build only the milestone you are given; the acceptance criteria are the definition of done. When they all pass, verify each one, say how to check it, and stop.

## Hard rules for you, the AI

- **You never see Share.** Do not search for, open or ask about the real Share codebase. Build from the brief only.
- **Data files in number order.** Copy each file from `~/Dev/hfu-hackathon-2026/data/` into `data/` here only when the current milestone needs it. Do not write code against a file we have not reached.
- **No real auth.** A role switcher or "sign in as" picker is enough (M6).
- **Never fabricate records.** Everything on screen comes from the data files or a user action.
- **Commit after every milestone** when asked. At the end the team tags `freeze` and pushes; remind us if we seem to be running out of time.

## Stack

- .NET 10, ASP.NET Core **Razor Pages** (server-rendered, no separate front end)
- **EF Core with SQLite** (`Data Source=app.db`), code-first models and migrations
- **GOV.UK Frontend** CSS from `https://cdn.jsdelivr.net/npm/govuk-frontend@5/dist/govuk/govuk-frontend.min.css` in the shared layout, replacing the template's Bootstrap. Use GOV.UK classes and components (tags for statuses, summary lists, tables) consistently
- Plain HTTP only: remove `app.UseHttpsRedirection()` and run with `dotnet run --urls http://127.0.0.1:5050`
- Ingest is a repeatable **"process next file"** action: a button on an admin page (best for the live demo) backed by a service class, idempotent, logging skipped rows. Each run is stored as an `IngestRun` (file, counts, skipped rows with reasons); see the demo plan below

## Known gotchas (found while proving the stack)

- Packages needed: `Microsoft.EntityFrameworkCore.Sqlite`, `Microsoft.EntityFrameworkCore.Design`, `Microsoft.EntityFrameworkCore.Tools`, `Microsoft.VisualStudio.Web.CodeGeneration.Design`
- Global tools: `dotnet-ef`, `dotnet-aspnet-codegenerator` (if not found, add `~/.dotnet/tools` to PATH)
- The scaffolder needs `Microsoft.EntityFrameworkCore.Tools` and defaults to SQL Server unless told otherwise. Working command:
  `dotnet aspnet-codegenerator razorpage -m <Model> -dc <Namespace>.AppDbContext -udl -outDir Pages/<Models> --databaseProvider sqlite`
- After a model change: `dotnet ef migrations add <Name>` then `dotnet ef database update`
- `dotnet new sln` now creates `Share.slnx` (fine). Layout: `src/Share.Web` (Razor Pages) and `tests/Share.Tests` (xUnit) referencing it. Setup takes about 20 seconds with a warm NuGet cache
- Delete the template's `wwwroot/lib` (Bootstrap/jQuery), `_Layout.cshtml.css` and the Privacy page. Load GOV.UK Frontend JS as a module from the same CDN (`.../govuk-frontend.min.js`, `initAll()`), and add the `js-enabled govuk-frontend-supported` body class script. Fonts resolve from the CDN without extra setup
- Read the connection string from `ConnectionStrings:Default`, falling back to `Data Source=app.db`, and call `Database.Migrate()` at startup so tests can point at their own db file
- Add `public partial class Program;` at the bottom of `Program.cs` so `WebApplicationFactory<Program>` can see it
- The scaffolder emits **Bootstrap markup** (`form-control`, `btn`, `h4` that skips heading levels) and Create/Edit/Delete pages we do not want (records only arrive from files). Prefer writing GOV.UK list/detail pages directly, using shared partials (status tag, summary list). If you do scaffold, convert the output to GOV.UK markup before moving on
- Port 5050 may be held by an old stack-proof app; check with `lsof -nP -iTCP:5050 -sTCP:LISTEN`

## Testing

- **Unit tests** for all logic: parsing, matching by GWF then UAN, visa status precedence, case formation, derived check status, Enhanced DBS rule
- **Journey tests** for each major feature: `Microsoft.AspNetCore.Mvc.Testing` (`WebApplicationFactory<Program>`) plus **AngleSharp** to parse the HTML. A shared factory points `ConnectionStrings:Default` at a unique temp SQLite file and deletes it after the tests (call `SqliteConnection.ClearAllPools()` first). No Playwright, because browser installs cost too much time
- Every page gets the shared accessibility assertions: a non-empty `<title>`, exactly one `h1`, the skip link to `#main-content`, and a `label[for]` for every visible input, select and textarea
- Run the full suite (unit and journey tests, `dotnet test` from the repo root) after every major change, meaning whenever you reach a point you would be ready to commit. Not after every minor edit
- Always run it before you say a milestone is done

## Working agreement

- Never `git commit` without being asked. After each milestone: run the tests, list how to check each acceptance criterion, and suggest a commit message
- **Keep `DECISIONS.md` up to date.** Every time you make a major decision (a data model, a matching or status rule, a library, a trade-off, a change of plan, or anything you chose not to do), add a numbered entry at the bottom under a dated heading for the milestone. Each entry says what you decided, why, and what you turned down. Update it after every implementation, before suggesting the commit, so it goes into the same commit. Add entries only; never rewrite earlier ones. If a decision is reversed, add a new entry that names the earlier one
- Accessibility and GDS patterns come first: one h1, labelled inputs, keyboard-reachable actions, and statuses as GOV.UK tags with words (never colour alone)

## Terminology

- A household's unit of casework is a **case**, everywhere: code, database, URLs, page text and references (`CASE-0003`). Never use "accommodation request" or `AccommodationRequest`, even though the brief uses it as a synonym
- Sponsor and host are different roles held by a `Person`: the sponsor backs the visa, the host is whoever the guests live with. Guests are separate records

## Looks: a real GOV.UK service at all times

The site must look like a real government service after every milestone, not just at the end. Never leave a page unstyled "for later".

- Layout from the GOV.UK page template: skip link, GOV.UK header with "The Share" logo and service name, service navigation, an alpha phase banner, `govuk-width-container` and `main#main-content`, GOV.UK footer
- Page titles follow `<Page> - The Share - GOV.UK`. Headings go down in order with no skipped levels. Use `govuk-heading-l` for the h1 and a caption where it helps (for example, the case reference)
- Use GOV.UK components, never home-made ones: summary lists for record details, tables with a caption for lists, tags for statuses (one wording and one colour per status, in a shared partial), a notification banner for success messages, back links or breadcrumbs, and the warning button for destructive actions
- Forms: GOV.UK fieldsets, radios and selects, an error summary plus inline errors on validation failure, and a POST-redirect-GET after every action
- Filters use GET forms so the URL can be shared and keyboard users can reach them. Every link to a related record is a real link with meaningful text (no "click here")

## Demo plan: build towards these

Scoring has three parts: Works, Looks and Wow (see `SCORING.md` in the brief). Wow means a never-built ★ milestone working live, the data story told, a live ingest during the demo, or a rematch. These are the agreed priorities. Build each one in the milestone named, not before.

### 1. Deterministic reset and seed (build in M1, extend in every milestone after)

The demo must come out exactly the same every time, and nobody should ever have to reset the database by hand.

- One `DemoSeeder` service is the only way to reset and seed. **Reset** deletes and re-creates the database (`EnsureDeleted` then `Migrate`). **Seed through N** resets, then processes data files 01 to N through the real ingest service, in number order. It never writes records directly and never invents data
- If a later milestone needs user actions in the demo state (for example, checks passed on the hero case), they go in one scripted list in `DemoSeeder` and are applied through the same services the UI calls. Each one must be easy to explain to the room
- Every milestone that adds a model or feature extends the seeder in the same change, and adds a test that seeding twice gives identical results
- **Admin page "Demo controls"** (anyone can click it during the demo): a "Process next file" button, a "Reset and load files through" select with a button, and a warning button for "Reset to empty". Show which file is next and which files are already loaded
- **Console:** `dotnet run --project src/Share.Web -- seed --through 6` and `-- reset` run the same `DemoSeeder` and exit without starting the web server. Handle this in `Program.cs` before `app.Run()`
- Determinism rules: process files in order and rows in file order. No `Guid.NewGuid()` or `DateTime.Now` in ingested data. Case references and IDs are derived from the data or from insertion order on an empty database. Every list has an explicit `OrderBy`. "Today" (for ages and the Enhanced DBS rule) comes from an injected `TimeProvider`, which can be pinned with `Clock:Today` in config
- Journey tests set up their state through `DemoSeeder` too

### 2. "What just happened" after each ingest (start in M1, grow with each milestone)

- "Process next file" POSTs, then redirects to `/Admin/Ingests/{id}`, which shows a success notification banner ("06-arrivals.csv processed"), a summary list of what changed (applications added, cases formed, visa statuses changed, guests arrived, alerts raised), links to each changed record, and a table of skipped rows with reasons
- A list of all ingest runs, linked from the admin page, gives the history
- In M1 it shows only applications added and rows skipped. Each later milestone adds its own lines

### 3. Case timeline (one event log from M1, rendered on the case in M5 Ticket 2)

- From M1, ingest writes a `CaseEvent`-style log row for each meaningful change (application received, visa status changed, guest arrived), with the `IngestRunId` that caused it. From M4, check updates also write a row. The same log powers the "what just happened" page and the timeline, so build it once
- Data events use the timestamp from the data (`eventDateTime`, decision date, arrival datetime), never the ingest time, so the timeline is deterministic. User actions use the `TimeProvider`
- In M5, render the timeline on the case detail page with the GOV.UK timeline pattern (newest first, each entry with a date, a heading and a short description, plus a link to the related record where there is one). This is the centrepiece of the demo story

### 4. Suggested duplicates with fuzzy matching (M14 ★, the first backlog pick after M5)

- Groundwork in M2: store each guest's given name, family name, date of birth, nationality and passport number as separate columns, so M14 is mostly matching logic
- Matching rules, ranked by confidence: same passport number (high); same normalised full name and date of birth (high); full names within Levenshtein distance 2 and the same date of birth (medium); family name the same and given names within distance 1, with a different date of birth (low). Normalise by lowercasing, trimming, removing diacritics, apostrophes and hyphens, and collapsing spaces
- Write Levenshtein as a small pure function with thorough unit tests (empty strings, equal strings, one insert, delete and substitution, transliteration variants such as Oleksandr and Oleksander). Group candidates by date of birth or family-name initial so the comparison doesn't check every pair
- Each suggestion shows both guests side by side in summary lists, the reason as words ("Names 1 letter apart, same date of birth") and a confidence tag. "Not a duplicate" is remembered against a stable pair key (GWF numbers in sorted order), so a dismissed pair stays dismissed as more files are ingested. A reset clears dismissals, so the demo starts clean. "Accept" marks one guest as a duplicate of the other (or hands over to M13 if we built it)
- Before the demo, read the data with our own eyes and find the best pair to show, ideally one that only appears after a live ingest
