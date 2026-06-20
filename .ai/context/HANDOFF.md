# Handoff

Date: 2026-06-20

## Current State

- Active implementation plans: `.ai/implementation-plans/active/2026-06-17-publisher-google-oauth-app.md`, `.ai/implementation-plans/active/2026-06-17-plan-catalog-management.md`, `.ai/implementation-plans/active/2026-06-20-student-overview-management.md`, and `.ai/implementation-plans/active/2026-06-20-student-overview-embedded-editing.md`.
- The app is now a real WPF/Core/Infrastructure local-first pilot build, not sample-backed.
- User-facing product name, internal namespace, project prefix, and app-data folder are `My Small Business Ledger` / `MySmallBusinessLedger`.
- Runtime composition uses SQLite under `%APPDATA%\visualtech.dk\MySmallBusinessLedger`, Google Calendar through the official API client, QuestPDF invoice generation, staged PDF storage, invoice metadata persistence, and OS print request after PDF save.
- `Application > Settings` configures company profile, address plus an unlabeled second address/city line, invoice folder with manual entry or Browse picker, Google calendar/account label, publisher Google sign-in app status, local Google authorization status, and Connect Google Calendar / Forget Google sign-in actions; it also shows read-only app data/database paths. The Google calendar field defaults to `primary`, meaning the signed-in account's main calendar, and its tooltip explains where to find a separate calendar ID.
- Student editor also uses two stacked address fields; the ViewModel combines them into one multiline student address for existing Core/SQLite/invoice behavior.
- `Student > Manage Students` is the only Student menu action. It opens a comprehensive overview listing active and inactive students. The selected student pane embeds the Student editor behavior for student fields, Active flag, active-plan assignment, copied plan snapshot, plan start/end dates, and schedule. Edits use explicit Save/Cancel; Add Student creates an in-pane draft. Invoice double-click opens the existing invoice viewer. Selection changes in Manage Students immediately refresh the main window around the selected student, and closing Manage Students applies the final selection again. The old standalone Add/Edit Student window and main-menu entries have been removed. The window and student/invoice grid columns are remembered under the existing app-data placement/width stores.
- `Plan > Manage Plans` now manages a durable SQLite plan catalog with Draft, Active, and Retired states. Draft plans can be incomplete, edited, deleted, and published; publishing requires all fields except `Retire on`. New Drafts default to blank name/price, one lesson per week, 60 minutes, one student, move/cancel flexibility, `Paid monthly in advance`, first day of the current month, and blank `Retire on`. Active plans are immutable except for name-only rename, `Retire on` scheduling, and retirement; Retired plans are immutable except for name-only rename and can be reactivated. `Add Plan` saves changed Draft fields before creating the next Draft and prompts before discarding/saving an unsaved Active `Retire on` change.
- Student plan periods now store the selected catalog plan identity plus copied commercial terms. The student editor offers only publish-complete Active catalog plans for assignment. Selecting one stores the plan identity and copies plan fields, including payment terms, into the student's saved plan period; existing student plan period commercial terms and issued invoices are not rewritten by later catalog lifecycle changes or name-only renames. If no Active plan is selected, the current-plan detail labels/values are hidden.
- Plan name changes are recorded in SQLite `PlanNameChanges` with old name, new name, and change date/time. Student plan displays follow the current catalog display name through `PlanCatalogId`; issued invoice lines store `PlanCatalogId` for lineage and keep the plan name stored when the invoice was issued for invoice display/PDF output.
- The main-window Open Calendar action opens Google Calendar in the browser with the configured Google account email as an `authuser` hint, avoiding the old hardcoded `/u/0/` first-browser-account behavior when possible.
- The app now uses publisher-owned Google Desktop OAuth configuration from `Configuration\google-oauth-client.json` in the app output. The local source file is `src\MySmallBusinessLedger.App\Configuration\google-oauth-client.json`, ignored by Git; the template is committed beside it.
- Window placement is stored under `%APPDATA%\visualtech.dk\MySmallBusinessLedger\window-placement.json`; Manage Plans column widths are stored under `%APPDATA%\visualtech.dk\MySmallBusinessLedger\grid-column-widths.json`.
- Completed/Planned lesson state is manual. Google Calendar keeps the first-line `$` student marker and uses a later `Status: Completed` summary line for completed lessons.
- Write Invoice opens candidate selection, invoice preview, optional price adjustment, staged issue/save/print, dashboard refresh, and read-only invoice viewer.

## Safety Invariants To Preserve

- Do not mark an invoice issued unless final PDF storage and invoice metadata save succeed.
- Do not silently overwrite an issued invoice PDF.
- Do not silently edit issued invoices.
- Invoiced lessons are not editable.
- Completed lessons can only be toggled back to Planned after confirmation; while Completed, no date/time edit or delete is allowed.
- Google Calendar write failures must be visible immediately and must not pretend local state changed.
- OAuth tokens must remain Windows-protected and must not be stored in SQLite or plaintext config.
- Do not restore the old user-selected Google OAuth JSON setup flow unless Ruben explicitly asks for a migration fallback.
- Do not allow Draft or Retired catalog plans to be assigned to students.
- Do not mutate existing student plan period commercial-term snapshots or issued invoices when a catalog plan changes lifecycle state or display name.
- Do not derive old invoice display text from current plan catalog names or rename history; issued invoice line snapshots are authoritative for invoice text.
- Deactivating/reactivating a student must not mutate plan periods, schedule entries, or issued invoices.

## Verification Snapshot

- Latest focused invoice test run passed: 42 invoice-related tests.
- Focused OAuth/Settings test run passed: 24 tests.
- Full build passed on 2026-06-17 after Google calendar settings follow-up: `dotnet build MySmallBusinessLedger.sln -nodeReuse:false` with 0 warnings and 0 errors.
- Full test suite passed on 2026-06-18 after Plan catalog draft-default/payment-term dropdown follow-up: `dotnet test MySmallBusinessLedger.sln -nodeReuse:false` with 161/161 tests passing.
- Focused Student overview tests passed on 2026-06-20: `dotnet test tests\MySmallBusinessLedger.Tests\MySmallBusinessLedger.Tests.csproj --filter "FullyQualifiedName~StudentOverview|FullyQualifiedName~StudentsCanBeListedIncludingInactiveStudents|FullyQualifiedName~ManageStudentsCommand" -nodeReuse:false` with 7/7 tests passing.
- Full build passed on 2026-06-20 after Student overview implementation: `dotnet build MySmallBusinessLedger.sln -nodeReuse:false` with 0 warnings and 0 errors.
- Full test suite passed on 2026-06-20 after Student overview implementation: `dotnet test MySmallBusinessLedger.sln -nodeReuse:false` with 168/168 tests passing.
- Focused embedded student editing tests passed on 2026-06-20: `dotnet test tests\MySmallBusinessLedger.Tests\MySmallBusinessLedger.Tests.csproj --filter "FullyQualifiedName~StudentOverview|FullyQualifiedName~StudentEditor" -nodeReuse:false` with 19/19 tests passing.
- Full test suite passed on 2026-06-20 after embedded student editing implementation: `dotnet test MySmallBusinessLedger.sln -nodeReuse:false` with 171/171 tests passing.
- Focused Student menu/overview/editor cleanup tests passed on 2026-06-20: `dotnet test tests\MySmallBusinessLedger.Tests\MySmallBusinessLedger.Tests.csproj --filter "FullyQualifiedName~MainWindowViewModel|FullyQualifiedName~StudentOverview|FullyQualifiedName~StudentEditor" -nodeReuse:false` with 40/40 tests passing.
- Full test suite passed on 2026-06-20 after Student menu/dialog cleanup: `dotnet test MySmallBusinessLedger.sln -nodeReuse:false` with 170/170 tests passing.
- Full test suite passed on 2026-06-20 after Manage Students selection handoff: `dotnet test MySmallBusinessLedger.sln -nodeReuse:false` with 170/170 tests passing.
- Full test suite passed on 2026-06-20 after live Manage Students selection sync: `dotnet test MySmallBusinessLedger.sln -nodeReuse:false` with 171/171 tests passing.
- Focused plan identity/rename/student display tests passed on 2026-06-20: `dotnet test tests\MySmallBusinessLedger.Tests\MySmallBusinessLedger.Tests.csproj --filter "FullyQualifiedName~SqlitePersistenceTests|FullyQualifiedName~PlanCatalogViewModelTests|FullyQualifiedName~EditorViewModelTests|FullyQualifiedName~StudentOverviewViewModelTests|FullyQualifiedName~PlanCatalogUseCaseTests" -nodeReuse:false` with 64/64 tests passing.
- Focused SQLite persistence tests passed on 2026-06-20 after case-insensitive rename duplicate hardening: `dotnet test tests\MySmallBusinessLedger.Tests\MySmallBusinessLedger.Tests.csproj --filter "FullyQualifiedName~SqlitePersistenceTests" -nodeReuse:false` with 14/14 tests passing.
- Full build passed on 2026-06-20 after plan identity/name-rename implementation: `dotnet build MySmallBusinessLedger.sln -nodeReuse:false` with 0 warnings and 0 errors.
- Full test suite passed on 2026-06-20 after plan identity/name-rename implementation: `dotnet test MySmallBusinessLedger.sln -nodeReuse:false` with 174/174 tests passing.
- Focused invoice lineage tests passed on 2026-06-20: lineage-focused slice 5/5 and invoice-related slice 45/45.
- Full build passed on 2026-06-20 after invoice plan-lineage implementation: `dotnet build MySmallBusinessLedger.sln -nodeReuse:false` with 0 warnings and 0 errors.
- Full test suite passed on 2026-06-20 after invoice plan-lineage implementation: `dotnet test MySmallBusinessLedger.sln -nodeReuse:false` with 174/174 tests passing.
- Focused Plan catalog tests passed on 2026-06-17: Stage 1 Core plan tests 9/9, Stage 2 SQLite persistence tests 10/10, Stage 3 Manage Plans tests 10/10, Stage 4 student assignment/editor/persistence tests 23/23.
- Real browser OAuth consent reached Google's "Received verification code" page for maintainer testing; Stage 6 remains open for app-side dashboard/settings verification after these follow-up fixes.
- `dotnet format MySmallBusinessLedger.sln --verify-no-changes --no-restore` may still report broad line-ending/whitespace issues across existing files; avoid a sweeping format rewrite unless Ruben explicitly wants that cleanup.

## Recommended Next Actions

1. Place the real publisher OAuth Desktop JSON at `src\MySmallBusinessLedger.App\Configuration\google-oauth-client.json`.
2. Complete maintainer pilot setup through `Application > Settings`, then use Connect Google Calendar.
3. Run the Student overview final user test from `.ai/implementation-plans/active/2026-06-20-student-overview-management.md`.
4. Run the remaining OAuth/calendar pilot test against a safe Google Calendar/test invoice folder.
5. Visually review the invoice PDF layout and WPF dialogs; PDF layout polish is intentionally deferred.
6. Create follow-up plans for broader administration views, multi-period student plan history editing, backup/export, VAT reporting, and invoice layout touch-ups.
