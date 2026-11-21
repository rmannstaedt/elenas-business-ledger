# Project Profile

## Project identity

Working name: Elena's Business Ledger

A Windows app for managing student lessons, invoices, and payments.

Current app version: 1.4.0

Internal namespace/project prefix:
ElenasBusinessLedger

## Target framework and platform

Target framework: .NET 11. IDE builds require Visual Studio 18.12 or later.
Platform: Windows desktop
UI: WPF
Architecture: local-first desktop app
Installer/distribution: single-file Inno Setup installer with a self-contained .NET 11 build for Windows x64. The `scripts\build-installer.ps1` script creates `artifacts\installer\ElenasBusinessLedger-1.4.0-Setup.exe`. The installer build stages each attempt separately and replaces the same-named output only after a successful Inno compilation and candidate check. The per-user installer installs to `%LOCALAPPDATA%\Programs\MySmallBusinessLedger`, creates a Start menu shortcut, offers an optional desktop shortcut, has no auto-update behavior, and must preserve `%APPDATA%\visualtech.dk\MySmallBusinessLedger` during install, replacement, and uninstall.

## Solution shape

src/
  ElenasBusinessLedger.App
  ElenasBusinessLedger.Core
  ElenasBusinessLedger.Infrastructure

tests/
  ElenasBusinessLedger.Tests

## Persistence and storage

Database: SQLite
Database access: EF Core SQLite 11.
Database schema evolution: EF Core migrations, applied by the WPF startup bootstrapper before repositories are used
Plan catalog: stored in SQLite with Draft, Active, and Retired lifecycle state; Draft plans are editable/deletable and may be incomplete, publishing requires all fields except Retire on, Active plans are immutable/assignable except for name-only rename, Retire on scheduling, and immediate retirement, and Retired plans are immutable/reactivatable except for name-only rename but not assignable
Student plan periods: stored with a required catalog plan identity plus copied commercial-term snapshots including price, cadence, duration, flexibility, student count, effective date, payment terms, and start/end dates; catalog lifecycle changes and name-only renames must not rewrite copied commercial terms or issued invoice data
Issued invoice lines: stored with the exact local lesson identity, catalog plan identity for lineage, plus issued-time plan name and price snapshots for durable invoice display/PDF output; issued invoice company snapshots include the bank account, MobilePay account, and payment instructions used at issue time; old invoice display must use the stored invoice snapshot, not current catalog names or derived rename history; schema upgrades preserve older unlinked line details in `ArchivedInvoiceLines` and show a startup warning. Invoice history uses the stable student ID and retains invoice-time names and addresses
Invoice PDFs: stored as files, not BLOBs
Invoice number and PDF filename display: invoice numbers are formatted with at least three digits, and final PDF filenames use `Invoice <invoice number> <period> - <student full name>.pdf`, for example `Invoice 001 Juni 2026 - Mira Vale.pdf`
Invoice PDF generation: QuestPDF; Danish invoice PDFs use a toner-light header, show the invoice period/month, group lesson services by issued plan and price, list all included lessons in a clear indented lesson area that stays in one column through 10 lessons and uses two columns above 10 lessons, use `CVR` for the CVR label, include configured bank/MobilePay account lines and payment instructions when present, keep the invoice to one page, and add a plan cut-over note when a single invoice covers a mid-month plan change
Invoice VAT calculation: VAT applicability is based on the invoice period start date, not the invoice issue/print date
Economy Overview reporting: available from `Economy > Overview`; reads `Issued` SQLite invoice rows, stored invoice totals, a stored first application-backed invoice month, and explicit legacy manual month overrides, not stored PDF files; filters by invoice period start year, groups displayed month totals by invoice period start month/quarter, shows invoice number/PDF filename/net/VAT/total, shows selected-year plus four-prior-year totals, leaves month/quarter/year amount cells blank where no displayed total exists, exports one sectioned machine-friendly CSV with a `Source` column, and opens the stored PDF on invoice-row double-click. Manual month overrides are DKK-only replacement net/VAT totals before the cut-over month; they do not create invoice rows, invoice numbers, lesson rows, or PDFs. The cut-over controls where manual editing is allowed, not whether real issued invoices remain visible; issued invoices before the cut-over display read-only unless the same month also has a manual override. Mixed manual/application-backed months must fail visibly instead of being silently combined or hidden. Blocking economy data errors keep returned overview rows visible and disable export; Admin Mode keeps manual save/cancel actions available when overview rows are visible so invalid manual overrides can be removed. Saving manual totals persists only editable legacy manual months and clears stale manual overrides for months backed by issued invoices.
Invoice PDF safety: reserve the complete invoice snapshot as `Issuing` and render from it. Stage and verify the PDF, save its intended location and checksum, publish without overwriting, finalize as `Issued` with lesson locks, then request print. A failure before the checkpoint releases the number. After the checkpoint, retry verifies the existing staged or final PDF and keeps the same invoice. The preview offers explicit choices if a retry changes invoice details.
Invoice printer: configured from `Application > Settings`, stored in SQLite company profile data, selectable from installed Windows printers when enumeration is available, typeable, changeable, and clearable; if missing during first invoice print/reprint, the app opens the printer selection dialog directly and remembers the selected printer before continuing; the invoice preview stays open during issue/print, blocks Cancel/window close from hiding an in-progress result, and prompts after an extended wait to confirm the user still wants to wait; `Printed` means the configured printer existed and the Windows print request was launched without exception, not that physical output or spooler completion was verified
Google Calendar account tokens: stored locally with Windows user data protection under collision-resistant hashed token-key filenames, never in SQLite or plaintext config; legacy escaped token filenames are read and migrated to hashed filenames on successful lookup
Google Calendar selected calendar/account label: stored in SQLite app settings; the main-window browser launcher uses the configured account email as a Google Calendar account hint
Runtime app data folder: `%APPDATA%\visualtech.dk\MySmallBusinessLedger`
Runtime SQLite database path: `%APPDATA%\visualtech.dk\MySmallBusinessLedger\ledger.db`
Runtime startup/composition: `App.xaml.cs` delegates database migration, service graph creation, startup jobs, and main-window construction to App-layer startup/bootstrapper classes
Publisher Google OAuth Desktop client configuration: local/release file `src\ElenasBusinessLedger.App\Configuration\google-oauth-client.json`, copied to app output under `Configuration\google-oauth-client.json` when present; the real file is ignored by Git. See the [setup guide](../docs/setup.md#set-up-a-google-oauth-client).
Google OAuth protected token folder: `%APPDATA%\visualtech.dk\MySmallBusinessLedger\GoogleTokens`
Window placement file path: `%APPDATA%\visualtech.dk\MySmallBusinessLedger\window-placement.json`
Grid column width file path: `%APPDATA%\visualtech.dk\MySmallBusinessLedger\grid-column-widths.json`
Backup: version 1.4.0 verifies a SQLite backup in the app data folder's `Backups` directory before migrating an existing database. Copy the invoice folder separately for a full backup.

## External integrations

Google Calendar API: official Google Calendar .NET client
Google authentication: external browser OAuth only for explicit Settings connection/reconnection; startup and automatic dashboard refresh use saved local credentials non-interactively and do not launch browser consent
Google OAuth app identity: publisher-owned Desktop OAuth client; users authorize their own Google account at runtime
Google OAuth Calendar scope: request `https://www.googleapis.com/auth/calendar.events.owned`, not broader all-calendar event scopes; the configured calendar should be owned by the signed-in Google account, and app-level lesson matching still ignores events whose first summary line does not match the `$` student marker
Google Calendar event time zones: write payloads must use Google/IANA time zone IDs; convert Windows local time zone IDs such as `Romance Standard Time` before creating or moving events
Student selection, billing, and linked lesson ownership use `StudentId`. Calendar import resolves new titles by trimmed, case-insensitive short name and leaves ambiguous matches unlinked. Calendar writes hold affected lessons out of billing until the acknowledged result is persisted or reviewed. Appointments without an applicable plan remain visible pending explicit assignment.

Invoice sequencing uses a single new reservation per SQLite ledger and exposes the oldest pending item through `Invoice > Resume Invoice`. Publication checkpoints protect the allocated number and staged/final PDF. Issuance is durable before printing, and a failed print request can be retried on the same invoice.

Google Calendar completed-lesson marker: keep the first line as `$ <student short name>` or `$<student short name>`; active student short-name matching is case-insensitive and canonicalizes to the stored student short-name casing; Completed is represented by a later `Status: Completed` summary line, and absence of that line means Planned
Lesson source of truth: Google Calendar controls scheduling for uninvoiced lessons. Local SQLite `Lessons` rows hold billing data and are retained when an event is missing from a refresh. A calendar-scoped event key protects existing links when the configured calendar changes. App-initiated lesson population, date/time edits, deletes, and planned/completed state changes must update the exact uninvoiced local row before reporting success; refresh/reconciliation is a recovery path, not the ordinary correctness path. Invoiced lessons are locked locally and later Google Calendar edits/deletions must not alter billed/auditable lesson state. Lesson month/window repository queries must be bounded in SQLite before applying exact local-date filtering in memory.
Google Calendar population: main-window `Populate Calendar` uses the selected student's saved schedule for the full displayed month and the plan in effect on each occurrence date. It skips exact matching Google Calendar lessons and invoice-locked local slots for the selected student. Created calendar lessons are saved to local SQLite before success is reported. Version 1 does not detect other conflicts.
Calendar availability: invoice issue must rely on the accepted invoice preview/snapshot rather than requiring a fresh Google Calendar read at the final print/save moment
Write Invoice selection: when opened from the main window, the candidate dialog must prefer the currently displayed student when available before falling back to today/time-based invoice candidates
PDF generation: QuestPDF Community license accepted for v1

## UI policy

WPF + MVVM
No business logic in code-behind
Danish user-facing invoice text
My-facing UI must stay simple
Settings **Save** uses one SQLite transaction for company, calendar, and Economy changes. Browser OAuth uses the explicit connection action.

Company profile, two-line company address entry, bank/MobilePay accounts, invoice folder with typed or folder-picker selection, invoice printer with remembered selector/typed value/clear action, invoice payment instructions selected from `Betales ved modtagelse`, `8 dage fra fakturadato`, `30 dage fra fakturadato`, or custom text, Google calendar/account label, Google sign-in app status, local Google authorization status, connect/reconnect controls, read-only app data/database paths, the durable first application-backed invoice month, and session-only Admin Mode for Economy Overview manual editing are configured or shown from `Application > Settings`; Admin Mode starts unchecked on each application startup and must be explicitly saved for the current session before Economy Overview allows manual month edits; first application-backed invoice month input is normalized to the first day of the selected month; the Google calendar defaults to `primary`; `Application > About Elena's Business Ledger` shows the app name and current version from assembly metadata
Student editor uses two stacked address fields and stores them as one multiline student address value.
Student management is available from `Student > Manage Students`; it opens a Students list window plus a separate Student Details window that share the same selected-student state. The main window `Student Details` button opens the same details window for the currently displayed student. The list window shows active and inactive students and `Add student`; the fixed-size details window shows selected-student fields, current assigned plan, plan assignment, schedule, and invoices. Selecting a student in the list updates the details window immediately, Save closes Student Details after a successful save, Cancel discards edits and closes Student Details, the title-bar close button prompts when there are unsaved edits, invoice double-click opens the existing viewer, selection mirrors into the main window immediately and on close, and `Student.IsActive` changes persist only when the details editor is saved.
Main-window `Add Lesson` defaults to today in the current month or the first day of another displayed month. It uses the student's first saved slot for the weekday or the first saved slot overall, and the assigned plan in effect on the draft date when it is selectable. A draft with a valid date and plan saves on that click, with selection restored by the created calendar event id. If a plan is unavailable for the date, selecting an Active plan saves the draft. If no Active plan exists, the window shows `Create or publish an active plan first.` Rows without a known time are written as all-day Google Calendar events. Incomplete draft rows must survive automatic refreshes. Main lesson row time selectors offer a no-time option plus 08:00 through 20:00 in 15-minute increments while still allowing typed time input. Reconciled uninvoiced lesson rows show an editable Plan dropdown. Rapid plan changes preserve the final selected plan, and invoiced lesson plans are locked.
Main-window student arrows navigate by lesson occurrence date/time, not alphabetically by student name; Google Calendar lessons in the loaded previous/current/next month window define the occurrence order, the same student may appear more than once, the target lesson is selected, and the displayed month changes when the target occurrence is in another month. The student-name dropdown lists other active student short names alphabetically and switches directly to that student without selecting a lesson. Saved standard schedules provide fallback occurrences only when that loaded window has no Google Calendar lessons.
Plans are managed from `Plan > Manage Plans`; new Draft plans start with blank name/price, default lesson/duration/student/flexibility values, payment terms selected from `Paid monthly in advance` or `Paid per lesson`, and an effective date set to the first day of the current month. `Add Plan` saves changed Draft data before creating the next Draft, and changed Active `Retire on` values are prompted before adding. Closing Manage Plans with unsaved editable changes prompts to save, discard, or cancel the close. Active and Retired plan names may be renamed through a name-only save action; the rename is stored in plan-name history and does not unlock commercial fields. The student editor offers only publish-complete Active catalog plans, stores the selected plan identity, and copies the selected plan's commercial terms into the student's plan period. The Manage Plans window remembers its size/position and list column widths.
Economy overview is available from `Economy > Overview`; the window defaults to the current calendar year, provides previous/next year arrows, shows issued invoice rows plus month/quarter totals and a Years table for the selected year and previous four years, exports the displayed data to CSV through an explicit save dialog, remembers window placement, and opens the stored invoice PDF on invoice-row double-click. When Admin Mode has been checked and saved from `Application > Settings` for the current session, the overview allows editing only legacy month net/VAT values before the stored first application-backed invoice month and only when the displayed month is not backed by an application-issued invoice; totals are derived, and non-admin/application-backed months remain read-only. Saving manual totals ignores read-only invoice-backed month rows and removes any stored manual override for those invoice-backed months.
Resizable desktop windows must remember size and position, including subwindows, and restore inside the current visible screen area; fixed-size dialogs remember position without restoring stale saved sizes

## Build and test commands

```powershell
dotnet build ElenasBusinessLedger.sln -nodeReuse:false
dotnet test ElenasBusinessLedger.sln -nodeReuse:false
powershell -ExecutionPolicy Bypass -File scripts\build-installer.ps1
```

## Test policy

Unit tests: xUnit
UI automation: FlaUI later, where practical
Use IClock for time-sensitive logic
Use fake Google Calendar client for calendar integration tests

## Hard requirements

- Do not mark invoice issued unless PDF storage succeeds.
- Do not mark invoice printed unless the configured printer exists and the Windows print request launches without exception; do not imply physical output or spooler completion has been verified.
- Do not let Cancel/window close hide an invoice issue/print operation that is still in progress; extended-wait prompts must not imply the operation was safely cancelled.
- Do not silently overwrite an existing issued invoice PDF.
- Release the reservation and discard staging if staging or verification fails before a publication checkpoint is saved. After that checkpoint, retain the reservation and verify the saved artifact on retry.
- Print only once the invoice and exact lesson locks have finalized in SQLite.
- Do not silently edit issued invoices.
- Do not derive old invoice display text from current plan catalog names or rename history; issued invoice snapshots are authoritative for invoice text.
- Do not charge VAT when business profile says NotRegistered.
- Do not create invoice rows, invoice numbers, invoice PDFs, lesson rows, or invoice-list entries for legacy manual Economy Overview month totals.
- Do not silently mix manual Economy Overview month overrides with application-backed issued invoice totals; report the conflict visibly, keep issued invoice months read-only, and remove stale manual overrides for issued invoice months when manual totals are saved.
- Do not persist Admin Mode; it must start unchecked on each application startup even though it is enabled through `Application > Settings` for the current session.
- Do not store CPR numbers.
- Ignore Google Calendar entries that do not use the documented first-line `$` student marker.
- Preserve the first-line Google Calendar marker when adding/removing app-owned lesson status lines.
- Convert Windows time zone IDs to Google/IANA time zone IDs before writing Google Calendar event start/end payloads.
- Notify the user immediately when a Google Calendar lesson update fails.
- If Google Calendar creates a lesson event but the post-create refresh fails, preserve the user-selected plan identity by saving the local lesson with the created event id; if that local save fails, attempt to remove the created Google Calendar event before reporting failure.
- If Populate Calendar creates one or more Google Calendar lesson events but the post-create refresh fails, save local lesson rows for the created event ids before reporting the refresh failure.
- Do not report successful app-driven lesson population, date/time edit, delete, or planned/completed state change until the matching uninvoiced local SQLite lesson row has also been created, updated, or removed as appropriate.
- Automatic refresh must not discard incomplete main-window lesson drafts.
- Do not silently create duplicate Google Calendar lessons for the same student at the same date/time when using Populate Calendar.
- Do not imply Populate Calendar detects all conflicts; v1 skips exact duplicates only.
- Do not make Google Calendar sync failure corrupt local data.
- Do not use Google Calendar as the billing/tax audit source for invoiced lessons; use the locked local Lessons row and issued invoice snapshots.
- Do not infer invoiced, completed, or paid state from descriptive student/date/time/plan/month matches; apply that state only through the exact local lesson id linked from an issued invoice line.
- Do not persist OAuth access or refresh tokens in plaintext; token file names must not collide for distinct token keys.
- Do not launch interactive Google OAuth from startup or automatic refresh; use `Application > Settings > Connect Google Calendar` for deliberate browser authorization.
- Do not allow Draft or Retired catalog plans to be assigned to students.
- Do not mutate existing student plan period commercial-term snapshots or issued invoices when a catalog plan is edited as Draft, published, retired, reactivated, or renamed; plan renames may update catalog display names and plan-name history while student periods keep referring to the same catalog plan identity.
- Deactivating or reactivating a student must not mutate that student's plan periods, schedule, or issued invoices.
