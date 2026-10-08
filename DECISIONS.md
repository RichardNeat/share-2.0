# Decision log

A running timeline of the major decisions made while building this service, oldest first. Each entry says what we decided, why, and what we turned down.

## 2026-10-07 · Before the clock starts

### 1. Stack: .NET 10 Razor Pages, EF Core with SQLite, GOV.UK Frontend from the CDN

- **Why:** Server-rendered pages keep everything in one project with no separate front end. SQLite needs no setup. The stack was proven before the hackathon (see "Known gotchas" in CLAUDE.md)
- **Turned down:** a separate JavaScript front end, which would mean two builds and two sets of tests

### 2. Run the tests at commit points, not after every edit

- **Why:** Running the full suite after every small edit wastes time against the clock. Running it at each point we'd commit still catches problems before they land
- **What:** Unit and journey tests (`dotnet test`) run after every major change and always before a milestone is called done

### 3. No Playwright and no video recordings of tests

- **Why:** The browser download and the need for a real running server cost too much of a 75-minute build. The app is server-rendered, so AngleSharp journey tests check what the acceptance criteria need
- **Turned down:** Playwright tests that record videos, for wow points. If we want a video for the demo, a screen recording does the job for free

### 4. Deterministic reset and seed from M1

- **Why:** The demo has to come out the same every time, and resetting by hand wastes time and breaks
- **What:** One `DemoSeeder` resets the database and replays data files 01 to N through the real ingest service. It never invents records. It is available from an admin page (so anyone can click it during the demo) and from a console command. Every later milestone extends it
- **Open point:** Scripted user actions in the seed (for example, checks already passed on the hero case) are allowed for now, applied through the same services as the UI. The team may decide to use data files only

### 5. "What just happened" page after each ingest

- **Why:** The live ingest is a required part of the demo. A summary of what changed, with links, turns it into a moment
- **What:** Each ingest is saved as an `IngestRun`, and the browser lands on that run's page: changes, links and skipped rows with reasons

### 6. One event log powers the ingest summary and the case timeline

- **Why:** M5 Ticket 2 asks for a case timeline. Recording events from M1 means the timeline is mostly ready by M5, and the same log feeds the ingest summary, so we build it once
- **What:** Data events use timestamps from the data, never the ingest time, so the timeline is the same on every run

### 7. M14 Suggested duplicates ★ with Levenshtein matching is the first backlog pick

- **Why:** A ★ milestone working live scores top marks for wow, and fuzzy name matching is the part of it that surprises people
- **What:** Store guest name parts, date of birth and passport as separate columns from M2 so M14 is mostly matching logic

### 8. GOV.UK look at every milestone, not polished at the end

- **Why:** Looks is a third of the score, and the site must be ready to demo at any commit
- **What:** The GOV.UK page template and components throughout, with the rules set out in CLAUDE.md

## 2026-10-08 · M1 Applications arrive

### 9. Use "The Share" artwork as the site logo, not the GOV.UK crown

- **Why:** The team chose it. It also avoids copying the GOV.UK crown onto a service that isn't on GOV.UK
- **What:** The centre square is cropped from the original image and shrunk to a 288 px PNG (177 KB), shown at 72 px in the GOV.UK header with the alt text "The Share home". The 3.3 MB original stays out of git. Everything else keeps the GOV.UK template: service navigation, phase banner and footer

### 10. A submission's identity is its submission GUID

- **Why:** It is unique per submission in the feed. A submission whose GUID is already stored is counted as "already ingested", not added again, so reprocessing a file never duplicates records
- **Turned down:** keying on the UAN alone, because nothing we have reached yet shows whether a UAN can be resubmitted

### 11. Store every answer as well as the extracted fields

- **Why:** The detail page must show all of a submission's answers. Keeping them in their original order means no question is lost, even ones we have not modelled yet
- **What:** Name parts, date of birth, nationality, passport and GWF are also extracted into their own columns on each person (groundwork for M14). The application's sponsor and council come from the lead applicant. The council is "Local authority of UK address", because the brief says a case's council comes from the accommodation address

### 12. A submission missing its GUID, UAN or people is skipped with a reason

- **Why:** The feed contains deliberate oddities, and we must never crash or guess. Blank or malformed answers (such as a date that doesn't parse) become empty, not errors

### 13. "Process next file" means the first file in number order with no ingest run yet

- **Why:** Simple, and it stays strictly in number order. A file type we can't handle yet shows a clear error on the demo controls page instead of crashing
- **What:** The data directory defaults to `data/` at the repo root and can be changed with `DataDirectory` in config. Tests point it at the repo's `data/` and pin `Clock:Today` to 2026-10-01

### 14. Reset uses `EnsureDeleted` then `Migrate`

- **Why:** It is the simplest way to guarantee a truly empty state, and new tables are picked up automatically as models are added. Ingest runs are numbered from 1 again after every reset, so links and IDs repeat exactly

### 15. The header shows the logo and the name "The Share" together; page titles say "The Share"

- **Why:** On its own the logo sat small and top-aligned in an empty black bar, and nothing said what the service was. Putting a larger logo (88 px) beside the name and the tagline "Homes for Ukraine casework" fills the header and matches the artwork
- **What:** The logo has empty alt text because the link already reads "The Share, Homes for Ukraine casework". The service name moves out of the service navigation so it isn't shown twice. The header's blue border runs full width. Page titles become `<Page> - The Share - GOV.UK`. Wide tables scroll inside their own focusable box, so the page never scrolls sideways on a phone
