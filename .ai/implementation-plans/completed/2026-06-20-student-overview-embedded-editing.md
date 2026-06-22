# Implementation Plan: Student Overview Embedded Editing

Status: Completed
Created: 2026-06-20
Last updated: 2026-06-23
Owner/context: Ruben and Codex; Ruben approved the recommended approach to make the Manage Students detail pane editable with explicit Save/Cancel, reusing the existing Student editor behavior in-place instead of opening a separate dialog.

## Related Docs, Standards, And Skills

- `README.md`
- `AGENTS.md`
- `.ai/project-profile.md`
- `docs/business-requirements-and-logic.md`
- `.ai/context/HANDOFF.md`
- `.ai/standards/wpf-mvvm-standards.md`
- `.ai/standards/testing-and-verification.md`
- `.agents/skills/implementation-plan`
- `superpowers:test-driven-development`
- `.agents/skills/documentation-refresh`

## Goal

Embed the existing student editing workflow into `Student > Manage Students` so the selected student's fields, active-plan assignment, plan start/end dates, active flag, and schedule can be edited directly in the overview window with explicit Save/Cancel.

## Non-Goals

- Do not add free-form editing of catalog plan fields in Manage Students.
- Do not change catalog plan lifecycle behavior.
- Do not change invoice issuing, invoice viewer behavior, invoice payment status behavior, or Google Calendar sync.
- Do not mutate issued invoice snapshots.
- Do not introduce new production dependencies.

## Assumptions

- The embedded editor should reuse `StudentEditorViewModel` and `IStudentEditorService` behavior where practical.
- Save remains explicit; field edits are not auto-saved.
- Cancel should discard unsaved field changes and restore the selected student's latest persisted snapshot.
- Selecting a different student while there are unsaved changes may discard the current in-pane draft without a prompt for this first pass unless implementation complexity reveals an easy, existing prompt pattern.
- Active catalog plan assignment must keep copying the selected Active plan into the student's plan period, preserving existing immutable snapshot behavior.
- Add Student can create a new editable draft in the same pane, then refresh/select the saved student after Save.

## Current Problem / Current State

- Before this plan, Manage Students showed read-only selected-student fields and delegated Add/Edit to `StudentEditorWindowLauncher`.
- `StudentEditorViewModel` already handles two-line address, active plan selection, copied plan fields, schedule rows, active flag, validation error display, and SaveCommand.
- `StudentEditorViewModel.SaveAsync` formerly requested host close on successful save, which was dialog-oriented behavior.
- `StudentOverviewViewModel` owns selected-student display strings and separate Activate/Deactivate commands.

## Desired End State

- Manage Students shows editable student fields directly in the middle/detail pane.
- The current plan pane becomes the existing Student editor's plan assignment/detail surface.
- The schedule editor appears in the Manage Students detail area.
- Save persists through the existing student editor service and refreshes the student list/detail/invoices while preserving selection.
- Cancel discards unsaved editor changes.
- Add Student starts a new in-pane editor draft.
- Deactivate/Activate may remain as direct buttons or be represented by the embedded Active checkbox, but the persisted change must still go through explicit Save unless using the existing dedicated action.

## Behavior-Preserving Boundary

- The reusable Student editor ViewModel/service behavior remains the editing source of truth; the old standalone Add/Edit Student dialog was deliberately removed after embedding.
- Existing dashboard active-student filtering remains unchanged.
- Existing invoice viewer and invoice payment behavior remain unchanged.
- Existing copied student plan snapshots remain the source of truth for student plan details and invoices.
- Draft and Retired catalog plans remain unassignable.

## Intentional Behavior Changes

- Manage Students no longer requires opening a separate Edit Student dialog for routine student edits.
- Manage Students exposes plan assignment and schedule editing directly.
- Selecting a student in Manage Students immediately refreshes the main window around that student; closing Manage Students applies the final selected student again.
- Save/Cancel buttons in the overview control embedded student edits.

## Affected Files And Surfaces

- `src/MySmallBusinessLedger.App/ViewModels/StudentEditorViewModel.cs`
- `src/MySmallBusinessLedger.App/ViewModels/StudentOverviewViewModel.cs`
- `src/MySmallBusinessLedger.App/Views/StudentOverviewView.xaml`
- `src/MySmallBusinessLedger.App/Services/StudentOverviewWindowLauncher.cs`
- `src/MySmallBusinessLedger.App/App.xaml.cs`
- `tests/MySmallBusinessLedger.Tests/App/ViewModels/StudentOverviewViewModelTests.cs`
- `tests/MySmallBusinessLedger.Tests/App/ViewModels/EditorViewModelTests.cs`
- Documentation/context files after implementation.

## Risks And Mitigations

- Risk: Embedded editing duplicates dialog behavior and diverges.
  - Mitigation: Reuse `StudentEditorViewModel` rather than copy field properties into the overview ViewModel.
- Risk: Successful embedded save closes the Manage Students window.
  - Mitigation: Add a host mode or save completion option to `StudentEditorViewModel` so embedded save can refresh in place.
- Risk: Selecting a catalog plan accidentally rewrites historical plan snapshots.
  - Mitigation: Keep using `StudentEditorViewModel`/`SaveStudentUseCase`, which creates the current request from the active selected plan and preserved copied plan fields.
- Risk: The UI becomes too tall.
  - Mitigation: Put editable details and schedule in a scrollable pane and keep invoices in the right pane.
- Risk: Unsaved changes get lost unexpectedly.
  - Mitigation: Use explicit Save/Cancel and keep selection-change behavior simple and documented for this pass; add prompts later if needed.

## Staged Implementation Plan

### Stage 1: ViewModel Contract And Red Tests

Type: test / design
Status: Complete

Steps:

- Add failing tests proving Manage Students exposes an embedded `StudentEditorViewModel` for the selected student.
- Add failing tests proving Save refreshes the overview and preserves/selects the saved student.
- Add failing tests proving Cancel discards unsaved edits.
- Add failing tests proving Add Student creates an in-pane editable draft and saves through the existing editor service.
- Preserve Student editor ViewModel save/validation behavior while allowing the embedded host to stay open on save.

Verification:

```powershell
dotnet test tests\MySmallBusinessLedger.Tests\MySmallBusinessLedger.Tests.csproj --filter "FullyQualifiedName~StudentOverview|FullyQualifiedName~StudentEditor" -nodeReuse:false
```

### Stage 2: Embedded Editor Implementation

Type: implementation / UI
Status: Complete

Steps:

- Let `StudentEditorViewModel` support embedded-host save behavior without closing the host.
- Inject `IStudentEditorService` and `IPlanCatalogService` into `StudentOverviewViewModel` or a small overview editor factory.
- Replace read-only selected-student detail strings in the overview with an embedded `StudentEditorViewModel`.
- Update Add/Save/Cancel/Edit command behavior around the embedded editor.
- Keep invoice list and invoice double-click behavior unchanged.
- Update `StudentOverviewView.xaml` to bind to the embedded editor fields, active plan selector/details, Save/Cancel, and schedule editor.

Verification:

```powershell
dotnet test tests\MySmallBusinessLedger.Tests\MySmallBusinessLedger.Tests.csproj --filter "FullyQualifiedName~StudentOverview|FullyQualifiedName~StudentEditor" -nodeReuse:false
dotnet build MySmallBusinessLedger.sln -nodeReuse:false
```

### Stage 3: Documentation And Full Verification

Type: documentation / verification
Status: Complete

Steps:

- Update README, business requirements, project profile, handoff, and this plan completion log.
- Run focused tests, full build, and full solution tests.

Verification:

```powershell
dotnet test MySmallBusinessLedger.sln -nodeReuse:false
```

## Decision Points / Open Questions

- No open decision points. Ruben chose embedded editor approach 1 with explicit Save functionality.

## Final User Test Instructions

Post-completion user test: mandatory.

1. Build and run the app.
2. Open `Student > Manage Students`.
3. Select an existing student and edit a harmless field, then press Cancel; confirm the persisted value is unchanged.
4. Edit the field again and press Save; confirm the list and detail pane refresh with the saved value.
5. Select a different Active plan, set Starts on/Ends on as needed, press Save, and confirm the current plan pane reflects the copied plan snapshot.
6. Add a temporary test student from Manage Students, fill all required fields, select an Active plan, press Save, and confirm the new student appears in the list.
7. Double-click an invoice for a student and confirm the existing invoice viewer still opens.
8. Confirm the `Student` menu contains only `Manage Students`, and that the Manage Students `Close` button is compact and right-aligned.

Expected result: Manage Students becomes the main student workbench with explicit in-pane editing while preserving copied plan snapshots, invoices, and existing editor validation.

## Completion Log

- 2026-06-20:
  - Stage: Plan creation
  - Files changed: `.ai/implementation-plans/active/2026-06-20-student-overview-embedded-editing.md`
  - Summary: Created the implementation plan for embedded student editing in Manage Students.
  - Tests/verification run: Not run; planning-only change.
  - Results: Ready for TDD implementation.
- 2026-06-20:
  - Stage: Stages 1-3 implementation
  - Files changed: Student editor ViewModel, student overview ViewModel, student overview XAML, overview launcher/composition, focused ViewModel tests, README, business requirements, project profile, handoff, and this plan.
  - Summary: Embedded explicit Save/Cancel student editing into Manage Students, including Active flag, active-plan assignment, copied plan details, plan dates, schedule rows, Add Student drafts, existing invoice viewer double-click, and embedded-host save behavior.
  - Tests/verification run: `dotnet test tests\MySmallBusinessLedger.Tests\MySmallBusinessLedger.Tests.csproj --filter "FullyQualifiedName~StudentOverview|FullyQualifiedName~StudentEditor" -nodeReuse:false`; `dotnet build MySmallBusinessLedger.sln -nodeReuse:false`; `dotnet test MySmallBusinessLedger.sln -nodeReuse:false`.
  - Results: Focused tests passed 19/19; full build passed with 0 warnings and 0 errors; full solution tests passed 171/171.
- 2026-06-20:
  - Stage: Student menu/dialog cleanup
  - Files changed: Main window menu/ViewModel/composition, old standalone Student editor window/launcher files, overview Close-button layout, focused tests, business requirements, handoff, and this plan.
  - Summary: Removed the old standalone Student Add/Edit menu entries, launcher, and window after Manage Students became the student editing surface. The reusable Student editor ViewModel/service behavior remains embedded in Manage Students.
  - Tests/verification run: `dotnet test tests\MySmallBusinessLedger.Tests\MySmallBusinessLedger.Tests.csproj --filter "FullyQualifiedName~MainWindowViewModel|FullyQualifiedName~StudentOverview|FullyQualifiedName~StudentEditor" -nodeReuse:false`; `dotnet test MySmallBusinessLedger.sln -nodeReuse:false`.
  - Results: Focused tests passed 40/40; full solution tests passed 170/170.
- 2026-06-20:
  - Stage: Manage Students selection handoff
  - Files changed: Main window ViewModel, student overview launcher contract/implementation, focused main-window test, README, business requirements, project profile, handoff, and this plan.
  - Summary: Manage Students now opens with the main window's current student selected where possible, publishes selection changes to the main window immediately, returns the selected student on close, and the main window refreshes around that student.
  - Tests/verification run: `dotnet test tests\MySmallBusinessLedger.Tests\MySmallBusinessLedger.Tests.csproj --filter "FullyQualifiedName~MainWindowViewModelTests.ManageStudentsCommandRefreshesDashboardWithSelectedOverviewStudent" -nodeReuse:false`; `dotnet test tests\MySmallBusinessLedger.Tests\MySmallBusinessLedger.Tests.csproj --filter "FullyQualifiedName~MainWindowViewModel|FullyQualifiedName~StudentOverview" -nodeReuse:false`; `dotnet test MySmallBusinessLedger.sln -nodeReuse:false`.
  - Results: New focused test passed 1/1; affected focused tests passed 31/31; full solution tests passed 170/170.
- 2026-06-20:
  - Stage: Live Manage Students selection sync
  - Files changed: Student overview ViewModel event, overview launcher callback contract/implementation, main window ViewModel, focused main-window test, README, business requirements, project profile, handoff, and this plan.
  - Summary: Manage Students now publishes selected-student changes while the window remains open, so the main window updates as soon as the user selects another student.
  - Tests/verification run: `dotnet test tests\MySmallBusinessLedger.Tests\MySmallBusinessLedger.Tests.csproj --filter "FullyQualifiedName~MainWindowViewModelTests.ManageStudentsSelectionImmediatelyRefreshesDashboard" -nodeReuse:false`; `dotnet test tests\MySmallBusinessLedger.Tests\MySmallBusinessLedger.Tests.csproj --filter "FullyQualifiedName~MainWindowViewModel|FullyQualifiedName~StudentOverview" -nodeReuse:false`.
  - Results: New focused test passed 1/1; affected focused tests passed 32/32.
- 2026-06-23:
  - Stage: Completion housekeeping
  - Files changed: `.ai/implementation-plans/completed/2026-06-20-student-overview-embedded-editing.md`, `MySmallBusinessLedger.sln`
  - Summary: Rechecked the embedded Manage Students implementation, including in-pane editing, explicit Save/Cancel, Add Student drafts, old menu/dialog cleanup, immediate main-window selection sync, invoice viewer double-click preservation, and later unsaved-change prompts. Moved the plan to completed history.
  - Tests/verification run: solution build after moving the plan and updating solution items.
  - Results: Completed.
  - Deviations: None.
  - Risks/open questions: None for the implementation plan.
