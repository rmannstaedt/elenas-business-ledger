# Implementation plan for the Ledger’s SOLID findings

Status: Active  
Created: 2026-11-21  
Last updated: 2026-11-21  
Owner/context: Elena's Business Ledger, design findings from the invoice and commenting passes

## Goal and scope

Resolve the seven identified dependency-inversion, interface-segregation, and single-responsibility issues. Keep the four existing projects and their current startup flow. The refactor is justified because narrow contracts will make focused tests simpler, and explicit write ownership will make calendar and invoice recovery easier to reason about.

This is a behavior-preserving refactor. Scope covers the named seams, their callers, targeted tests, and affected comments and architecture documentation. Schema changes, package changes, visual redesign, versioning, installers, publishing, and unrelated component splits are outside this plan.

## Related guidance

- [Project profile](../../project-profile.md), [architecture principles](../../standards/architecture-principles.md), [coding standards](../../standards/coding-standards.md#comments-and-xml-documentation), and [verification standards](../../standards/testing-and-verification.md)
- [Business requirements and logic](../../../docs/business-requirements-and-logic.md) and [prior code-review fixes](../completed/2026-11-21-code-review-fixes.md)
- `implementation-plan` and `refactor-planning` for this plan, `plan-runner` for execution, and `code-review`, `architecture-review`, `ui-review`, and `documentation-refresh` for final verification

The active Economy, OAuth, local lesson, Student Overview, and UI plans cover their original features and acceptance work. Use their verified behavior as input and keep their existing status and scope.

## Findings and target ownership

| Finding | Current coupling | Target |
| --- | --- | --- |
| F1 · Dependency inversion | `CoreInvoiceWorkflowService` creates `LocalInvoiceFileStore` during Issue and Resume. | Startup supplies a folder-aware factory returning `IInvoiceFileStore`. |
| F2 · Interface segregation | `EconomyOverviewQuery` needs only issued-invoice reporting rows from `IInvoiceRepository`. | Economy depends on a narrow read contract implemented by the existing SQLite invoice repository. |
| F3 · Dependency inversion | `CoreAppSettingsService` depends on the SQLite calendar repository and creates its SQLite settings writer through it. | Settings uses a calendar settings reader and an atomic settings writer contract. Startup binds them to Infrastructure. |
| F4 · Single responsibility | `StartupDashboardQuery` both projects dashboard data and saves reconciled lessons. | An explicit Core refresh use case coordinates reconciliation and a projection without writes. |
| F5 · Single responsibility | `CalendarWriteReceipt` represents a write outcome and can also fetch a month of events. | The receipt carries outcomes and existing results. The calling workflow owns any follow-up refresh. |
| F6 · Interface segregation | `InvoiceCandidateDialogViewModel` uses one lookup from the full `IInvoiceWorkflowService`. | The picker depends on a candidate reader implemented by the existing workflow service. |
| F7 · Interface segregation | `InvoicePrinterConfigurationService` uses settings load/save through an interface that also exposes Google authorization. | The printer depends on Settings preferences. The Settings screen uses a separate authorization capability for Connect and Forget. |

## Behavior to preserve

- **Invoice identity and recovery.** Keep numbering rules, lesson locks, saved billing values, statuses, current PDF filenames, and error/confirmation results. The verified PDF checkpoint keeps its stored paths and checksum even after Settings changes the configured invoice folder. Printing follows durable issuance. Reprint uses the saved PDF and number.
- **Economy and invoice picker.** Preserve period-based reporting, stored totals, manual override eligibility and cleanup, CSV values, date ranges, student IDs, ordering, and the picker's existing selection and fallbacks.
- **Settings and printer selection.** Preserve the SQLite transaction spanning company, calendar, and optional Economy settings. Keep the distinction between a printer already saved, cancellation, missing company settings, and save failure. Retain the existing printer selection save path and default-calendar handling. Google sign-in remains an explicit action. Token locations, protection, and authorization checks stay compatible.
- **Calendar and dashboard.** Preserve the calendar scope and stable student and lesson identities. Acknowledged changes reach the local ledger before a follow-up dashboard list read. Rejected and uncertain outcomes keep their current hold and error behavior. Protect issued and reserved lessons, retain local records missing from calendar results, and keep planless appointments visible and unavailable to invoice until linked. Preserve what the dashboard queries and writes for the selected student, with the same date window and navigation behavior.
- **Deployment and contracts.** Use the existing SQLite schema, installer and app-data identifiers, Windows target, UI commands, and artifact formats. Internal constructor and interface changes are in scope. Check for other consumers before removing members.

## Staged implementation

Each stage starts from the previous verified state. Revert only the failed stage's changes during execution, preserving the user's existing working-tree edits. The runner can continue automatically after a stage passes its gate.

### Stage 1. Record the baseline and close test gaps

Type: verification and characterization  
Status: Not started

- Record the working-tree status and build and run the full Windows suite before moving interfaces. Use isolated build output if Visual Studio has the app open.
- Locate callers and shares of the invoice repository, the workflow service, the Settings snapshot and writer, the two receipt refresh paths, and dashboard construction. Confirm the test seams in `EconomyOverviewQueryTests`, `EconomyOverviewServiceTests`, `InvoiceViewModelTests`, `InvoicePrinterConfigurationServiceTests`, `AppServiceTests`, `GoogleCalendarGatewayTests`, `UseCaseTests`, and `SqlitePersistenceTests`.
- Add characterization tests only where the current suite misses implementation-sensitive behavior. Prioritize recovery after an invoice-folder change, an acknowledged calendar write followed by failed refresh, and a second dashboard refresh against the same event ID. Keep these tests at the owning boundary where practical.
- **Gate and rollback.** Build and record baseline results. Pause if a test contradicts the behavior listed above or exposes an unrelated failure that prevents a meaningful comparison. Stage rollback covers any added characterization tests. Record discoveries in this plan.

### Stage 2. Narrow Economy and invoice-picker reads

Type: implementation and focused verification  
Status: Not started  
Findings: F2 and F6

- Extract an issued-invoice reporting reader, such as `IEconomyInvoiceReader`, with `ListIssuedInvoiceOverviewRowsAsync`. Wire `EconomyOverviewQuery` and the issued-month check in `CoreEconomyOverviewService` to that reader. Keep the existing `SqliteInvoiceRepository` implementation, SQL, projection, and context factory. Update callers and remove the reporting member from the broader contract when they have migrated.
- Extract an invoice-candidate reader with `LoadCandidatesAsync`. Have the existing workflow service implement it. Pass the narrow contract into `InvoiceCandidateDialogViewModel` while keeping `InvoiceWorkflowWindowLauncher` able to preview, issue, recover, and view invoices. A composite interface is acceptable for the launcher if it avoids duplicate registrations.
- Simplify the relevant fakes so they implement only the dependencies used by each client. Run `EconomyOverviewQueryTests`, `EconomyOverviewServiceTests`, `InvoiceViewModelTests`, and the invoice reporting checks in `SqlitePersistenceTests`. Verify manual amounts in invoice-backed months are still screened and the picker still prefers the current student by ID.
- **Gate and rollback.** Confirm compiler references from the two read paths point at read contracts and that report totals and picker selections match the baseline. Revert the reader extraction and its test changes together if either client regresses. Update the affected type headers.

### Stage 3. Inject invoice PDF storage at startup

Type: implementation and invoice recovery verification  
Status: Not started  
Finding: F1

- Supply `CoreInvoiceWorkflowService` with a factory such as `Func<string, IInvoiceFileStore>`. Compose the concrete `LocalInvoiceFileStore` in `ApplicationServiceGraph`. Reuse one construction path for `IssueAsync` and `ResumeAsync` while preserving their current error results and reading the folder at operation time.
- Keep reservation, checksum, PDF finalization, and printing logic in the current owners. The factory receives the configured folder, and a saved checkpoint still governs where that invoice is recovered. Keep the reprint path on the stored PDF.
- Use a test factory to check that Issue and Resume resolve the expected folder. Exercise real temporary PDFs and SQLite in the existing recovery tests. Cover interruption after publication, a conflicting final file, and print failure after durable issuance. Run `UseCaseTests`, `InvoiceInfrastructureTests`, `InvoiceViewModelTests`, `InvoiceRecoveryDialogTests`, and the reservation and recovery checks in `SqlitePersistenceTests`.
- **Gate and rollback.** Verify one invoice number and one PDF per issuance attempt, and the same checksum on resume. Stop if an old invoice is redirected to the new default folder or the ordering of ledger commit, PDF publication, and printing changes. Revert the factory and wiring as a unit. Update composition and recovery comments.

### Stage 4. Invert Settings persistence and narrow printer preferences

Type: implementation and transaction verification  
Status: Not started  
Findings: F3 and F7

- Move the value-only `CalendarSettings` record to a Core namespace and update references while preserving its fields and existing storage mapping. Give `CoreAppSettingsService` an atomic writer contract and a calendar preference reader using the existing loader-delegate pattern where it fits. Bind `SqliteAppSettingsWriter` and the SQLite reader in `ApplicationServiceGraph`. Keep company, calendar, and Economy cleanup inside the writer's current transaction.
- Use `IAppSettingsService` for Load and Save. Put Connect and Forget behind a dedicated authorization contract used by `SettingsViewModel` and supplied by its launcher. Keep OAuth status visible in the Settings snapshot. Compose the production printer catalog and token-store collaborators at startup. Retain the existing calendar client behavior, token path, token migration, and error results.
- Keep `InvoicePrinterConfigurationService` on the preferences contract. Preserve the remembered-printer check, selector cancellation, current default-calendar fallback, and propagation of save errors. A printer-only persistence path would alter existing write behavior and can be considered separately.
- Run `AppServiceTests`, `SettingsViewModelTests`, `InvoicePrinterConfigurationServiceTests`, `GoogleCalendarClientFactoryTests`, and `FailedEconomyWriteRollsBackWholeSettingsSave`. Exercise a late Economy write failure and reopen Settings to verify that company, calendar, and Economy values all stayed at their prior values. Test both absent and present calendar preferences during first-time printer setup.
- **Gate and rollback.** Verify Settings still commits atomically and the printer path requires no Google authorization capability. Stop if startup refresh can prompt for OAuth or separate saves can leave partial preferences. Roll back the dependency, namespace, and composition changes together. Refresh Settings and composition comments.

### Stage 5. Give post-write calendar refresh an explicit owner

Type: implementation and failure-order verification  
Status: Not started  
Finding: F5

- Move the asynchronous list refresh out of `CalendarWriteReceipt` to a small shared operation in the Core lesson workflows, called by `EditLessonUseCase` and `DeleteLessonUseCase` after their local write. Keep the receipt's acknowledgement, errors, and any list supplied by a compatibility gateway. Its pure result mapping may remain with the type.
- Preserve the compatibility adapters in `IGoogleCalendarGateway` and the error returned when the post-write list read fails. With an existing list on the receipt, reuse it. With a production write-only receipt, list the same month once after the local write. Keep cancellation and hold handling equivalent.
- Run the edit and delete scenarios in `UseCaseTests`, `GoogleCalendarGatewayTests`, and `ACalendarMutationAndInvoiceReservationProtectTheSameLocalLesson`. Use an ordered fake or integration test to assert **acknowledged write → local update → list refresh**. Cover an invoice preview immediately after a successful deletion with a failed refresh, plus rejected and uncertain writes.
- **Gate and rollback.** Verify the receipt has no gateway call and that failed refresh cannot undo a confirmed local delete or make an uncertain edit billable. Revert the receipt and call-site changes together if ordering or outcome changes. Refresh the receipt header and the workflow comment at the commit-before-refresh boundary.

### Stage 6. Separate dashboard reconciliation from projection

Type: implementation and lifecycle verification  
Status: Not started  
Finding: F4

- Name the current entry point as a Core refresh use case, such as `LoadDashboardUseCase`. Give it an explicit reconciliation step that owns `ILessonRepository.SaveAsync`. Build the display result through a projection that has no write dependency. Update `CoreStartupDashboardService`, `ApplicationBootstrapper`, and direct test callers together. Keep the existing WPF refresh entry point.
- Carry typed calendar observations, matched local lessons, and invoice statuses through one refresh. Preserve the order of ownership resolution, selected-student filtering, reconciliation, and projection. Preserve the scoped Google event ID check and guarded legacy adoption. Feed the saved record returned by the repository into the output so a new local ID is available immediately.
- Keep the existing behavior on protected, missing, and planless rows. Preserve previous/current/next month navigation, the current transaction and locking rules, and current query volume. When a plan or local link is absent, use the existing display rules without inserting an invalid catalog ID or another student's record.
- Run dashboard scenarios in `UseCaseTests`, `AppServiceTests`, `MainWindowViewModelTests`, and relevant SQLite checks. Verify a new event is persisted once and retains its ID across two refreshes. Then reopen preview and check that only the intended eligible lesson can be billed. Cover colliding labels, changed or missing events on locked lessons, a held calendar change, and an appointment that needs a plan.
- **Gate and rollback.** Confirm the projection can be exercised with data alone and that the use case still applies exactly the expected writes for the selected student. Stop on duplicate local lessons, invoice-lock drift, selection changes, or additional calendar reads. Roll back the extraction and composition together. Update the dashboard data-flow documentation and affected type headers.

### Stage 7. Integrated verification and documentation

Type: verification, review, and documentation  
Status: Not started

- Build and run the full suite. Use `code-review` and `architecture-review` to trace dependencies from the existing WPF entry points through the narrower contracts and compare each write path with its next reader. Use `ui-review` on observable Settings, printer, invoice, and dashboard transitions.
- Use `documentation-refresh` for affected architecture guidance and invoice/calendar flow descriptions. Keep the concise type and method comments aligned with the new ownership and report concrete SOLID issues found during implementation.
- **Gate and rollback.** All seven findings must map to a verified design change. Require a clean build, passing regression tests, and a clean whitespace check. Back out the smallest failing stage if integrated tests expose a behavior change. Record the outcome and move this plan to completed after automated verification. The manual acceptance below follows implementation.

## Verification commands

Run from the solution root. At each implementation gate use the relevant test classes or method names above as `FullyQualifiedName` filters. For baseline and final verification run the full suite.

```powershell
dotnet build ElenasBusinessLedger.sln -m:1 -nodeReuse:false
dotnet test ElenasBusinessLedger.sln --no-build --no-restore -m:1 -nodeReuse:false
git diff --check
```

When using an isolated output directory, pass the same `--artifacts-path` to both .NET commands. Preserve existing test doubles for Google and Windows printing, and use temporary SQLite databases and PDF folders for storage and recovery checks.

## Review risks and decisions

The highest-risk changes are the order of calendar reconciliation and the Settings transaction boundary. Invoice storage is sensitive because checkpoints refer to existing files. Verify the before-and-after behavior at each gate and keep these three stages independently reversible.

The recommended approach uses the existing implementations behind smaller interfaces and one explicit reconciliation use case. A solution-wide repository split, another application project, and printer-only persistence would broaden scope beyond the identified problems. No design decision is required from the maintainer to start the behavior-preserving sequence. Pause before any change that would alter UI behavior, saved settings semantics, schema, or file formats.

## Final User Test Instructions

Post-completion user test: mandatory.

Use a disposable Windows user profile or an existing test harness configured with a disposable SQLite ledger. Use fictional students, a dedicated Google calendar, a temporary invoice folder, and a virtual or test printer.

1. Open **Application > Settings**, save changes to the test company and calendar, then reopen Settings. Confirm the values persisted. Use **Connect Google Calendar** on the trial account, close Settings, and refresh the dashboard. Verify appointments load and ordinary refresh does not open a browser.
2. In the trial calendar, move an unbilled lesson and refresh the Ledger twice. Verify that one local row moves and the intended student remains selected. Cancel a different lesson through the Ledger and open invoice preview immediately. Verify the cancelled lesson is absent and existing invoice-linked lessons keep their status.
3. Open **Write Invoice** with the fictional student selected. Confirm the picker keeps the selected student and the preview shows the expected lesson lines and totals. If no printer is remembered, first cancel the printer picker and verify issuance has not occurred. Then choose the trial printer and issue. Verify a single invoice number, the saved PDF, and the corresponding Economy row.
4. Exercise the automated interrupted-publication scenario to create a checkpoint predictably, then confirm **Invoice > Resume Invoice** can recover the same number and verified PDF independently of the current student selection. Reprint an issued trial invoice and verify the saved invoice number and Economy totals agree with the original issuance.

Inspect Student Invoices, the dashboard lesson rows, **Economy > Overview**, and files in the trial invoice folder. Close the app before cleaning up. Remove the trial calendar and disposable artifacts when they are no longer needed.

## Completion log

- 2026-11-21 — Recorded all seven findings and verified their current call sites and existing test coverage by source inspection. The baseline build and tests are scheduled for Stage 1. Planning added this file.

