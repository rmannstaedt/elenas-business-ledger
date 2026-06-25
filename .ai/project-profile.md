# Project Profile

## Project identity

Working name: My Small Business Ledger

Internal namespace/project prefix:
MySmallBusinessLedger

## Target framework and platform

Target framework: .NET 10
Platform: Windows desktop
UI: WPF
Architecture: local-first desktop app

## Solution shape

src/
  MySmallBusinessLedger.App
  MySmallBusinessLedger.Core
  MySmallBusinessLedger.Infrastructure

tests/
  MySmallBusinessLedger.Tests

## Persistence and storage

Database: SQLite
Database access: EF Core SQLite
Database schema evolution: EF Core migrations, applied by the WPF startup bootstrapper before repositories are used
Plan catalog: stored in SQLite with Draft, Active, and Retired lifecycle state; Draft plans are editable/deletable and may be incomplete, publishing requires all fields except Retire on, Active plans are immutable/assignable except for name-only rename, Retire on scheduling, and immediate retirement, and Retired plans are immutable/reactivatable except for name-only rename but not assignable
Student plan periods: stored with a required catalog plan identity plus copied commercial-term snapshots including price, cadence, duration, flexibility, student count, effective date, payment terms, and start/end dates; catalog lifecycle changes and name-only renames must not rewrite copied commercial terms or issued invoice data
Issued invoice lines: stored with catalog plan identity for lineage plus issued-time plan name and price snapshots for durable invoice display/PDF output; issued invoice company snapshots include the bank account, MobilePay account, and payment instructions used at issue time; old invoice display must use the stored invoice snapshot, not current catalog names or derived rename history
Invoice PDFs: stored as files, not BLOBs
Invoice number and PDF filename display: invoice numbers are formatted with at least three digits, and final PDF filenames use `<invoice number>-<student full name>.pdf`, for example `001-Mira Vale.pdf`
Invoice PDF generation: QuestPDF; Danish invoice PDFs use a toner-light header with `studio.example.test`, show the invoice period/month, group lesson services by issued plan and price, list all included lessons in one clear list, keep the invoice to one page by flowing longer lesson lists into multiple columns in the middle lesson area, use `CVR` for the CVR label, include configured bank/MobilePay account lines and payment instructions when present, and add a plan cut-over note when a single invoice covers a mid-month plan change
Invoice PDF safety: reserve invoice metadata as `Issuing`, write to staging, verify PDF bytes, publish without overwriting, finalize the invoice as `Issued`, then request print; staging/write/verify/publish failures before final PDF publication must discard any staged PDF and release the reserved number so retries do not skip invoice numbers
Invoice printer: configured from `Application > Settings`, stored in SQLite company profile data, selectable from installed Windows printers when enumeration is available, typeable, changeable, and clearable; if missing during first invoice print/reprint, the app opens the printer selection dialog directly and remembers the selected printer before continuing; `Printed` means the configured printer existed and the Windows print request was launched without exception, not that physical output or spooler completion was verified
Google Calendar account tokens: stored locally with Windows user data protection, never in SQLite or plaintext config
Google Calendar selected calendar/account label: stored in SQLite app settings; the main-window browser launcher uses the configured account email as a Google Calendar account hint
Runtime app data folder: `%APPDATA%\visualtech.dk\MySmallBusinessLedger`
Runtime SQLite database path: `%APPDATA%\visualtech.dk\MySmallBusinessLedger\ledger.db`
Runtime startup/composition: `App.xaml.cs` delegates database migration, service graph creation, startup jobs, and main-window construction to App-layer startup/bootstrapper classes
Publisher Google OAuth Desktop client configuration: local/release file `src\MySmallBusinessLedger.App\Configuration\google-oauth-client.json`, copied to app output under `Configuration\google-oauth-client.json` when present; the real file is ignored by Git and the committed template is `src\MySmallBusinessLedger.App\Configuration\google-oauth-client.template.json`
Google OAuth protected token folder: `%APPDATA%\visualtech.dk\MySmallBusinessLedger\GoogleTokens`
Window placement file path: `%APPDATA%\visualtech.dk\MySmallBusinessLedger\window-placement.json`
Grid column width file path: `%APPDATA%\visualtech.dk\MySmallBusinessLedger\grid-column-widths.json`
Backup: no automated backup in version 1.0; keep persistence design compatible with later manual backup/export

## External integrations

Google Calendar API: official Google Calendar .NET client
Google authentication: external browser OAuth
Google OAuth app identity: publisher-owned Desktop OAuth client; users authorize their own Google account at runtime
Google OAuth Calendar scope: request `https://www.googleapis.com/auth/calendar.events.owned`, not broader all-calendar event scopes; the configured calendar should be owned by the signed-in Google account, and app-level lesson matching still ignores events whose first summary line does not match the `$` student marker
Google Calendar event time zones: write payloads must use Google/IANA time zone IDs; convert Windows local time zone IDs such as `Romance Standard Time` before creating or moving events
Google Calendar completed-lesson marker: keep the first line as `$ <student short name>` or `$<student short name>`; Completed is represented by a later `Status: Completed` summary line, and absence of that line means Planned
Google Calendar population: main-window `Populate Calendar` seeds lessons for the selected student from saved student schedule entries; current month starts at today inclusive, future months seed the whole month, past months cannot be seeded, exact same-student same-date/time duplicates are skipped, and v1 does not detect other conflicts
Calendar availability: invoice issue must rely on the accepted invoice preview/snapshot rather than requiring a fresh Google Calendar read at the final print/save moment
Write Invoice selection: when opened from the main window, the candidate dialog must prefer the currently displayed student when available before falling back to today/time-based invoice candidates
PDF generation: QuestPDF Community license accepted for v1

## UI policy

WPF + MVVM
No business logic in code-behind
Danish user-facing invoice text
My-facing UI must stay simple
Company profile, two-line company address entry, bank/MobilePay accounts, invoice folder with typed or folder-picker selection, invoice printer with remembered selector/typed value/clear action, invoice payment instructions selected from `Betales ved modtagelse`, `8 dage fra fakturadato`, `30 dage fra fakturadato`, or custom text, Google calendar/account label, Google sign-in app status, local Google authorization status, connect/reconnect controls, and read-only app data/database paths are configured or shown from `Application > Settings`; the Google calendar defaults to `primary`
Student editor uses two stacked address fields and stores them as one multiline student address value.
Student management is available from `Student > Manage Students`; it lists active and inactive students, embeds the Student editor for selected-student fields/current assigned plan/plan assignment/schedule, keeps explicit Save/Cancel, opens invoices on double-click through the existing viewer, mirrors student selection into the main window immediately and on close, and persists `Student.IsActive` changes only when the embedded editor is saved.
Main-window `Add Lesson` creates an editable draft row; when both date and time are valid the draft autosaves to Google Calendar, and incomplete draft rows must survive automatic refreshes. Main lesson row time selectors offer 08:00 through 20:00 in 15-minute increments while still allowing typed time input.
Main-window student arrows navigate by lesson occurrence date/time, not alphabetically by student name; Google Calendar lessons in the loaded previous/current/next month window define the occurrence order, the same student may appear more than once, the target lesson is selected, and the displayed month changes when the target occurrence is in another month. The student-name dropdown lists other active student short names alphabetically and switches directly to that student without selecting a lesson. Saved standard schedules provide fallback occurrences only when that loaded window has no Google Calendar lessons.
Plans are managed from `Plan > Manage Plans`; new Draft plans start with blank name/price, default lesson/duration/student/flexibility values, payment terms selected from `Paid monthly in advance` or `Paid per lesson`, and an effective date set to the first day of the current month. `Add Plan` saves changed Draft data before creating the next Draft, and changed Active `Retire on` values are prompted before adding. Active and Retired plan names may be renamed through a name-only save action; the rename is stored in plan-name history and does not unlock commercial fields. The student editor offers only publish-complete Active catalog plans, stores the selected plan identity, and copies the selected plan's commercial terms into the student's plan period. The Manage Plans window remembers its size/position and list column widths.
Desktop windows must remember size and position, including subwindows, and restore inside the current visible screen area

## Build and test commands

```powershell
dotnet build MySmallBusinessLedger.sln -nodeReuse:false
dotnet test MySmallBusinessLedger.sln -nodeReuse:false
```

## Test policy

Unit tests: xUnit
UI automation: FlaUI later, where practical
Use IClock for time-sensitive logic
Use fake Google Calendar client for calendar integration tests

## Hard requirements

- Do not mark invoice issued unless PDF storage succeeds.
- Do not mark invoice printed unless the configured printer exists and the Windows print request launches without exception; do not imply physical output or spooler completion has been verified.
- Do not silently overwrite an existing issued invoice PDF.
- Do not consume or skip an invoice number, or leave a staged PDF behind, when invoice staging, PDF writing, verification, or final PDF publish fails before the final PDF is published.
- Do not silently edit issued invoices.
- Do not derive old invoice display text from current plan catalog names or rename history; issued invoice snapshots are authoritative for invoice text.
- Do not charge VAT when business profile says NotRegistered.
- Do not store CPR numbers.
- Ignore Google Calendar entries that do not use the documented first-line `$` student marker.
- Preserve the first-line Google Calendar marker when adding/removing app-owned lesson status lines.
- Convert Windows time zone IDs to Google/IANA time zone IDs before writing Google Calendar event start/end payloads.
- Notify the user immediately when a Google Calendar lesson update fails.
- Automatic refresh must not discard incomplete main-window lesson drafts.
- Do not silently create duplicate Google Calendar lessons for the same student at the same date/time when using Populate Calendar.
- Do not imply Populate Calendar detects all conflicts; v1 skips exact duplicates only.
- Do not make Google Calendar sync failure corrupt local data.
- Do not persist OAuth access or refresh tokens in plaintext.
- Do not allow Draft or Retired catalog plans to be assigned to students.
- Do not mutate existing student plan period commercial-term snapshots or issued invoices when a catalog plan is edited as Draft, published, retired, reactivated, or renamed; plan renames may update catalog display names and plan-name history while student periods keep referring to the same catalog plan identity.
- Deactivating or reactivating a student must not mutate that student's plan periods, schedule, or issued invoices.
