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
Invoice PDFs: stored as files, not BLOBs
Invoice PDF generation: QuestPDF
Invoice PDF safety: write to staging, verify PDF bytes, publish without overwriting, then persist invoice metadata and request print
Google Calendar account tokens: stored locally with Windows user data protection, never in SQLite or plaintext config
Google Calendar selected calendar/account label: stored in SQLite app settings
Runtime app data folder: `%LOCALAPPDATA%\visualtech.dk\MySmallBusinessLedger`
Runtime SQLite database path: `%LOCALAPPDATA%\visualtech.dk\MySmallBusinessLedger\ledger.db`
Google Calendar login setup file path: `%LOCALAPPDATA%\visualtech.dk\MySmallBusinessLedger\google-client-secrets.json`
Google OAuth protected token folder: `%LOCALAPPDATA%\visualtech.dk\MySmallBusinessLedger\GoogleTokens`
Backup: no automated backup in version 1.0; keep persistence design compatible with later manual backup/export

## External integrations

Google Calendar API: official Google Calendar .NET client
Google authentication: external browser OAuth
Google Calendar completed-lesson marker: keep the first line as `$ <student short name>` or `$<student short name>`; Completed is represented by a later `Status: Completed` summary line, and absence of that line means Planned
Calendar availability: invoice issue must rely on the accepted invoice preview/snapshot rather than requiring a fresh Google Calendar read at the final print/save moment
PDF generation: QuestPDF Community license accepted for v1

## UI policy

WPF + MVVM
No business logic in code-behind
Danish user-facing invoice text
My-facing UI must stay simple
Company profile, invoice folder, Google Calendar ID/account label, Google Calendar login setup file, and read-only app data/database paths are configured or shown from `Application > Settings`
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
