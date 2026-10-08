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

### 39. Caseworkers can record an arrival, which unlocks check 4 (builds on decision 35)

- **Why:** The team wants council users to be able to say guests have arrived, not just wait for the Home Office arrivals feed. The feed only says someone reached the UK, not that they're at the accommodation, and some arrivals may never appear in it
- **What:** Check 4 has a "Record arrival" link that opens a GOV.UK date input (day, month, year, plain HTML): "When did the guests arrive at the accommodation?" The date must be real and today or earlier, with GOV.UK's standard date error messages. Once recorded, check 4 opens. The case shows "Guests arrived on … (recorded by a caseworker)" with a "Change arrival date" link, and the timeline gets "Guests arrived" (or "Arrival date changed")
- **M5:** an arrival in the feed will also unlock check 4. Recording an arrival opens the check but doesn't pass it; the caseworker still sets check 4's status
- **Not yet:** which caseworker recorded it. There are no users until M6
- **Turned down:** leaving check 4 open all the time, which would allow "guests arrived" to pass before anyone had arrived

### 40. Tags in tables wrap instead of being forced onto one line (replaces the nowrap rule from M3)

- **Why:** The team spotted "Enhanced DBS required" being cut off in the case list. Forcing tags onto one line made them wider than the Status column, so they spilled out of the table's scroll box
- **What:** Tags in tables have no width cap, so the table sizes the Status column to fit them. A tag wraps inside its cell only when space really runs out (checked at 1280px, 1000px and 390px)

## 2026-10-08 · M6 Who sees what

### 41. Three demo users, chosen on the demo controls page and remembered in a cookie

- **Why:** The brief rules out real authentication. A "User type" choice on the demo controls page is one click during the demo, and a cookie keeps it across pages without any sign-in machinery
- **What:** Central admin (sees everything), Birmingham council user and Exeter council user. With no cookie, and for console commands and seeding, the user is the central admin, so nothing changes for anyone who never switches. Every page shows who you are signed in as, with a tag for the council and a "Change user" link back to the demo controls
- **Turned down:** a login screen, and a user picker in the header, which would crowd it. We also did not hide the demo controls from council users: anyone must be able to drive the demo

### 42. Row-level scoping is done once, as EF Core global query filters on the database context

- **Why:** The brief says scoping runs through every list, detail page and search. Filtering in each page would be easy to forget on the next page we add. A global filter means every query is scoped, including counts on the home page, related records on detail pages, and searches when we build them
- **What:** An application, its guests and its timeline events belong to the application's council (the accommodation address, the same council as their case). An accommodation belongs to its own council. A sponsor or host belongs to every council they have an application in, and their page lists only the applications in yours. A record with no council is seen by the central admin only
- **Turned down:** a `Where` on each page, and a separate council column on guests and people, which would copy data we already hold

### 43. Another council's record is "Page not found", not "Forbidden"

- **Why:** Saying "you can't see this" confirms the record exists. Because of the filters, the record simply isn't found, and the page returns 404 with a GOV.UK "Page not found" page that points to the demo controls to change user

### 44. Ingest ignores the scoping; the ingest summary is scoped

- **Why:** Matching sponsors, accommodations and repeat submissions must see every council's records, or a council user clicking "Process next file" would create duplicates. The "What changed" list on the ingest page only links to records you can open, with a note saying so; the counts cover the whole file

### 45. Cases are scoped by their own council (after merging M3)

- **Why:** M3 landed on main while M6 was on its branch. A case carries its council (from the accommodation), so it gets the same filter as everything else, and the Cases list, case pages and the home page count are scoped with no page changes
- **What:** A "Case formed" event is seen only by the case's council, as "Application received" events are by the application's. Ingest also reads cases with the filters off, so a council user processing a file never creates a duplicate case

### 46. Safeguarding checks are scoped through their case (after merging M4)

- **Why:** A check has no council of its own. Checks are only ever loaded with their case, which is filtered, so viewing or updating a check on another council's case is "Page not found". "Check updated" and "Case status changed" events carry the case, so the event filter scopes them too
- **Turned down:** a separate filter on checks, which would repeat the case's council for no gain

## 2026-10-08 · Merging M6 into main

### 47. Regenerate the model snapshot after M21 and M24 merged without it

- **Why:** The notifications (M21) and announcements (M24) pull requests each added a migration but not the matching model snapshot change. EF then reported "pending model changes" at startup and every journey test failed, on main as well as here
- **What:** The snapshot now includes both tables. No new migration: the M21 and M24 migrations already create exactly those tables and indexes, so a new one would try to create them twice. A teammate made the same fix on main at the same time; the two merged cleanly into one snapshot

## 2026-10-08 · Fixes after merging M21

### 48. Fix structural problems on the merged notifications page (M21 groundwork), leaving its TODOs alone

- **Why:** Once M21 was merged, the team asked for the page to be fixed. It wrapped itself in a second `<main>` inside the layout's, set its title to "Notifications - The Share - GOV.UK" (so the layout added the suffix again), and its banner had a header but no content
- **What:** One `<main>` (from the layout), the title "Notifications", and a standard "Important" notification banner holding the unread count. The unfinished parts (notification list, mark as read, the bell partial, the current user) stay as the M21 team left them
- **Guard:** the shared accessibility test now fails any page with more than one `<main>` or a doubled title, and covers the Notifications and Announcements pages

## 2026-10-08 · Demo extras

### 49. A "Do not press" button in the header (a rickroll)

- **Why:** The team wanted a joke for the demo
- **What:** A link styled as GOV.UK's red warning button, on the right of the header. It stays a plain link, not `role="button"`: it takes you somewhere, and without JavaScript the Space key wouldn't work on a link pretending to be a button. On a phone it drops below the logo at full width. It uses GOV.UK's yellow focus style and is reached by keyboard straight after the logo
- **What:** It goes to our own page, `/do-not-press` ("We told you not to press it"), which embeds the video with YouTube's privacy-enhanced player (`youtube-nocookie.com`, autoplay, no related videos) and a titled `iframe`. The team didn't want a link to YouTube itself, where ads and the YouTube page would spoil the joke. YouTube may still play an ad before an embedded video; hosting the file ourselves would avoid that, but we don't have the rights
- **What:** GOV.UK's header container has a clearfix `::after`, which counted as a flex item and pulled the button into the middle; it is switched off for this header
- **Fix:** the first try showed "This video is unavailable". Testing variants side by side showed YouTube refuses embeds on pages served from a bare IP address (`127.0.0.1`) but plays them on `localhost` and real hostnames. The page now redirects from `127.0.0.1` to `localhost`, CLAUDE.md says to run on `localhost`, and the page has a "Watch it on YouTube instead" link as a fallback
- **Turned down:** going back to a plain link to YouTube, where the ads and YouTube page would spoil the joke

## 2026-10-08 · Home page

### 50. The home page is a welcome banner, then announcements, then tiles

- **Why:** The team asked for a welcoming front page in that order. Tiles give one obvious way into each part of the service, and their counts follow the signed-in council, so a council user sees their own workload at a glance
- **What:** The welcome banner is GOV.UK brand blue with white text and holds the page's one h1. Announcements sit in a single standard notification banner (not the green success one, which GOV.UK keeps for confirming an action) and it always shows, saying "There are no announcements" when there are none. Tiles are a list of links; the whole tile is clickable, with the heading link as the one keyboard stop
- **Turned down:** a GOV.UK panel for the welcome, which is meant for confirmation pages, and hiding the announcements banner when empty, which would make it vanish in the demo

### 51. The "Dev Share" announcement is part of the demo seed, and the tiles lose their heading

- **Why:** An announcement typed in on screen would vanish at the next reset. Putting it in `DemoSeeder` as a scripted action, applied through the same announcement service as the admin page, means it shows on every run
- **What:** "Dev Share: Built from scratch in one hour" is added after every reset, including "Reset to empty", because an announcement is a message to users, not casework data. The "What do you want to do?" heading is removed at the team's request; each tile title becomes an h2 so headings still go down in order, and the tile list is labelled "Parts of the service" for screen readers
- **Turned down:** writing the announcement straight into the database, which the reset would clear

## 2026-10-08 · M5 Guests move

### 52. Each matched arrivals row is stored as a decision update; the visa status is re-derived from all of them

- **Why:** The brief's precedence rule (Arrived > Issued > Withdrawn > Refused > Confirmed) is about conflicting updates over time, so we keep every update and derive the status from the whole set, rather than overwriting it with the latest row
- **What:** `VisaStatusRules.FromUpdate` maps one decision (Issued or GRANT with an arrival time is Arrived, without one is Issued, Voided or anything unknown is Confirmed). `Derive` takes the highest-precedence status, or Pending when there are none. Both are unit-tested for every case
- **Idempotent:** a row already stored for that file (same file, same row number) is counted as "already ingested", not applied twice

### 53. Match arrivals by GWF first (the application's or any guest's), then by UAN; skip what matches nothing

- **Why:** The brief's rule. Some rows carry only one of the two references
- **What:** Skipped rows go in the ingest summary with a reason: "No application matches this GWF or UAN", "No GWF or UAN to match on", or a date that won't parse. In file 02, `GWF000000001` is skipped and the Tkachenko row matches on UAN alone

### 54. An arrival applies to the whole application, and unlocks check 4

- **Why:** Only lead applicants have a GWF number, and `PERSON_IDENTIFIER` does not link to anyone in the application files, so the feed can't tell us which family member landed. We say "Kateryna Melnyk and 2 family members arrived", not claim to know each person
- **What:** Arrival times in the feed are UK local time (dd/MM/yyyy HH:mm:ss), converted to UTC across GMT and BST, and shown back in UK time. Port codes are shown with names ("Luton (LTN)"). Check 4 opens and the case page shows where and when they landed. As with a caseworker's arrival (decision 39), the check is opened, not passed

### 55. The case timeline is the event log, newest first, in the shape of the MOJ timeline pattern

- **Why:** M5 Ticket 2. The event log has been recorded since M1 (decision 6), so this was mostly rendering
- **What:** Application received, case formed, visa status changed, guests arrived, check updated and case status changed. Each entry has a heading, a `<time>` element and a short description, linking to the application where there is one. Plain HTML and CSS, no JavaScript
- **What:** Events with the same timestamp are ordered by what logically happens first (received, then case formed, then status, arrival, check and case status), not by database ID, which EF does not guarantee. The ingest summary uses the same order, oldest first

### 56. Files 03 to 06 replay through "Process next file"; file 07 (offers) waits for M12

- **Why:** M5 says both feed types should replay in number order. Files 03 to 06 are visa applications and arrivals only, and ingest cleanly: 20 applications, 15 cases and 19 decision updates, with one deliberately unmatched row
- **Open:** file 07 is the first offers-of-accommodation file. "Process next file" stops there with a clear message until offers are built (M12), so files 08 onwards wait too

## 2026-10-08 · Case page side navigation

### 57. The case page has a side navigation with its sections and the actions you can take now

- **Why:** The case page grew to five sections. The team asked for accessible side navigation, including links to each action flow
- **What:** On the left, in the shape of the MOJ side navigation: "Case sections" (summary, safeguarding checks, guests, visa applications, case history) jump to headings on the same page, and "Actions" links to each flow ("Update check 1: accommodation exists", "Record arrival" while check 4 is locked, "Change arrival date" once recorded). It is a `<nav aria-label="Case">` landmark with real links and descriptive link text. It sticks while scrolling on wider screens (CSS `position: sticky`) and sits above the content on a phone. No JavaScript
- **Turned down:** GOV.UK tabs, which need JavaScript (we use none) and would hide most of the case during the demo. Also turned down: a separate page per section, which would let the navigation show where you are but costs a click per view. Worth revisiting if the case page keeps growing
- **Bug caught by a test:** lower-casing check names for link text turned "DBS" into "dBS"; acronyms now keep their capitals

## 2026-10-08 · M12 Fresh offers

### 58. Offers (expressions of interest) are their own records, ingested from files 07, 12 and 18

- **What:** One `Offer` per submission reference, so reprocessing never duplicates. The offer's council is taken from its town or city: the files give no council, and both councils are cities (Topsham's offer gives "Exeter"). Offers are scoped by council like everything else, and listed on a new Offers page, open first
- **Result:** All 20 data files now run end to end through "Process next file": 14 offers, 59 guests, and only the two deliberately unmatched arrivals rows skipped

### 59. A case needs a rematch when any of its safeguarding checks has failed

- **Why:** The brief says a failed check or a withdrawn sponsor breaks a sponsorship. A failed check is the signal we have. A withdrawn visa is the guest withdrawing, not the sponsor, so it does not count
- **What:** A red "Needs rematch" tag on the case list and case page, a warning on the case page, and a "Rematch to an open offer" action in the side navigation

### 60. Rematch is two GOV.UK steps: choose an offer ranked by fit, then confirm

- **What:** Offers are ranked by room for the household (children can use an adult's bed, not the other way round), then the same council, then available now, then the tightest fit, then soonest available. Each option says why ("Room for this household of 1 adult and 2 children", "Same council (Exeter)", "Available now"), and unsuitable offers are still listed but labelled "Too small". The confirm page is a check-answers summary, with a warning that all checks will reset
- **On confirm:** the case gets the offer's accommodation (reused if we already hold that address) and its host as a `Person` (matched by email, like a sponsor). The case's council follows the new address. All four checks and any caseworker-recorded arrival are reset. The offer is marked taken by the case, and the timeline records the move and the status change
- **Turned down:** matching households to offers automatically. The brief asks for a caseworker to pick and confirm

### 61. Cases grow as later files add families to the same sponsor and address

- **Seen in the data:** by file 08 the Melnyk case becomes "Melnyk and Romanyuk households", and a third family joins in file 19. Tests now find cases by the start of their title rather than an exact one, and rematch tests assert the best-ranked offer rather than a fixed address

## 2026-10-08 · M14 Suggested duplicates ★

### 62. Duplicate guests are suggested by five rules, ranked by confidence

- **What:** High: same passport number, or same name and date of birth. Medium: names 1 or 2 letters apart (Levenshtein distance) with the same date of birth, or passport numbers 1 character apart. Low: same family name and given names at most 1 letter apart, but different dates of birth. Each suggestion lists every reason in words. Pairs rank by their best confidence, then by a score summing all reasons
- **What:** Names are compared after normalising (case, accents, apostrophes, hyphens and spacing), so only real spelling differences count. Family members on the same application are never paired. Levenshtein is a small pure function with unit tests for inserts, deletes, substitutions and transliteration
- **In the data:** we searched all 59 guests by eye and by script. There are two real pairs, both arriving in file 05: Olena Kovalenko and Olena Kovalenco (same passport, one letter apart) and two Dmytro Shevchenko records (passports one character apart). The rules find both and nothing else. A looser search turned up only unrelated people
- **Turned down:** grouping candidates before comparing. With 59 guests, comparing every pair is instant and simpler

### 63. "Not a duplicate" is remembered against a stable pair key; accepting marks one record as the duplicate of the other

- **What:** The pair key is each guest's application UAN and position on it, so a dismissed pair stays dismissed as more files arrive (tested by ingesting file 06 after dismissing). A reset clears dismissals, as planned in CLAUDE.md
- **What:** To accept, the reviewer chooses which record to keep (GOV.UK radios, with an error if none is chosen). The other guest gets "Duplicate of", shown as a tag on the guests list and an inset on its page linking to the kept record, and both cases' timelines record it. We did not build M13's full merge
- **Scoping:** a council sees only pairs within its own council; central admin sees all

### 64. The test form helper now sends only checked radios and checkboxes

- **Why:** It sent every radio's value, so a "nothing chosen" test actually sent a choice. Earlier tests always set radio values explicitly, so they never noticed
