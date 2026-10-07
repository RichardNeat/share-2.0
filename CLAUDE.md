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
- Ingest is a repeatable **"process next file"** action: a button on an admin page (best for the live demo) backed by a service class, idempotent, logging skipped rows

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

## Testing (every change)

- **Unit tests** for all logic: parsing, matching by GWF then UAN, visa status precedence, case formation, derived check status, Enhanced DBS rule
- **Journey tests** for each major feature: `Microsoft.AspNetCore.Mvc.Testing` (`WebApplicationFactory<Program>`) plus **AngleSharp** to parse the HTML. A shared factory points `ConnectionStrings:Default` at a unique temp SQLite file and deletes it after the tests (call `SqliteConnection.ClearAllPools()` first). No Playwright, because browser installs cost too much time
- Every page gets the shared accessibility assertions: a non-empty `<title>`, exactly one `h1`, the skip link to `#main-content`, and a `label[for]` for every visible input, select and textarea
- Run `dotnet test` from the repo root before you say a milestone is done

## Working agreement

- Never `git commit` without being asked. After each milestone: run the tests, list how to check each acceptance criterion, and suggest a commit message
- Accessibility and GDS patterns come first: one h1, labelled inputs, keyboard-reachable actions, and statuses as GOV.UK tags with words (never colour alone)
