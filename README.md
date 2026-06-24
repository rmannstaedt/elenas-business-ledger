# My Small Business Ledger

This repository contains a small local-first ledger application for independent piano teachers.

Primary goal:

> Make it easy for a teacher to create, print, and store invoices while keeping students, lesson plans, Google Calendar, payments, and VAT/tax tracking consistent.

## Documents

1. `docs/business-requirements-and-logic.md` — product design, scope, architecture, requirements, and key decisions.

## Solution shape

```text
src/
  MySmallBusinessLedger.App
  MySmallBusinessLedger.Core
  MySmallBusinessLedger.Infrastructure

tests/
  MySmallBusinessLedger.Tests
```

## Build and test

```powershell
dotnet build MySmallBusinessLedger.sln -nodeReuse:false
dotnet test MySmallBusinessLedger.sln -nodeReuse:false
```

## Pilot setup

Run the WPF app from Visual Studio or with:

```powershell
dotnet run --project src\MySmallBusinessLedger.App\MySmallBusinessLedger.App.csproj
```

On first setup, open `Application > Settings` and configure:

- company name, address, VAT status, invoice folder, invoice printer, and invoice payment instructions; the invoice folder can be typed or selected with Browse, and the remembered printer can be selected, typed, changed, or cleared;
- Google calendar and account label/email. The calendar defaults to `primary`, which means the main calendar of the Google account used at sign-in.

The app uses the publisher Google sign-in configuration bundled with the application. For local development, put the real Desktop OAuth JSON at `src\MySmallBusinessLedger.App\Configuration\google-oauth-client.json`; the file is intentionally ignored by Git and copied to the app output when present. Use `Application > Settings > Connect Google Calendar` to authorize the configured Google account in the browser. Settings reports whether Google authorization token files are saved locally; use `Forget Google sign-in` to clear local authorization and force a fresh browser consent flow.

Google OAuth access/refresh tokens are stored with Windows user data protection.

Runtime data lives under `%APPDATA%\visualtech.dk\MySmallBusinessLedger`:

- `ledger.db` - local SQLite database;
- `GoogleTokens\` - Windows-protected Google OAuth token store;
- `window-placement.json` - remembered window sizes and positions;
- `grid-column-widths.json` - remembered grid column widths.

The invoice folder, invoice printer, and invoice payment instructions are configured in Settings and remembered in the local SQLite database. If no invoice printer is configured when invoice printing or reprinting is first attempted, the app opens the printer selection dialog directly and remembers the chosen printer before continuing. The invoice folder may be outside the app data folder. `Write Invoice` opens the candidate dialog with the currently displayed main-window student preselected when that student is available, before falling back to today/time-based candidates. Invoice PDFs use Danish text, a toner-light header, the website `studio.example.test`, the invoice period/month, grouped lesson services by issued plan and price, one clear included-lesson list, configured payment instructions when present, and `CVR` when a CVR is configured. If an invoice covers a mid-month plan change, the PDF includes a cut-over note. The app reserves invoice metadata, writes invoice PDFs through a staging/verification/publish flow, finalizes the issued invoice only after the PDF is published, and does not silently overwrite an existing issued invoice PDF. If staging, PDF writing, verification, or final PDF publish fails before the PDF is published, the app discards any staged PDF and releases the reserved number so retrying does not skip invoice numbers. Invoice numbers display with at least three digits, and final PDF file names use the invoice number plus student full name, for example `001-Mira Vale.pdf`. Printing uses the remembered printer; `Printed` means the configured printer existed and Windows accepted the print request launch, not that the app verified physical paper output.
SQLite schema evolution uses EF Core migrations. The WPF app applies pending migrations at startup before repositories are used.

Plans are managed from `Plan > Manage Plans`. New plans start as incomplete Drafts, where they can be edited, deleted, or published. Draft payment terms are selected from `Paid monthly in advance` or `Paid per lesson`, with `Paid monthly in advance` as the default. Publishing requires all plan fields except `Retire on`. Published Active plans are immutable and can be assigned to students; their `Retire on` date can still be changed, and they can be retired immediately. Retired plans are immutable and hidden from student assignment until reactivated. Assigning a plan to a student stores the catalog plan identity and copies the commercial fields, including payment terms, into the student's plan period. Plan names can be renamed as display names and are recorded in name-change history; student plan displays follow the renamed catalog plan, while copied commercial terms and issued invoice data are not rewritten. Issued invoice lines store both the catalog plan identity for lineage and the issued-time plan name snapshot used for invoice display/PDF output.

Students are managed from `Student > Manage Students`. The overview lists active and inactive students, embeds the student editor for the selected student, shows issued invoices with payment and print status, and keeps explicit Save/Cancel for edits. Add Student creates an in-pane draft. Selecting an Active catalog plan copies that plan into the student's plan period; changing the Active checkbox only persists when Save is pressed. Plan periods, schedules, and issued invoices remain intact unless the user explicitly saves edited student data. Double-clicking an invoice opens the invoice viewer, where an issued invoice can be reprinted from its stored PDF without creating a new invoice number or changing invoice content. Selecting a student in Manage Students immediately refreshes the main window around that student, and closing Manage Students applies the final selection again.

The main window can populate Google Calendar from the selected student's saved standard schedule. `Populate Calendar` seeds the current month from today through month end, or a future month for the whole month. Past months cannot be seeded. Exact duplicate lessons for the same student at the same date/time are skipped; version 1 does not detect other calendar conflicts. Student arrows navigate Google Calendar lesson occurrences by date/time rather than student names; the same student can appear multiple times, the target lesson is selected, and the displayed month changes when the target occurrence is in another month. The student-name dropdown lists other active student short names alphabetically and switches directly to that student without selecting a lesson. Saved standard schedules provide fallback navigation only when there are no Google Calendar lessons in the loaded previous/current/next month window. `Add Lesson` creates an editable draft row; once both date and time are valid, the draft is written to Google Calendar automatically. Incomplete draft rows remain visible across automatic refreshes instead of being discarded. Main lesson rows offer time selector options from 08:00 through 20:00 while still allowing typed time input.

## Current key assumptions

- Under-30 teaching support remains in the data model because future students may require it (but out of scope for version 1.0).
- Lønsumsafgift is out of scope for version 1.0.
- Google Calendar is the operational lesson schedule, with two-way sync.
- Google Calendar integration uses the official Google Calendar API client, external browser OAuth, and Windows-protected local token storage. The app requests the narrower `https://www.googleapis.com/auth/calendar.events.owned` OAuth scope so it can read, create, move, complete, and delete lesson events on calendars owned by the signed-in Google account; entries still must match the documented `$` student marker before the app treats them as lessons.
- The Google OAuth app identity is publisher-owned and supplied from `Configuration\google-oauth-client.json`, not selected by each user in Settings.
- The main window Open Calendar action opens Google Calendar in the browser with the configured Google account email as the account hint.
- Runtime data is stored under `%APPDATA%\visualtech.dk\MySmallBusinessLedger`.
- The WPF app now uses real SQLite/Core/Calendar-backed services; if Google Calendar is not configured, the dashboard reports that instead of showing sample lesson rows.
- Company and Google Calendar account setup, connection, and authorization reset are available from `Application > Settings`.
- Plan catalog management is available from `Plan > Manage Plans`; only Active catalog plans are assignable to students.
- Student overview management is available from `Student > Manage Students`; it lists all students, supports in-pane student/plan/schedule editing with explicit Save/Cancel, mirrors student selection into the main window immediately and on close, and preserves copied plan commercial-term snapshots and issued invoices unless edited student data is saved.
- The main window `Add Lesson` action creates editable draft rows that autosave to Google Calendar once both date and time are valid; incomplete draft rows survive automatic refreshes.
- The main window `Populate Calendar` action writes scheduled lessons directly to Google Calendar for the selected student/month, skipping exact duplicate same-student same-date/time lessons only.
- The app stores issued invoice PDFs in a configurable invoices folder.
- Invoice PDFs are generated with QuestPDF, staged and verified before final storage, and never silently overwritten. Failed pre-publication issue attempts discard staged PDFs and release the reserved invoice number. Invoice printing uses the remembered Settings printer and can be retried from the invoice viewer using the stored issued PDF.
- The initial UI recommendation is WPF + .NET 10.
- The initial database recommendation is SQLite.
- Database access and schema evolution use EF Core SQLite migrations.
- The PDF dependency is QuestPDF.
