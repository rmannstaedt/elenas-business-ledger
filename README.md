# My Small Business Ledger

This repository contains a small local-first ledger application for independent piano teachers.

Current app version: 1.20.

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

## Build the installer

The application can be packaged as a single-file Inno Setup installer for Windows x64:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\build-installer.ps1
```

The script publishes a self-contained Release build and creates:

```text
artifacts\installer\MySmallBusinessLedger-1.20.0-Setup.exe
```

Inno Setup 6 must be installed at `C:\Program Files (x86)\Inno Setup 6\ISCC.exe`, or pass `-InnoSetupCompiler` to the script. If the release installer should include Google Calendar sign-in support, place the real publisher OAuth Desktop JSON at `src\MySmallBusinessLedger.App\Configuration\google-oauth-client.json` before building; that file remains ignored by Git.

The installer installs per-user under `%LOCALAPPDATA%\Programs\MySmallBusinessLedger`, creates a Start menu shortcut, and offers an optional desktop shortcut. It installs only this version and does not auto-update. Later installers use the same application identity and install location so they replace application files without deleting runtime data. Install, upgrade, and uninstall must leave `%APPDATA%\visualtech.dk\MySmallBusinessLedger` intact, including `ledger.db`, Settings data, Google token files, window placement, and grid column widths.

The installer is not code-signed in version 1.20, so Windows SmartScreen may warn before running it.

## Pilot setup

Run the WPF app from Visual Studio or with:

```powershell
dotnet run --project src\MySmallBusinessLedger.App\MySmallBusinessLedger.App.csproj
```

On first setup, open `Application > Settings` and configure:

- company name, address, VAT status, bank registration/account number, MobilePay account, invoice folder, invoice printer, and invoice payment instructions selected from fixed terms or custom text; the invoice folder can be typed or selected with Browse, and the remembered printer can be selected, typed, changed, or cleared;
- Google calendar and account label/email. The calendar defaults to `primary`, which means the main calendar of the Google account used at sign-in.

The app uses the publisher Google sign-in configuration bundled with the application. For local development, put the real Desktop OAuth JSON at `src\MySmallBusinessLedger.App\Configuration\google-oauth-client.json`; the file is intentionally ignored by Git and copied to the app output when present. Use `Application > Settings > Connect Google Calendar` to authorize the configured Google account in the browser. Startup/dashboard refreshes reuse saved local authorization only and do not open browser consent; if authorization is missing, reconnect from Settings. Settings reports whether Google authorization token files are saved locally for the configured account; use `Forget Google sign-in` to clear local authorization and force a fresh browser consent flow.

Google OAuth access/refresh tokens are stored with Windows user data protection, under collision-resistant hashed token-key filenames. Legacy escaped token filenames are still read and migrated to hashed filenames on successful token lookup.

Runtime data lives under `%APPDATA%\visualtech.dk\MySmallBusinessLedger`:

- `ledger.db` - local SQLite database;
- `GoogleTokens\` - Windows-protected Google OAuth token store;
- `window-placement.json` - remembered window positions and resizable window sizes;
- `grid-column-widths.json` - remembered grid column widths.

The invoice folder, invoice printer, bank registration/account number, MobilePay account, and invoice payment instructions are configured in Settings and remembered in the local SQLite database. Payment instructions are selected from `Betales ved modtagelse`, `8 dage fra fakturadato`, `30 dage fra fakturadato`, or custom text. If no invoice printer is configured when invoice printing or reprinting is first attempted, the app opens the printer selection dialog directly and remembers the chosen printer before continuing. The invoice folder may be outside the app data folder. `Write Invoice` opens the candidate dialog with the currently displayed main-window student preselected when that student is available, before falling back to today/time-based candidates. Invoice PDFs use Danish text, a toner-light header, the website `studio.example.test`, the invoice period/month, grouped lesson services by issued plan and price, one clear indented included-lesson list that stays in one column through 10 lessons and uses two columns above 10 lessons, configured bank registration/account number, MobilePay account lines, and payment instructions when present, and `CVR` when a CVR is configured. If an invoice covers a mid-month plan change, the PDF includes a cut-over note. The app reserves invoice metadata, writes invoice PDFs through a staging/verification/publish flow, finalizes the issued invoice only after the PDF is published, and does not silently overwrite an existing issued invoice PDF. If staging, PDF writing, verification, or final PDF publish fails before the PDF is published, the app discards any staged PDF and releases the reserved number so retrying does not skip invoice numbers. After final PDF publication, if database finalization fails, the app keeps the `Issuing` reservation and recorded published PDF path for retry/support recovery, does not mark the invoice issued, and does not print. The invoice preview stays open while writing/printing is in progress, prevents Cancel/window close from hiding the in-progress result, and asks whether to keep waiting when the operation takes longer than expected. Invoice numbers display with at least three digits, and final PDF file names include the invoice number, invoice period, and student full name, for example `Invoice 001 Juni 2026 - Mira Vale.pdf`. Printing uses the remembered printer; `Printed` means the configured printer existed and Windows accepted the print request launch, not that the app verified physical paper output.
SQLite schema evolution uses EF Core migrations. The WPF app applies pending migrations at startup before repositories are used. Economy cut-over settings and legacy manual month totals are stored in SQLite as DKK net/VAT amounts, without creating invoice rows, invoice numbers, lesson rows, or PDFs. If a legacy invoice-line row cannot be tied to an exact local lesson identity during an upgrade, the app skips that old line instead of inventing a match, shows a startup warning, and keeps the invoice header plus any safely linkable lines.

Plans are managed from `Plan > Manage Plans`. New plans start as incomplete Drafts, where they can be edited, deleted, or published. Draft payment terms are selected from `Paid monthly in advance` or `Paid per lesson`, with `Paid monthly in advance` as the default. Publishing requires all plan fields except `Retire on`. Published Active plans are immutable and can be assigned to students; their `Retire on` date can still be changed, and they can be retired immediately. Retired plans are immutable and hidden from student assignment until reactivated. Closing Manage Plans with unsaved editable changes prompts to save, discard, or cancel the close. Assigning a plan to a student stores the catalog plan identity and copies the commercial fields, including payment terms, into the student's plan period. Plan names can be renamed as display names and are recorded in name-change history; student plan displays follow the renamed catalog plan, while copied commercial terms and issued invoice data are not rewritten. Issued invoice lines store the exact local lesson id, the catalog plan identity for lineage, and the issued-time plan name snapshot used for invoice display/PDF output.

Students are managed from `Student > Manage Students`. The workflow opens a Students list window and a separate Student Details window that share the same selected-student state. The main window also has a `Student Details` button that opens the same details window for the currently displayed student. The list window shows active and inactive students; the details window shows the selected student's editor, current plan, schedule, and issued invoices with payment and print status. Add student creates a draft in the details window. Selecting an Active catalog plan copies that plan into the student's plan period; changing the Active checkbox only persists when Save is pressed. Student Details has Save and Close actions: Save persists edits and keeps the window open, while Close prompts to save, discard, or cancel the close when unsaved edits exist. Plan periods, schedules, and issued invoices remain intact unless the user explicitly saves edited student data. Double-clicking an invoice opens the invoice viewer, where an issued invoice can be reprinted from its stored PDF without creating a new invoice number or changing invoice content. Selecting a student in Manage Students immediately refreshes the main window around that student, and closing Manage Students or a dashboard-opened Student Details window refreshes the main window around the final selected student.

Economy reporting is available from `Economy > Overview`. The overview defaults to the current calendar year, can move to the previous or next year, and reads financial data from SQLite rather than parsing stored PDF files. Application-backed months use issued invoice totals grouped by invoice period start year/month/quarter. Legacy paper-invoice months can be represented by explicit manual month overrides before a stored first application-backed invoice month. The cut-over month controls where manual legacy editing is allowed; it does not hide real issued invoices before that date. Issued invoices continue to display read-only unless that same month also has a manual override, which is a blocking data conflict. Manual month values are DKK replacement totals for net and VAT, with total derived as net plus VAT; they do not create invoice rows, invoice numbers, lesson rows, or PDFs, and they do not appear in the invoice list. Admin Mode is a session-only Settings option that starts unchecked each startup and must be saved from `Application > Settings` before Economy Overview allows legacy month net/VAT editing. The first application-backed invoice month is also configured in Settings, visible only while Admin Mode is checked there; selecting any date in that control is normalized to the first day of the selected month. If a month would contain both a manual override and application-issued invoices, the overview fails visibly instead of silently mixing or hiding data; it still renders the returned overview rows with a blocking error and disables export, while Admin Mode keeps manual Save/Cancel available so invalid manual overrides can be removed. Saving manual totals only persists editable legacy manual months and clears stale manual overrides for months backed by issued invoices. The window shows invoice number, PDF filename, net, VAT, and total amounts, shows a Years summary for the selected year and previous four years, leaves month/quarter/year amount cells blank where no displayed total exists, and exports one machine-friendly CSV with a `Source` column for invoice, manual, and aggregate provenance. Double-clicking an invoice row opens the stored PDF.

The main window can populate Google Calendar from the selected student's saved standard schedule. `Populate Calendar` seeds the current month from today through month end, or any other displayed month for the whole month. Exact duplicate lessons for the same student at the same date/time are skipped; version 1 does not detect other calendar conflicts. Successful app-driven lesson population, date/time edits, delete, and planned/completed state changes update the local SQLite lesson ledger before reporting success, so invoice preview does not depend on a later refresh to see those changes. Student arrows navigate Google Calendar lesson occurrences by date/time rather than student names; the same student can appear multiple times, the target lesson is selected, and the displayed month changes when the target occurrence is in another month. The student-name dropdown lists other active student short names alphabetically and switches directly to that student without selecting a lesson. Calendar lesson `$` student markers match active saved student short names case-insensitively, then use the stored student short-name casing in dashboard and local lesson records. Saved standard schedules provide fallback navigation only when there are no Google Calendar lessons in the loaded previous/current/next month window. `Add Lesson` creates an editable draft row when at least one Active plan exists; otherwise it shows `Create or publish an active plan first.` When the displayed month is not the current month, that row starts on the first day of the displayed month. Date and plan are required; time is optional. Once date and plan are valid, the draft is written to Google Calendar automatically with the selected plan identity preserved. Lessons without a known time are written as all-day Google Calendar events, display with blank time, and print without a time. Incomplete draft rows remain visible across automatic refreshes instead of being discarded. Main lesson rows offer a no-time option plus time selector options from 08:00 through 20:00 while still allowing typed time input, and reconciled uninvoiced lessons expose an editable Plan dropdown where rapid changes preserve the final selected plan.

## Current key assumptions

- Under-30 teaching support remains in the data model because future students may require it (but out of scope for version 1.20).
- Lønsumsafgift is out of scope for version 1.20.
- Google Calendar is the operational schedule surface for uninvoiced lessons; local SQLite Lessons records are the durable billing ledger and invoiced lessons ignore later calendar edits/deletions.
- Google Calendar integration uses the official Google Calendar API client, external browser OAuth for explicit Settings connection, and Windows-protected local token storage with hashed token-key filenames. Startup refreshes use saved local credentials non-interactively and do not launch browser OAuth. The app requests the narrower `https://www.googleapis.com/auth/calendar.events.owned` OAuth scope so it can read, create, move, complete, and delete lesson events on calendars owned by the signed-in Google account; entries still must match the documented `$` student marker before the app treats them as lessons.
- The Google OAuth app identity is publisher-owned and supplied from `Configuration\google-oauth-client.json`, not selected by each user in Settings.
- The main window Open Calendar action opens Google Calendar in the browser with the configured Google account email as the account hint.
- Runtime data is stored under `%APPDATA%\visualtech.dk\MySmallBusinessLedger`.
- The WPF app now uses real SQLite/Core/Calendar-backed services; if Google Calendar is not configured or authorization is missing, the dashboard reports that instead of showing sample lesson rows or opening browser OAuth unexpectedly.
- Company and Google Calendar account setup, connection, authorization reset, the durable first application-backed invoice month, and session-only Economy Overview Admin Mode are available from `Application > Settings`; Admin Mode is always off at startup and must be explicitly saved for the current session.
- The app version is visible from `Application > About My Small Business Ledger`.
- Plan catalog management is available from `Plan > Manage Plans`; only Active catalog plans are assignable to students.
- Student overview management is available from `Student > Manage Students`; it opens coordinated Students and Student Details windows, supports selected-student/plan/schedule editing with Save in the details window and an unsaved-change prompt on Close, mirrors student selection into the main window immediately and on close, and preserves copied plan commercial-term snapshots and issued invoices unless edited student data is saved. The main window `Student Details` button opens the same details window for the currently displayed student.
- Economy overview reporting is available from `Economy > Overview`; it uses issued SQLite invoice totals and explicit pre-cut-over manual month overrides by invoice period year, not PDF parsing, and exports one sectioned CSV with invoice, month, quarter, and year rows plus a source/provenance column.
- The main window `Add Lesson` action creates editable draft rows that autosave to Google Calendar once date and plan are valid; time is optional, in non-current months the draft date defaults to the first day of the displayed month, and incomplete draft rows survive automatic refreshes.
- The main window `Populate Calendar` action writes scheduled lessons directly to Google Calendar and saves created local billing rows for the selected student/month before reporting success; if the post-create refresh fails, created event ids are still saved locally before the refresh failure is reported. Exact duplicate same-student same-date/time lessons are skipped only.
- The app stores issued invoice PDFs in a configurable invoices folder.
- Invoice PDFs are generated with QuestPDF, staged and verified before final storage, and never silently overwritten. Failed pre-publication issue attempts discard staged PDFs and release the reserved invoice number. Once the final PDF has been published, database finalization failures preserve the in-progress reservation and published path for retry/support recovery instead of deleting the only durable evidence. Invoice printing uses the remembered Settings printer and can be retried from the invoice viewer using the stored issued PDF.
- The initial UI recommendation is WPF + .NET 10.
- The initial database recommendation is SQLite.
- Database access and schema evolution use EF Core SQLite migrations.
- The PDF dependency is QuestPDF.
