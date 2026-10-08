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

## 2026-10-08 · M2 People and places

### 16. Sponsors and hosts are one `Person` record with roles; guests stay separate

- **Why:** The team clarified that sponsor and host are different roles. The sponsor backs the visa; the host is whoever the guests live with, and that may or may not be the sponsor. When one person is both (Clare Osborne sponsors and hosts application 1), they must be one record, not a sponsor record plus a host record
- **What:** A `Person` has sponsored applications and hosted applications. The Sponsors and Hosts lists both link to one person page, which shows "Sponsor" and "Host" tags. Guests (the people on the visa form) stay as their own `Guest` records
- **Turned down:** separate Sponsor and Host tables, which would duplicate anyone holding both roles

### 17. The host is the sponsor only when the application says the guests stay at the sponsor's address

- **Why:** The team said not to treat the sponsor as the host by default
- **What:** "Yes" means the sponsor is the host. "No" means the named host is. If the question is unanswered, there is no host, and the page shows "Not given" rather than a guess
- **Data oddity kept as-is:** application 7 says the guests won't stay at the sponsor's address, but gives the same address and names Daniel Park as host. We show exactly that

### 18. Match people on unique identifiers only, never on name or date of birth

- **Why:** The team pointed out that two different people can share both a name and a date of birth. An earlier draft of this milestone matched sponsors on name plus date of birth; it never shipped
- **What:** A sponsor is identified by their email address (lower-cased and trimmed). We checked by eye that every application in the brief's data gives one and that no email is shared by different people. A sponsor without an email is never merged. A host named on an application (not the sponsor) comes with a name only, so is never merged: one record per application
- **Later:** possible duplicates (the same host twice, say) are left to M14's suggestions, which a person confirms

### 19. An accommodation is identified by its address and council

- **Why:** The brief says so, and an address identifies a property, so it is a fair key. It is compared after normalising case, punctuation and spacing
- **What:** Two applications to the same address in the same council share one accommodation record

### 20. Guests are not merged across applications

- **Why:** A guest has no identifier that holds across applications. Family members have no GWF number, and we won't match on names. Merging people is M13/M14's job, with a human confirming
- **What:** `ApplicationPerson` was renamed `Guest`, and the application's list of people is now called `Guests`

### 21. Console `reset` and `seed` run before the startup migration

- **Why:** This milestone's migration drops and recreates the people table. Running the console commands first means they can always recover an old local `app.db`, instead of failing on a migration that conflicts with stale data

## 2026-10-08 · M3 Build the case

### 22. A case groups applications with the same sponsor (by email) and the same accommodation (by address and council)

- **Why:** The brief's rule, applied with the identifiers we chose in M2, so we still never match on names
- **What:** If an application is missing its sponsor or its accommodation, it gets a case of its own. Without both, we can't know who else belongs with it, and we won't guess. In file 01, Clare Osborne's two applications form one case and the other six stand alone: 7 cases from 8 applications
- **What:** The case's council comes from the accommodation (falling back to the application's "Local authority of UK address", which is the same answer)

### 23. Case reference `AR-` plus a number, and a title made from the households' family names

- **Why:** References must be the same on every reseed, and caseworkers think in households. The number comes from ingest order on an empty database
- **What:** "Melnyk household", or "Kovalenko and Lysenko households" when two families share a case, listed in the order they applied

### 24. Case status reads "Checks required" until M4

- **Why:** The ticket asks for a status now. M4's rule gives "Checks required" for a case with no passing or failed checks, so this is the true value, not a placeholder that will change meaning
- **What:** All five case statuses have one wording and one tag colour each, ready for M4

### 25. "Today" is pinned to 2026-10-08 in `appsettings.json`

- **Why:** Ages (the youngest guest now, the Enhanced DBS rule in M4) must come out the same in every demo run. Tests pin 2026-10-01. Change `Clock:Today` to move it, or remove it to use the real date
- **What:** Ages are in whole years. Someone born on 29 February has their birthday on 1 March in other years, as UK law treats it, which also errs towards "under 18". A unit test caught .NET's default of 28 February

### 26. Ingest records a "Case formed" event for each new case

- **Why:** It feeds the "What just happened" page now (with links to the new cases) and the case timeline in M5. "Application received" events now carry their case too

### 27. Say "case" only, never "accommodation request" (replaces the `AR-` prefix in decision 23)

- **Why:** The team wants one term. The brief uses "accommodation request" as a synonym for case, which is where the `AR-` prefix and a sentence on the Cases page came from
- **What:** References are now `CASE-0003`. The Cases page sentence and a code comment no longer mention accommodation requests. CLAUDE.md has a new Terminology section so the term doesn't come back

## 2026-10-08 · GDS and accessibility review (after M3)

### 28. Audit every page with axe-core in headless Chrome, at desktop and phone width

- **Why:** The team asked for a full check of GDS styling and accessibility. Our journey tests cover structure (title, one h1, skip link, labels) but not colour contrast, ARIA or layout at phone width
- **What:** `tools/a11y-audit.mjs` drives headless Chrome over the DevTools protocol (no Playwright, no installs), runs axe-core for WCAG 2.2 AA plus best practice, and checks for sideways scrolling at 1280px and 390px. CLAUDE.md now says to run it before each milestone commit
- **Result:** 16 pages at 2 widths, no WCAG violations and no sideways scrolling

### 29. Accept axe's `region` best-practice note on the phase banner and back links

- **Why:** GOV.UK Design System guidance places both before `<main>`. GDS guidance wins over a best-practice rule. It is listed as a known issue in the accessibility statement

### 30. Fixes from the review

- **Focus contrast:** when "The Share" header link had keyboard focus, the tagline stayed light grey on the yellow focus background. It now turns black like the rest of the link. axe can't catch this because it doesn't apply focus; screenshots of tabbing through the page did
- **Table regions:** every scrollable table box was labelled "Scrollable table", so two on one page were indistinguishable. Each is now labelled by its own table caption. A table box is a keyboard stop only when it actually scrolls (decided in the browser, updated on resize), so desktop users don't tab through empty stops. Its focus ring gains a black inner edge so it shows on white
- **Right component:** "There are no more files to process" used the error summary, which GOV.UK reserves for form validation. It is now an "Important" notification banner
- **Right class:** the reference line under each case title used `govuk-hint`, a form-only class. It now uses a small `app-secondary-text` class in GOV.UK's secondary text colour

### 31. Add an accessibility statement, linked from the footer

- **Why:** Every GOV.UK service has one, and the scoring criteria reward a true sentence about accessibility
- **What:** It claims only what we have done and tested, says plainly that no specialist audit or assistive technology testing has happened, and lists the known issue

## 2026-10-08 · M4 Safeguarding checks

### 32. Case status is derived on every read by one pure function, never stored

- **Why:** The brief says the status recalculates whenever a check changes. Deriving it each time means it can never go stale, and the rule lives in one place (`SafeguardingRules.Derive`) with a unit test for every branch, in the brief's order: any failed check; all four done; checks 1 to 3 done; at least one passed; otherwise checks required
- **What:** "Done" means Passed or No Longer Required. "No longer required" on its own is not a pass, so it doesn't make a case "Checks partially completed"

### 33. A check gets a database row only when someone first updates it

- **Why:** No rows to create at ingest, nothing to backfill for existing cases, and a missing row simply reads "Not started"
- **What:** At most one row per case and check (a unique index). A failure reason is stored only while the check is Failed. A DBS type is stored only on check 3

### 34. The Enhanced DBS rule works in three places

- **Shown:** A case with any guest under 18 (on the pinned date) shows an orange "Enhanced DBS required" tag next to its status, on both the case list and the case page. The case page also has a warning naming each child and their age
- **Enforced:** Marking check 3 as passed requires a DBS type. On a case with a child, choosing "Standard DBS" is refused with an error
- **Re-checked:** If check 3 passed with a standard DBS and a child is later on the case, that pass counts as "In progress" and the case page says why. A standard DBS never completes check 3 on such a case, even after the fact

### 35. Check 4 is locked until a guest on the case has arrived

- **Why:** The brief says arrivals unlock it (M5). The rule is "any application on the case has visa status Arrived", already unit-tested, so M5 only has to set that status
- **What:** Check 4 shows "Available when guests arrive" with no Update link, and its update page redirects back to the case
- **Open for M5:** if arrivals turn out to be per person (the arrivals file has `PERSON_IDENTIFIER`), decide whether the first arrival unlocks the check or the whole household

### 36. Check updates write "Check updated" and "Case status changed" events

- **Why:** They feed the M5 timeline. A status change is only logged when the derived status actually moves
- **What:** The pinned clock now fixes only the date. Times on user actions use the real time of day, so timeline entries made during a demo show sensible times

### 37. No conditional reveal on the check form: the failure reason is a plain field

- **Why:** GOV.UK Frontend's script adds `aria-expanded` to the radio that reveals extra content, and axe flags that as a critical ARIA error. A plain textarea with the hint "Only needed if the check failed" is simpler, has no ARIA problem, and works without JavaScript

### 38. The service uses no JavaScript at all

- **Why:** The team asked us to avoid JavaScript where we can, for accessibility. GOV.UK Frontend is built to work without it, and every page is server-rendered
- **What:** Removed the GOV.UK Frontend script and `initAll`, the `js-enabled` body class, every `data-module` hook, the mobile "Menu" button, and our table script. On a phone the navigation shows as a plain list. Error summaries and banners still announce through `role="alert"`, and their links still go to the fields. Each wide table is now always a labelled, keyboard-reachable region (the standard no-JavaScript pattern), at the cost of one extra tab stop per table on desktop. The accessibility statement says the service uses no JavaScript
- **Replaces:** the "table is a tab stop only when it scrolls" part of decision 30, which needed a script
