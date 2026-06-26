# Implementation Plan: Local Lessons Ledger

Status: Active
Created: 2026-06-26
Last updated: 2026-06-26
Owner/context: My Small Business Ledger; local-first WPF app for piano lesson invoicing and audit-safe lesson records.

## Related Docs And Standards

- `AGENTS.md`
- `README.md`
- `.ai/project-profile.md`
- `.ai/standards/architecture-principles.md`
- `.ai/standards/database-standards.md`
- `.ai/standards/persistence-and-artifacts.md`
- `.ai/standards/testing-and-verification.md`
- `.ai/standards/wpf-mvvm-standards.md`
- `docs/business-requirements-and-logic.md`

## Related Skills

- `implementation-plan` for this cross-layer schema/workflow change.
- `code-review` after implementation because this changes durable lesson and invoice behavior.
- `documentation-refresh` after implementation because source-of-truth and UI behavior change.
- `architecture-review` after implementation because persistence and Google Calendar ownership changes.
- `ui-review` after implementation because the lesson grid gains an editable Plan column.

## Goal

Introduce a local SQLite-backed `Lessons` table as the durable lesson ledger, including student and plan references, Google Calendar linkage, lesson state, invoicing lock state, and invoice linkage. Keep Google Calendar as the operational source for uninvoiced scheduling edits, while preventing Google Calendar edits or deletions from mutating invoiced lesson records.

## Non-Goals

- No full offline-first bidirectional sync engine beyond the current app startup/refresh/write-through flow.
- No tax reporting module.
- No credit notes or correction invoices.
- No support for arbitrary non-app-owned Google Calendar events beyond the existing `$ <student short name>` marker.
- No calendar conflict detection beyond existing exact duplicate behavior.

## Assumptions

- The app may add EF Core migrations; startup already applies pending migrations.
- Existing pilot/test data may be migrated with straightforward best-effort matching from Google Calendar on refresh.
- Active catalog plans remain the choices for new/editable lessons.
- Issued invoice lines remain immutable invoice-time snapshots.
- Multiple invoices in the same month for the same student are allowed when later uninvoiced lessons exist.

## Current Problem / Current State

The app currently treats Google Calendar entries as the lesson source of truth and derives invoice/payment state from issued invoice lines. That is unsafe for billing and tax audit because Google Calendar events are mutable outside the app. The main lesson grid also cannot durably store per-lesson plan selection, because lessons are not locally represented before invoice issue.

## Desired End State

- Local `Lessons` records are durable and include:
  - local lesson identity;
  - `StudentId`;
  - `PlanCatalogId`;
  - `StartsAt`;
  - `DurationMinutes`;
  - `State`;
  - nullable `GoogleCalendarEventId`;
  - nullable Google update token/timestamp when available;
  - nullable `InvoiceNumber`;
  - audit timestamps.
- Uninvoiced lessons reconcile from Google Calendar on refresh.
- App-created/edited/deleted uninvoiced lessons write through to Google Calendar.
- Invoiced lessons remain locked locally and ignore later Google Calendar edits/deletions.
- Invoice preview and issue use local uninvoiced lessons, not raw Google Calendar events.
- The main lesson grid shows columns: Date, Time, Plan, State, Invoiced, Paid.
- The Plan column is editable via dropdown only while the lesson is not invoiced.

## Behavior-Preserving Boundary

The following must remain unchanged unless this plan explicitly says otherwise:

- user-visible behavior: student/month navigation, date picker, time picker, state toggle, delete confirmation, payment toggle, student invoice list, settings, plan catalog, student management.
- public APIs / CLI / UI contracts: no CLI changes.
- durable file formats / artifacts / logs / reports: issued invoice PDF staging and final naming remain intact except invoice candidate logic permits multiple invoices per month.
- database schema / migrations / persistence behavior: existing tables must be preserved; new migration must not drop existing invoice, student, plan, or settings data.
- security / trust boundaries: OAuth tokens remain Windows-protected; Google Calendar scope remains `calendar.events.owned`.
- error handling / failure reporting: Google Calendar write failures remain immediate user-visible errors.
- performance/resource characteristics: month refresh remains bounded to the loaded month window.

## Intentional Behavior Changes

- Google Calendar is no longer the billing/audit source of truth for invoiced lessons.
- Local lessons become durable before invoice issue.
- Google Calendar changes are ignored for invoiced lessons.
- A student can receive more than one invoice for the same month when new uninvoiced lessons are added later.
- Lesson rows show and allow editing the selected Plan until invoiced.
- Add Lesson defaults its date to the first day of the displayed month when the displayed month is not the current month.
- Lesson date is required, but lesson time is optional. Lessons without a known time sync to Google Calendar as all-day events and display/print without a time.

## Affected Files, Projects, Commands, Docs, Schemas, Or Artifacts

- `src/MySmallBusinessLedger.Core`: lesson records, lesson repository port, startup dashboard query, invoice preview/candidate use cases, lesson add/edit/delete use cases.
- `src/MySmallBusinessLedger.Infrastructure`: EF entities, `LedgerDbContext`, migrations, SQLite lesson repository, Google Calendar gateway integration.
- `src/MySmallBusinessLedger.App`: main window ViewModel/display model/XAML, app composition, lesson editor service contracts.
- `tests/MySmallBusinessLedger.Tests`: core use-case tests, infrastructure persistence/calendar tests, WPF ViewModel/UI tests.
- `docs/business-requirements-and-logic.md`, `README.md`, `.ai/project-profile.md` after implementation.

## Risks And Mitigations

- Risk: duplicate local lessons when importing existing calendar events. Mitigation: match by Google event id first, then same student/date-time only for non-invoiced unlinked rows.
- Risk: calendar delete accidentally hides invoiced work. Mitigation: retain invoiced local rows even when the calendar event disappears.
- Risk: invoice candidate logic still assumes one invoice per student/month. Mitigation: change eligibility to "has uninvoiced lessons in period".
- Risk: plan dropdown edits could change invoice history. Mitigation: block plan edits once `InvoiceNumber` is present.
- Risk: migration complexity. Mitigation: additive schema only; no destructive migration.

## Open Questions

- [x] Question: Should Google Calendar remain source of truth for uninvoiced lessons?
  - Answer: yes. Calendar edits win until a lesson is invoiced; invoiced lessons ignore later calendar edits/deletions.
- [x] Question: Should multiple invoices for the same student/month be allowed?
  - Answer: yes, when later uninvoiced lessons exist in that month.
- [ ] Question: Should locally stored lesson plan changes be mirrored visibly into Google Calendar event text?
  - Recommended/default answer: no for v1; keep calendar as scheduling surface and local DB as billing metadata.
  - Alternatives: add a private/visible calendar marker or extended properties.
  - Why it matters: visible calendar text may confuse users and private extended properties need API support/tests.
  - Exact answer needed: can remain undecided if the first implementation keeps plan metadata local only.

## Staged Implementation Plan

### Stage 1: Domain And Persistence Model

Type: test + implementation
Status: Completed

Goal: Add durable local lesson records and repository operations without changing UI behavior yet.

Affected files/classes/schemas/commands/UI surfaces:

- Core lesson model/record and repository port.
- Infrastructure EF entity, context, SQLite repository, migration.
- Persistence tests.

Concrete steps:

1. Add failing tests for saving/loading lessons with student id, plan id, starts-at, duration, state, Google event id, and invoice linkage.
2. Add Core lesson record and `ILessonRepository`.
3. Add EF `LessonEntity`, indexes, relationships to students/plans/invoices where practical, and migration.
4. Add SQLite repository operations for period/student queries, upsert by Google event id, update mutable fields, lock to invoice, and delete uninvoiced lesson.

Behavior that must remain unchanged:

- Existing student/plan/invoice persistence tests keep passing.

Tests to add/update:

- `SqlitePersistenceTests` coverage for lesson round-trip and invoiced lock metadata.

Verification commands/manual checks:

- `dotnet test MySmallBusinessLedger.sln --filter "FullyQualifiedName~SqlitePersistenceTests" -nodeReuse:false`

Risks:

- Migration generation may require local .NET 10 SDK and EF tool availability.

Rollback point:

- Remove new lesson entity/repository/migration before any use-case changes.

Stop/review criteria:

- Lesson persistence tests pass and no existing persistence test regresses.

Documentation impact:

- Document schema/ownership after implementation.

Follow-on actions:

- Stage 2.

Runner may continue automatically after this stage: yes.

### Stage 2: Calendar Reconciliation And Lesson Use Cases

Type: test + implementation
Status: Completed

Goal: Reconcile calendar events into local uninvoiced lessons and make app lesson mutations write through to Google Calendar plus local DB.

Affected files/classes/schemas/commands/UI surfaces:

- Startup dashboard query/service.
- Lesson add/edit/delete/state use cases.
- Google Calendar gateway and event client contracts if needed.

Concrete steps:

1. Add failing tests that calendar edits update uninvoiced local lessons.
2. Add failing tests that calendar edits/deletes do not mutate invoiced local lessons.
3. Add failing tests that app edits write to Google Calendar and local lesson records.
4. Implement reconciliation in Application/Core use cases, with Infrastructure adapters only performing technical persistence/calendar operations.

Behavior that must remain unchanged:

- Existing `$ <student>` marker parsing and completed state marker behavior.
- Google write failures remain user-visible operation failures.

Tests to add/update:

- Startup dashboard and lesson use-case tests.
- Calendar gateway tests only if the gateway contract changes.

Verification commands/manual checks:

- `dotnet test MySmallBusinessLedger.sln --filter "FullyQualifiedName~UseCaseTests|FullyQualifiedName~GoogleCalendarGatewayTests" -nodeReuse:false`

Risks:

- Reconciling old calendar events may create rows for lessons already represented only by issued invoice lines. Mitigate by matching invoice lines and marking matching lessons as invoiced during reconciliation when possible.

Rollback point:

- Revert Stage 2 while keeping unused local lesson persistence from Stage 1.

Stop/review criteria:

- Reconciliation semantics are proven by tests.

Documentation impact:

- Update requirements after implementation.

Follow-on actions:

- Stage 3.

Runner may continue automatically after this stage: yes.

### Stage 3: Invoice Workflow Changes

Type: test + implementation
Status: Completed

Goal: Invoice only local uninvoiced lessons, lock issued lessons, and allow later invoices in the same month.

Affected files/classes/schemas/commands/UI surfaces:

- Invoice candidate selection and preview use cases.
- Invoice issue/finalization use case and repository interactions.
- Invoice filename/candidate tests.

Concrete steps:

1. Add failing tests that invoice preview uses local lesson plan selection.
2. Add failing tests that issued invoice locks the referenced lessons.
3. Add failing tests that a student with prior invoice in the same month remains eligible when later uninvoiced lessons exist.
4. Implement invoice candidate/preview/issue changes.

Behavior that must remain unchanged:

- Issued invoice snapshots remain immutable and PDF naming still includes invoice number.

Tests to add/update:

- Core invoice use-case tests and persistence tests.

Verification commands/manual checks:

- `dotnet test MySmallBusinessLedger.sln --filter "FullyQualifiedName~Invoice" -nodeReuse:false`

Risks:

- Existing candidate behavior may depend on "no invoice this month"; update docs and tests explicitly.

Rollback point:

- Revert invoice use-case changes while retaining prior stages.

Stop/review criteria:

- Multiple-invoice-in-month behavior is explicitly tested.

Documentation impact:

- Update invoice rules.

Follow-on actions:

- Stage 4.

Runner may continue automatically after this stage: yes.

### Stage 4: Main Window UI Plan Column

Type: test + implementation
Status: Completed

Goal: Show and edit per-lesson plan in the lesson grid.

Affected files/classes/schemas/commands/UI surfaces:

- `MainWindow.xaml`
- `MainWindowViewModel`
- `LessonDisplayModel`
- lesson editor service contracts
- WPF tests

Concrete steps:

1. Add failing ViewModel tests for Add Lesson first-of-display-month default.
2. Add failing ViewModel/WPF tests for column order Date, Time, Plan, State, Invoiced, Paid.
3. Add failing ViewModel tests that plan selection saves for uninvoiced lessons and is unavailable for invoiced lessons.
4. Implement display model plan options, dropdown binding, save plumbing, and command availability.

Behavior that must remain unchanged:

- Date/time inline editing, optional lesson time, auto-save, incomplete draft preservation, delete, state toggle, invoice/payment display.

Tests to add/update:

- `MainWindowViewModelTests`.

Verification commands/manual checks:

- `dotnet test MySmallBusinessLedger.sln --filter "FullyQualifiedName~MainWindowViewModelTests" -nodeReuse:false`

Risks:

- Draft auto-save may fire too early while the user is still editing date/time. Mitigate with debounced draft auto-save and refreshed-row selection preservation.

Rollback point:

- Revert UI/ViewModel changes while retaining backend local lesson model.

Stop/review criteria:

- UI tests prove column/order/editability semantics.

Documentation impact:

- Update UI behavior docs/README.

Follow-on actions:

- Stage 5.

Runner may continue automatically after this stage: yes.

### Stage 5: Verification, Review, And Docs

Type: verification + documentation + review
Status: In progress

Goal: Verify the full cross-layer change and align docs/context.

Affected files/classes/schemas/commands/UI surfaces:

- Entire solution.
- README and business requirements.
- AI context if present.

Concrete steps:

1. Run targeted tests from prior stages.
2. Run full build and test.
3. Update docs to describe local Lessons as durable source and Google Calendar as uninvoiced sync surface.
4. Perform code-review/architecture-review/ui-review pass.

Behavior that must remain unchanged:

- Existing invoice PDF safety and OAuth token safety.

Tests to add/update:

- Any gaps found during review.

Verification commands/manual checks:

- `dotnet build MySmallBusinessLedger.sln -nodeReuse:false`
- `dotnet test MySmallBusinessLedger.sln -nodeReuse:false`

Risks:

- Full WPF tests may be environment-sensitive.

Rollback point:

- Revert final documentation if implementation is rolled back.

Stop/review criteria:

- Full verification status is known and documented.

Documentation impact:

- Required.

Follow-on actions:

- Move plan to completed after user acceptance.

Runner may continue automatically after this stage: no. Blocking action required: user acceptance/manual test.

## Verification Strategy

- TDD for new persistence, reconciliation, invoice, and UI behavior.
- Targeted test filters per stage.
- Full `dotnet build MySmallBusinessLedger.sln -nodeReuse:false`.
- Full `dotnet test MySmallBusinessLedger.sln -nodeReuse:false`.
- Manual UI test for Add Lesson date default, Plan dropdown editability, invoice lock behavior, and adding a later same-month lesson after issuing an invoice.

## Follow-On Review And Documentation

- Code review required: yes - cross-layer durable state and invoice behavior.
- Architecture review required: yes - source-of-truth and integration boundary changed.
- UI review required: yes - lesson grid and editability changed.
- Documentation refresh required: yes - business rules and project profile currently describe Google Calendar as truth.

## Review Stop Points

- After Stage 1 if migration generation fails or requires destructive schema changes.
- After Stage 2 if Google Calendar reconciliation cannot safely distinguish invoiced lessons.
- After Stage 3 if invoice candidate behavior reveals hidden one-invoice-per-month assumptions.
- Before Stage 5 completion for user manual acceptance.

## Final User Test Instructions

Post-completion user test: mandatory.

Prerequisites/setup:

- Run the WPF app with a test SQLite database and configured Google Calendar test account/calendar.
- Have at least one student with a `Prøvetime` plan and one normal active plan.

Exact user actions or commands:

1. Navigate to a non-current month and click `Add Lesson`.
2. Confirm the new lesson date picker starts on the first day of the displayed month.
3. Choose `Prøvetime` in the Plan column, set a date/time, and let it save.
4. Issue an invoice for that lesson.
5. Move/delete the matching Google Calendar event externally.
6. Refresh the app.
7. Add another lesson in the same month under a different plan and issue another invoice.

Expected successful result:

- The invoiced `Prøvetime` lesson remains visible/locked locally despite external calendar edits/deletes.
- The later same-month lesson remains editable until invoiced and can be invoiced separately.
- Invoice PDF names remain unique because invoice number is included.

Expected failure/safety result, if relevant:

- Google Calendar write failures show an immediate app error and do not silently mark local changes complete.

Where to inspect artifacts, reports, logs, database state, or UI state:

- Main lesson grid.
- Student invoices list.
- SQLite `Lessons` and invoice tables in the app data database.
- Invoice PDF folder.

Cleanup/reset instructions:

- Delete test Google Calendar events and reset the test SQLite database if needed.

## Completion Log

- 2026-06-26:
  - Stage: Plan creation
  - Files changed: `.ai/implementation-plans/active/2026-06-26-local-lessons-ledger.md`
  - Summary: Captured approved local Lessons ledger and Google Calendar reconciliation design.
  - Tests/verification run: not yet run
  - Results: pending
  - Deviations: none
  - Risks/open questions: local plan metadata is not proposed to be mirrored visibly into Google Calendar for v1.
- 2026-06-26:
  - Stage: 1-4 implementation
  - Files changed: Core lesson model/repository/use cases, SQLite Lessons entity/repository/migration, startup dashboard reconciliation, invoice candidate/preview/issue flow, main-window lesson grid/view model/service wiring, use-case/persistence/WPF tests.
  - Summary: Added durable local Lessons rows, reconciled uninvoiced calendar lessons into local records, kept invoiced lessons locked against later calendar edits/deletes, switched invoice candidate/preview/issue flow to local uninvoiced lessons, added Plan column/dropdown for uninvoiced local lessons, and defaulted Add Lesson in non-current months to the first day of the displayed month.
  - Tests/verification run: `dotnet test tests\MySmallBusinessLedger.Tests\MySmallBusinessLedger.Tests.csproj -nodeReuse:false` with `MSBL_RUN_UI_TESTS=1`; `dotnet build MySmallBusinessLedger.sln -nodeReuse:false`; `dotnet test MySmallBusinessLedger.sln -nodeReuse:false` with `MSBL_RUN_UI_TESTS=1`
  - Results: build passed with 0 warnings; solution tests passed, 289 tests
  - Deviations: newly autosaved draft rows become fully plan-editable after immediate dashboard reconciliation/refresh because the Google Calendar add response does not carry a local lesson id.
  - Risks/open questions: local plan metadata is still not mirrored visibly into Google Calendar for v1.
- 2026-06-26:
  - Stage: Optional lesson time support
  - Files changed: Core lesson/calendar/invoice models, Google Calendar event client/gateway, SQLite lesson/invoice-line schema, main-window lesson display/save flow, invoice date display, requirements/context docs, ViewModel/calendar tests.
  - Summary: Made lesson time optional. Date-only lessons save as Google Calendar all-day events, persist known-time state locally, remain selected after reconciliation, become plan-editable after immediate refresh, and display/print without fake midnight times. Draft autosave is debounced so quick date-then-time edits save the intended timed lesson instead of prematurely saving all-day.
  - Tests/verification run: `dotnet test tests\MySmallBusinessLedger.Tests\MySmallBusinessLedger.Tests.csproj -nodeReuse:false --filter "FullyQualifiedName~MainWindowViewModelTests"`; `dotnet test MySmallBusinessLedger.sln -nodeReuse:false`
  - Results: main-window tests passed, 70 tests; solution tests passed, 293 tests
- 2026-06-26:
  - Stage: Startup regression hardening
  - Files changed: `StartupDashboardQuery`, `UseCaseTests`
  - Summary: Prevented calendar entries for unknown/inactive students from being selected or reconciled into local Lessons rows, avoiding invalid local `StudentId` persistence on startup.
  - Tests/verification run: `dotnet test tests\MySmallBusinessLedger.Tests\MySmallBusinessLedger.Tests.csproj --filter "FullyQualifiedName~UseCaseTests.StartupDashboardIgnoresCalendarLessonsWithoutKnownActiveStudentWhenUsingLocalLessonLedger" -nodeReuse:false`; `dotnet test tests\MySmallBusinessLedger.Tests\MySmallBusinessLedger.Tests.csproj --filter "FullyQualifiedName~UseCaseTests.StartupDashboard" -nodeReuse:false`
  - Results: passed
  - Deviations: none
  - Risks/open questions: none
