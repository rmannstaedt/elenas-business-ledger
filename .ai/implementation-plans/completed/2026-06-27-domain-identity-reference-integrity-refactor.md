# Implementation Plan: Domain Identity And Reference Integrity Refactor

Status: Completed
Created: 2026-06-27
Last updated: 2026-06-27
Owner/context: Ruben and Codex. Created after architecture review found that lesson creation and invoice state can still depend on weak lineage, post-refresh UI repair, and split persistence operations.

## Related Docs And Standards

- `AGENTS.md`
- `.ai/project-profile.md`
- `.ai/standards/architecture-principles.md`
- `.ai/standards/coding-standards.md`
- `.ai/standards/database-standards.md`
- `.ai/standards/domain-data-integrity.md`
- `.ai/standards/persistence-and-artifacts.md`
- `.ai/standards/review-checklists.md`
- `.ai/standards/testing-and-verification.md`
- `.ai/standards/wpf-mvvm-standards.md`
- `.ai/context/HANDOFF.md`
- `docs/business-requirements-and-logic.md`
- `README.md`

## Related Skills

- `refactor-planning` - created this plan.
- `plan-runner` - should execute this plan stage by stage.
- `architecture-review` - should review after the structural stages.
- `code-review` - should review high-risk Core/Infrastructure changes before final acceptance.
- `documentation-refresh` - should align docs and context after behavior and schema are corrected.
- `ui-review` - should review the main-window row behavior if UI command sequencing changes.

## Refactor Verdict

Refactoring is required before further invoice and lesson behavior is built on top of the current flow.

The issue is not only a local bug. It is a structural identity and transaction problem:

- a user-selected lesson plan can be lost between the editable row, the Core add-lesson request, Google Calendar, local reconciliation, and the later UI patch;
- invoice issue finalization and lesson invoice-locking happen as separate operations;
- invoice lines store `LessonId`, but the database does not yet enforce that relationship as a foreign key;
- several use cases still support a legacy "no local lesson repository" mode even though local lessons are now the durable billing ledger.

Because invoices, payment state, completion state, and lesson billing are financial/audit-adjacent domain state, the repair should happen at the identity and persistence contract level, not by adding more UI refresh patches.

## Goal

Make lesson identity, plan identity, invoice-line lineage, and invoice state transitions exact, durable, and database-enforced.

Specifically:

- lesson creation must preserve the user's selected plan from the UI through Core, Infrastructure, Google Calendar write, local lesson persistence, and dashboard refresh;
- a lesson must never be marked completed, invoiced, paid, or otherwise locked merely because it shares student/date/time/plan/month with another lesson;
- invoice lines must reference the exact local lesson rows they bill;
- invoice issue and lesson invoice-locking must succeed or fail as one business unit of work, with no misleading half-issued state;
- direct Google Calendar lessons remain supported, but Google event IDs are external scoped identifiers mapped to local lessons, not the billing identity itself.

## Non-Goals

- Do not redesign the invoice PDF layout.
- Do not add a general audit trail for lesson edits.
- Do not implement Google Calendar conflict detection.
- Do not implement invoice credit notes, voiding, or corrections.
- Do not broaden OAuth scopes.
- Do not change the user-facing rule that date and plan are required for lessons, while time is optional.
- Do not preserve old invoice data during this refactor. The current user has stated that invoice data will be cleared/reset before this work is applied. Still keep migrations deterministic and explicit.
- Do not make Google Calendar the source of truth for billing identity. It is a calendar integration and external event source; local `Lesson.Id` is the billing/audit identity.

## Assumptions

- The app is still a WPF desktop app with Core/Application policy independent of WPF, EF Core, SQLite, Google APIs, printer integration, and PDF generation.
- Local SQLite is the durable ledger for students, plans, local lessons, issued invoices, invoice lines, payment status, and invoice snapshots.
- Google Calendar remains the user-visible scheduling integration and may contain manually created lesson appointments.
- Google Calendar event IDs can be stored as external mappings for reconciliation, but they are not sufficient as the invoice line identity because events can be edited, deleted, recreated, imported, or originate outside the app.
- One local lesson may be billed by at most one issued invoice line.
- Invoice PDF output remains an immutable snapshot of issued invoice content.
- Invoiced lessons remain locked for billing-relevant fields.
- The user may reset the local database before this refactor, so migration compatibility for existing invoice rows is not required in this cycle.

## Current Problem / Current State

### 1. Add Lesson Drops Selected Plan Identity

Current shape:

- `LessonAddRequest` carries student, start date/time, and known-time state, but not the selected `PlanCatalogId`.
- `MainWindowViewModel` creates the add request without the selected plan identity.
- After refresh/reconciliation, `ApplyPendingDraftPlanSelection` attempts to patch the created row back to the requested plan in the UI layer.
- `StartupDashboardQuery` reconciles newly discovered calendar lessons by using the student's active/default plan.

Observed risk:

- A user can add a lesson with a non-default plan.
- Reconciliation can create or show another row using the student's default plan.
- The non-default lesson may not appear correctly in Google Calendar/local state.
- Later invoice/payment/completion state may be applied to rows that look similar but are not the same business object.

### 2. Invoice Finalization And Lesson Locking Are Split

Current shape:

- `IssueInvoiceUseCase` finalizes/reserves/records the invoice.
- It then separately calls `ILessonRepository.MarkInvoicedAsync`.
- Candidate selection and preview filter local lessons by `lesson.InvoiceNumber is null`.

Observed risk:

- If invoice finalization succeeds and lesson marking fails, the invoice exists but its lessons can still appear uninvoiced.
- If later matching is based on descriptive values, a new lesson can inherit invoiced/paid/completed state from an old invoice row.
- This violates the rule that issued invoice lines are the durable local lesson-invoice record.

### 3. Invoice Line LessonId Is Not Database-Enforced

Current shape:

- `IssuedInvoiceLineEntity.LessonId` exists.
- `LedgerDbContext` has an index on `LessonId`.
- The schema does not yet declare a foreign key from issued invoice lines to local lessons.

Observed risk:

- The database can store orphaned, default, stale, or duplicated lesson references.
- The schema does not enforce the domain rule that each issued line bills an exact local lesson.
- Tests can pass while the durable contract remains weaker than the domain requires.

### 4. Legacy Optional Local-Ledger Paths Remain

Current shape:

- Several Core use cases accept nullable `ILessonRepository?` and have fallback paths that operate without local lessons.
- Startup reconciliation still has legacy descriptive status lookup behavior.
- Invoice preview/candidate selection still has branches that infer from calendar lessons when the local lesson repository is absent.

Observed risk:

- New code has to reason about two billing models: local lessons as durable ledger, and legacy calendar-only inference.
- Review and testing can miss bugs because the same use case has both exact-ID and weak-descriptive modes.
- Future changes can accidentally revive the old weak identity model.

### 5. Documentation And Plan State Drift

Current shape:

- Some docs now say date and plan are enough for lesson autosave, time optional.
- Some docs still say date and time are required.
- Active implementation plans include work that appears implemented or partially superseded, and solution-file plan listings may not match the active directory.

Observed risk:

- Future work may implement against stale requirements.
- Reviewers may miss identity issues because current state and plan state are not fully aligned.

## Desired End State

### Core

- Lesson creation request includes the selected plan identity.
- Core validates that date and plan are present; time remains optional.
- Add/update lesson use cases preserve `Lesson.Id`, `PlanCatalogId`, optional time state, `GoogleCalendarEventId`, invoice lock state, and current display snapshots deliberately.
- Invoice preview/candidate selection uses exact local lesson identity and issued invoice line lineage, not descriptive matching.
- Invoice issue finalization exposes one business operation for issuing invoice lines and locking the referenced lessons.
- Core use cases no longer have nullable local-lesson repository modes for invoice and dashboard billing flows.

### Infrastructure

- SQLite schema enforces issued invoice line to local lesson relationship with a foreign key.
- SQLite schema enforces uniqueness where the domain requires it, especially one issued invoice line per lesson.
- Invoice finalization and lesson locking are persisted in one transaction boundary.
- No transaction remains open across slow PDF file writing, printer dialogs, Google network calls, or user interaction.
- Local lesson rows keep a scoped Google Calendar event ID mapping when available.

### App/UI

- Main-window lesson rows send the selected plan identity when creating or saving a lesson.
- UI no longer repairs plan identity after refresh as a normal control path.
- Refresh/reconciliation does not duplicate a just-created lesson with the student's default plan.
- Row state is derived from local lesson identity and exact invoice-line links.

### Documentation

- README, project profile, business requirements, handoff, and active plan state agree on:
  - date + plan required for lesson save;
  - time optional, with all-day Google Calendar events when no time is known;
  - local lesson identity is the billing identity;
  - issued invoice lines reference exact local lessons and preserve issued-time snapshots;
  - invoice issue and lesson invoice-locking are one business unit.

## Behavior-Preserving Boundary

The implementation must preserve these user-visible behaviors unless a stage explicitly calls out a behavior change:

- `Add Lesson` creates an editable row for the currently displayed student/month.
- Date and plan are required for a new lesson to be saved.
- Time is optional.
- Lessons without a known time are represented in Google Calendar as all-day lesson events and display with blank time in the app/invoices.
- The user can choose a non-default plan for an individual lesson.
- Direct Google Calendar lessons created by the user are still read by the app.
- Direct Google Calendar lessons that do not carry a local plan identity use the student's active plan for initial local reconciliation.
- Invoiced lesson rows remain locked for billing-relevant edits.
- Invoice numbers, invoice period, PDF naming, payment instructions, VAT handling, print status, and one-page PDF layout remain unchanged.
- Invoice PDFs remain immutable snapshots once issued.
- The first click after app reactivation must not be swallowed by refresh/rebuild behavior.

## Intentional Behavior Changes

- A user-selected plan for a newly added lesson becomes part of the create operation. It is no longer a post-refresh UI patch.
- A local lesson cannot be invoiced twice.
- An issued invoice line cannot reference a missing lesson.
- Invoice finalization cannot leave an invoice issued while its lessons remain invoiceable.
- Legacy calendar-only invoice paths are removed from normal Core use cases.
- Documentation that still says time is mandatory is updated to say date and plan are mandatory, time optional.

## Affected Files, Projects, Commands, Docs, Schemas, Or Artifacts

Likely source files:

- `src/MySmallBusinessLedger.Core/UseCases/Lessons/LessonAddRequest.cs`
- `src/MySmallBusinessLedger.Core/UseCases/Lessons/AddLessonUseCase.cs`
- `src/MySmallBusinessLedger.Core/UseCases/Lessons/PopulateCalendarUseCase.cs`
- `src/MySmallBusinessLedger.Core/UseCases/Startup/StartupDashboardQuery.cs`
- `src/MySmallBusinessLedger.Core/UseCases/Invoices/InvoiceCandidateSelectionUseCase.cs`
- `src/MySmallBusinessLedger.Core/UseCases/Invoices/InvoicePreviewUseCase.cs`
- `src/MySmallBusinessLedger.Core/UseCases/Invoices/IssueInvoiceUseCase.cs`
- `src/MySmallBusinessLedger.Core/Ports/ILessonRepository.cs`
- `src/MySmallBusinessLedger.Core/Ports/IInvoiceRepository.cs`
- `src/MySmallBusinessLedger.Core/Ports/IGoogleCalendarGateway.cs`
- `src/MySmallBusinessLedger.Infrastructure/Calendar/GoogleCalendarGateway.cs`
- `src/MySmallBusinessLedger.Infrastructure/Persistence/Entities.cs`
- `src/MySmallBusinessLedger.Infrastructure/Persistence/LedgerDbContext.cs`
- `src/MySmallBusinessLedger.Infrastructure/Persistence/SqliteLessonRepository.cs`
- `src/MySmallBusinessLedger.Infrastructure/Persistence/SqliteInvoiceRepository.cs`
- `src/MySmallBusinessLedger.Infrastructure/Persistence/Migrations/*`
- `src/MySmallBusinessLedger.App/ViewModels/MainWindowViewModel.cs`
- `src/MySmallBusinessLedger.App/ViewModels/LessonEditorViewModel.cs`
- `src/MySmallBusinessLedger.App/Services/CoreLessonEditorService.cs`
- `src/MySmallBusinessLedger.App/Services/ConfiguredGoogleCalendarGateway.cs`

Likely tests:

- `tests/MySmallBusinessLedger.Tests/Core/UseCases/UseCaseTests.cs`
- `tests/MySmallBusinessLedger.Tests/App/ViewModels/MainWindowViewModelTests.cs`
- `tests/MySmallBusinessLedger.Tests/App/Services/AppServiceTests.cs`
- `tests/MySmallBusinessLedger.Tests/Infrastructure/Persistence/SqlitePersistenceTests.cs`
- `tests/MySmallBusinessLedger.Tests/Infrastructure/Calendar/GoogleCalendarGatewayTests.cs`

Likely docs/context:

- `README.md`
- `.ai/project-profile.md`
- `.ai/context/HANDOFF.md`
- `docs/business-requirements-and-logic.md`
- active/completed implementation-plan files after the work is complete
- `MySmallBusinessLedger.sln` solution items if the repo keeps plan/docs files listed there

## Risks And Mitigations

- Risk: holding a database transaction while writing/verifying/publishing PDFs or calling Google Calendar.
  - Mitigation: keep DB transaction scoped to DB finalization only; use existing staged PDF/release/compensation behavior before the DB commit point.

- Risk: breaking direct Google Calendar-created lessons.
  - Mitigation: keep reconciliation path that creates local lesson rows from matching Google events and assigns the student's active plan only when no local lesson identity/plan is known.

- Risk: duplicate rows from calendar refresh after Add Lesson.
  - Mitigation: persist local lesson first or carry the selected plan identity through the add request, then reconcile by Google event id/local id rather than by default-plan inference.

- Risk: stale tests encode legacy nullable repository behavior.
  - Mitigation: update tests to assert the required local-ledger invariant and delete tests that exist only to support the old calendar-only invoice model.

- Risk: migration conflicts with existing developer data.
  - Mitigation: user has approved clearing invoice data; document any destructive reset requirement and keep fresh-database migration tests.

- Risk: `Lesson.InvoiceNumber` duplicates information now represented by issued invoice lines.
  - Mitigation: either remove it in the same refactor or keep it as a denormalized locked-state cache with tests proving it is consistent with invoice lines. The recommended staged path is to keep it temporarily but make invoice lines authoritative.

## Open Questions

Recommended defaults are included so execution can proceed unless the user overrides them.

1. Should `LessonEntity.InvoiceNumber` be removed or kept?
   - Recommended: keep initially as a denormalized display/lock cache, but make issued invoice lines authoritative and add tests proving consistency. Consider a later cleanup once UI queries no longer need the cache.

2. Should `IssuedInvoiceLineEntity.LessonId` be unique?
   - Recommended: yes. A lesson can appear on at most one issued invoice line.

3. Should selected plan identity be written into Google Calendar event extended properties?
   - Recommended: not required for this repair. Store plan identity locally; treat Google Calendar as schedule integration. Extended properties can be considered later if cross-device/local-database reconstruction from calendar alone becomes a requirement.

4. Should this work remove all legacy migration compatibility for existing issued invoices?
   - Recommended: yes for the current user data, because the user will clear invoice data. Still keep the migration explicit and safe for fresh databases.

## Staged Implementation Plan

### Stage 1: Characterize The Identity Failures

Status: Completed 2026-06-27

Goal:

- Add failing tests that prove the exact data-integrity defects before changing implementation.

Concrete steps:

1. Add a Core or App-level test for adding a lesson with a non-default selected plan:
   - student default plan is Plan A;
   - user creates same-date lesson with selected Plan B;
   - after refresh/reconciliation, exactly one lesson exists for that created event;
   - the lesson keeps Plan B.
2. Add a no-time variant of the same test:
   - date + selected plan are enough;
   - all-day Google request is produced;
   - selected plan survives.
3. Add an invoice collision test:
   - create an invoiced lesson for a student/date/period;
   - create a new separate lesson with the same student/date or same month;
   - assert the new lesson is not completed, invoiced, paid, or locked unless its own `Lesson.Id` is linked to an issued invoice line.
4. Add a persistence test that issuing an invoice creates invoice lines with exact lesson IDs.
5. Add a persistence constraint test that one lesson cannot be attached to two invoice lines.

Behavior-preserving boundary:

- Tests should describe expected existing business rules. Do not change UI behavior in this stage.

Verification:

```powershell
dotnet test tests\MySmallBusinessLedger.Tests\MySmallBusinessLedger.Tests.csproj --filter FullyQualifiedName~MainWindowViewModelTests -nodeReuse:false
dotnet test tests\MySmallBusinessLedger.Tests\MySmallBusinessLedger.Tests.csproj --filter FullyQualifiedName~UseCaseTests -nodeReuse:false
dotnet test tests\MySmallBusinessLedger.Tests\MySmallBusinessLedger.Tests.csproj --filter FullyQualifiedName~SqlitePersistenceTests -nodeReuse:false
```

Expected result:

- New tests fail before implementation.

Stage result:

- Added MainWindowViewModel characterization assertions proving draft autosave must send the selected `PlanCatalogId` in `LessonAddRequest` for timed, date-only, and non-default-plan lesson creation.
- Added SQLite model characterization tests proving `IssuedInvoiceLineEntity.LessonId` must be a required foreign key to `LessonEntity` with restrictive delete behavior and a unique one-line-per-lesson index.
- Verification run:
  - `dotnet test tests\MySmallBusinessLedger.Tests\MySmallBusinessLedger.Tests.csproj --filter FullyQualifiedName~MainWindowViewModelTests -nodeReuse:false`
- Verification result:
  - Failed as expected before implementation because `LessonAddRequest` does not yet expose `PlanCatalogId`.
- No behavior changed in this stage.
- No blocking user action.

Review stop:

- Stop if the failing tests cannot reproduce the observed issue. Do not proceed with structural changes until the failure is captured.

### Stage 2: Carry Plan Identity Through Lesson Creation

Status: Completed 2026-06-27

Goal:

- Make the selected `PlanCatalogId` part of the lesson create command and local persistence path.

Concrete steps:

1. Extend `LessonAddRequest` with selected `PlanCatalogId`.
2. Update Core validation so date + plan are required and time remains optional.
3. Update `AddLessonUseCase` so selected plan identity is known before Google Calendar refresh/reconciliation.
4. Update `IGoogleCalendarGateway` call sites and fakes for the new request shape.
5. Update `MainWindowViewModel` and `LessonEditorViewModel` to send the selected plan identity with the add request.
6. Remove or demote `ApplyPendingDraftPlanSelection` from the normal create path.
7. Ensure `PopulateCalendarUseCase` still uses the student's schedule/default active plan explicitly when seeding calendar lessons.
8. Ensure direct Google Calendar-created events still receive the active plan during local reconciliation only when no existing local row/plan identity is known.

Behavior-preserving boundary:

- Existing Add Lesson UX remains editable.
- Existing time optional behavior remains.
- Existing Populate Calendar behavior remains.

Verification:

```powershell
dotnet test tests\MySmallBusinessLedger.Tests\MySmallBusinessLedger.Tests.csproj --filter FullyQualifiedName~UseCaseTests -nodeReuse:false
dotnet test tests\MySmallBusinessLedger.Tests\MySmallBusinessLedger.Tests.csproj --filter FullyQualifiedName~MainWindowViewModelTests -nodeReuse:false
dotnet test tests\MySmallBusinessLedger.Tests\MySmallBusinessLedger.Tests.csproj --filter FullyQualifiedName~GoogleCalendarGatewayTests -nodeReuse:false
```

Review stop:

- Stop if a selected non-default plan can still be replaced by the student's default plan after refresh.

Stage result:

- Extended `LessonAddRequest` with selected `PlanCatalogId`.
- Added `LessonAddResult` so the Google Calendar gateway returns the exact created event id plus the refreshed month list.
- Updated `AddLessonUseCase` to validate student and selected plan, create the Google Calendar event, and immediately persist the local `LessonRecord` with the selected plan identity and created Google event id.
- Updated the main-window autosave path to send `PlanCatalogId` as part of lesson creation.
- Removed the normal post-refresh `ApplyPendingDraftPlanSelection` repair path.
- Updated Populate Calendar to fill lesson add requests with the student's active plan explicitly.
- Updated app composition, configured calendar wrapper, Google gateway tests, Core tests, and main-window tests for the new contract.
- Verification run:
  - `dotnet build MySmallBusinessLedger.sln -nodeReuse:false`
  - `dotnet test tests\MySmallBusinessLedger.Tests\MySmallBusinessLedger.Tests.csproj --filter FullyQualifiedName~UseCaseTests -nodeReuse:false`
  - `dotnet test tests\MySmallBusinessLedger.Tests\MySmallBusinessLedger.Tests.csproj --filter FullyQualifiedName~MainWindowViewModelTests -nodeReuse:false`
  - `dotnet test tests\MySmallBusinessLedger.Tests\MySmallBusinessLedger.Tests.csproj --filter FullyQualifiedName~GoogleCalendarGatewayTests -nodeReuse:false`
- Verification result:
  - All Stage 2 focused verification passed.
- Deviation:
  - Kept the old modal `LessonEditorViewModel` compiling with a plan id of `0`; the normal main-window Add Lesson path owns date+plan creation now. This legacy launcher should be removed or updated if it is revived.
- No blocking user action.

### Stage 3: Make Local Lesson Identity The Invoice Candidate Authority

Status: Completed 2026-06-27

Goal:

- Remove normal invoice/candidate/preview behavior that can infer invoiceability from calendar-only or descriptive fields.

Concrete steps:

1. Make `ILessonRepository` required in:
   - `StartupDashboardQuery`;
   - `InvoiceCandidateSelectionUseCase`;
   - `InvoicePreviewUseCase`;
   - `IssueInvoiceUseCase`.
2. Remove nullable repository fallbacks and legacy descriptive status lookup paths.
3. Ensure candidate selection reads local lessons and their exact invoice state only.
4. Ensure preview lines carry `LessonId` through every DTO used by issue finalization.
5. Ensure dashboard read models expose invoice/payment state by local lesson identity.
6. Update dependency injection/composition so all use cases receive the local repository.
7. Remove tests/fakes that only exist for the old no-local-ledger branch, or rewrite them to use a local lesson fake.

Behavior-preserving boundary:

- Direct Google Calendar-created lessons still appear after reconciliation into local lessons.
- Users should not see a calendar-only lesson as invoiceable until it has a local lesson row.

Verification:

```powershell
dotnet test tests\MySmallBusinessLedger.Tests\MySmallBusinessLedger.Tests.csproj --filter FullyQualifiedName~UseCaseTests -nodeReuse:false
dotnet test tests\MySmallBusinessLedger.Tests\MySmallBusinessLedger.Tests.csproj --filter FullyQualifiedName~AppServiceTests -nodeReuse:false
```

Review stop:

- Stop if invoice preview can still be built without exact local lesson IDs.

Completion notes:

- `ILessonRepository` is now required by startup dashboard, invoice candidate selection, invoice preview, and invoice issue use cases.
- Removed the normal calendar-only invoice candidate/preview path. Candidates and preview lines are now based on local `LessonRecord` rows only.
- Removed the legacy descriptive invoice status lookup from startup dashboard state. Invoice/payment display state now comes from invoice line status matched by exact local lesson id.
- Updated app composition and tests/fakes to supply the lesson repository explicitly.
- Rewrote candidate/preview tests away from calendar-only/descriptive invoice assumptions and toward exact local lessons.
- Verification run:

```powershell
dotnet build MySmallBusinessLedger.sln -nodeReuse:false
dotnet test tests\MySmallBusinessLedger.Tests\MySmallBusinessLedger.Tests.csproj --filter FullyQualifiedName~UseCaseTests -nodeReuse:false
dotnet test tests\MySmallBusinessLedger.Tests\MySmallBusinessLedger.Tests.csproj --filter FullyQualifiedName~AppServiceTests -nodeReuse:false
```

- Verification result:
  - Build passed.
  - `UseCaseTests` passed: 55/55.
  - `AppServiceTests` passed: 3/3.
  - Review stop satisfied: invoice preview construction requires local lesson repository + plan catalog repository and local lesson rows carry the `LessonId`.

### Stage 4: Finalize Invoice And Lock Lessons In One Database Unit

Status: Completed 2026-06-27

Goal:

- Ensure invoice issue finalization and lesson invoice-locking cannot split into a misleading partial state.

Concrete steps:

1. Introduce a Core port method representing the business operation, for example:
   - `FinalizeIssuedInvoiceWithLessonLocksAsync(...)`, or
   - an invoice repository method that persists the issued invoice, invoice lines, and lesson locks atomically.
2. Implement the method in SQLite using one database transaction.
3. Keep existing PDF staging/publish logic outside the database transaction.
4. Preserve the existing invoice-number release behavior when staging, PDF writing, verification, or final PDF publish fails before finalization.
5. After final PDF publish succeeds, commit the issued invoice and lesson locks together.
6. If DB finalization fails after final PDF publish, surface a clear failure and add a reconciliation strategy:
   - either remove/mark the published PDF as failed before returning, if safe;
   - or create an explicit failed issue state that does not present as a valid issued invoice.
7. Update `IssueInvoiceUseCase` so it no longer separately calls `lessonRepository.MarkInvoicedAsync` after invoice finalization.
8. Add rollback tests where a simulated lesson-lock failure leaves no issued invoice and no locked lessons.

Behavior-preserving boundary:

- Successful invoices still produce the same invoice number, PDF file, invoice rows, print status, and dashboard refresh.
- Failed pre-publish flows still release invoice numbers and clean staging files.

Verification:

```powershell
dotnet test tests\MySmallBusinessLedger.Tests\MySmallBusinessLedger.Tests.csproj --filter FullyQualifiedName~UseCaseTests -nodeReuse:false
dotnet test tests\MySmallBusinessLedger.Tests\MySmallBusinessLedger.Tests.csproj --filter FullyQualifiedName~SqlitePersistenceTests -nodeReuse:false
```

Review stop:

- Stop if an issued invoice can exist while its included lessons remain invoiceable.

Completion notes:

- Added `IInvoiceRepository.FinalizeIssuedInvoiceWithLessonLocksAsync(...)` as the single business persistence operation for invoice finalization plus lesson locking.
- Implemented the SQLite method with a database transaction. It validates that invoice line lesson ids match the requested locks, that all lessons exist, and that no lesson is already locked to another invoice.
- Updated `IssueInvoiceUseCase` to reject drafts with missing lesson ids and to call the atomic invoice repository method instead of finalizing first and then calling `ILessonRepository.MarkInvoicedAsync`.
- Added published-PDF cleanup on post-publish DB finalization failure: the just-published file is deleted and the invoice reservation is released.
- Added use-case and SQLite regression coverage for successful atomic lock, already-locked lesson rollback, and post-publish finalization failure cleanup.
- Verification run:

```powershell
dotnet build MySmallBusinessLedger.sln -nodeReuse:false
dotnet test tests\MySmallBusinessLedger.Tests\MySmallBusinessLedger.Tests.csproj --filter FullyQualifiedName~UseCaseTests -nodeReuse:false
dotnet test tests\MySmallBusinessLedger.Tests\MySmallBusinessLedger.Tests.csproj --filter FullyQualifiedName~SqlitePersistenceTests -nodeReuse:false
```

- Verification result:
  - Build passed.
  - `UseCaseTests` passed: 56/56.
  - `SqlitePersistenceTests` passed: 26/26.
  - Review stop satisfied: the issue flow cannot produce an issued invoice through the normal path without atomically locking the exact local lesson ids.

### Stage 5: Enforce Database Reference Integrity

Status: Completed 2026-06-27

Goal:

- Move lesson-invoice reference integrity from convention to enforced SQLite schema.

Concrete steps:

1. Update EF model for `IssuedInvoiceLineEntity`:
   - required `LessonId`;
   - foreign key to `LessonEntity.Id`;
   - delete behavior `Restrict` or equivalent so billed lessons cannot be deleted under issued invoice lines;
   - unique index on `LessonId` if one lesson can appear on at most one issued invoice line.
2. Consider a foreign key from `LessonEntity.InvoiceNumber` to issued invoices if `InvoiceNumber` remains as a cache. If not feasible, add repository tests that keep it consistent.
3. Generate a migration.
4. Review migration manually for destructive behavior and identity defaults.
5. Because current invoice data will be reset, avoid complex backfill for existing invoice lines. Still make the intended reset explicit in the migration notes or plan completion log.
6. Add SQLite tests that:
   - missing lesson reference fails;
   - duplicate lesson reference fails;
   - deleting an invoiced lesson fails or is blocked by repository policy;
   - fresh database creation succeeds.

Behavior-preserving boundary:

- Normal app startup and automatic schema initialization still work.
- Existing fresh-database workflows still seed/reference plans and lessons correctly.

Verification:

```powershell
dotnet test tests\MySmallBusinessLedger.Tests\MySmallBusinessLedger.Tests.csproj --filter FullyQualifiedName~SqlitePersistenceTests -nodeReuse:false
dotnet build MySmallBusinessLedger.sln -nodeReuse:false
```

Review stop:

- Stop if the migration introduces nullable/default lesson IDs into finalized invoice lines.

Completion notes:

- Added an EF relationship from `IssuedInvoiceLineEntity.LessonId` to `LessonEntity.Id` with restrictive delete behavior.
- Changed the `IssuedInvoiceLines.LessonId` index to unique, enforcing one issued invoice line per lesson.
- Added migration `20260627170000_AddIssuedInvoiceLineLessonReference`.
- Updated the model snapshot.
- Per user direction, no compatibility backfill was added for old invoice data. Existing data can be reset; fresh data and current flows create valid lesson ids before invoicing.
- Kept `LessonEntity.InvoiceNumber` as a cache for current UI/read-model convenience, but invoice truth now comes from `IssuedInvoiceLines.LessonId` and exact invoice line status lookups.
- Verification run:

```powershell
dotnet build MySmallBusinessLedger.sln -nodeReuse:false
dotnet test tests\MySmallBusinessLedger.Tests\MySmallBusinessLedger.Tests.csproj --filter FullyQualifiedName~SqlitePersistenceTests -nodeReuse:false
```

- Verification result:
  - Build passed.
  - `SqlitePersistenceTests` passed: 26/26.
  - Review stop satisfied: `LessonId` is required, has an FK to `Lessons`, and has a unique index.

### Stage 6: Clean Up Redundant Or Misleading State

Status: Completed 2026-06-27

Goal:

- Reduce future ambiguity after the exact identity path is in place.

Concrete steps:

1. Audit all uses of `Lesson.InvoiceNumber`.
2. Decide whether it remains a denormalized display/lock cache or can be removed.
3. If kept:
   - document that issued invoice lines are authoritative;
   - add repository tests proving the cache stays consistent with invoice lines;
   - ensure payment status display does not infer by descriptive matching.
4. If removed:
   - update read models, repositories, invoice candidate queries, UI displays, and migrations to join through invoice lines.
5. Audit all status display logic for matching by student/date/time/plan/period.
6. Remove any remaining helper method whose purpose is to patch identity after refresh.

Behavior-preserving boundary:

- The lesson table still shows Invoiced and Paid status truthfully.
- Invoice list status button behavior remains unchanged.

Verification:

```powershell
dotnet test tests\MySmallBusinessLedger.Tests\MySmallBusinessLedger.Tests.csproj --filter FullyQualifiedName~UseCaseTests -nodeReuse:false
dotnet test tests\MySmallBusinessLedger.Tests\MySmallBusinessLedger.Tests.csproj --filter FullyQualifiedName~MainWindowViewModelTests -nodeReuse:false
```

Review stop:

- Stop if any UI status can be computed without an exact lesson id or invoice id source.

Completion notes:

- Audited invoice/payment/completion display paths for remaining weak descriptive matching.
- Removed the obsolete `ILessonRepository.MarkInvoicedAsync` port and SQLite implementation. Lesson invoice-locking is now owned by `IInvoiceRepository.FinalizeIssuedInvoiceWithLessonLocksAsync(...)` only.
- Kept `Lesson.InvoiceNumber` as the planned denormalized display/lock cache. Issued invoice lines remain authoritative, and the SQLite finalization transaction updates the cache only after validating exact invoice line lesson ids.
- Added runtime SQLite constraint coverage proving:
  - issued invoice lines cannot reference missing lessons;
  - a lesson cannot be reused by a second issued invoice line;
  - a referenced lesson cannot be deleted.
- Replaced the add-foreign-key migration operation with a SQLite-safe table rebuild migration. The migration preserves valid referenced rows but does not invent local lesson identity for old invoice rows. This matches the accepted reset-friendly compatibility stance for this cycle.
- Updated older persistence tests so issued/reserved invoice lines reference real local lesson rows instead of invented ids.
- Verification run:

```powershell
dotnet test tests\MySmallBusinessLedger.Tests\MySmallBusinessLedger.Tests.csproj --filter FullyQualifiedName~SqlitePersistenceTests -nodeReuse:false
dotnet test tests\MySmallBusinessLedger.Tests\MySmallBusinessLedger.Tests.csproj --filter FullyQualifiedName~UseCaseTests -nodeReuse:false
dotnet test tests\MySmallBusinessLedger.Tests\MySmallBusinessLedger.Tests.csproj --filter FullyQualifiedName~MainWindowViewModelTests -nodeReuse:false
```

- Verification result:
  - `SqlitePersistenceTests` passed: 29/29.
  - `UseCaseTests` passed: 56/56.
  - `MainWindowViewModelTests` passed: 78/78.
  - Review stop satisfied: no remaining status path found that applies invoice/payment state without an exact lesson id or invoice id source.
  - No blocking user action.

### Stage 7: Documentation, Plan State, And Review Closure

Status: Completed 2026-06-27

Goal:

- Align docs and review artifacts with the corrected architecture.

Concrete steps:

1. Update `README.md` and `.ai/project-profile.md` to say:
   - Add Lesson requires date + plan;
   - time is optional;
   - selected lesson plan identity is preserved during creation;
   - local lesson identity is authoritative for invoicing.
2. Update `docs/business-requirements-and-logic.md` if any wording still implies descriptive matching or calendar-only invoice identity.
3. Update `.ai/context/HANDOFF.md` with final architecture, invariants, migration notes, and verified commands.
4. Move completed/superseded implementation plans out of `active` only after verifying their work and documenting evidence.
5. Update `MySmallBusinessLedger.sln` solution items if the repo expects active/completed plan files or new standards to be listed there.
6. Run `code-review` focused on identity/reference integrity.
7. Run `architecture-review` focused on dependency direction, transaction ownership, and removal of legacy paths.

Verification:

```powershell
dotnet build MySmallBusinessLedger.sln -nodeReuse:false
dotnet test MySmallBusinessLedger.sln -nodeReuse:false
```

Review stop:

- Stop if documentation still disagrees about whether time is required or whether invoice lines reference exact lessons.

Completion notes:

- Updated `README.md`, `.ai/project-profile.md`, `docs/business-requirements-and-logic.md`, and `.ai/context/HANDOFF.md` so they agree that lessons require date + plan, time is optional, and local `Lesson.Id` is the billing/audit identity.
- Documented that issued invoice lines are authoritative for lesson-invoice lineage and that `Lesson.InvoiceNumber` is only a denormalized display/lock cache maintained by invoice finalization.
- Preserved the reset-friendly stance for this migration: old invoice rows are not repaired with invented lesson identities. Fresh/reset data is the supported path for this cycle.
- Ran code review focused on domain identity, lineage, exact references, failure compensation, and UI repair paths. No blocking findings remained after adding Google Calendar cleanup when local lesson persistence fails.
- Ran architecture review focused on dependency direction, transaction ownership, local ledger authority, and legacy descriptive matching. No critical/high findings remained. Follow-up candidates remain:
  - remove or update the stale modal lesson editor path before reviving it, because the main-window add path now owns date + plan creation;
  - consider a later cleanup plan to remove the `Lesson.InvoiceNumber` cache once all read models join directly through issued invoice lines.
- Verification run:

```powershell
dotnet test tests\MySmallBusinessLedger.Tests\MySmallBusinessLedger.Tests.csproj --filter FullyQualifiedName~SqlitePersistenceTests -nodeReuse:false
dotnet test tests\MySmallBusinessLedger.Tests\MySmallBusinessLedger.Tests.csproj --filter FullyQualifiedName~UseCaseTests -nodeReuse:false
dotnet test tests\MySmallBusinessLedger.Tests\MySmallBusinessLedger.Tests.csproj --filter FullyQualifiedName~MainWindowViewModelTests -nodeReuse:false
dotnet build MySmallBusinessLedger.sln -nodeReuse:false
dotnet test MySmallBusinessLedger.sln -nodeReuse:false
```

- Verification result:
  - `SqlitePersistenceTests` passed: 29/29.
  - `UseCaseTests` passed: 57/57.
  - `MainWindowViewModelTests` passed: 78/78.
  - Solution build passed with 0 warnings and 0 errors.
  - Full solution test passed: 312/312.
  - Review stop satisfied: documentation and implementation now agree that time is optional, date + plan are required, and invoice lines reference exact local lessons.

## Verification Strategy

Use staged, focused verification first, then full solution verification.

Focused verification commands:

```powershell
dotnet test tests\MySmallBusinessLedger.Tests\MySmallBusinessLedger.Tests.csproj --filter FullyQualifiedName~UseCaseTests -nodeReuse:false
dotnet test tests\MySmallBusinessLedger.Tests\MySmallBusinessLedger.Tests.csproj --filter FullyQualifiedName~MainWindowViewModelTests -nodeReuse:false
dotnet test tests\MySmallBusinessLedger.Tests\MySmallBusinessLedger.Tests.csproj --filter FullyQualifiedName~SqlitePersistenceTests -nodeReuse:false
dotnet test tests\MySmallBusinessLedger.Tests\MySmallBusinessLedger.Tests.csproj --filter FullyQualifiedName~GoogleCalendarGatewayTests -nodeReuse:false
```

Full verification commands:

```powershell
dotnet build MySmallBusinessLedger.sln -nodeReuse:false
dotnet test MySmallBusinessLedger.sln -nodeReuse:false
```

Manual/user verification after implementation:

1. Reset invoice data as planned.
2. Start the application fresh.
3. Select a student whose default plan is not the plan to be tested.
4. Add a lesson with date + non-default plan and no time.
5. Confirm exactly one lesson is created, the selected plan remains selected, and Google Calendar shows the all-day lesson.
6. Add another lesson on the same date with a different plan.
7. Confirm neither lesson inherits completion, invoiced, or paid state from the other.
8. Issue an invoice for only one of those lessons.
9. Confirm only that exact lesson is locked/invoiced/paid according to the invoice status.
10. Create another same-date lesson after the invoice is paid.
11. Confirm the new lesson remains uninvoiced and unpaid.
12. Restart the app and confirm all states remain exact.

## Follow-On Review And Documentation

After implementation:

- Run `code-review` against the changed Core, Infrastructure, and App files.
- Run `architecture-review` against the final structure.
- Run `documentation-refresh`.
- Consider a later refactor plan to remove `Lesson.InvoiceNumber` if Stage 6 keeps it temporarily.
- Consider a later explicit edit-trail/audit plan if Lena needs history of lesson date/plan/time changes.

## Review Stop Points

Stop and get user confirmation if any of these become necessary:

- changing user-visible invoice numbering or PDF file naming;
- deleting real user data instead of the approved invoice-data reset;
- dropping support for Google Calendar-created lessons;
- broadening OAuth scopes;
- changing payment status semantics;
- replacing the existing invoice issue staging/publish flow;
- making time mandatory again.

## Final User Test Instructions

Post-completion user test is mandatory.

The final acceptance test should focus on the exact bug that triggered this plan:

1. Start the app from a clean/restarted state.
2. Pick a student with a default recurring plan.
3. Add a same-day lesson with a different selected plan.
4. Wait for refresh/reconciliation.
5. Verify no duplicate default-plan lesson appears.
6. Verify Google Calendar and the app show the selected plan lesson.
7. Issue and pay an invoice for one lesson.
8. Add another same-day/same-student lesson afterward.
9. Verify the new lesson remains Planned, not invoiced, and not paid.

## Completion Log

- 2026-06-27: Plan created from architecture-review findings and the observed fatal invoice/lesson identity bug. No source code changed.
- 2026-06-27: Plan executed and completed. Exact local lesson identity now flows through lesson creation, Google Calendar creation, local persistence, invoice lines, invoice finalization, database constraints, and docs. Full solution build and 312-test run passed.
