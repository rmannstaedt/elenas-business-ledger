# Handoff

Date: 2026-06-17

## Current State

- Active implementation plan: `.ai/implementation-plans/active/2026-06-17-publisher-google-oauth-app.md`.
- The app is now a real WPF/Core/Infrastructure local-first pilot build, not sample-backed.
- User-facing product name, internal namespace, project prefix, and app-data folder are `My Small Business Ledger` / `MySmallBusinessLedger`.
- Runtime composition uses SQLite under `%APPDATA%\visualtech.dk\MySmallBusinessLedger`, Google Calendar through the official API client, QuestPDF invoice generation, staged PDF storage, invoice metadata persistence, and OS print request after PDF save.
- `Application > Settings` configures company profile, invoice folder, Google Calendar ID/account label, shows publisher Google sign-in app status, and includes Connect Google Calendar / Forget Google sign-in actions; it also shows read-only app data/database paths.
- The app now uses publisher-owned Google Desktop OAuth configuration from `Configuration\google-oauth-client.json` in the app output. The local source file is `src\MySmallBusinessLedger.App\Configuration\google-oauth-client.json`, ignored by Git; the template is committed beside it.
- Window placement is stored under `%APPDATA%\visualtech.dk\MySmallBusinessLedger\window-placement.json`.
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

## Verification Snapshot

- Latest focused invoice test run passed: 42 invoice-related tests.
- Focused OAuth/Settings test run passed: 24 tests.
- Full build passed on 2026-06-17: `dotnet build MySmallBusinessLedger.sln -nodeReuse:false` with 0 warnings and 0 errors.
- Full test suite passed on 2026-06-17: `dotnet test MySmallBusinessLedger.sln -nodeReuse:false` with 110/110 tests passing.
- Real browser OAuth consent is not yet manually verified; Stage 6 remains open.
- `dotnet format MySmallBusinessLedger.sln --verify-no-changes --no-restore` may still report broad line-ending/whitespace issues across existing files; avoid a sweeping format rewrite unless Ruben explicitly wants that cleanup.

## Recommended Next Actions

1. Run full build and full test suite.
2. Place the real publisher OAuth Desktop JSON at `src\MySmallBusinessLedger.App\Configuration\google-oauth-client.json`.
3. Complete maintainer pilot setup through `Application > Settings`, then use Connect Google Calendar.
4. Run the final user test from the active plan against a safe Google Calendar/test invoice folder.
5. Visually review the invoice PDF layout and WPF dialogs; PDF layout polish is intentionally deferred.
6. Create follow-up plans for admin overview, plan administration, backup/export, VAT reporting, and invoice layout touch-ups.
