# Implementation Plan: Economy Overview

Status: Active
Created: 2026-06-27
Last updated: 2026-06-27
Owner/context: Ruben / My Small Business Ledger v1.10, database-backed financial overview feature

## Related Docs And Standards

- `AGENTS.md`
- `README.md`
- `docs/business-requirements-and-logic.md`
- `.ai/project-profile.md`
- `.ai/context/HANDOFF.md`
- `.ai/standards/architecture-principles.md`
- `.ai/standards/coding-standards.md`
- `.ai/standards/testing-and-verification.md`
- `.ai/standards/domain-data-integrity.md`
- `.ai/standards/database-standards.md`
- `.ai/standards/persistence-and-artifacts.md`
- `.ai/standards/ui-design-standards.md`
- `.ai/standards/wpf-mvvm-standards.md`

## Related Skills

- `implementation-plan` for this plan
- `plan-runner` to execute this plan stage by stage
- `code-review` after implementation because this changes financial reporting across layers
- `ui-review` after implementation because this adds a new WPF window/menu workflow
- `documentation-refresh` after implementation because public UI behavior and reporting/export rules change

## Goal

Add `Economy > Overview`, opening a database-backed Economy Overview window for the selected calendar year. The window shows issued invoices, month totals, quarter totals, and year-to-date totals, supports year back/forward navigation, exports the displayed data to one CSV file, and opens an invoice PDF on invoice-row double-click.

## Non-Goals

- Do not parse invoice PDF files for financial data.
- Do not add accounting ledger entries, payments export, VAT return filing, or bookkeeping-system integration.
- Do not change invoice issue, print, payment, or PDF generation behavior.
- Do not add invoice editing or correction workflows.
- Do not split one invoice across multiple months or quarters.
- Do not introduce new production dependencies unless a later approved decision explicitly adds one.

## Assumptions

- The overview uses SQLite `IssuedInvoices` rows and stored issued totals.
- Only `InvoiceIssueStatus.Issued` invoices are included; `Issuing` recovery/reservation rows remain hidden.
- The selected report year is matched by `InvoicePeriod.From.Year`.
- Month and quarter aggregation uses `InvoicePeriod.From.Month`; if a future invoice period spans multiple months, it is reported in the start month/quarter.
- Initial year is the app clock's current calendar year.
- Visible and exported invoice filename is `Path.GetFileName(PdfFilePath)`, not the full path.
- CSV amounts use invariant decimal formatting such as `1234.50`.
- The UI may show reader-friendly money formatting, but CSV must stay machine-friendly.
- Blank months and quarters are included as rows with empty amount fields.
- Double-click opens the PDF; single-click only selects the row.

## Current Problem / Current State

The app has student-scoped invoice lists and invoice viewer/reprint workflows, but no year-level financial overview. Existing invoice list read models do not include stored net/VAT/total amounts, so the new report needs a dedicated financial read model rather than deriving data from PDFs or UI display models.

## Desired End State

The main menu contains `Economy > Overview`. The new window shows:

- year navigation with previous/next arrows and the selected year;
- an invoice list with invoice number, filename, net, VAT, and total;
- month totals for January through December with blank amount cells when no invoice belongs to the month;
- quarter totals for Q1 through Q4 with blank amount cells when no invoice belongs to the quarter;
- year-to-date net, VAT, and total;
- an Export CSV button;
- double-click invoice PDF opening through the existing invoice PDF launcher.

The data is loaded from a dedicated Core/App read model, with database filtering bounded to the selected period year and no PDF parsing.

## Behavior-Preserving Boundary

The following must remain unchanged unless this plan explicitly says otherwise:

- user-visible behavior: existing invoice issue/preview/viewer/reprint/payment-status/student-management workflows.
- public APIs / CLI / UI contracts: existing menu items remain in place; the new menu only adds `Economy > Overview`.
- durable file formats / artifacts / logs / reports: issued PDFs and existing app data files are unchanged; CSV export is a new user-selected artifact only.
- database schema / migrations / persistence behavior: no schema change is expected; use existing issued invoice stored columns.
- security / trust boundaries: no new network calls; CSV export is explicit user action to a user-chosen path.
- error handling / failure reporting: existing invoice PDF open and error-message patterns remain intact.
- performance/resource characteristics: queries must be bounded by the selected period year and should not load unrelated invoice history.

## Intentional Behavior Changes

- Add the `Economy > Overview` menu item.
- Add a read-only Economy Overview window for year-based invoice reporting.
- Add one-file CSV export of the displayed overview data.
- Add invoice PDF opening from the overview invoice list on double-click.

## Affected Files, Projects, Commands, Docs, Schemas, Or Artifacts

- `src/MySmallBusinessLedger.Core/Invoices/*` or `src/MySmallBusinessLedger.Core/UseCases/*` for overview records/query.
- `src/MySmallBusinessLedger.Core/Ports/IInvoiceRepository.cs` for the dedicated year-query contract.
- `src/MySmallBusinessLedger.Infrastructure/Persistence/SqliteInvoiceRepository.cs` for SQLite query implementation.
- `src/MySmallBusinessLedger.App/Services/*` for overview service, launcher, CSV export, and file dialog boundary.
- `src/MySmallBusinessLedger.App/ViewModels/*` for overview ViewModel/display models.
- `src/MySmallBusinessLedger.App/Views/*` for the WPF window.
- `src/MySmallBusinessLedger.App/MainWindow.xaml` and `MainWindowViewModel.cs` for menu command wiring.
- `src/MySmallBusinessLedger.App/Startup/*` or composition root wiring for dependency injection.
- `tests/MySmallBusinessLedger.Tests/*` for Core, persistence, ViewModel, and WPF wiring tests.
- `README.md`, `docs/business-requirements-and-logic.md`, `.ai/project-profile.md`, `.ai/context/HANDOFF.md` for behavior/context refresh.
- `MySmallBusinessLedger.sln` for implementation-plan bookkeeping.

## Risks And Mitigations

- Risk: grouping by issue date instead of invoice period would misstate month/quarter/year reporting. Mitigation: tests with invoice issue date outside the period year/month.
- Risk: accidentally including `Issuing` recovery rows. Mitigation: repository tests for exclusion.
- Risk: UI or CSV formatting could mix localized display text with machine-readable export values. Mitigation: separate display formatting from CSV formatter tests.
- Risk: ViewModel could own financial aggregation. Mitigation: aggregation lives in Core/use-case/read model; ViewModel only presents returned rows.
- Risk: CSV paths and user cancellation. Mitigation: file picker service returns no path on cancel; export command leaves state unchanged and reports only actual failures.
- Risk: WPF test cross-talk. Mitigation: existing test assembly serialization remains in place; use focused ViewModel tests where possible.

## Open Questions

None. The user has approved:

- database-backed data, not PDF parsing;
- invoice period/year, using invoice period rather than issue date;
- invoice list amount columns `Net`, `VAT`, and `Total`;
- one CSV file with a `Section` column;
- initial year as current calendar year;
- blank month/quarter rows included in UI/export;
- double-click opens invoice PDF;
- visible/exported invoice filename only;
- machine-friendly CSV amounts.

## Staged Implementation Plan

### Stage 1: Core Read Model And SQLite Query

Type: implementation + test
Status: Completed on 2026-06-27

Goal:

Create a database-backed economy overview read model/query that returns issued invoice rows for a selected period year, including stored net/VAT/total amounts and PDF filename/path data needed by the UI.

Affected files/classes/schemas/commands/UI surfaces:

- `IInvoiceRepository`
- `SqliteInvoiceRepository`
- new Core economy overview records/use case under `Core/Invoices` or `Core/UseCases`
- persistence/Core tests

Concrete steps:

1. Add records for economy invoice rows and totals data, preserving `InvoiceNumber`, `InvoicePeriod`, invoice date, PDF path, total without VAT, VAT amount, total, and currency.
2. Add repository contract for listing issued invoice overview rows by selected period year.
3. Implement SQLite query bounded by `PeriodFrom` year and `IssueStatus == Issued`, ordered by invoice number.
4. Add a Core query/use case that builds invoice rows, all 12 month summaries, all 4 quarter summaries, and year-to-date totals.
5. Use `Period.From` month/quarter for grouping.

Behavior that must remain unchanged:

- Existing invoice list, issue, payment, and viewer methods continue working.
- No database schema change unless implementation discovers a missing persisted field.

Tests to add/update:

- Query includes invoices by period year even when issue date is outside that year.
- Query excludes `Issuing` invoices.
- Month/quarter/year totals sum stored issued totals.
- Blank months and quarters are represented.
- Invoice rows expose filename from PDF path while preserving PDF path for opening.

Verification commands/manual checks:

- Focused tests for economy overview and SQLite invoice repository.
- Build if Core/Infrastructure contracts change broadly.

Risks:

- EF query translation for `DateOnly.Year` may need careful expression shape.

Rollback point:

- Revert Core records, repository contract additions, and SQLite query/tests.

Stop/review criteria:

- Stop if a schema migration becomes necessary; update plan before continuing.

Documentation impact:

- None until final docs stage.

Follow-on actions:

- Stage 2 CSV and App service.

Runner may continue automatically after this stage: yes.

### Stage 2: CSV Export And App Service Boundary

Type: implementation + test
Status: Completed on 2026-06-27

Goal:

Provide an App/Core service boundary for loading overview data and exporting the displayed overview as one CSV file with stable machine-readable fields.

Affected files/classes/schemas/commands/UI surfaces:

- App service interfaces/classes
- CSV formatter/export service
- tests for CSV output

Concrete steps:

1. Add an app-facing `IEconomyOverviewService` or equivalent that calls the Core query.
2. Add a CSV formatter that accepts the overview snapshot and returns CSV text.
3. Include one header and rows with `Section`, label/type fields, invoice fields when relevant, and `Net`, `VAT`, `Total` amount columns.
4. Format CSV decimal values with invariant culture and two decimal places.
5. Represent blank month/quarter rows with empty amount cells.
6. Keep filesystem/file dialog interaction outside Core.

Behavior that must remain unchanged:

- Existing exports/artifacts, if any, remain unchanged.

Tests to add/update:

- CSV includes invoice, month, quarter, and year-to-date sections.
- CSV includes blank month/quarter rows.
- CSV escapes commas/quotes/newlines.
- CSV amount fields use invariant decimal formatting.
- CSV uses filename only, not full PDF path.

Verification commands/manual checks:

- Focused CSV/service tests.

Risks:

- CSV column naming must stay stable enough for spreadsheet import.

Rollback point:

- Revert new service/CSV classes and tests.

Stop/review criteria:

- Stop if the export needs a durable file format more complex than CSV.

Documentation impact:

- Document final CSV behavior in docs stage.

Follow-on actions:

- Stage 3 UI/menu wiring.

Runner may continue automatically after this stage: yes.

### Stage 3: WPF Economy Overview Window And Menu Wiring

Type: implementation + test
Status: Completed on 2026-06-27

Goal:

Add `Economy > Overview`, a dedicated WPF overview window, year navigation, export command, and invoice double-click PDF opening.

Affected files/classes/schemas/commands/UI surfaces:

- `MainWindow.xaml`
- `MainWindowViewModel`
- new Economy Overview ViewModel/display models
- new Economy Overview View/XAML/code-behind for WPF-specific double-click and file dialog mechanics
- app startup/composition wiring
- launcher/service interfaces
- WPF/ViewModel tests

Concrete steps:

1. Add an `Economy` top-level menu with `Overview`.
2. Add command on `MainWindowViewModel` that opens the Economy Overview window through a launcher service.
3. Add overview ViewModel with selected year initialized from `IUiClock`, previous/next year commands, load state, invoice/month/quarter/year rows, export command, selected invoice, and open invoice PDF command.
4. Add a window with compact operational layout: year selector, export button, invoice grid, month grid, quarter grid, year-to-date summary, Close button.
5. Use existing `IInvoicePdfLauncher` for opening invoice PDFs.
6. Use a file-save dialog service for explicit CSV destination selection.
7. Remember the overview window position/size if it is resizable, or position only if fixed-size.
8. Keep code-behind limited to WPF event bridging such as double-click.

Behavior that must remain unchanged:

- Existing main window menus and commands remain available.
- Existing invoice double-click behavior in other windows remains unchanged.

Tests to add/update:

- Main window exposes `Economy > Overview` menu bound to the command.
- ViewModel initializes current year from clock and reloads on previous/next year.
- Export command writes CSV only after user selects a path; cancel does not write.
- Invoice double-click/open command passes the stored PDF path to `IInvoicePdfLauncher`.
- UI state handles an empty year with blank summaries and no invoices.

Verification commands/manual checks:

- Focused ViewModel/WPF tests.
- Manual smoke after full implementation: open menu, navigate years, export CSV, double-click invoice.

Risks:

- Dense financial window can become visually cluttered. Mitigation: use compact separate tables and stable fixed columns.

Rollback point:

- Revert new UI files, launcher/service wiring, and menu additions.

Stop/review criteria:

- Stop if UI layout cannot fit without horizontal scrolling at the project minimum desktop size.

Documentation impact:

- Document new menu and export behavior in docs stage.

Follow-on actions:

- Stage 4 documentation, reviews, and verification.

Runner may continue automatically after this stage: yes.

### Stage 4: Documentation Refresh, Reviews, And Final Verification

Type: documentation + review + verification
Status: Completed

Goal:

Update durable docs/context for the new Economy Overview feature, run follow-on review checks, and verify the full solution.

Affected files/classes/schemas/commands/UI surfaces:

- `README.md`
- `docs/business-requirements-and-logic.md`
- `.ai/project-profile.md`
- `.ai/context/HANDOFF.md`
- active implementation plan completion log

Concrete steps:

1. Refresh docs with the new `Economy > Overview` behavior, data source, period-year grouping, CSV export shape, and double-click PDF opening.
2. Run code-review posture on financial/data-integrity and repository/query changes.
3. Run ui-review posture on WPF layout, command availability, empty state, export behavior, and double-click behavior.
4. Run full test suite and build.
5. Update plan completion log and final user test instructions.
6. If complete, move this plan to `.ai/implementation-plans/completed/` and update `MySmallBusinessLedger.sln` plan bookkeeping.

Behavior that must remain unchanged:

- Existing docs for invoice issue/PDF/payment behavior must not be weakened.

Tests to add/update:

- No new tests expected unless review discovers gaps.

Verification commands/manual checks:

- `dotnet test MySmallBusinessLedger.sln -nodeReuse:false -p:BaseOutputPath=C:\Tmp\MySmallBusinessLedgerTestBin\`
- `dotnet build MySmallBusinessLedger.sln -nodeReuse:false -p:BaseOutputPath=C:\Tmp\MySmallBusinessLedgerTestBin\`
- `git diff --check`

Risks:

- Full suite may expose unrelated dirty-tree/WPF timing issues. Mitigation: inspect failures before changing production code.

Rollback point:

- Revert docs/context and plan status updates if implementation must remain active.

Stop/review criteria:

- Stop if full verification fails and the failure is not clearly unrelated.

Documentation impact:

- This is the documentation stage.

Follow-on actions:

- User performs final manual acceptance test.

Runner may continue automatically after this stage: yes.

## Verification Strategy

- Use focused tests for Core/use-case aggregation, SQLite query behavior, CSV formatting, ViewModel year/export/open commands, and WPF menu/window wiring.
- Run full test suite with temp output path because Visual Studio/running app may lock normal build outputs.
- Run full build with temp output path.
- Run `git diff --check`.
- Manual post-completion smoke test in the app.

## Follow-On Review And Documentation

- Code review required: yes - financial reporting and cross-layer query/export behavior.
- Architecture review required: no - the feature follows existing Core/App/Infrastructure/WPF layering.
- UI review required: yes - new WPF menu/window/export/double-click workflow.
- Documentation refresh required: yes - new public UI behavior and CSV export rules.

## Review Stop Points

- Stop before continuing if a schema migration becomes necessary.
- Stop if any new safety-sensitive financial behavior decision appears.
- Stop if verification fails in a way that could affect report correctness.

## Final User Test Instructions

Post-completion user test: mandatory.

Prerequisites/setup:

- Use a development database with at least one issued invoice in the current or a nearby year.
- Ensure the invoice PDF path points to an existing PDF if double-click PDF opening should be tested.

Exact user actions or commands:

1. Start the app.
2. Open `Economy > Overview`.
3. Confirm the initial year is the current calendar year.
4. Use the previous/next year arrows and confirm invoice/month/quarter/year-to-date data changes for the selected period year.
5. Double-click an invoice row and confirm the stored PDF opens.
6. Click `Export CSV`, choose a safe destination, and open the exported CSV in a text editor or spreadsheet.

Expected successful result:

- The overview shows issued invoice rows for the selected invoice-period year only.
- Invoice rows show invoice number, PDF filename, net, VAT, and total.
- Month and quarter rows without invoices have blank amount cells.
- Year-to-date totals match the sum of displayed invoices.
- CSV contains one file with sectioned rows for invoices, months, quarters, and year-to-date, using invariant amount values such as `1234.50`.

Expected failure/safety result, if relevant:

- Cancelling the CSV save dialog creates no file and leaves the overview open.
- Years without invoices show no invoice rows and blank month/quarter totals rather than errors.
- Missing PDF path/open failures are shown as a user-visible error and do not change report data.

Where to inspect artifacts, reports, logs, database state, or UI state:

- Inspect the selected CSV export path.
- Inspect issued invoice rows in SQLite only if needed to reconcile totals.

Cleanup/reset instructions:

- Delete any test CSV export files created during manual testing.
- Do not delete issued invoices or PDFs unless using disposable test data.

## Completion Log

- 2026-06-27:
  - Stage: Plan creation
  - Files changed: `.ai/implementation-plans/active/2026-06-27-economy-overview.md`
  - Summary: Created active implementation plan for database-backed Economy Overview.
  - Tests/verification run: Not run for plan-only change.
  - Results: Plan ready for `plan-runner`.
  - Deviations: None.
  - Risks/open questions: None.
- 2026-06-27:
  - Stage: Stage 1 - Core Read Model And SQLite Query
  - Files changed: `src/MySmallBusinessLedger.Core/Invoices/EconomyInvoiceRecord.cs`, `src/MySmallBusinessLedger.Core/Ports/IInvoiceRepository.cs`, `src/MySmallBusinessLedger.Core/UseCases/Invoices/EconomyOverview.cs`, `src/MySmallBusinessLedger.Core/UseCases/Invoices/EconomyOverviewQuery.cs`, `src/MySmallBusinessLedger.Infrastructure/Persistence/SqliteInvoiceRepository.cs`, `tests/MySmallBusinessLedger.Tests/Core/UseCases/EconomyOverviewQueryTests.cs`, `tests/MySmallBusinessLedger.Tests/Infrastructure/Persistence/SqlitePersistenceTests.cs`, `tests/MySmallBusinessLedger.Tests/Core/UseCases/UseCaseTests.cs`, `tests/MySmallBusinessLedger.Tests/App/Services/AppServiceTests.cs`
  - Summary: Added a Core economy overview read model/query and a bounded SQLite projection over issued invoices by `PeriodFrom` year, excluding `Issuing` rows and preserving stored totals plus PDF path/filename data.
  - Tests/verification run: `dotnet test tests\MySmallBusinessLedger.Tests\MySmallBusinessLedger.Tests.csproj --filter "FullyQualifiedName~EconomyOverview" -nodeReuse:false`
  - Results: Passed, 2/2 tests.
  - Deviations: The new `Core.UseCases.Economy` namespace files live under the existing `UseCases/Invoices` folder because the sandbox denied creating a new `UseCases/Economy` folder; namespace and layer ownership remain as planned.
  - Risks/open questions: None.
  - End-of-stage user action: No blocking user action.
  - Next recommended stage: Stage 2 - CSV Export And App Service Boundary.
- 2026-06-27:
  - Stage: Stage 2 - CSV Export And App Service Boundary
  - Files changed: `src/MySmallBusinessLedger.App/Services/IEconomyOverviewService.cs`, `src/MySmallBusinessLedger.App/Services/CoreEconomyOverviewService.cs`, `src/MySmallBusinessLedger.App/Services/EconomyOverviewCsvFormatter.cs`, `tests/MySmallBusinessLedger.Tests/App/Services/EconomyOverviewServiceTests.cs`
  - Summary: Added an app-facing economy overview load service with failure mapping and a CSV formatter that emits one sectioned, machine-readable file shape with invariant amount fields.
  - Tests/verification run: `dotnet test tests\MySmallBusinessLedger.Tests\MySmallBusinessLedger.Tests.csproj --filter "FullyQualifiedName~EconomyOverviewServiceTests" -nodeReuse:false`
  - Results: Passed, 2/2 tests.
  - Deviations: None.
  - Risks/open questions: None.
  - End-of-stage user action: No blocking user action.
  - Next recommended stage: Stage 3 - WPF Economy Overview Window And Menu Wiring.
- 2026-06-27:
  - Stage: Stage 3 - WPF Economy Overview Window And Menu Wiring
  - Files changed: `src/MySmallBusinessLedger.App/MainWindow.xaml`, `src/MySmallBusinessLedger.App/Startup/ApplicationBootstrapper.cs`, `src/MySmallBusinessLedger.App/Startup/ApplicationServiceGraph.cs`, `src/MySmallBusinessLedger.App/Services/IEconomyOverviewLauncher.cs`, `src/MySmallBusinessLedger.App/Services/IEconomyOverviewExportDialog.cs`, `src/MySmallBusinessLedger.App/Services/IEconomyOverviewCsvFileService.cs`, `src/MySmallBusinessLedger.App/Services/WindowsEconomyOverviewExportDialog.cs`, `src/MySmallBusinessLedger.App/Services/EconomyOverviewCsvFileService.cs`, `src/MySmallBusinessLedger.App/Services/EconomyOverviewWindowLauncher.cs`, `src/MySmallBusinessLedger.App/ViewModels/EconomyOverviewViewModel.cs`, `src/MySmallBusinessLedger.App/Views/EconomyOverviewView.xaml`, `src/MySmallBusinessLedger.App/Views/EconomyOverviewView.xaml.cs`, `src/MySmallBusinessLedger.App/ViewModels/MainWindowViewModel.cs`, `tests/MySmallBusinessLedger.Tests/App/ViewModels/EconomyOverviewViewModelTests.cs`, `tests/MySmallBusinessLedger.Tests/App/ViewModels/MainWindowViewModelTests.cs`
  - Summary: Added `Economy > Overview`, the Economy Overview WPF window, ViewModel year navigation, CSV export destination handling, invoice PDF double-click/open command, and startup composition.
  - Tests/verification run: `dotnet test tests\MySmallBusinessLedger.Tests\MySmallBusinessLedger.Tests.csproj --filter "FullyQualifiedName~EconomyOverview" -nodeReuse:false`; `dotnet test tests\MySmallBusinessLedger.Tests\MySmallBusinessLedger.Tests.csproj --filter "FullyQualifiedName~EconomyMenuShowsOverview|FullyQualifiedName~EconomyOverviewCommandOpensEconomyOverview|FullyQualifiedName~EconomyOverviewViewModelTests" -nodeReuse:false`
  - Results: Passed, 8/8 tests and 5/5 tests.
  - Deviations: None.
  - Risks/open questions: The window layout still needs final manual visual acceptance in the running WPF app.
  - End-of-stage user action: No blocking user action.
  - Next recommended stage: Stage 4 - Documentation Refresh, Reviews, And Final Verification.
- 2026-06-27:
  - Stage: Stage 4 - Documentation Refresh, Reviews, And Final Verification
  - Files changed: `README.md`, `docs/business-requirements-and-logic.md`, `.ai/project-profile.md`, `.ai/context/HANDOFF.md`, `.ai/implementation-plans/active/2026-06-27-economy-overview.md`, `src/MySmallBusinessLedger.App/Services/EconomyOverviewWindowLauncher.cs`, `src/MySmallBusinessLedger.App/ViewModels/EconomyOverviewViewModel.cs`, `tests/MySmallBusinessLedger.Tests/App/ViewModels/EconomyOverviewViewModelTests.cs`
  - Summary: Refreshed durable docs/context for `Economy > Overview`, reviewed the financial query/export/UI flow, tightened initial-load cancellation propagation found during review, and added a regression test for that contract.
  - Tests/verification run: `dotnet test tests\MySmallBusinessLedger.Tests\MySmallBusinessLedger.Tests.csproj --filter "FullyQualifiedName~EconomyOverview" -nodeReuse:false -p:BaseOutputPath=C:\Tmp\MySmallBusinessLedgerTestBin\`; `dotnet test MySmallBusinessLedger.sln -nodeReuse:false -p:BaseOutputPath=C:\Tmp\MySmallBusinessLedgerTestBin\`; `dotnet build MySmallBusinessLedger.sln -nodeReuse:false -p:BaseOutputPath=C:\Tmp\MySmallBusinessLedgerTestBin\`; `git diff --check`
  - Results: Focused Economy tests passed, 9/9; full test suite passed, 374/374; full build succeeded with 0 warnings and 0 errors; `git diff --check` reported no whitespace errors, only existing CRLF normalization warnings for touched files.
  - Deviations: None.
  - Risks/open questions: Manual visual/user acceptance remains: open `Economy > Overview`, navigate years, export CSV, and double-click an invoice PDF using development data.
  - End-of-stage user action: Perform the final user test instructions above.
  - Next recommended stage: Move plan to `.ai/implementation-plans/completed/` and update solution plan bookkeeping.
