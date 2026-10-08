
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
