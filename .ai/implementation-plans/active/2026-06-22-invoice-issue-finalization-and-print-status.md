# Implementation Plan: Invoice Issue Finalization And Print Status

Status: Active
Created: 2026-06-22
Last updated: 2026-06-23
Owner/context: Ruben / Codex, My Small Business Ledger local-first WPF app

## Related Docs And Standards

- `AGENTS.md`
- `README.md`
- `.ai/project-profile.md`
- `docs/business-requirements-and-logic.md`
- `.ai/standards/architecture-principles.md`
- `.ai/standards/database-standards.md`
- `.ai/standards/persistence-and-artifacts.md`
- `.ai/standards/security-and-boundaries.md`
- `.ai/standards/wpf-mvvm-standards.md`
- `.ai/standards/testing-and-verification.md`

## Related Skills

- `implementation-plan` for this cross-layer durable workflow, schema, and UI behavior change
- `plan-runner` to execute this plan stage by stage
- `code-review` after implementation because invoice issuing is durable accounting behavior
- `documentation-refresh` after implementation because invoice state, print state, settings, and safety guarantees change
- `architecture-review` only if implementation changes project/layer ownership beyond the planned ports/services
- `ui-review` after UI changes to settings, invoice issuing messages, and invoice list/viewer print state

## Goal

Make invoice issuing recoverable and truthful by separating durable invoice issuance from retryable printing.

The app should reserve an invoice number in SQLite before writing/publishing the PDF, mark the invoice as issued only after the final PDF has been successfully published, and track only the app's latest print outcome with a compact print state.

## Non-Goals

- Do not build a print audit trail or print-attempt history table.
- Do not try to detect or record PDF printing done outside the application.
- Do not create a new invoice number when reprinting an existing issued invoice.
- Do not change invoice calculation, invoice PDF content/layout, VAT rules, plan snapshot rules, or Google Calendar lesson matching except where tests must adapt to new invoice state fields.
- Do not introduce a cloud service, background Windows service, or new production dependency unless implementation proves a Windows printer API requires one and the trade-off is reviewed first.

## Assumptions

- There are no real issued invoices yet, but the implementation should still preserve existing pilot/test rows when practical.
- `IssuedInvoiceLine.PlanName` remains the authoritative issued-time display/PDF snapshot.
- `PlanCatalogId` on issued invoice lines remains lineage only; it must not be used to recalculate old invoice text.
- A configured invoice printer belongs with application settings/company profile data, not with each invoice as audit history.
- "Printed" means the application submitted the PDF to the configured printer and Windows reported the job completed successfully. It does not prove physical paper output, and the UI/docs must not imply that.
- Reprinting an issued invoice is allowed and does not mutate invoice snapshot or issue status.

## Current Problem / Current State

`IssueInvoiceUseCase` currently writes and verifies a staged PDF, publishes the final PDF, then saves the issued invoice row, then prints. If final PDF publish succeeds but SQLite save fails, the final invoice PDF exists but the database does not know the invoice was issued. Retrying can reuse the same next invoice number and then fail because the final PDF already exists.

Printing is currently part of the issuing flow result, but printing is operational and retryable. It should not decide whether the invoice safely exists.

The current printer implementation shells the PDF print action through `SavedPdfPrinter`/`ShellPrintProcessLauncher`. It validates that the PDF file exists and that a print request can be launched, but it does not model a configured invoice printer, online/availability checks, spooler completion, or persistent print status.

## Desired End State

- Invoice issuing has explicit durable states:
  - `Issuing`: invoice number and full invoice snapshot are reserved in SQLite, but the final PDF has not yet been confirmed as published.
  - `Issued`: final PDF path is stored and the database/PDF agree that the invoice exists.
- Normal invoice lists and invoice viewers show only `Issued` invoices.
- Recovery is explicit:
  - If an `Issuing` row has no published PDF, the app can retry or safely fail without creating a false issued invoice.
  - If an `Issuing` row has a published, verified final PDF, the app can finalize it as `Issued`.
  - If an `Issued` row exists and printing fails, the invoice remains issued and can be reprinted.
- Invoice print state is compact:
  - `PrintStatus`: `NotPrinted`, `Printed`, or `PrintFailed`
  - `PrintedAt`: nullable timestamp
  - `LastPrintError`: nullable text
- Application Settings includes a configured invoice printer selection or entry.
- Invoice issuing checks the configured printer before printing. If the printer is unavailable or Windows reports the print job failed, the invoice remains `Issued`, print state becomes `PrintFailed`, and the user sees a useful message.
- Reprint from the app uses the already-issued PDF and updates only print state.

## Behavior-Preserving Boundary

The following must remain unchanged unless this plan explicitly says otherwise:

- user-visible behavior: invoice preview, invoice editing before issue, invoice viewer, student invoice lists, and payment status toggling should remain familiar except for new print-state visibility/messages and reprint controls.
- public APIs / CLI / UI contracts: no CLI exists; WPF commands should remain explicit and user-driven.
- durable file formats / artifacts / logs / reports: invoice PDFs remain normal files under the configured invoice folder; no PDF overwrite is allowed.
- database schema / migrations / persistence behavior: invoice snapshots remain durable and are not recalculated from current student/plan data; payment status remains independent from issue/print status.
- security / trust boundaries: user-selected invoice folder and configured printer are local settings; no secrets are introduced.
- error handling / failure reporting: partial success must be reported truthfully, especially "invoice issued but printing failed".
- performance/resource characteristics: invoice issuing remains a short foreground operation; no long-running background service is introduced.

## Intentional Behavior Changes

- Issuing reserves invoice number/snapshot before PDF publish.
- Issued invoice rows gain issue and print state fields.
- Printing no longer participates in deciding whether an invoice exists.
- A failed print after successful issue records `PrintFailed` rather than making the issue operation fail as if the invoice were not saved.
- Users can reprint an issued invoice from the app.
- Application Settings gains a configured invoice printer.

## Affected Files, Projects, Commands, Docs, Schemas, Or Artifacts

- Core invoice model/use cases:
  - `src/MySmallBusinessLedger.Core/Invoices/IssuedInvoice.cs`
  - `src/MySmallBusinessLedger.Core/Invoices/IssuedInvoiceSummary.cs`
  - `src/MySmallBusinessLedger.Core/Invoices/InvoiceDraft.cs`
  - `src/MySmallBusinessLedger.Core/UseCases/Invoices/IssueInvoiceUseCase.cs`
  - new or updated use case for reprinting an issued invoice
- Core ports:
  - `src/MySmallBusinessLedger.Core/Ports/IInvoiceRepository.cs`
  - `src/MySmallBusinessLedger.Core/Ports/IInvoicePrinter.cs`
  - possible new printer-status port if separating "print PDF" from "query printer/job state" is cleaner
- Infrastructure:
  - `src/MySmallBusinessLedger.Infrastructure/Persistence/Entities.cs`
  - `src/MySmallBusinessLedger.Infrastructure/Persistence/LedgerDbContext.cs`
  - `src/MySmallBusinessLedger.Infrastructure/Persistence/SqliteInvoiceRepository.cs`
  - `src/MySmallBusinessLedger.Infrastructure/Persistence/SqliteCompanyProfileRepository.cs`
  - `src/MySmallBusinessLedger.Infrastructure/Persistence/Migrations/*`
  - `src/MySmallBusinessLedger.Infrastructure/Persistence/LedgerDbContextModelSnapshot.cs`
  - `src/MySmallBusinessLedger.Infrastructure/Invoices/Printing/*`
- App/settings/UI:
  - `src/MySmallBusinessLedger.App/ViewModels/SettingsViewModel.cs`
  - `src/MySmallBusinessLedger.App/Views/SettingsView.xaml`
  - `src/MySmallBusinessLedger.App/ViewModels/InvoiceViewModel.cs`
  - `src/MySmallBusinessLedger.App/ViewModels/InvoiceDisplayModel.cs`
  - `src/MySmallBusinessLedger.App/ViewModels/InvoiceViewerViewModel.cs`
  - main/student invoice list views where print status should be visible
  - app services/launchers around invoice workflow
- Tests:
  - Core use-case tests for issue ordering, recovery, and print-state outcomes
  - Infrastructure persistence tests for schema/round-trip/recovery fields
  - Infrastructure printer tests with fake printer/job monitor
  - App ViewModel tests for messages, reprint command, and print-status display
- Database schema:
  - invoice issue status
  - print status
  - printed timestamp
  - last print error
  - configured invoice printer name
- Docs/context:
  - `README.md`
  - `.ai/project-profile.md`
  - `docs/business-requirements-and-logic.md`
  - `.ai/context/HANDOFF.md`

## Risks And Mitigations

- Risk: the new `Issuing` state leaks into normal invoice lists and looks like a real issued invoice.
  - Mitigation: repository list/view queries filter to `Issued`; tests cover pending rows.
- Risk: a database row says `Issued` while no final PDF exists.
  - Mitigation: only finalization can set `Issued`, and it must verify the final PDF path first.
- Risk: a final PDF exists but the row remains `Issuing` after a crash/failure.
  - Mitigation: recovery path verifies the final PDF and finalizes the row before retrying or reports a clear reconciliation error.
- Risk: invoice numbers skip after abandoned `Issuing` reservations.
  - Mitigation: a failure before final PDF publication must release the reservation so retrying reuses the same invoice number; only published-final-PDF/database-disagreement cases should remain in explicit recovery/finalization handling.
- Risk: Windows printer APIs may report spooler success rather than physical output.
  - Mitigation: UI/docs define Printed as "Windows reported the print job completed"; do not claim paper output.
- Risk: settings UI becomes cluttered.
  - Mitigation: add only one configured invoice printer field/selector and a concise status/check result.
- Risk: invoice schema migrations accidentally rewrite or weaken issued invoice snapshots.
  - Mitigation: use narrow EF Core migrations, keep issued snapshot fields intact, and verify fresh and migrated SQLite databases.

## Open Questions

- [ ] Question: Which Windows printing API gives the most truthful, testable "job completed" signal for the current WPF/.NET 10 app?
  - Recommended/default answer: implement behind an Infrastructure port, start with the most direct Windows print queue/spooler API available without a new dependency, and represent the result conservatively.
  - Alternatives: keep shell-based printing and mark success only when launch succeeds; use a third-party PDF/printing package; skip completion monitoring and use `PrintFailed` only for preflight/launch failures.
  - Why it matters: this controls how honest `PrintStatus.Printed` can be.
  - Exact answer needed: implementation discovery should identify the API and document the exact success boundary before coding the printer adapter.
  - Implementation discovery: Stage 3 currently uses Windows spooler enumeration through `winspool.drv` to list and validate installed printers, then uses shell `printto` to send the PDF to the configured printer. The current success boundary is "configured printer exists and the print request was launched without an exception"; it does not yet monitor the Windows spooler job to completion.

## Staged Implementation Plan

### Stage 1: Core Invoice State Contract

Type: implementation/test
Status: Complete

Goal:

Define typed invoice issue and print states in Core and update use-case contracts so issuing can reserve, finalize, and print without conflating those states.

Affected files/classes/schemas/commands/UI surfaces:

- `IssuedInvoice`, `IssuedInvoiceSummary`, `IssueInvoiceResult`
- `IInvoiceRepository`
- `IssueInvoiceUseCase`
- Core invoice tests and fake repositories/printers

Concrete steps:

1. Add typed Core values for `InvoiceIssueStatus` and `InvoicePrintStatus`.
2. Extend issued invoice models/summaries with issue status and compact print fields.
3. Replace the repository pattern of `GetNextInvoiceNumberAsync` plus `SaveIssuedInvoiceAsync` with reservation/finalization operations, for example:
   - reserve invoice snapshot and number as `Issuing`
   - finalize reserved invoice as `Issued` after final PDF verification/publish
   - update print status after print/reprint
4. Refactor `IssueInvoiceUseCase` so it reserves first, writes/verifies/publishes PDF, finalizes issue, then attempts print.
5. Ensure print failure produces an issued invoice result with print status `PrintFailed`, not an invoice issue failure.

Behavior that must remain unchanged:

- PDF write/verify/publish still happens before an invoice becomes `Issued`.
- Final PDF publishing still does not overwrite an existing invoice PDF.
- Invoice snapshots and line plan-name display remain issued-time snapshots.

Tests to add/update:

- Failing PDF write/verify/publish leaves no `Issued` invoice.
- Successful publish followed by print failure returns issued invoice with `PrintFailed`.
- Successful publish followed by print success returns issued invoice with `Printed`.
- Repository fake proves reservation happens before file publish and finalization happens after publish.

Verification commands/manual checks:

```powershell
dotnet test MySmallBusinessLedger.sln --filter IssueInvoice -nodeReuse:false
```

Risks:

- Port changes ripple through many tests.
- Existing tests may still assume print success is part of issue success.

Rollback point:

- Revert Core model/port/use-case changes before database schema changes.

Stop/review criteria:

- Stop if the issue/print state model requires changing invoice PDF snapshot semantics.

Documentation impact:

- Draft notes for project profile and business requirements.

Follow-on actions:

- Proceed to SQLite persistence only after Core tests define the desired workflow.

Runner may continue automatically after this stage: yes.

### Stage 2: SQLite Persistence And Recovery

Type: implementation/test
Status: Complete

Goal:

Persist invoice issue/print state and make reservation/finalization/recovery durable in SQLite.

Affected files/classes/schemas/commands/UI surfaces:

- `Entities.cs`
- `LedgerDbContext.cs`
- `SqliteInvoiceRepository.cs`
- EF Core migration files
- `LedgerDbContextModelSnapshot.cs`
- SQLite persistence tests

Concrete steps:

1. Add invoice issue status and print fields to invoice persistence entity/schema.
2. Add configured invoice printer name to company profile persistence.
3. Implement repository reservation so invoice number allocation and `Issuing` row creation happen in one database operation.
4. Implement finalization so `Issued` is set only after final PDF path is present and verified by the application workflow.
5. Update list/read queries so normal invoice lists show only `Issued` rows.
6. Add recovery helper/query for `Issuing` rows so the issue workflow can reconcile before retrying.
7. Add an EF Core migration for new columns with conservative defaults:
   - existing rows become `Issued`
   - existing rows start as `NotPrinted`
   - printer setting defaults to blank/unconfigured

Behavior that must remain unchanged:

- Payment status remains independent from issue and print status.
- Existing invoice snapshot fields round-trip unchanged.
- Existing issued invoice rows remain readable.

Tests to add/update:

- Reserving invoice creates an `Issuing` row and reserves the number.
- Finalizing invoice stores final PDF path and marks `Issued`.
- Normal invoice list excludes `Issuing` rows.
- Existing/pilot rows migrate to `Issued` and `NotPrinted`.
- Print state updates round-trip.
- Configured invoice printer round-trips with company profile.

Verification commands/manual checks:

```powershell
dotnet test tests\MySmallBusinessLedger.Tests\MySmallBusinessLedger.Tests.csproj --filter SqlitePersistence -nodeReuse:false
```

Risks:

- Current table name `IssuedInvoices` may become semantically awkward if it contains `Issuing` rows.
- Migration defaults must be conservative for existing pilot databases.

Rollback point:

- Revert persistence changes before updating UI or real app settings behavior.

Stop/review criteria:

- Stop if migration would silently drop or rewrite invoice snapshot data.

Documentation impact:

- Update database/schema notes in docs and handoff after implementation.

Follow-on actions:

- Keep future invoice schema changes in EF Core migrations with focused migration tests.

Runner may continue automatically after this stage: yes.

### Stage 3: Printer Configuration And Infrastructure Adapter

Type: implementation/test
Status: Complete

Goal:

Add a configured invoice printer and implement truthful printer availability/job-completion reporting behind Infrastructure ports.

Affected files/classes/schemas/commands/UI surfaces:

- `CompanyProfile`
- settings service/view model/view
- `IInvoicePrinter` or a new printer status/check port
- `SavedPdfPrinter`, `ShellPrintProcessLauncher`, or replacement Windows printer adapter
- infrastructure printer tests

Concrete steps:

1. Extend `CompanyProfile` with configured invoice printer name.
2. Add settings UI for invoice printer, preferably a selector populated from available Windows printers; allow blank/unconfigured only if invoice issuing can still save without printing.
3. Implement printer preflight:
   - configured printer exists
   - printer appears available/online according to Windows
   - PDF file exists before submitting
4. Implement print submission and job-completion monitoring with conservative status mapping.
5. Return structured print results that distinguish unconfigured printer, offline/unavailable printer, failed submission, failed/completed job, and cancellation.
6. Keep all Windows/printer APIs in Infrastructure.

Behavior that must remain unchanged:

- The app can still issue/save the invoice PDF even if printing fails.
- The app does not infer or record printing done outside the application.

Tests to add/update:

- Missing configured printer returns failure without mutating issue status.
- Offline/unavailable printer returns `PrintFailed`.
- Completed print job returns `Printed` and sets `PrintedAt`.
- Failed print job returns `PrintFailed` and stores `LastPrintError`.
- Settings view model saves/loads configured printer.

Verification commands/manual checks:

```powershell
dotnet test MySmallBusinessLedger.sln --filter Printer -nodeReuse:false
dotnet test MySmallBusinessLedger.sln --filter SettingsViewModel -nodeReuse:false
```

Risks:

- Windows printer status may be driver-dependent.
- Automated tests should use fake printer adapters, not real printers.

Rollback point:

- Revert printer adapter/settings changes while keeping issue finalization if needed.

Stop/review criteria:

- Stop if the only available approach requires a third-party dependency or cannot report completion truthfully enough for `Printed`.

Documentation impact:

- Document the exact meaning of "Printed" and printer setup requirements.

Follow-on actions:

- UI review for settings layout and messages.

Runner may continue automatically after this stage: no. Blocking action required: confirm the discovered printer API/status boundary before wiring final UI wording.

### Stage 4: UI Workflow And Reprint

Type: implementation/test
Status: Complete

Goal:

Expose issue/print state truthfully in the WPF app and allow reprinting an issued invoice without changing invoice data.

Affected files/classes/schemas/commands/UI surfaces:

- invoice preview view model/view
- invoice viewer view model/view
- main window invoice display model/list
- student overview invoice display model/list
- invoice workflow service/launcher
- app ViewModel tests

Concrete steps:

1. Update invoice issue UI messages:
   - issued and printed
   - issued but print failed
   - issue failed before PDF publish/finalization
   - recovery/reconciliation required
2. Add print status display in invoice lists where useful.
3. Add a reprint command to the invoice viewer or invoice list context/button.
4. Ensure reprint uses the stored issued PDF path and never creates a new invoice number.
5. Update command availability based on typed state and configured printer status.
6. Ensure failed reprint updates only print state/error fields.

Behavior that must remain unchanged:

- Existing invoice viewer opens existing issued invoice snapshots.
- Payment status toggles remain separate from print status.
- Student/main selection behavior remains unchanged.

Tests to add/update:

- Invoice preview closes or stays open according to the new issued/print outcome rules.
- Invoice viewer reprint command calls reprint service for existing invoice number/path.
- Failed print shows a useful error and keeps invoice issued.
- Invoice lists display print state without showing `Issuing` rows as normal invoices.

Verification commands/manual checks:

```powershell
dotnet test MySmallBusinessLedger.sln --filter InvoiceViewModel -nodeReuse:false
dotnet test MySmallBusinessLedger.sln --filter MainWindowViewModel -nodeReuse:false
dotnet test MySmallBusinessLedger.sln --filter StudentOverviewViewModel -nodeReuse:false
```

Risks:

- UI wording may imply stronger print certainty than Windows can provide.
- Reprint controls may clutter the invoice viewer if not kept modest.

Rollback point:

- Revert UI changes while keeping lower-layer state work if Core/Infrastructure are stable.

Stop/review criteria:

- Stop if user-visible issue/print messages become ambiguous.

Documentation impact:

- Update user-facing docs and project profile after UI behavior is final.

Follow-on actions:

- Run `ui-review`.

Runner may continue automatically after this stage: yes.

### Stage 5: Recovery Scenario Verification And Documentation Refresh

Type: verification/documentation/review
Status: Implemented; awaiting user manual printer/invoice test

Goal:

Verify failure/recovery behavior, refresh durable docs, and review the safety-sensitive workflow.

Affected files/classes/schemas/commands/UI surfaces:

- focused Core/Infrastructure/App tests
- README/project profile/business rules/handoff
- active implementation-plan completion log

Concrete steps:

1. Add or confirm tests for interrupted states:
   - `Issuing` row without final PDF
   - `Issuing` row with verified final PDF
   - final PDF conflict with no matching row
2. Run full build and full test suite.
3. Refresh docs/context with issue status, print status, configured printer, and reprint behavior.
4. Run `code-review` focused on invoice finalization, database state, and printing boundaries.
5. Run `ui-review` focused on settings and invoice print-state truthfulness.
6. Update this plan's completion log and recommend moving completed/superseded plans out of `active` as separate cleanup if still stale.

Behavior that must remain unchanged:

- No issued invoice snapshot is mutated by plan/student edits.
- No final invoice PDF is silently overwritten.
- No print failure corrupts or deletes issued invoice data.

Tests to add/update:

- Any missing recovery tests identified by code-review.

Verification commands/manual checks:

```powershell
dotnet build MySmallBusinessLedger.sln -nodeReuse:false
dotnet test MySmallBusinessLedger.sln -nodeReuse:false
```

Risks:

- Manual printer testing depends on available hardware/printer configuration.

Rollback point:

- If full verification fails, keep changes unmerged/uncommitted until corrected.

Stop/review criteria:

- Stop if any test shows database/PDF disagreement after a reported successful issue.

Documentation impact:

- Required.

Follow-on actions:

- Move this plan to completed when verification and user test pass.

Runner may continue automatically after this stage: no. Blocking action required: user manual printer/invoice test.

## Verification Strategy

- Core unit tests for issue state transitions, print-state outcomes, and no-print-on-prepublish-failure behavior.
- SQLite persistence tests for reservation/finalization, migration defaults, print status, and configured printer persistence.
- Infrastructure printer tests using fake printer/job adapters; no tests should require a real printer.
- App/ViewModel tests for settings, invoice issue messages, invoice list display, and reprint command behavior.
- Full solution verification:

```powershell
dotnet build MySmallBusinessLedger.sln -nodeReuse:false
dotnet test MySmallBusinessLedger.sln -nodeReuse:false
```

- Manual WPF smoke test with a real or virtual printer configured.

## Follow-On Review And Documentation

- Code review required: yes — invoice issuing is durable accounting behavior with database/filesystem consistency risk.
- Architecture review required: maybe — only if implementation changes project/layer ownership beyond the planned Core ports and Infrastructure adapters.
- UI review required: yes — user-visible issue/print state and settings behavior change.
- Documentation refresh required: yes — project profile, business rules, README, and handoff must reflect new issue/print state and configured printer behavior.

## Review Stop Points

- After Stage 1, review the Core issue/print state model before committing to schema changes.
- After Stage 3, review the exact Windows printer status boundary and wording before final UI.
- Before completion, review recovery tests proving database/PDF consistency across failure points.

## Final User Test Instructions

Post-completion user test: mandatory.

Prerequisites/setup:

1. Configure company settings and invoice folder.
2. Configure the invoice printer in `Application > Settings`.
3. Use a real printer or a Windows virtual printer that exposes enough status for the chosen adapter.

Exact user actions or commands:

1. Start the app.
2. Create or select a student with an active plan and lesson data that can be invoiced.
3. Open invoice workflow and issue an invoice.
4. Confirm that the invoice PDF appears in the configured invoice folder.
5. Confirm that the invoice appears in the main/student invoice list as issued.
6. Confirm that successful printing marks the invoice as printed.
7. Turn off/unavailable the configured printer or select an unavailable test printer.
8. Reprint the same invoice from the app.

Expected successful result:

- A successfully issued invoice has one invoice number, one final PDF, an `Issued` issue state, and either `Printed` or `PrintFailed` depending on Windows print outcome.
- Reprinting does not create a new invoice number and does not change invoice snapshot content.

Expected failure/safety result, if relevant:

- If printing fails, the invoice remains issued and can be reprinted.
- If PDF staging/write/verification/publish fails before final PDF publication, the app reports the issue failure, discards any staged PDF, releases the reservation, and does not skip the invoice number on retry.
- If PDF finalization fails after final PDF publication, the app reports the issue failure and does not show a false issued invoice.
- If recovery detects a final PDF for an `Issuing` row, the app finalizes or reports a clear reconciliation error rather than silently creating a second invoice.

Where to inspect artifacts, reports, logs, database state, or UI state:

- Configured invoice folder for final PDF.
- `%APPDATA%\visualtech.dk\MySmallBusinessLedger\ledger.db` for invoice issue/print state.
- Main window and Manage Students invoice lists for issue/print display.
- Invoice viewer for reprint behavior.

Cleanup/reset instructions:

- Use a test invoice folder and test database when possible.
- Delete only test PDFs and test database copies that were created for this manual test.
- Do not delete real invoice PDFs or real `ledger.db` data as cleanup.

## Completion Log

- 2026-06-22:
  - Stage: Planning
  - Files changed: `.ai/implementation-plans/active/2026-06-22-invoice-issue-finalization-and-print-status.md`
  - Summary: Planned recoverable invoice issue finalization, compact print status, configured invoice printer, and reprint behavior.
  - Tests/verification run: Not run; planning only.
  - Results: Ready for implementation after user approval.
  - Deviations: None.
  - Risks/open questions: Printer completion semantics must be confirmed during implementation discovery before final UI wording.
- 2026-06-23:
  - Stage: Stage 1 - Core invoice state contract
  - Files changed: `src/MySmallBusinessLedger.Core/Invoices/InvoiceIssueStatus.cs`, `src/MySmallBusinessLedger.Core/Invoices/InvoicePrintStatus.cs`, `src/MySmallBusinessLedger.Core/Invoices/IssuedInvoice.cs`, `src/MySmallBusinessLedger.Core/Invoices/IssuedInvoiceSummary.cs`, `src/MySmallBusinessLedger.Core/Invoices/InvoiceDraft.cs`, `src/MySmallBusinessLedger.Core/Ports/IInvoiceRepository.cs`, `src/MySmallBusinessLedger.Core/UseCases/Invoices/IssueInvoiceUseCase.cs`, `src/MySmallBusinessLedger.App/Services/CoreInvoiceWorkflowService.cs`, `src/MySmallBusinessLedger.App/Startup/ApplicationServiceGraph.cs`, `src/MySmallBusinessLedger.App/ViewModels/InvoiceDisplayModel.cs`, `src/MySmallBusinessLedger.App/ViewModels/InvoiceViewerViewModel.cs`, `src/MySmallBusinessLedger.Infrastructure/Persistence/SqliteInvoiceRepository.cs`, `tests/MySmallBusinessLedger.Tests/Core/UseCases/UseCaseTests.cs`, and `tests/MySmallBusinessLedger.Tests/App/Services/AppServiceTests.cs`
  - Summary: Added typed invoice issue and print states, extended invoice models/summaries with compact print fields, changed `IssueInvoiceUseCase` to reserve before PDF work, finalize only after publish, then update print status after print/reprint outcome. Transitional SQLite methods compile against the existing schema until Stage 2 adds durable columns.
  - Tests/verification run: `dotnet test MySmallBusinessLedger.sln --filter IssueInvoice -nodeReuse:false`.
  - Results: Passed 3/3 focused tests.
  - Deviations: Kept legacy `GetNextInvoiceNumberAsync` and `SaveIssuedInvoiceAsync` methods temporarily for existing tests/callers; Stage 2 will migrate persistence tests and can remove or de-emphasize legacy paths after durable reservation/finalization is implemented.
  - Risks/open questions: Printer completion semantics remain the Stage 3 decision gate.
  - Next recommended stage: Stage 2 - SQLite Persistence And Recovery.
- 2026-06-23:
  - Stage: Stage 2 - SQLite persistence and recovery
  - Files changed: `src/MySmallBusinessLedger.Core/Company/CompanyProfile.cs`, `src/MySmallBusinessLedger.Core/Ports/IInvoiceRepository.cs`, `src/MySmallBusinessLedger.Infrastructure/Persistence/Entities.cs`, `src/MySmallBusinessLedger.Infrastructure/Persistence/LedgerDbContext.cs`, `src/MySmallBusinessLedger.Infrastructure/Persistence/SqliteCompanyProfileRepository.cs`, `src/MySmallBusinessLedger.Infrastructure/Persistence/SqliteInvoiceRepository.cs`, `src/MySmallBusinessLedger.Infrastructure/Persistence/Migrations/20260623085927_AddInvoiceIssuePrintState.cs`, `src/MySmallBusinessLedger.Infrastructure/Persistence/Migrations/20260623085927_AddInvoiceIssuePrintState.Designer.cs`, `src/MySmallBusinessLedger.Infrastructure/Persistence/Migrations/LedgerDbContextModelSnapshot.cs`, `tests/MySmallBusinessLedger.Tests/Infrastructure/Persistence/SqlitePersistenceTests.cs`, `tests/MySmallBusinessLedger.Tests/Core/UseCases/UseCaseTests.cs`, and `tests/MySmallBusinessLedger.Tests/App/Services/AppServiceTests.cs`
  - Summary: Added durable SQLite fields for invoice issue status, print status, printed timestamp, last print error, and configured invoice printer name. Normal invoice list/view/payment queries now filter to `Issued`; `GetIssuingInvoiceAsync` provides a deliberate recovery path for reserved rows. Existing baseline invoice rows migrate to `Issued` and `NotPrinted`.
  - Tests/verification run: `dotnet test tests\MySmallBusinessLedger.Tests\MySmallBusinessLedger.Tests.csproj --filter SqlitePersistence -nodeReuse:false`.
  - Results: Passed 17/17 focused SQLite persistence tests.
  - Deviations: Updated the previous baseline compatibility test to migrate from the prior baseline migration rather than `EnsureCreated` with the current model, because the latter creates the new columns before migrations run and is not the real old-to-new compatibility path.
  - Risks/open questions: Printer completion semantics remain unresolved and block Stage 3 implementation.
  - Next recommended stage: Stage 3 - Printer Configuration And Infrastructure Adapter after the Windows printer API/status boundary is confirmed.
- 2026-06-23:
  - Stage: Stage 3 - Printer configuration and infrastructure adapter
  - Files changed: `src/MySmallBusinessLedger.Core/Ports/IInvoicePrinter.cs`, `src/MySmallBusinessLedger.Core/UseCases/Invoices/IssueInvoiceUseCase.cs`, `src/MySmallBusinessLedger.Infrastructure/Invoices/Printing/IPrintProcessLauncher.cs`, `src/MySmallBusinessLedger.Infrastructure/Invoices/Printing/IPrinterCatalog.cs`, `src/MySmallBusinessLedger.Infrastructure/Invoices/Printing/SavedPdfPrinter.cs`, `src/MySmallBusinessLedger.Infrastructure/Invoices/Printing/ShellPrintProcessLauncher.cs`, `src/MySmallBusinessLedger.Infrastructure/Invoices/Printing/WindowsPrinterCatalog.cs`, `src/MySmallBusinessLedger.App/Services/IAppSettingsService.cs`, `src/MySmallBusinessLedger.App/Services/CoreAppSettingsService.cs`, `src/MySmallBusinessLedger.App/Startup/ApplicationServiceGraph.cs`, `src/MySmallBusinessLedger.App/ViewModels/SettingsViewModel.cs`, `src/MySmallBusinessLedger.App/Views/SettingsView.xaml`, `tests/MySmallBusinessLedger.Tests/Infrastructure/Invoices/InvoiceInfrastructureTests.cs`, `tests/MySmallBusinessLedger.Tests/Core/UseCases/UseCaseTests.cs`, and `tests/MySmallBusinessLedger.Tests/App/ViewModels/SettingsViewModelTests.cs`
  - Summary: Added configured-printer flow through company settings, Settings ViewModel/UI support for selecting, typing, remembering, and clearing the invoice printer, Windows installed-printer enumeration via `winspool.drv`, and `printto` launch against the configured printer. Invoice issuing now passes the remembered printer name to the printer port.
  - Tests/verification run: `dotnet test MySmallBusinessLedger.sln --filter "Printer|SettingsViewModel|IssueInvoice" -nodeReuse:false`.
  - Results: Passed 24/24 focused tests.
  - Deviations: The current printer adapter validates that the configured printer exists and that the print request launches, but it does not yet monitor the Windows spooler job through completion. This is weaker than the original "Windows reported the job completed" wording.
  - Risks/open questions: Resolved by user decision on 2026-06-23: option 1 is sufficient. `Printed` means the configured printer existed and Windows accepted the launch request without exception; the app does not monitor physical output or spooler completion.
  - Next recommended stage: Stage 4 - UI Workflow And Reprint.
- 2026-06-23:
  - Stage: Stage 4 - UI workflow and reprint
  - Files changed: `src/MySmallBusinessLedger.Core/UseCases/Invoices/ReprintIssuedInvoiceUseCase.cs`, `src/MySmallBusinessLedger.Core/UseCases/Invoices/ReprintInvoiceResult.cs`, `src/MySmallBusinessLedger.App/Services/IInvoiceWorkflowService.cs`, `src/MySmallBusinessLedger.App/Services/CoreInvoiceWorkflowService.cs`, `src/MySmallBusinessLedger.App/Services/InvoiceWorkflowWindowLauncher.cs`, `src/MySmallBusinessLedger.App/ViewModels/InvoiceViewerViewModel.cs`, `src/MySmallBusinessLedger.App/ViewModels/InvoiceDisplayModel.cs`, `src/MySmallBusinessLedger.App/Views/InvoiceViewerView.xaml`, `src/MySmallBusinessLedger.App/MainWindow.xaml`, `src/MySmallBusinessLedger.App/Views/StudentOverviewView.xaml`, `tests/MySmallBusinessLedger.Tests/Core/UseCases/UseCaseTests.cs`, and `tests/MySmallBusinessLedger.Tests/App/ViewModels/InvoiceViewModelTests.cs`
  - Summary: Added a reprint use case that loads an already issued invoice, prints only its stored PDF path through the remembered printer, and updates only print status/error fields. The invoice viewer now shows print status and last print error, offers a reprint command for invoices with published PDFs, and refreshes print state after reprint. Main and Manage Students invoice lists now show compact print status.
  - Tests/verification run: `dotnet test tests\MySmallBusinessLedger.Tests\MySmallBusinessLedger.Tests.csproj --filter ReprintIssuedInvoice -nodeReuse:false`; `dotnet test MySmallBusinessLedger.sln --filter InvoiceViewModel -nodeReuse:false`; `dotnet test MySmallBusinessLedger.sln --filter MainWindowViewModel -nodeReuse:false`; `dotnet test MySmallBusinessLedger.sln --filter StudentOverviewViewModel -nodeReuse:false`.
  - Results: Reprint focused tests passed 2/2; InvoiceViewModel passed 9/9; MainWindowViewModel passed 26/26; StudentOverviewViewModel passed 13/13.
  - Deviations: Reprint command availability is based on an existing published PDF and active workflow service; configured printer availability is validated by the printer adapter when the command runs so the UI reports current printer errors truthfully.
  - Risks/open questions: UI wording intentionally says "Print request sent" in the invoice viewer to avoid implying verified physical output.
  - Next recommended stage: Stage 5 - Recovery Scenario Verification And Documentation Refresh.
- 2026-06-23:
  - Stage: Stage 5 - Recovery scenario verification and documentation refresh
  - Files changed: `README.md`, `.ai/project-profile.md`, `docs/business-requirements-and-logic.md`, `.ai/context/HANDOFF.md`, `.ai/implementation-plans/active/2026-06-22-invoice-issue-finalization-and-print-status.md`, `src/MySmallBusinessLedger.App/ViewModels/InvoicePreviewViewModel.cs`, `src/MySmallBusinessLedger.App/ViewModels/InvoiceViewerViewModel.cs`, `src/MySmallBusinessLedger.App/Services/InvoiceWorkflowWindowLauncher.cs`, `src/MySmallBusinessLedger.App/Startup/ApplicationServiceGraph.cs`, `src/MySmallBusinessLedger.Infrastructure/Invoices/Printing/ShellPrintProcessLauncher.cs`, and `tests/MySmallBusinessLedger.Tests/App/ViewModels/InvoiceViewModelTests.cs`
  - Summary: Refreshed durable docs/context for invoice issue reservation/finalization, remembered printer selection, first-print Settings prompt, reprint behavior, and the launch-success meaning of `Printed`. Focused code/UI review found and fixed two gaps: first print now offers Settings when no printer is configured, and shell print launch now treats `Process.Start` returning `null` as failure.
  - Tests/verification run: `dotnet test MySmallBusinessLedger.sln --filter InvoiceViewModel -nodeReuse:false`; `dotnet build MySmallBusinessLedger.sln -nodeReuse:false`; `dotnet test MySmallBusinessLedger.sln -nodeReuse:false`.
  - Results: InvoiceViewModel passed 11/11; full build passed with 0 warnings and 0 errors; full test suite passed 203/203.
  - Deviations: Recovery is verified at repository/query level for reserved `Issuing` rows and normal issued-only list/view/payment paths; no separate automated reconciliation worker/UI exists yet for final-PDF/database disagreement beyond the planned `GetIssuingInvoiceAsync` recovery seam.
  - Risks/open questions: Manual WPF printer/invoice smoke test is still required before moving this plan to completed.
  - Next recommended stage: User manual printer/invoice test, then move this plan to completed if the smoke test passes.
