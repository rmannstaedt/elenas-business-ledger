# Implementation Plan: Economy Manual Month Overrides

Status: Active
Created: 2026-06-28
Last updated: 2026-06-28
Owner/context: Ruben / My Small Business Ledger v1.20 follow-up

## Related Docs And Standards

- `AGENTS.md`
- `README.md`
- `.ai/project-profile.md`
- `.ai/context/HANDOFF.md`
- `docs/business-requirements-and-logic.md`
- `.ai/standards/coding-standards.md`
- `.ai/standards/testing-and-verification.md`
- `.ai/standards/database-standards.md`
- `.ai/standards/domain-data-integrity.md`
- `.ai/standards/ui-design-standards.md`
- `.ai/standards/wpf-mvvm-standards.md`

## Related Skills

- `implementation-plan` for this plan
- `plan-runner` to execute this plan stage by stage
- `code-review` for final cross-layer/database/UI risk review
- `documentation-refresh` because public UI behavior, durable settings, and schema change
- `ui-review` because Economy Overview editability and Admin Mode command state change

## Goal

Add a session-only Admin Mode and database-backed manual economy month overrides so legacy paper-invoice months can be entered directly in Economy Overview without creating duplicate application invoices. Admin Mode and first application-backed invoice month setup live behind `Application > Settings`, so enabling manual editing requires an explicit Settings visit and Save.

## Non-Goals

- Do not create legacy invoice rows, invoice numbers, invoice PDFs, or lesson rows for the old paper process.
- Do not support multiple currencies; this application remains DKK-only.
- Do not add password/security enforcement around Admin Mode; it is a deliberate UI edit mode, not an authorization boundary.
- Do not build a full bookkeeping/VAT return module.
- Do not migrate historical paper invoice detail lines; only month-level net/VAT totals are entered.

## Assumptions

- The business will make a clean cut-over at a month boundary.
- A month is either legacy/manual or application-backed, not both.
- Manual monthly values are replacement/override totals for that month, not additive adjustments.
- Net and VAT are editable; Total is derived as Net + VAT.
- Blank/zero manual values can remove the manual override for a month.
- Admin Mode is held only in application memory and starts disabled on every application startup.

## Current Problem / Current State

`Economy > Overview` currently reads only issued SQLite invoice rows through `EconomyOverviewQuery`, aggregates them by invoice period year/month/quarter, and exports the displayed data to CSV. Earlier paper-invoice months cannot be represented without creating duplicate invoices, which would be misleading and invalid.

## Desired End State

Economy Overview can display current application-issued invoice totals plus legacy manual month overrides. A stored cut-over month defines which months can be edited as legacy/manual months; it does not hide real application-issued invoices before that month. If no cut-over month is stored yet, all months are treated as application-backed/read-only. When Admin Mode has been checked and saved from `Application > Settings` for the current session, legacy month net/VAT values can be edited and saved in Economy Overview only when the displayed month is not already backed by an application-issued invoice. Quarters and Years immediately reflect the displayed month totals. The Invoices table remains limited to real application-issued invoices.

## Behavior-Preserving Boundary

The following must remain unchanged unless this plan explicitly says otherwise:

- User-visible behavior: normal non-admin Economy Overview remains read-only, still opens PDFs on invoice-row double-click, and still exports through the explicit CSV save dialog.
- Public UI contracts: `Economy > Overview`, year arrows, invoice rows, month/quarter/year tables, and close behavior remain available.
- Durable artifacts: existing invoice PDFs and issued invoice rows are not rewritten.
- Database behavior: issued invoices remain the source for application-backed months; `Issuing` reservations remain hidden.
- Security/trust boundaries: Google Calendar OAuth and app settings behavior remain unchanged except for adding session-only Admin Mode.
- Error handling: failed loads/saves show user-visible messages and do not report success.
- Performance/resource characteristics: overview queries remain bounded to selected/current five-year windows.

## Intentional Behavior Changes

- Add a session-only Admin Mode checkbox inside `Application > Settings`, initially unchecked each startup.
- Add a durable economy cut-over month setting inside `Application > Settings`, shown only while Admin Mode is checked. When no cut-over is stored, all months are application-backed/read-only.
- Add a durable manual monthly economy override table.
- In Admin Mode, allow editing only legacy/manual months in the Months table.
- Use manual override totals as replacement month totals for legacy months.
- Recompute Quarters and Years from displayed monthly totals.
- Prevent or flag mixed manual/app-backed months instead of silently hiding invoices.
- Include manual override data/source in the CSV export so exported totals remain auditable.

## Affected Files, Projects, Commands, Docs, Schemas, Or Artifacts

- Core:
  - `src/MySmallBusinessLedger.Core/UseCases/Invoices/EconomyOverview.cs`
  - `src/MySmallBusinessLedger.Core/UseCases/Invoices/EconomyOverviewQuery.cs`
  - new economy settings/manual override port and records
- Infrastructure:
  - `src/MySmallBusinessLedger.Infrastructure/Persistence/Entities.cs`
  - `src/MySmallBusinessLedger.Infrastructure/Persistence/LedgerDbContext.cs`
  - new SQLite repository for economy settings/manual overrides
  - EF migration and model snapshot
- App:
  - `src/MySmallBusinessLedger.App/MainWindow.xaml`
  - `src/MySmallBusinessLedger.App/ViewModels/MainWindowViewModel.cs`
  - `src/MySmallBusinessLedger.App/ViewModels/EconomyOverviewViewModel.cs`
  - `src/MySmallBusinessLedger.App/Views/EconomyOverviewView.xaml`
  - `src/MySmallBusinessLedger.App/Services/*EconomyOverview*`
  - startup composition
- Tests:
  - core economy query tests
  - persistence/migration tests
  - app service/view model/view tests
  - main menu tests
- Docs:
  - `README.md`
  - `.ai/project-profile.md`
  - `.ai/context/HANDOFF.md`
  - `docs/business-requirements-and-logic.md`

## Risks And Mitigations

- Risk: manual override silently hides issued invoices in a mixed month.
  - Mitigation: query detects mixed manual/app-backed months and reports a validation failure; UI displays the failure instead of choosing one silently.
- Risk: Admin Mode persists accidentally.
  - Mitigation: keep Admin Mode in `MainWindowViewModel`/Settings dialog result state only; do not write it to SQLite or durable settings.
- Risk: cut-over setting creates unclear defaults.
  - Mitigation: when no cut-over is stored, treat all months as application-backed/read-only; Admin Mode must explicitly set and save the cut-over from Settings before legacy month edits are allowed.
- Risk: free-text money input creates parsing/culture bugs.
  - Mitigation: ViewModel parses using Danish UI culture and invariant fallback; Core stores decimals and formats DKK consistently.
- Risk: schema migration breaks existing user database.
  - Mitigation: additive migration only; create new tables/indexes without touching invoice/student/lesson rows.

## Open Questions

None. User approved replacement/override manual month semantics, session-only Admin Mode, DKK-only storage, and an explicit stored cut-over month.

## Staged Implementation Plan

### Stage 1: Domain Contracts And Query Semantics

Type: implementation/test
Status: Completed 2026-06-28

Goal:
Add Core models/ports/use-case behavior for cut-over-aware manual month overrides without touching WPF yet.

Affected files/classes/schemas/commands/UI surfaces:
- `EconomyOverview`
- `EconomyOverviewQuery`
- new Core economy records/port
- economy query tests

Concrete steps:
1. Add Core records for manual month override, cut-over settings, month source, and save request.
2. Add a Core port for loading/saving economy settings and manual month overrides.
3. Update `EconomyOverviewQuery` to load issued invoice records and manual overrides for selected year plus previous four years.
4. Display manual overrides as replacement month totals for months before cut-over.
5. Display issued invoice totals for any month without a manual override, including pre-cut-over months that already contain application-issued invoices.
6. Detect mixed manual/app-backed data for the same month and return a failure through the app service.
7. Add tests proving manual overrides replace month totals, aggregate into quarters/years, blank months stay blank, and mixed months fail.

Behavior that must remain unchanged:
- Issued invoices still sort by invoice number.
- Invoices table still lists only real issued invoices.
- Empty months/quarters/years still display blank amount cells.

Tests to add/update:
- `EconomyOverviewQueryTests` for manual override replacement and mixed-month detection.
- Fakes updated for the new port.

Verification commands/manual checks:
- `dotnet test tests\MySmallBusinessLedger.Tests\MySmallBusinessLedger.Tests.csproj --filter "FullyQualifiedName~EconomyOverviewQuery" -nodeReuse:false -p:BaseOutputPath=C:\Tmp\MySmallBusinessLedgerTestBin\`

Risks:
- Incorrect cut-over comparison around year boundaries.

Rollback point:
- Revert Core economy contract/query changes and tests.

Stop/review criteria:
- Stop if the query needs a new public behavior not covered by approved design.

Documentation impact:
- None until Stage 5.

Follow-on actions:
- Stage 2 persistence.

Runner may continue automatically after this stage: yes.

### Stage 2: SQLite Persistence And Migration

Type: implementation/test
Status: Completed 2026-06-28

Goal:
Store cut-over month and manual month overrides durably in SQLite with additive migration.

Affected files/classes/schemas/commands/UI surfaces:
- `Entities.cs`
- `LedgerDbContext.cs`
- migrations
- new SQLite repository
- startup composition
- persistence tests

Concrete steps:
1. Add `EconomySettings` table with cut-over year/month.
2. Add `EconomyManualMonthOverrides` table keyed uniquely by year/month with net/vat decimal values and timestamps.
3. Configure EF indexes, required fields, and decimal storage.
4. Implement SQLite repository for loading/saving settings and upserting/deleting manual overrides.
5. Wire repository into `ApplicationServiceGraph`.
6. Generate/review EF migration.
7. Add persistence tests for fresh schema, migration, upsert/delete, and unique year/month behavior.

Behavior that must remain unchanged:
- Existing invoice, lesson, student, calendar, and settings tables are not rewritten.
- Startup migration still allows existing databases to open.

Tests to add/update:
- `SqlitePersistenceTests` migration/table assertions and repository round trips.

Verification commands/manual checks:
- focused persistence tests for migration and manual override repository.

Risks:
- EF migration generation may require available .NET SDK/EF tool.

Rollback point:
- Revert migration, repository, EF entity/context changes.

Stop/review criteria:
- Stop if migration would require destructive schema changes.

Documentation impact:
- None until Stage 5.

Follow-on actions:
- Stage 3 App services/view models.

Runner may continue automatically after this stage: yes.

### Stage 3: App Services, Admin Mode, And ViewModel Editing

Type: implementation/test
Status: Completed 2026-06-28

Goal:
Expose session-only Admin Mode and editable Economy Overview month rows through MVVM.

Affected files/classes/schemas/commands/UI surfaces:
- `MainWindow.xaml`
- `MainWindowViewModel`
- `IEconomyOverviewService` and `CoreEconomyOverviewService`
- `EconomyOverviewWindowLauncher`
- `EconomyOverviewViewModel`
- tests

Concrete steps:
1. Add session-only Admin Mode state owned by the main application session.
2. Add Admin Mode checkbox to `Application > Settings` and return the saved session value to the main window.
3. Pass Admin Mode state to Economy Overview view models.
4. Add first application-backed invoice month editing/display to Settings in Admin Mode.
5. Add editable month net/VAT fields only for allowed legacy months while Admin Mode is enabled, excluding months already backed by application-issued invoices.
6. Add Save/Cancel manual changes commands.
7. Call application service to persist cut-over and manual overrides, then reload/recompute overview.
8. Preserve non-admin read-only behavior.

Behavior that must remain unchanged:
- Settings remains the deliberate, higher-friction place for durable company/calendar/printer settings and economy cut-over setup.
- Admin Mode is not persisted and starts disabled each startup.
- Export and PDF opening behavior stay unchanged.

Tests to add/update:
- Settings dialog/view model tests for Admin Mode and first application-backed invoice month.
- ViewModel tests for admin editability, save/cancel, disabled editing outside Admin Mode, and cut-over enforcement.
- App service tests for save failure reporting.

Verification commands/manual checks:
- focused app ViewModel/service tests for Economy Overview and MainWindow menu.

Risks:
- WPF DataGrid editing can update fields without committing before Save.

Rollback point:
- Revert App service/view model/XAML changes.

Stop/review criteria:
- Stop if a new dialog/interaction is needed beyond approved Admin Mode controls.

Documentation impact:
- None until Stage 5.

Follow-on actions:
- Stage 4 XAML polish and CSV.

Runner may continue automatically after this stage: yes.

### Stage 4: XAML UI And CSV Auditability

Type: implementation/test
Status: Completed 2026-06-28

Goal:
Make the Economy Overview UI clear and export manual-source data in a machine-friendly way.

Affected files/classes/schemas/commands/UI surfaces:
- `EconomyOverviewView.xaml`
- `EconomyOverviewCsvFormatter`
- view tests
- service tests

Concrete steps:
1. Add compact manual Save/Cancel controls to Economy Overview without disrupting the current non-admin layout.
2. Make Months Net/VAT cells editable only through bound editable display fields when Admin Mode allows it.
3. Keep Total read-only and derived.
4. Show source/validation state clearly enough that manual values are not confused with invoice rows.
5. Extend CSV with a source/manual section or source column so manual override data is auditable while preserving machine-friendly decimals.
6. Update view tests for table chrome/editability expectations.

Behavior that must remain unchanged:
- Current visual table border polish stays intact.
- Summary tables do not expose resize handles or empty columns.

Tests to add/update:
- CSV formatter tests for manual month rows/source.
- View tests for admin editability and existing table chrome.

Verification commands/manual checks:
- focused Economy Overview app tests.

Risks:
- Layout crowding in the right-side tables.

Rollback point:
- Revert XAML/CSV changes.

Stop/review criteria:
- Stop if a visual redesign beyond compact admin controls is required.

Documentation impact:
- None until Stage 5.

Follow-on actions:
- Stage 5 docs and full verification.

Runner may continue automatically after this stage: yes.

### Stage 5: Documentation, Review, And Full Verification

Type: documentation/verification/review
Status: Completed 2026-06-28

Goal:
Update docs/context, run verification, and review the finished cross-layer feature.

Affected files/classes/schemas/commands/UI surfaces:
- `README.md`
- `.ai/project-profile.md`
- `.ai/context/HANDOFF.md`
- `docs/business-requirements-and-logic.md`
- plan completion log

Concrete steps:
1. Document Admin Mode session-only behavior.
2. Document cut-over month and manual month override semantics.
3. Document that manual months do not create invoices/PDFs and are not mixed with app-backed invoice months.
4. Run focused and full test/build verification.
5. Run follow-on code review, UI review, and documentation-refresh checks or record why they are not run.
6. Update this plan completion log.

Behavior that must remain unchanged:
- Existing documented Economy Overview behavior remains accurate for non-admin/application-backed months.

Tests to add/update:
- None unless docs reveal mismatch.

Verification commands/manual checks:
- `dotnet test MySmallBusinessLedger.sln -nodeReuse:false -p:BaseOutputPath=C:\Tmp\MySmallBusinessLedgerTestBin\`
- `dotnet build MySmallBusinessLedger.sln -nodeReuse:false -p:BaseOutputPath=C:\Tmp\MySmallBusinessLedgerTestBin\`
- `git diff --check`

Risks:
- Full test suite may expose unrelated dirty-worktree issues; report rather than reverting unrelated work.

Rollback point:
- Revert documentation changes only if implementation remains valid.

Stop/review criteria:
- Stop on failed required verification.

Documentation impact:
- Required.

Follow-on actions:
- Recommend moving plan to completed after user acceptance.

Runner may continue automatically after this stage: yes.

## Verification Strategy

- Core query tests for replacement semantics, cut-over comparison, and mixed-month failure.
- SQLite persistence tests for migration/schema and upsert/delete behavior.
- App ViewModel/service tests for Admin Mode, edit/save/cancel, and error display.
- CSV tests for machine-friendly output and manual-source auditability.
- WPF view tests for table chrome/editability.
- Full solution test and build before completion.

## Follow-On Review And Documentation

- Code review required: yes - cross-layer database/UI/financial behavior.
- Architecture review required: no - the feature follows existing Core/App/Infrastructure layering.
- UI review required: yes - Admin Mode editability affects user-visible safety.
- Documentation refresh required: yes - durable behavior, schema, and UI behavior change.

## Review Stop Points

- Stop if migration would destructively change existing user data.
- Stop if mixed-month handling cannot be made fail-safe.
- Stop if WPF editing requires a materially different interaction model.
- Stop on failed build/tests unless the failure is clearly unrelated and explicitly reported.

## Final User Test Instructions

Post-completion user test: mandatory.

Prerequisites/setup:
- Use a test or backup copy of the SQLite database before entering real legacy totals.
- Know the first application-backed invoice month.

Exact user actions or commands:
1. Start the application.
2. Open `Application > Settings` and confirm Admin Mode is unchecked.
3. Open `Economy > Overview` and confirm month totals are read-only.
4. Open `Application > Settings`, check Admin Mode, set the first application-backed invoice month, and save Settings.
5. Open `Economy > Overview` and enter net/VAT values for one legacy month before the cut-over.
6. Save the manual month changes in Economy Overview.
7. Confirm the Month, Quarter, and Year tables update.
8. Export CSV and verify the manual values are included as machine-friendly decimals.
9. Close and restart the application.
10. Confirm Admin Mode is unchecked again in `Application > Settings`, while the saved manual month totals still display.

Expected successful result:
- Manual values persist in SQLite, display as DKK totals, aggregate into quarters/years, and export to CSV.
- No invoice number, invoice row, lesson row, or PDF is created for manual legacy values.

Expected failure/safety result, if relevant:
- Application-backed months cannot be manually edited.
- A month with both a manual override and issued application invoices is reported as an error instead of silently choosing one.

Where to inspect artifacts, reports, logs, database state, or UI state:
- UI: `Economy > Overview`.
- Database: `%APPDATA%\visualtech.dk\MySmallBusinessLedger\ledger.db`.
- CSV: user-selected export destination.

Cleanup/reset instructions:
- Enable Admin Mode from Settings, remove test manual month values from Economy Overview, or restore the database backup/test copy.

## Completion Log

- 2026-06-28:
  - Stage: Plan created
  - Files changed: `.ai/implementation-plans/active/2026-06-28-economy-manual-month-overrides.md`
  - Summary: Captured approved Admin Mode, cut-over, and manual month override design.
  - Tests/verification run: Not run; plan-only change.
  - Results: Plan ready for `plan-runner`.
  - Deviations: None.
  - Risks/open questions: None.
- 2026-06-28:
  - Stage: Stage 1 - Domain Contracts And Query Semantics
  - Files changed: `src/MySmallBusinessLedger.Core/Ports/IEconomyRepository.cs`, `src/MySmallBusinessLedger.Core/UseCases/Invoices/EconomyOverview.cs`, `src/MySmallBusinessLedger.Core/UseCases/Invoices/EconomyOverviewQuery.cs`, `src/MySmallBusinessLedger.App/Services/CoreEconomyOverviewService.cs`, `tests/MySmallBusinessLedger.Tests/Core/UseCases/EconomyOverviewQueryTests.cs`
  - Summary: Added Core economy settings/manual override contracts; changed Economy Overview query to aggregate manual overrides before cut-over and application invoices for non-manual months; mixed manual/application data now returns validation failures.
  - Tests/verification run: `dotnet test tests\MySmallBusinessLedger.Tests\MySmallBusinessLedger.Tests.csproj --filter "FullyQualifiedName~EconomyOverviewQuery" -nodeReuse:false -p:BaseOutputPath=C:\Tmp\MySmallBusinessLedgerTestBin\`
  - Results: Passed, 4/4 tests.
  - Deviations: No cut-over setting now means all months are application-backed/read-only, avoiding a hidden default that could classify existing invoices as legacy.
  - Risks/open questions: None.
- 2026-06-28:
  - Stage: Stage 2 - SQLite Persistence And Migration
  - Files changed: `src/MySmallBusinessLedger.Infrastructure/Persistence/Entities.cs`, `src/MySmallBusinessLedger.Infrastructure/Persistence/LedgerDbContext.cs`, `src/MySmallBusinessLedger.Infrastructure/Persistence/SqliteEconomyRepository.cs`, `src/MySmallBusinessLedger.Infrastructure/Persistence/Migrations/20260627232113_AddEconomyManualMonthOverrides.cs`, `src/MySmallBusinessLedger.Infrastructure/Persistence/Migrations/20260627232113_AddEconomyManualMonthOverrides.Designer.cs`, `src/MySmallBusinessLedger.Infrastructure/Persistence/Migrations/LedgerDbContextModelSnapshot.cs`, `src/MySmallBusinessLedger.App/Startup/ApplicationServiceGraph.cs`, `tests/MySmallBusinessLedger.Tests/Infrastructure/Persistence/SqlitePersistenceTests.cs`
  - Summary: Added additive SQLite tables for economy cut-over settings and manual month overrides, implemented upsert/delete repository behavior, and wired the repository into application composition.
  - Tests/verification run: `dotnet test tests\MySmallBusinessLedger.Tests\MySmallBusinessLedger.Tests.csproj --filter "FullyQualifiedName~EconomySettingsAndManualMonthOverridesCanBeSavedLoadedUpdatedAndCleared|FullyQualifiedName~MigrationsCreateCurrentSchemaForFreshDatabase" -nodeReuse:false -p:BaseOutputPath=C:\Tmp\MySmallBusinessLedgerTestBin\`
  - Results: Passed, 2/2 tests.
  - Deviations: EF migration generation required elevated filesystem access after sandbox denial in `obj`; generated migration is additive only.
  - Risks/open questions: None.
- 2026-06-28:
  - Stage: Stage 3 - App Services, Admin Mode, And ViewModel Editing
  - Files changed: `src/MySmallBusinessLedger.App/MainWindow.xaml`, `src/MySmallBusinessLedger.App/ViewModels/MainWindowViewModel.cs`, `src/MySmallBusinessLedger.App/ViewModels/EconomyOverviewViewModel.cs`, `src/MySmallBusinessLedger.App/Services/IEconomyOverviewLauncher.cs`, `src/MySmallBusinessLedger.App/Services/EconomyOverviewWindowLauncher.cs`, `src/MySmallBusinessLedger.App/Services/IEconomyOverviewService.cs`, `src/MySmallBusinessLedger.App/Services/CoreEconomyOverviewService.cs`, `src/MySmallBusinessLedger.Core/Ports/IEconomyRepository.cs`, `src/MySmallBusinessLedger.Infrastructure/Persistence/SqliteEconomyRepository.cs`, and related tests.
  - Summary: Added session-only Admin Mode state, passed it into Economy Overview, added manual cut-over/month editing state and Save/Cancel commands, and added atomic settings-plus-overrides repository save support.
  - Tests/verification run: `dotnet test tests\MySmallBusinessLedger.Tests\MySmallBusinessLedger.Tests.csproj --filter "FullyQualifiedName~EconomyOverviewViewModel" -nodeReuse:false -p:BaseOutputPath=C:\Tmp\MySmallBusinessLedgerTestBin\`; `dotnet test tests\MySmallBusinessLedger.Tests\MySmallBusinessLedger.Tests.csproj --filter "FullyQualifiedName~ApplicationMenuShowsSettingsAdminModeAndAbout|FullyQualifiedName~EconomyOverviewCommandOpensEconomyOverview" -nodeReuse:false -p:BaseOutputPath=C:\Tmp\MySmallBusinessLedgerTestBin\`
  - Results: Passed, 7/7 EconomyOverviewViewModel tests and 2/2 MainWindow menu/launcher tests.
  - Deviations: None.
  - Risks/open questions: XAML editing templates and CSV audit output still pending in Stage 4.
- 2026-06-28:
  - Stage: Stage 4 - XAML UI And CSV Auditability
  - Files changed: `src/MySmallBusinessLedger.App/Views/EconomyOverviewView.xaml`, `src/MySmallBusinessLedger.App/Services/EconomyOverviewCsvFormatter.cs`, `tests/MySmallBusinessLedger.Tests/App/Views/EconomyOverviewViewTests.cs`, `tests/MySmallBusinessLedger.Tests/App/Services/EconomyOverviewServiceTests.cs`
  - Summary: Added compact manual-edit controls to Economy Overview, made legacy month net/VAT cells editable through XAML templates while keeping totals derived/read-only, and extended CSV export with a machine-friendly `Source` column for issued invoice, issued month, manual override, and displayed aggregate rows.
  - Tests/verification run: `dotnet test tests\MySmallBusinessLedger.Tests\MySmallBusinessLedger.Tests.csproj --filter "FullyQualifiedName~EconomyOverviewView|FullyQualifiedName~EconomyOverviewViewModel|FullyQualifiedName~EconomyOverviewService|FullyQualifiedName~CsvFormatter" -nodeReuse:false -p:BaseOutputPath=C:\Tmp\MySmallBusinessLedgerTestBin\`; `dotnet build MySmallBusinessLedger.sln -nodeReuse:false -p:BaseOutputPath=C:\Tmp\MySmallBusinessLedgerTestBin\`
  - Results: Passed, 13/13 focused tests. Build passed with 0 warnings and 0 errors.
  - Deviations: Export provenance is represented as a `Source` column instead of an additional source section, preserving one row shape for machine-friendly CSV consumers.
  - Risks/open questions: None. Documentation, final verification, and follow-on reviews remain in Stage 5.
- 2026-06-28:
  - Stage: Stage 5 - Documentation, Review, And Full Verification
  - Files changed: `README.md`, `.ai/project-profile.md`, `.ai/context/HANDOFF.md`, `docs/business-requirements-and-logic.md`, `src/MySmallBusinessLedger.Infrastructure/Persistence/SqliteEconomyRepository.cs`, `tests/MySmallBusinessLedger.Tests/Infrastructure/Persistence/SqlitePersistenceTests.cs`, and this plan.
  - Summary: Documented session-only Admin Mode, DKK-only manual legacy month overrides, cut-over semantics, CSV source auditability, and no invoice/PDF/lesson creation for manual months. Follow-on review found and fixed persistence edges: saving the cut-over now prunes new or stored manual overrides that fall in application-backed months, and direct non-zero manual overrides are rejected unless a cut-over month exists.
  - Tests/verification run: `dotnet test tests\MySmallBusinessLedger.Tests\MySmallBusinessLedger.Tests.csproj --filter "FullyQualifiedName~EconomySettingsAndManualMonthOverridesCanBeSavedLoadedUpdatedAndCleared|FullyQualifiedName~ManualMonthOverridesRequireSavedCutOver" -nodeReuse:false -p:BaseOutputPath=C:\Tmp\MySmallBusinessLedgerTestBin\`; `dotnet test MySmallBusinessLedger.sln -nodeReuse:false -p:BaseOutputPath=C:\Tmp\MySmallBusinessLedgerTestBin\`; `dotnet build MySmallBusinessLedger.sln -nodeReuse:false -p:BaseOutputPath=C:\Tmp\MySmallBusinessLedgerTestBin\`; `git diff --check`
  - Results: Focused persistence regressions passed, 2/2 tests. Full solution tests passed, 389/389 tests. Full solution build passed with 0 warnings and 0 errors. `git diff --check` passed with no whitespace errors; Git reported LF-to-CRLF working-copy warnings for touched files.
  - Deviations: Repository saves now proactively remove new or stored manual overrides on or after the saved cut-over month, and refuse non-zero manual override saves before a cut-over exists, to preserve the approved "manual overrides only before cut-over, issued invoices never silently mixed with manual overrides" invariant.
  - Risks/open questions: Plan remains active until Ruben completes the mandatory user acceptance test; move it to completed after acceptance.
- 2026-06-28:
  - Stage: Post-plan UI refinement - Settings-owned Admin Mode and cut-over setup
  - Files changed: `src/MySmallBusinessLedger.App/MainWindow.xaml`, `src/MySmallBusinessLedger.App/ViewModels/MainWindowViewModel.cs`, `src/MySmallBusinessLedger.App/ViewModels/SettingsViewModel.cs`, `src/MySmallBusinessLedger.App/ViewModels/EconomyOverviewViewModel.cs`, `src/MySmallBusinessLedger.App/Views/SettingsView.xaml`, `src/MySmallBusinessLedger.App/Views/EconomyOverviewView.xaml`, `src/MySmallBusinessLedger.App/Services/IAppSettingsService.cs`, `src/MySmallBusinessLedger.App/Services/CoreAppSettingsService.cs`, `src/MySmallBusinessLedger.App/Services/ISettingsLauncher.cs`, `src/MySmallBusinessLedger.App/Services/SettingsWindowLauncher.cs`, `src/MySmallBusinessLedger.App/Services/InvoicePrinterConfigurationService.cs`, `src/MySmallBusinessLedger.App/Startup/ApplicationServiceGraph.cs`, related app tests, `README.md`, `.ai/project-profile.md`, `.ai/context/HANDOFF.md`, `docs/business-requirements-and-logic.md`, and this plan.
  - Summary: Removed the top-level Admin Mode menu item, moved Admin Mode and first application-backed invoice month setup into `Application > Settings`, preserved Admin Mode as session-only state returned by the saved Settings dialog, and left Economy Overview with manual Save/Cancel controls only after Admin Mode is enabled.
  - Tests/verification run: `dotnet test tests\MySmallBusinessLedger.Tests\MySmallBusinessLedger.Tests.csproj --filter "FullyQualifiedName~SettingsViewModel|FullyQualifiedName~MainWindowViewModelTests.ApplicationMenuShows|FullyQualifiedName~SettingsRefreshesDashboardAfterSave|FullyQualifiedName~EconomyOverviewCommandOpensEconomyOverview|FullyQualifiedName~EconomyOverviewViewModel|FullyQualifiedName~EconomyOverviewViewTests|FullyQualifiedName~AppSettingsServiceLoadsAndSavesEconomySettings|FullyQualifiedName~InvoicePrinterConfigurationServiceTests" -nodeReuse:false -p:BaseOutputPath=C:\Tmp\MySmallBusinessLedgerTestBin\`; `dotnet test MySmallBusinessLedger.sln -nodeReuse:false -p:BaseOutputPath=C:\Tmp\MySmallBusinessLedgerTestBin\`; `dotnet build MySmallBusinessLedger.sln -nodeReuse:false -p:BaseOutputPath=C:\Tmp\MySmallBusinessLedgerTestBin\`; `git diff --check`; stale top-level Admin Mode menu text search.
  - Results: Focused app/settings slice passed, 39/39 tests. Full solution tests passed, 392/392 tests. Full solution build passed with 0 warnings and 0 errors. `git diff --check` passed with no whitespace errors; Git reported LF-to-CRLF working-copy warnings for touched files. Stale top-level Admin Mode menu text search found no matches.
  - Deviations: The cut-over month is now edited in Settings rather than Economy Overview so the legacy-entry controls require deliberate navigation before use.
  - Risks/open questions: Plan remains active until Ruben completes the mandatory user acceptance test; move it to completed after acceptance.
- 2026-06-28:
  - Stage: Bugfix - Month-boundary cut-over input and conflict display
  - Files changed: `src/MySmallBusinessLedger.App/ViewModels/SettingsViewModel.cs`, `src/MySmallBusinessLedger.Core/UseCases/Invoices/EconomyOverviewQuery.cs`, `src/MySmallBusinessLedger.App/ViewModels/EconomyOverviewViewModel.cs`, related regression tests, `README.md`, `.ai/project-profile.md`, `.ai/context/HANDOFF.md`, `docs/business-requirements-and-logic.md`, and this plan.
  - Summary: Fixed the misleading day-level first application month input by normalizing any selected date to the first day of its month immediately. Economy Overview now returns and displays overview rows when a blocking economy data error exists, with export blocked and an actionable message naming the conflicting invoice/month. A later bugfix below keeps Admin Mode manual Save/Cancel available as the correction path when overview rows are visible.
  - Tests/verification run: `dotnet test tests\MySmallBusinessLedger.Tests\MySmallBusinessLedger.Tests.csproj --filter "FullyQualifiedName~FirstApplicationInvoiceMonthNormalizesSelectedDayImmediately|FullyQualifiedName~LoadAsyncFailsWhenIssuedInvoiceExistsBeforeCutOver|FullyQualifiedName~LoadAsyncDisplaysReturnedOverviewButDisablesActionsWhenDataHasBlockingError" -nodeReuse:false -p:BaseOutputPath=C:\Tmp\MySmallBusinessLedgerTestBin\`; `dotnet test tests\MySmallBusinessLedger.Tests\MySmallBusinessLedger.Tests.csproj --filter "FullyQualifiedName~EconomyOverview|FullyQualifiedName~SettingsViewModel" -nodeReuse:false -p:BaseOutputPath=C:\Tmp\MySmallBusinessLedgerTestBin\`; `dotnet test MySmallBusinessLedger.sln -nodeReuse:false -p:BaseOutputPath=C:\Tmp\MySmallBusinessLedgerTestBin\`; `dotnet build MySmallBusinessLedger.sln -nodeReuse:false -p:BaseOutputPath=C:\Tmp\MySmallBusinessLedgerTestBin\`
  - Results: Regression tests passed, 3/3. Focused Economy/Settings slice passed, 43/43. Full solution tests passed, 394/394. Full solution build passed with 0 warnings and 0 errors.
  - Deviations: Cut-over consistency remains a blocking data-integrity error; the UI now renders the conflicted overview for orientation rather than presenting empty tables.
  - Risks/open questions: Plan remains active until Ruben completes the mandatory user acceptance test; move it to completed after acceptance.
- 2026-06-28:
  - Stage: Bugfix - Pre-cut-over issued invoice visibility and wrapped error layout
  - Files changed: `src/MySmallBusinessLedger.Core/UseCases/Invoices/EconomyOverviewQuery.cs`, `src/MySmallBusinessLedger.App/ViewModels/EconomyOverviewViewModel.cs`, `src/MySmallBusinessLedger.App/Views/EconomyOverviewView.xaml`, `src/MySmallBusinessLedger.App/Views/EconomyOverviewView.xaml.cs`, related regression tests, `README.md`, `.ai/project-profile.md`, `.ai/context/HANDOFF.md`, `docs/business-requirements-and-logic.md`, and this plan.
  - Summary: Corrected the cut-over invariant so a stored first application-backed invoice month does not hide or invalidate real issued invoices before that month. The query now blocks only same-month manual override/application-issued invoice conflicts, issued pre-cut-over months remain visible/read-only, and Economy Overview wraps blocking error text in a bottom status area while expanding the window only when an error is present.
  - Tests/verification run: `dotnet test tests\MySmallBusinessLedger.Tests\MySmallBusinessLedger.Tests.csproj --filter "FullyQualifiedName~EconomyOverviewQueryTests|FullyQualifiedName~EconomyOverviewViewModelTests|FullyQualifiedName~EconomyOverviewViewTests|FullyQualifiedName~SettingsViewModel" -nodeReuse:false -p:BaseOutputPath=C:\Tmp\MySmallBusinessLedgerTestBin\`; `dotnet test MySmallBusinessLedger.sln -nodeReuse:false -p:BaseOutputPath=C:\Tmp\MySmallBusinessLedgerTestBin\`; `dotnet build MySmallBusinessLedger.sln -nodeReuse:false -p:BaseOutputPath=C:\Tmp\MySmallBusinessLedgerTestBin\`; `git diff --check`
  - Results: Focused Economy/Settings slice passed, 41/41. Full solution tests passed, 397/397. Full solution build passed with 0 warnings and 0 errors. `git diff --check` passed with no whitespace errors; Git reported LF-to-CRLF working-copy warnings for touched files.
  - Deviations: Same-month manual/application conflicts remain blocking data-integrity errors, but issued invoices before cut-over are no longer errors by themselves.
  - Risks/open questions: Plan remains active until Ruben completes the mandatory user acceptance test; move it to completed after acceptance.
- 2026-06-28:
  - Stage: Bugfix - Admin Mode Esc close guard
  - Files changed: `src/MySmallBusinessLedger.App/Views/EconomyOverviewView.xaml`, `src/MySmallBusinessLedger.App/Views/EconomyOverviewView.xaml.cs`, `tests/MySmallBusinessLedger.Tests/App/Views/EconomyOverviewViewTests.cs`, `.ai/context/HANDOFF.md`, and this plan.
  - Summary: Prevented the Economy Overview Close button from acting as the window Esc cancel target while Admin Mode/manual controls are active, so accidental Esc presses no longer close the window and discard typed manual values. Added an explicit Close-button click handler so clicking Close still closes in Admin Mode. Normal read-only Economy Overview still keeps Esc as a convenience close.
  - Tests/verification run: `dotnet test tests\MySmallBusinessLedger.Tests\MySmallBusinessLedger.Tests.csproj --filter "FullyQualifiedName~EconomyOverviewViewTests.CloseButtonDoesNotCloseWindowOnEscapeInAdminMode" -nodeReuse:false -p:BaseOutputPath=C:\Tmp\MySmallBusinessLedgerTestBin\`; `dotnet test tests\MySmallBusinessLedger.Tests\MySmallBusinessLedger.Tests.csproj --filter "FullyQualifiedName~EconomyOverviewViewTests.CloseButtonStillClosesWindowInAdminMode" -nodeReuse:false -p:BaseOutputPath=C:\Tmp\MySmallBusinessLedgerTestBin\`; `dotnet test tests\MySmallBusinessLedger.Tests\MySmallBusinessLedger.Tests.csproj --filter "FullyQualifiedName~EconomyOverviewViewTests|FullyQualifiedName~EconomyOverviewViewModelTests" -nodeReuse:false -p:BaseOutputPath=C:\Tmp\MySmallBusinessLedgerTestBin\`; `dotnet test MySmallBusinessLedger.sln -nodeReuse:false -p:BaseOutputPath=C:\Tmp\MySmallBusinessLedgerTestBin\`; `dotnet build MySmallBusinessLedger.sln -nodeReuse:false -p:BaseOutputPath=C:\Tmp\MySmallBusinessLedgerTestBin\`; `git diff --check`
  - Results: Esc regression test failed before the XAML fix and passed after it. Close-click regression test failed before the explicit click handler and passed after it. Focused Economy Overview view/view-model slice passed, 15/15. Full solution tests passed, 399/399. Full solution build passed with 0 warnings and 0 errors. `git diff --check` passed with no whitespace errors; Git reported LF-to-CRLF working-copy warnings for touched files.
  - Deviations: This prevents Esc only in Admin Mode; explicit Close and window-close behavior remain available.
  - Risks/open questions: Plan remains active until Ruben completes the mandatory user acceptance test; move it to completed after acceptance.
- 2026-06-28:
  - Stage: Bugfix - Manual save ignores issued-invoice months
  - Files changed: `src/MySmallBusinessLedger.App/ViewModels/EconomyOverviewViewModel.cs`, `src/MySmallBusinessLedger.App/Services/CoreEconomyOverviewService.cs`, `src/MySmallBusinessLedger.App/Startup/ApplicationServiceGraph.cs`, `tests/MySmallBusinessLedger.Tests/App/ViewModels/EconomyOverviewViewModelTests.cs`, `tests/MySmallBusinessLedger.Tests/App/Services/EconomyOverviewServiceTests.cs`, `README.md`, `.ai/project-profile.md`, `.ai/context/HANDOFF.md`, `docs/business-requirements-and-logic.md`, and this plan.
  - Summary: Fixed Economy Overview manual save so editing a true manual legacy month no longer harvests read-only issued-invoice month totals as manual overrides. The ViewModel now sends only editable manual rows, the app service clears stale manual overrides for months backed by issued invoices before reload, and Admin Mode keeps Save/Cancel available on visible blocking-error overviews so a bad manual row can be removed instead of stranding the user.
  - Tests/verification run: `dotnet test tests\MySmallBusinessLedger.Tests\MySmallBusinessLedger.Tests.csproj --filter "FullyQualifiedName~AdminModeSaveSkipsReadOnlyIssuedInvoiceLegacyMonths|FullyQualifiedName~SaveManualMonthOverridesDoesNotPersistManualTotalsForIssuedInvoiceMonths" -nodeReuse:false -p:BaseOutputPath=C:\Tmp\MySmallBusinessLedgerTestBin\`; `dotnet test tests\MySmallBusinessLedger.Tests\MySmallBusinessLedger.Tests.csproj --filter "FullyQualifiedName~LoadAsyncDisplaysReturnedOverviewDisablesExportButKeepsManualCorrectionActionsAvailable" -nodeReuse:false -p:BaseOutputPath=C:\Tmp\MySmallBusinessLedgerTestBin\`; `dotnet test tests\MySmallBusinessLedger.Tests\MySmallBusinessLedger.Tests.csproj --filter "FullyQualifiedName~AdminModeSaveSkipsReadOnlyIssuedInvoiceLegacyMonths|FullyQualifiedName~SaveManualMonthOverridesDoesNotPersistManualTotalsForIssuedInvoiceMonths|FullyQualifiedName~LoadAsyncDisplaysReturnedOverviewDisablesExportButKeepsManualCorrectionActionsAvailable" -nodeReuse:false -p:BaseOutputPath=C:\Tmp\MySmallBusinessLedgerTestBin\`; `dotnet test tests\MySmallBusinessLedger.Tests\MySmallBusinessLedger.Tests.csproj --filter "FullyQualifiedName~EconomyOverview|FullyQualifiedName~SettingsViewModel" -nodeReuse:false -p:BaseOutputPath=C:\Tmp\MySmallBusinessLedgerTestBin\`; `dotnet test MySmallBusinessLedger.sln -nodeReuse:false -p:BaseOutputPath=C:\Tmp\MySmallBusinessLedgerTestBin\`; `dotnet build MySmallBusinessLedger.sln -nodeReuse:false -p:BaseOutputPath=C:\Tmp\MySmallBusinessLedgerTestBin\`
  - Results: Manual-save regression tests failed before the implementation and passed after it. Correction-action test failed before the command availability split and passed after it. Focused Economy/Settings slice passed, 50/50. Full solution tests passed, 401/401. Full solution build passed with 0 warnings and 0 errors.
  - Deviations: Blocking economy data still disables export, but Admin Mode manual Save/Cancel remain available when an overview snapshot is visible so the user can remove invalid manual totals from issued-invoice months.
  - Risks/open questions: Plan remains active until Ruben completes the mandatory user acceptance test; move it to completed after acceptance.
