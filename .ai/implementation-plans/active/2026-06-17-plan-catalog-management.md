# Implementation Plan: Plan Catalog Management

Status: Active
Created: 2026-06-17
Last updated: 2026-06-17
Owner/context: Ruben and Codex; Ruben wants Plans to be managed independently from Students through a `Plan > Manage Plans` menu. Plans must be immutable once published, with a Draft -> Active -> Retired lifecycle, so the foundation for lessons and invoicing stays stable.

## Related Docs And Standards

- `README.md`
- `AGENTS.md`
- `.ai/project-profile.md`
- `docs/business-requirements-and-logic.md`
- `.ai/standards/architecture-principles.md`
- `.ai/standards/coding-standards.md`
- `.ai/standards/database-standards.md`
- `.ai/standards/testing-and-verification.md`
- `.ai/standards/ui-design-standards.md`
- `.ai/standards/wpf-mvvm-standards.md`

## Related Skills

- `implementation-plan` for this persistent plan.
- `plan-runner` to execute this plan stage by stage.
- `code-review` after persistence/use-case/UI behavior is implemented.
- `ui-review` after the Manage Plans window and student plan selection behavior are implemented.
- `documentation-refresh` after behavior, runtime topology, and current-state docs change.

## Goal

Add an independent immutable Plan catalog and a `Plan > Manage Plans` WPF workflow for adding draft plans, publishing draft plans, retiring active plans, reactivating retired plans, and deleting only draft plans, while preserving copied plan data on existing student plan periods and invoices.

## Non-Goals

- Do not mutate existing `StudentPlanPeriod` rows when a catalog plan changes lifecycle state.
- Do not rename retire to delete in the user interface.
- Do not allow editing active or retired plan fields.
- Do not allow assigning draft or retired plans to students.
- Do not add automatic price changes to active students.
- Do not build a full plan versioning/history UI in this plan.
- Do not add public Google Calendar, invoice, or VAT behavior changes.
- Do not introduce a web/backend service or new production dependency.

## Assumptions

- The catalog stores immutable published plan templates for future student assignments.
- A student plan period remains a snapshot of plan name, price, lesson count, duration, flexibility, number of students, and effective date at assignment time.
- Draft plans may be edited and truly deleted because they have never been assignment-eligible.
- Publishing a draft plan makes it active and immutable.
- Active plans may only be retired; their fields cannot be edited.
- Retired plans may only be reactivated; their fields cannot be edited and they cannot be assigned while retired.
- Existing student editor may continue to show the current plan as read-only until plan assignment is redesigned in a later stage of this plan.
- The current app uses `EnsureCreated`; adding catalog persistence means updating EF entities/model and tests carefully, with migration policy to be revisited before broader distribution.

## Current Problem / Current State

- `Plan` exists only as a Core object embedded in student plan periods.
- Student plan periods are persisted as copied fields on `StudentPlanPeriodEntity`.
- `StudentEditorView.xaml` has an `Add Plan` button, but `StudentEditorViewModel.ShowAddPlanPlaceholderAsync` only reports that plan administration will be implemented later.
- There is no plan catalog table, repository, application service, main menu item, or Manage Plans window.
- `docs/business-requirements-and-logic.md` says Plans are separate from Students and are contract/agreement concepts, but current UI makes plan creation appear inside the student editor.
- There is no Plan lifecycle state machine today.

## Desired End State

- Main menu contains a top-level `Plan` menu with `Manage Plans`.
- `Manage Plans` opens a WPF window listing all known plans grouped or filterable by state: Draft, Active, Retired.
- The selected plan appears in a detail pane where fields are editable only for Draft plans.
- Draft plans support `Delete` and `Publish`; they cannot be assigned to students.
- Active plans support `Retire` only; they cannot be edited except by changing lifecycle state to Retired.
- Retired plans support `Reactivate` only; they cannot be edited and cannot be assigned to students while retired.
- Only Active plans are offered for student assignment.
- Existing student plan periods and issued invoice lines remain unchanged after plan publish/retire/reactivate or draft edits/deletes.
- Automated tests prove lifecycle transitions, command availability, assignment eligibility, persistence, and student plan snapshot immutability.

## Behavior-Preserving Boundary

The following must remain unchanged unless this plan explicitly says otherwise:

- Student plan periods keep copied plan data and remain the source for invoice pricing.
- Issued invoices remain immutable snapshots and are not recalculated by catalog changes.
- Existing lesson filtering, lesson editing, invoice issue, PDF storage, payment status, Google Calendar sync, and settings behavior remain unchanged.
- Core remains independent of WPF, EF Core, SQLite, filesystem, and host composition.
- Infrastructure owns concrete SQLite entities/repositories.
- WPF ViewModels own presentation state and call application services rather than opening DbContexts.

## Intentional Behavior Changes

- Add a `Plan > Manage Plans` menu path.
- Add a durable plan catalog.
- Add a durable Plan lifecycle state machine: Draft, Active, Retired.
- Add Draft-only editing and true Draft-only deletion.
- Add Publish from Draft to Active and Reactivate from Retired to Active.
- Add Retire from Active to Retired.
- Restrict assignment eligibility to Active plans.
- Remove or de-emphasize the student editor's current placeholder `Add Plan` path once plan assignment has a clearer catalog-backed follow-up.

## Affected Files, Projects, Commands, Docs, Schemas, Or Artifacts

Expected source files:

- Create: `src/MySmallBusinessLedger.Core/Ports/IPlanCatalogRepository.cs`
- Create: `src/MySmallBusinessLedger.Core/Plans/PlanCatalogItem.cs` or equivalent catalog model
- Create: `src/MySmallBusinessLedger.Core/Plans/PlanCatalogState.cs`
- Create: `src/MySmallBusinessLedger.Core/UseCases/Plans/*`
- Create: `src/MySmallBusinessLedger.Infrastructure/Persistence/SqlitePlanCatalogRepository.cs`
- Modify: `src/MySmallBusinessLedger.Infrastructure/Persistence/Entities.cs`
- Modify: `src/MySmallBusinessLedger.Infrastructure/Persistence/LedgerDbContext.cs`
- Create: `src/MySmallBusinessLedger.App/Services/IPlanCatalogService.cs`
- Create: `src/MySmallBusinessLedger.App/Services/CorePlanCatalogService.cs`
- Create: `src/MySmallBusinessLedger.App/Services/IPlanCatalogLauncher.cs`
- Create: `src/MySmallBusinessLedger.App/Services/PlanCatalogWindowLauncher.cs`
- Create: `src/MySmallBusinessLedger.App/ViewModels/PlanCatalogViewModel.cs`
- Create: `src/MySmallBusinessLedger.App/ViewModels/PlanEditorViewModel.cs` only if inline editing becomes too large for one ViewModel
- Create: `src/MySmallBusinessLedger.App/Views/PlanCatalogView.xaml`
- Create: `src/MySmallBusinessLedger.App/Views/PlanCatalogView.xaml.cs`
- Modify: `src/MySmallBusinessLedger.App/MainWindow.xaml`
- Modify: `src/MySmallBusinessLedger.App/ViewModels/MainWindowViewModel.cs`
- Modify: `src/MySmallBusinessLedger.App/App.xaml.cs`
- Modify: `src/MySmallBusinessLedger.App/ViewModels/StudentEditorViewModel.cs` and `src/MySmallBusinessLedger.App/Views/StudentEditorView.xaml` when catalog-backed assignment replaces the placeholder.

Expected tests:

- Create: `tests/MySmallBusinessLedger.Tests/Core/Plans/PlanCatalogTests.cs`
- Create or modify: `tests/MySmallBusinessLedger.Tests/Core/UseCases/PlanCatalogUseCaseTests.cs`
- Modify: `tests/MySmallBusinessLedger.Tests/Infrastructure/Persistence/SqlitePersistenceTests.cs`
- Create: `tests/MySmallBusinessLedger.Tests/App/ViewModels/PlanCatalogViewModelTests.cs`
- Modify: `tests/MySmallBusinessLedger.Tests/App/ViewModels/MainWindowViewModelTests.cs`
- Modify: `tests/MySmallBusinessLedger.Tests/App/ViewModels/EditorViewModelTests.cs` when assignment behavior changes.

Expected docs/context:

- Modify: `README.md`
- Modify: `.ai/project-profile.md`
- Modify: `.ai/context/HANDOFF.md`
- Modify: `docs/business-requirements-and-logic.md` if behavior wording needs to distinguish catalog plans from assigned student plan snapshots.

Database/schema:

- Add a `PlanCatalog` / `Plans` table with durable columns for plan fields and lifecycle state.
- Add created/published/retired timestamps if useful for traceability.
- Add a uniqueness rule for non-deleted plan names if feasible.
- Keep `StudentPlanPeriodEntity` fields unchanged to preserve snapshot behavior.

## Risks And Mitigations

- Risk: Active or retired plan fields are edited accidentally, weakening plan immutability.
  - Mitigation: Core use cases reject field changes outside Draft; ViewModel disables fields unless selected plan is Draft; tests cover command availability and service validation.
- Risk: Draft deletion is confused with retire.
  - Mitigation: Use `Delete` only for Draft plans and `Retire` only for Active plans; hide irrelevant buttons by state.
- Risk: Catalog lifecycle changes accidentally update existing student plan periods.
  - Mitigation: Keep student periods as copied fields; add tests that publish/retire/reactivate plans and assert existing student periods and issued invoice lines are unchanged.
- Risk: EF `EnsureCreated` makes schema evolution awkward for real pilot data.
  - Mitigation: Add tests around new table creation and document migration risk; consider introducing explicit EF migrations in a separate stage if needed.
- Risk: Student editor still feels awkward if it cannot assign catalog plans after Manage Plans is added.
  - Mitigation: Include a later stage to replace the placeholder with catalog-backed plan selection or clearly scoped assignment flow.
- Risk: One Manage Plans ViewModel becomes too broad.
  - Mitigation: Split list/catalog responsibilities from edit-form state if tests or readability show pressure.

## Open Questions

- [x] Question: Should the user-facing destructive operation be `Delete` or `Retire`?
  - Recommended/default answer: Retire.
  - Alternatives: True delete for unused plans only; soft delete named Delete; no removal operation.
  - Why it matters: The plan catalog is durable business memory, and "delete" suggests removing history.
  - Exact answer received: Active plans use `Retire`; it should not be named delete. Draft-only deletion is allowed by the later lifecycle decision because Draft plans were never assignment-eligible.

- [x] Question: Should published Plans be mutable catalog records or immutable lifecycle records?
  - Recommended/default answer: Immutable lifecycle records.
  - Alternatives: Editable catalog with copied student snapshots; full versioned plans; soft-edit active plans with audit log.
  - Why it matters: Student lessons and invoicing need a stable foundation.
  - Exact answer received: Plans should be immutable with Draft, Active, and Retired states.

## Staged Implementation Plan

### Stage 1: Add Core Plan Lifecycle Contracts

Type: implementation / test
Status: Completed

Goal:

Introduce Core-owned plan catalog models, lifecycle state, repository port, and use cases without changing UI behavior.

Affected files/classes/schemas/commands/UI surfaces:

- Create Core plan catalog model, lifecycle enum, and repository port.
- Create use cases for listing plans, creating draft plans, saving draft fields, deleting drafts, publishing drafts, reactivating retired plans, and retiring active plans.
- Create focused Core tests.

Concrete steps:

- Write failing tests for creating a valid Draft plan and rejecting invalid draft fields.
- Write failing tests that Draft plans can be edited and deleted.
- Write failing tests that Draft plans can be published into Active state.
- Write failing tests that Active plans cannot have fields edited or be deleted.
- Write failing tests that Active plans can be retired into Retired state.
- Write failing tests that Retired plans cannot have fields edited or be deleted, but can be reactivated into Active state.
- Write failing tests that only Active plans are assignment eligible.
- Add a catalog model with plan fields plus `PlanCatalogState`.
- Add repository port methods for listing all plans, listing active assignable plans, creating drafts, saving draft fields, deleting drafts, publishing, retiring, and reactivating.
- Add use cases that validate plan values, enforce lifecycle transitions, and never touch students or invoices.
- Run focused Core plan tests.

Behavior that must remain unchanged:

- Existing `Plan` and `StudentPlanPeriod` behavior remains unchanged.
- Existing student save use case remains unchanged.

Tests to add/update:

- `PlanCatalogUseCaseTests` for validation, draft edit/delete, publish, retire, reactivate, immutable active/retired states, and active assignment filtering.

Verification commands/manual checks:

```powershell
dotnet test tests\MySmallBusinessLedger.Tests\MySmallBusinessLedger.Tests.csproj --filter "FullyQualifiedName~PlanCatalog" -nodeReuse:false
```

Risks:

- Duplicating `Plan` and catalog model semantics.
- Letting UI-only command availability become the only lifecycle guard.

Rollback point:

- Remove the new Core catalog model, port, use cases, and tests.

Stop/review criteria:

- Stop if catalog logic requires Infrastructure or UI dependencies in Core.

Documentation impact:

- None until behavior is wired.

Follow-on actions:

- Stage 2 persists the catalog.

Runner may continue automatically after this stage: yes.

### Stage 2: Persist Plan Lifecycle In SQLite

Type: implementation / test / database
Status: Completed

Goal:

Add SQLite persistence for catalog plans and lifecycle state while keeping student plan period snapshot storage unchanged.

Affected files/classes/schemas/commands/UI surfaces:

- Modify EF entities and `LedgerDbContext`.
- Add `SqlitePlanCatalogRepository`.
- Add persistence tests.

Concrete steps:

- Write failing persistence test `PlanCatalogLifecycleCanBePersistedAndListed`.
- Add a plan catalog entity/table with plan fields and lifecycle state.
- Configure required fields, max lengths, enum conversions, and active-name uniqueness or equivalent validation.
- Implement repository mapping between EF entity and Core catalog model.
- Add persistence tests proving Draft, Active, and Retired plans all appear in all-plan list.
- Add persistence tests proving only Active plans appear in assignable/active list.
- Add persistence tests proving Draft deletion removes only Draft records.
- Add a persistence test proving publish/retire/reactivate does not alter existing `StudentPlanPeriodEntity` rows.

Behavior that must remain unchanged:

- Existing student, invoice, settings, calendar persistence tests continue to pass.
- Student plan period table remains copied snapshot data.

Tests to add/update:

- `SqlitePersistenceTests` plan catalog tests.

Verification commands/manual checks:

```powershell
dotnet test tests\MySmallBusinessLedger.Tests\MySmallBusinessLedger.Tests.csproj --filter "FullyQualifiedName~SqlitePersistenceTests" -nodeReuse:false
```

Risks:

- Existing pilot databases created with `EnsureCreated` will not automatically receive new tables without migration support.

Rollback point:

- Remove entity, DbSet, model configuration, repository, and tests.

Stop/review criteria:

- Stop if supporting existing pilot databases requires a migration decision before UI work.

Documentation impact:

- Update project profile if migration policy changes.

Follow-on actions:

- Stage 3 adds application and UI service boundaries.

Runner may continue automatically after this stage: yes, unless migration policy blocks existing pilot data.

### Stage 3: Add State-Aware Manage Plans Window And Main Menu Entry

Type: implementation / test / UI
Status: Completed

Goal:

Expose lifecycle-aware catalog management from `Plan > Manage Plans`.

Affected files/classes/schemas/commands/UI surfaces:

- Add main menu item.
- Add service/launcher/ViewModel/View.
- Wire composition root.

Concrete steps:

- Write failing MainWindow/ViewModel test that `OpenManagePlansCommand` calls a plan catalog launcher.
- Write failing ViewModel tests for loading all plans grouped or filterable by Draft, Active, and Retired state.
- Write failing ViewModel tests that selected Draft plans expose editable fields plus `Delete` and `Publish`.
- Write failing ViewModel tests that selected Active plans expose read-only fields plus `Retire`.
- Write failing ViewModel tests that selected Retired plans expose read-only fields plus `Publish`.
- Write failing ViewModel tests that irrelevant buttons are hidden or disabled for each state.
- Add `Plan > Manage Plans` to `MainWindow.xaml`.
- Add `IPlanCatalogLauncher` and `PlanCatalogWindowLauncher`.
- Add `PlanCatalogViewModel` with commands for Add Draft, Save Draft, Delete Draft, Publish Draft, Save Retire On, Retire, Reactivate, and reload.
- Add `PlanCatalogView.xaml` with a list grouped by state and a selected-plan detail pane.
- Bind field editability to selected plan state; Draft editable, Active/Retired read-only.
- Use user-facing label `Retire` only for Active plans and `Delete` only for Draft plans.
- Wire repository/service/launcher in `App.xaml.cs`.

Behavior that must remain unchanged:

- Existing dashboard commands and menus continue to work.
- State transitions require explicit user intent in the Manage Plans window.
- UI state must mirror Core lifecycle rules and never imply active/retired plans are editable.

Tests to add/update:

- Main window command tests.
- Plan catalog ViewModel tests.
- WPF smoke test for Manage Plans view if practical.

Verification commands/manual checks:

```powershell
dotnet test tests\MySmallBusinessLedger.Tests\MySmallBusinessLedger.Tests.csproj --filter "FullyQualifiedName~PlanCatalogViewModelTests|FullyQualifiedName~MainWindowViewModelTests" -nodeReuse:false
```

Risks:

- UI may become too dense if list and edit form are cramped.

Rollback point:

- Remove menu item, launcher, ViewModel, view, service wiring, and tests.

Stop/review criteria:

- Stop for UI review if lifecycle grouping, read-only state, command availability, or selected-plan state is ambiguous.

Documentation impact:

- README and project profile need updates after behavior is verified.

Follow-on actions:

- Stage 4 connects student plan assignment to the catalog.

Runner may continue automatically after this stage: yes.

### Stage 4: Replace Student Add Plan Placeholder With Catalog Assignment

Type: implementation / test / UI
Status: Completed

Goal:

Make student plan assignment use only Active catalog plans while still storing copied plan snapshots on student plan periods.

Affected files/classes/schemas/commands/UI surfaces:

- Modify student editor service/ViewModel/view as needed.
- Possibly add a small plan selection dialog or inline selector.

Concrete steps:

- Write failing tests that selecting an Active catalog plan for a student copies all plan fields into `SaveStudentRequest.PlanPeriods`.
- Write failing tests that Draft and Retired plans are not offered for assignment.
- Write failing tests that publishing/retiring/reactivating a catalog plan later does not alter the saved student plan period.
- Replace the placeholder `Add Plan` behavior with catalog-backed selection or a scoped assignment flow.
- Ensure only Active plans are offered for new assignment.
- Preserve current plan display and schedule behavior.

Behavior that must remain unchanged:

- Existing student plan periods remain valid.
- Save student validation still rejects overlapping plan periods.
- Existing invoices remain unchanged.

Tests to add/update:

- Student editor ViewModel tests for catalog assignment.
- Persistence/use-case tests for snapshot immutability.

Verification commands/manual checks:

```powershell
dotnet test tests\MySmallBusinessLedger.Tests\MySmallBusinessLedger.Tests.csproj --filter "FullyQualifiedName~EditorViewModelTests|FullyQualifiedName~StudentUseCaseTests|FullyQualifiedName~SqlitePersistenceTests" -nodeReuse:false
```

Risks:

- Full multi-period plan editing may be larger than this stage.

Rollback point:

- Restore placeholder behavior and keep Manage Plans independent.

Stop/review criteria:

- Stop if student plan period UX needs a separate design decision for multiple historical periods.

Documentation impact:

- Update business requirements with catalog-vs-assigned-plan distinction.

Follow-on actions:

- Stage 5 documentation and review.

Runner may continue automatically after this stage: no. Blocking action required: confirm whether student plan assignment UI should be inline in Student Editor or a separate dialog if the simple selector is insufficient.

### Stage 5: Documentation Refresh, Review, And Final Verification

Type: documentation / review / verification
Status: Completed

Goal:

Verify and document the completed plan catalog behavior.

Affected files/classes/schemas/commands/UI surfaces:

- README, project profile, handoff, business requirements.
- Review checklists.

Concrete steps:

- Update docs to describe `Plan > Manage Plans`, Draft/Active/Retired lifecycle, Draft-only editing/deletion, Active-only assignment, Active retire, Retired reactivate, and assigned-plan snapshots.
- Search for stale `Add Plan` placeholder references.
- Run focused `code-review` for use-case/persistence behavior.
- Run focused `ui-review` for Manage Plans.
- Run full build and test suite.

Behavior that must remain unchanged:

- Docs must not claim catalog lifecycle changes update existing students.
- Docs must only use delete for Draft plans and retire for Active plans.

Tests to add/update:

- None unless review finds gaps.

Verification commands/manual checks:

```powershell
rg "delete plan|Add Plan.*placeholder|plan administration will be implemented" README.md .ai docs src tests
dotnet build MySmallBusinessLedger.sln -nodeReuse:false
dotnet test MySmallBusinessLedger.sln -nodeReuse:false
```

Risks:

- Documentation could blur catalog plans and student plan snapshots.

Rollback point:

- Restore docs if implementation is rolled back.

Stop/review criteria:

- Stop if reviews find snapshot mutation risk or misleading retire UI.

Documentation impact:

- This is the documentation stage.

Follow-on actions:

- Move plan to completed after user test succeeds.

Runner may continue automatically after this stage: no. Blocking action required: user performs final UI test.

## Verification Strategy

- Use TDD for Core use cases, SQLite repository behavior, ViewModel commands, and student assignment behavior.
- Run focused tests after each stage.
- Run full build and full test suite before final user testing:

```powershell
dotnet build MySmallBusinessLedger.sln -nodeReuse:false
dotnet test MySmallBusinessLedger.sln -nodeReuse:false
```

- Use manual WPF checks for menu placement, Manage Plans layout, keyboard/tab flow, state grouping, read-only field behavior, Draft `Delete`/`Publish`, Active `Retire`, Retired `Publish`, and student assignment clarity.

## Follow-On Review And Documentation

- Code review required: yes — persistence, snapshot immutability, validation, and lifecycle semantics are behavior-bearing.
- Architecture review required: light/focused if implementation pressure moves catalog policy into UI or Infrastructure.
- UI review required: yes — new menu/window, state grouping, read-only behavior, and lifecycle actions affect user trust and workflow clarity.
- Documentation refresh required: yes — product behavior and architecture state change.

## Review Stop Points

- After Stage 2 if existing pilot database migration/support is unclear.
- After Stage 3 for Manage Plans UX, lifecycle grouping, field editability, and action wording.
- Before Stage 4 if student plan assignment needs a larger interaction design.
- Before completion if any test suggests catalog lifecycle changes can mutate assigned student plan snapshots.

## Final User Test Instructions

Post-completion user test: mandatory.

Prerequisites/setup:

- App builds and runs against a safe local database.
- At least one existing student has a saved plan period.

Exact user actions or commands:

1. Run:

   ```powershell
   dotnet build MySmallBusinessLedger.sln -nodeReuse:false
   dotnet test MySmallBusinessLedger.sln -nodeReuse:false
   dotnet run --project src\MySmallBusinessLedger.App\MySmallBusinessLedger.App.csproj
   ```

2. Open `Plan > Manage Plans`.
3. Add a new plan with name, price, lessons per week, duration, flexibility, number of students, and effective date; confirm it is Draft.
4. Edit the Draft plan and save it.
5. Delete a separate Draft test plan and confirm it is removed.
6. Publish the kept Draft plan and confirm it becomes Active.
7. Confirm Active plan fields are read-only except `Retire on`, and only `Save retire date` / `Retire` actions are available.
8. Set a future `Retire on` date on the Active plan and confirm it saves.
9. Retire the Active plan and confirm it becomes Retired.
10. Confirm Retired plan fields are read-only and only `Reactivate` is available.
11. Reactivate the Retired plan and confirm it becomes Active again with no `Retire on` date.
12. Confirm the Manage Plans list columns appear as `Plan`, `Price`, `Duration`, `Lessons`, `Students`, `State`.
13. Resize the Manage Plans window and list columns, close/reopen it, and confirm size/placement and column widths are remembered.
14. Confirm only Active plans appear in the student assignment list/default assignment flow.
15. Open an existing student and confirm their already assigned plan data is unchanged.
16. Assign an Active catalog plan to a test student if Stage 4 is complete.
17. Write or preview an invoice for a student with an existing assigned plan and confirm pricing comes from the assigned snapshot, not from later plan lifecycle changes.

Expected successful result:

- Manage Plans opens from the Plan menu.
- Draft plans can be edited, deleted, and published.
- Active plans are read-only except `Retire on`, and can only be retired.
- Retired plans are read-only and can only be reactivated.
- Only Active plans can be assigned to students.
- Existing student plan periods and invoice data remain unchanged after plan lifecycle changes.

Expected failure/safety result, if relevant:

- Invalid Draft plan data shows validation and does not save.
- Active plan content edits and Retired plan edit attempts are rejected even if triggered outside the UI.
- Draft and Retired plans are hidden from normal future assignment.
- Catalog persistence failure reports an error without changing student plan data.

Where to inspect artifacts, reports, logs, database state, or UI state:

- Manage Plans window Draft/Active/Retired plan display.
- Student editor current plan display.
- `%APPDATA%\visualtech.dk\MySmallBusinessLedger\ledger.db` for durable catalog data.
- Invoice preview/issued invoice lines for assigned plan snapshot values.

Cleanup/reset instructions:

- Retire Active test catalog plans rather than deleting database rows.
- Delete only throwaway Draft plans.
- Use a disposable test database if repeated catalog experiments become noisy.

## Completion Log

- 2026-06-17:
  - Stage: Plan creation
  - Files changed: `.ai/implementation-plans/active/2026-06-17-plan-catalog-management.md`
  - Summary: Created initial plan for independent Plan catalog management; later revised to the stricter Draft/Active/Retired immutable lifecycle.
  - Tests/verification run: Not run; planning-only change.
  - Results: Plan ready for execution.
  - Deviations: None.
  - Risks/open questions: Existing pilot database schema evolution may require a migration decision before or during Stage 2.
- 2026-06-17:
  - Stage: Plan lifecycle revision
  - Files changed: `.ai/implementation-plans/active/2026-06-17-plan-catalog-management.md`
  - Summary: Revised plan to make Plans immutable after publishing, with Draft, Active, and Retired states. Draft plans can be edited/deleted/published; Active plans can only be retired; Retired plans can only be published; only Active plans can be assigned to students.
  - Tests/verification run: Not run; planning-only change.
  - Results: Plan updated for lifecycle-driven implementation.
  - Deviations: Replaced prior editable catalog plan model with a stricter lifecycle state machine.
  - Risks/open questions: Student assignment UI shape remains a Stage 4 review point.
- 2026-06-17:
  - Stage: Stage 1 Core plan lifecycle contracts
  - Files changed: `src/MySmallBusinessLedger.Core/Plans/PlanCatalogState.cs`, `src/MySmallBusinessLedger.Core/Plans/PlanCatalogItem.cs`, `src/MySmallBusinessLedger.Core/Ports/IPlanCatalogRepository.cs`, `src/MySmallBusinessLedger.Core/UseCases/Plans/*`, `tests/MySmallBusinessLedger.Tests/Core/UseCases/PlanCatalogUseCaseTests.cs`
  - Summary: Added Core plan catalog lifecycle types and use cases for Draft creation/edit/delete, Draft publish, Retired reactivate, Active retire, active-only assignment listing, and immutable Active/Retired state enforcement.
  - Tests/verification run: `dotnet test tests\MySmallBusinessLedger.Tests\MySmallBusinessLedger.Tests.csproj --filter "FullyQualifiedName~PlanCatalog" -nodeReuse:false`
  - Results: Passed 9/9 focused plan catalog tests.
  - Deviations: Added `ListPlansUseCase` alongside `ListAssignablePlansUseCase` for Stage 3 use.
  - Risks/open questions: None for Stage 1; database schema evolution remains a Stage 2 risk.
- 2026-06-17:
  - Stage: Stage 2 SQLite plan lifecycle persistence
  - Files changed: `src/MySmallBusinessLedger.Infrastructure/Persistence/Entities.cs`, `src/MySmallBusinessLedger.Infrastructure/Persistence/LedgerDbContext.cs`, `src/MySmallBusinessLedger.Infrastructure/Persistence/SqlitePlanCatalogRepository.cs`, `src/MySmallBusinessLedger.Infrastructure/Persistence/SqliteSchemaInitializer.cs`, `src/MySmallBusinessLedger.App/App.xaml.cs`, `tests/MySmallBusinessLedger.Tests/Infrastructure/Persistence/SqlitePersistenceTests.cs`
  - Summary: Added PlanCatalog SQLite entity, DbContext mapping, repository, and startup schema initializer that creates the PlanCatalog table for existing `EnsureCreated` pilot databases.
  - Tests/verification run: `dotnet test tests\MySmallBusinessLedger.Tests\MySmallBusinessLedger.Tests.csproj --filter "FullyQualifiedName~SqlitePersistenceTests" -nodeReuse:false`
  - Results: Passed 10/10 focused SQLite persistence tests, including lifecycle round-trip, active-only assignable listing, draft delete, student snapshot immutability, and existing database PlanCatalog table repair.
  - Deviations: Added `SqliteSchemaInitializer` to bridge the current no-migrations pilot database policy.
  - Risks/open questions: Explicit EF migrations may still be desirable before broader distribution, but the current pilot app can open existing databases safely for this new table.
- 2026-06-17:
  - Stage: Stage 3 state-aware Manage Plans window and menu entry
  - Files changed: `src/MySmallBusinessLedger.App/Services/IPlanCatalogService.cs`, `src/MySmallBusinessLedger.App/Services/CorePlanCatalogService.cs`, `src/MySmallBusinessLedger.App/Services/IPlanCatalogLauncher.cs`, `src/MySmallBusinessLedger.App/Services/PlanCatalogWindowLauncher.cs`, `src/MySmallBusinessLedger.App/ViewModels/PlanCatalogDisplayModel.cs`, `src/MySmallBusinessLedger.App/ViewModels/PlanCatalogViewModel.cs`, `src/MySmallBusinessLedger.App/Views/PlanCatalogView.xaml`, `src/MySmallBusinessLedger.App/Views/PlanCatalogView.xaml.cs`, `src/MySmallBusinessLedger.App/MainWindow.xaml`, `src/MySmallBusinessLedger.App/ViewModels/MainWindowViewModel.cs`, `src/MySmallBusinessLedger.App/App.xaml.cs`, `tests/MySmallBusinessLedger.Tests/App/ViewModels/MainWindowViewModelTests.cs`, `tests/MySmallBusinessLedger.Tests/App/ViewModels/PlanCatalogViewModelTests.cs`
  - Summary: Added `Plan > Manage Plans`, a launcher/service boundary, and a state-aware WPF catalog view where Draft plans are editable/deletable/publishable, Active plans are read-only/retire-only, and Retired plans are read-only/publishable.
  - Tests/verification run: `dotnet test tests\MySmallBusinessLedger.Tests\MySmallBusinessLedger.Tests.csproj --filter "FullyQualifiedName~PlanCatalogViewModelTests|FullyQualifiedName~ManagePlansCommand" -nodeReuse:false`
  - Results: Passed 10/10 focused Manage Plans tests.
  - Deviations: Used one cohesive `PlanCatalogViewModel` rather than adding a separate `PlanEditorViewModel`; the state gating is still split into display model fields, command availability, and Core use-case validation.
  - Risks/open questions: Stage 4 still needs the student editor to assign only Active catalog plans.
- 2026-06-17:
  - Stage: Stage 4 catalog-backed student plan assignment
  - Files changed: `src/MySmallBusinessLedger.App/Services/IPlanCatalogService.cs`, `src/MySmallBusinessLedger.App/Services/CorePlanCatalogService.cs`, `src/MySmallBusinessLedger.App/Services/StudentEditorWindowLauncher.cs`, `src/MySmallBusinessLedger.App/ViewModels/StudentEditorViewModel.cs`, `src/MySmallBusinessLedger.App/Views/StudentEditorView.xaml`, `src/MySmallBusinessLedger.App/App.xaml.cs`, `tests/MySmallBusinessLedger.Tests/App/ViewModels/EditorViewModelTests.cs`, `tests/MySmallBusinessLedger.Tests/App/ViewModels/PlanCatalogViewModelTests.cs`
  - Summary: Replaced the student editor's Add Plan placeholder with an Active-plan selector. Selecting an Active catalog plan copies its fields into the saved student plan snapshot, while opening an existing student does not silently rewrite existing copied plan data from the catalog.
  - Tests/verification run: `dotnet test tests\MySmallBusinessLedger.Tests\MySmallBusinessLedger.Tests.csproj --filter "FullyQualifiedName~EditorViewModelTests|FullyQualifiedName~PlanCatalogViewModelTests|FullyQualifiedName~ManagePlansCommand" -nodeReuse:false`; `dotnet test tests\MySmallBusinessLedger.Tests\MySmallBusinessLedger.Tests.csproj --filter "FullyQualifiedName~EditorViewModelTests|FullyQualifiedName~StudentUseCaseTests|FullyQualifiedName~SqlitePersistenceTests" -nodeReuse:false`
  - Results: Passed 21/21 focused editor/plan UI tests and 23/23 Stage 4 editor/student/persistence tests.
  - Deviations: Implemented assignment inline in the student editor rather than using a separate dialog because the current one-period editor can be served cleanly by one Active-plan selector.
  - Risks/open questions: Full multi-period plan history editing remains outside this stage.
- 2026-06-17:
  - Stage: Stage 5 documentation, review, and final verification
  - Files changed: `README.md`, `.ai/project-profile.md`, `.ai/context/HANDOFF.md`, `docs/business-requirements-and-logic.md`, `.ai/implementation-plans/active/2026-06-17-plan-catalog-management.md`, plus Stage 1-4 source and test files.
  - Summary: Refreshed durable docs/context for Plan catalog lifecycle, Active-only assignment, and copied student plan snapshots. Performed a focused code/UI review, found one new-student fallback issue, and fixed it so new students require an Active catalog plan before save.
  - Tests/verification run: `rg -n "Add Plan.*placeholder|plan administration will be implemented|plan administration,|delete plan" README.md .ai docs src tests`; `dotnet test tests\MySmallBusinessLedger.Tests\MySmallBusinessLedger.Tests.csproj --filter "FullyQualifiedName~EditorViewModelTests" -nodeReuse:false`; `dotnet build MySmallBusinessLedger.sln -nodeReuse:false`; `dotnet test MySmallBusinessLedger.sln -nodeReuse:false`
  - Results: Stale live-source/docs references were clean; historical implementation-plan references remained as historical context at the time. Editor tests passed 12/12. Full build passed with 0 warnings and 0 errors. Full test suite passed 148/148.
  - Deviations: Plan remains under `active/` until Ruben completes the manual UI test instructions.
  - Risks/open questions: Manual WPF layout/keyboard review and final user test are still recommended; explicit EF migrations remain a possible future improvement before broader distribution.
- 2026-06-17:
  - Stage: Stage 6 Retire language, Retire on scheduling, and Manage Plans polish
  - Files changed: `src/MySmallBusinessLedger.Core/Plans/PlanCatalogState.cs`, `src/MySmallBusinessLedger.Core/Plans/PlanCatalogItem.cs`, `src/MySmallBusinessLedger.Core/UseCases/Plans/*`, `src/MySmallBusinessLedger.Infrastructure/Persistence/Entities.cs`, `src/MySmallBusinessLedger.Infrastructure/Persistence/SqlitePlanCatalogRepository.cs`, `src/MySmallBusinessLedger.Infrastructure/Persistence/SqliteSchemaInitializer.cs`, `src/MySmallBusinessLedger.App/App.xaml.cs`, `src/MySmallBusinessLedger.App/Services/*PlanCatalog*`, `src/MySmallBusinessLedger.App/Services/DataGridColumnWidthStore.cs`, `src/MySmallBusinessLedger.App/ViewModels/PlanCatalog*`, `src/MySmallBusinessLedger.App/Views/PlanCatalogView.*`, `tests/MySmallBusinessLedger.Tests/**/*PlanCatalog*`, `tests/MySmallBusinessLedger.Tests/Infrastructure/Persistence/SqlitePersistenceTests.cs`, `tests/MySmallBusinessLedger.Tests/App/ViewModels/EditorViewModelTests.cs`, `README.md`, `.ai/project-profile.md`, `.ai/context/HANDOFF.md`, `docs/business-requirements-and-logic.md`
  - Summary: Standardized plan lifecycle user and domain language on Retire/Retired and Reactivate. Added nullable `RetireOn`, active-only due retirement at app startup with a visible warning if persistence rejects the update, active-plan retire-date updates, Reactivate clearing `RetireOn`, Retired assignment exclusion, Manage Plans column order, polished action labels, remembered Manage Plans column widths, and compatibility handling for existing databases that still store the legacy state value.
  - Tests/verification run: terminology scan for legacy plan API and UI wording; `dotnet build MySmallBusinessLedger.sln -nodeReuse:false`; `dotnet test tests\MySmallBusinessLedger.Tests\MySmallBusinessLedger.Tests.csproj --filter "FullyQualifiedName~PlanCatalog" -nodeReuse:false`; `dotnet test MySmallBusinessLedger.sln -nodeReuse:false`
  - Results: Search returns only intentional legacy database compatibility literals/test names. Build passed with 0 warnings and 0 errors. Focused PlanCatalog tests passed 26/26. Full test suite passed 153/153.
  - Deviations: Kept the legacy pre-rename state literal only inside schema compatibility code/tests so pilot databases created before this rename can still open.
  - Risks/open questions: Manual WPF check still recommended for the exact Manage Plans layout, remembered column widths, and startup due-retirement behavior against Ruben's real database.
