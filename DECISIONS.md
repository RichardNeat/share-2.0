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
