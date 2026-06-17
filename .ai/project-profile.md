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
Plan catalog: stored in SQLite with Draft, Active, and Retired lifecycle state; Draft plans are editable/deletable and may be incomplete, publishing requires all fields except Retire on, Active plans are immutable/assignable except for Retire on scheduling and immediate retirement, and Retired plans are immutable/reactivatable but not assignable
Student plan periods: stored as copied plan snapshots including payment terms; catalog lifecycle changes must not rewrite existing student plan periods or issued invoice data
Invoice PDFs: stored as files, not BLOBs
Invoice PDF generation: QuestPDF
Invoice PDF safety: write to staging, verify PDF bytes, publish without overwriting, then persist invoice metadata and request print
Google Calendar account tokens: stored locally with Windows user data protection, never in SQLite or plaintext config
Google Calendar selected calendar/account label: stored in SQLite app settings; the main-window browser launcher uses the configured account email as a Google Calendar account hint
Runtime app data folder: `%APPDATA%\visualtech.dk\MySmallBusinessLedger`
Runtime SQLite database path: `%APPDATA%\visualtech.dk\MySmallBusinessLedger\ledger.db`
Publisher Google OAuth Desktop client configuration: local/release file `src\MySmallBusinessLedger.App\Configuration\google-oauth-client.json`, copied to app output under `Configuration\google-oauth-client.json` when present; the real file is ignored by Git and the committed template is `src\MySmallBusinessLedger.App\Configuration\google-oauth-client.template.json`
Google OAuth protected token folder: `%APPDATA%\visualtech.dk\MySmallBusinessLedger\GoogleTokens`
Window placement file path: `%APPDATA%\visualtech.dk\MySmallBusinessLedger\window-placement.json`
Grid column width file path: `%APPDATA%\visualtech.dk\MySmallBusinessLedger\grid-column-widths.json`
Backup: no automated backup in version 1.0; keep persistence design compatible with later manual backup/export

## External integrations

Google Calendar API: official Google Calendar .NET client
Google authentication: external browser OAuth
Google OAuth app identity: publisher-owned Desktop OAuth client; users authorize their own Google account at runtime
Google Calendar completed-lesson marker: keep the first line as `$ <student short name>` or `$<student short name>`; Completed is represented by a later `Status: Completed` summary line, and absence of that line means Planned
Calendar availability: invoice issue must rely on the accepted invoice preview/snapshot rather than requiring a fresh Google Calendar read at the final print/save moment
PDF generation: QuestPDF Community license accepted for v1

## UI policy

WPF + MVVM
No business logic in code-behind
Danish user-facing invoice text
My-facing UI must stay simple
Company profile, two-line company address entry, invoice folder with typed or folder-picker selection, Google calendar/account label, Google sign-in app status, local Google authorization status, connect/reconnect controls, and read-only app data/database paths are configured or shown from `Application > Settings`; the Google calendar defaults to `primary`
Student editor uses two stacked address fields and stores them as one multiline student address value.
Plans are managed from `Plan > Manage Plans`; new Draft plans start with blank name/price, default lesson/duration/student/flexibility values, payment terms selected from `Paid monthly in advance` or `Paid per lesson`, and an effective date set to the first day of the current month. `Add Plan` saves changed Draft data before creating the next Draft, and changed Active `Retire on` values are prompted before adding. The student editor offers only publish-complete Active catalog plans and copies the selected plan into the student's plan period. The Manage Plans window remembers its size/position and list column widths.
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
- Do not silently overwrite an existing issued invoice PDF.
- Do not silently edit issued invoices.
- Do not charge VAT when business profile says NotRegistered.
- Do not store CPR numbers.
- Ignore Google Calendar entries that do not use the documented first-line `$` student marker.
- Preserve the first-line Google Calendar marker when adding/removing app-owned lesson status lines.
- Notify the user immediately when a Google Calendar lesson update fails.
- Do not make Google Calendar sync failure corrupt local data.
- Do not persist OAuth access or refresh tokens in plaintext.
- Do not allow Draft or Retired catalog plans to be assigned to students.
- Do not mutate existing student plan periods or issued invoices when a catalog plan is edited as Draft, published, retired, or reactivated.
