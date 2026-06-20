# Implementation Plan: Student Overview Management

Status: Implemented; awaiting final user test
Created: 2026-06-20
Last updated: 2026-06-20
Owner/context: Ruben and Codex; Ruben wants a comprehensive student management view similar in spirit to Manage Plans, with panes for students, selected student details, current plan, and invoices.

## Related Docs And Standards

- `README.md`
- `AGENTS.md`
- `.ai/project-profile.md`
- `docs/business-requirements-and-logic.md`
- `.ai/standards/architecture-principles.md`
- `.ai/standards/coding-standards.md`
- `.ai/standards/database-standards.md`
- `.ai/standards/testing-and-verification.md`
- `.ai/standards/wpf-mvvm-standards.md`

## Goal

Add `Student > Manage Students`, opening a WPF student overview window with:

- a left list of all students;
- selected student details;
- selected student's current plan snapshot;
- selected student's invoices, with double-click opening the existing invoice viewer;
- actions to add/edit students in the overview and deactivate/reactivate the selected student.

## Non-Goals

- Do not replace the existing Student editor ViewModel/service validation behavior.
- Do not redesign plan history editing.
- Do not change invoice issuing, payment toggling, PDF generation, or Google Calendar sync.
- Do not mutate issued invoice snapshots.
- Do not introduce new production dependencies.

## Assumptions

- The overview should list active and inactive students.
- The existing Student editor ViewModel/service remains the source for student editing behavior.
- Deactivate/reactivate changes only `Student.IsActive`; it preserves plan periods, schedule, and invoices.
- The current plan is the plan period active on the app clock date: `StartsOn <= today` and `EndsOn` is null or on/after today.
- Invoice double-click should use the existing `IInvoiceWorkflowLauncher.ShowInvoiceViewerAsync` path.

## Current Problem / Current State

- Main menu previously had `Student > Add Student` and `Student > Edit Student`, but no comprehensive student management window.
- `IStudentRepository` lists only active students for dashboard workflows.
- The main window already displays invoices for the current dashboard student and can open an invoice viewer.
- The Student editor behavior can add/edit student data and assign active catalog plans, but did not show a broad student list.

## Desired End State

- `Student > Manage Students` opens a student overview window.
- Selecting a student refreshes the details, current plan, and invoices.
- Add/Edit actions use the embedded Manage Students editor.
- Deactivate/Reactivate actions update only the selected student's active flag and refresh the list while preserving selection where possible.
- The overview window remembers size/position and relevant grid column widths.
- Tests prove repository listing, view model selection/detail/invoice loading, activation toggles, invoice open command, menu wiring, and basic WPF surface construction.

## Behavior-Preserving Boundary

- Existing dashboard student navigation continues to list active students only.
- Existing student editing validation and save behavior remain unchanged.
- Existing invoice viewer behavior remains unchanged.
- Existing invoice payment status behavior remains unchanged.
- Existing copied student plan snapshots remain the source of truth for student plan details and invoices.

## Intentional Behavior Changes

- Add `Student > Manage Students`.
- Add all-students listing support in the student repository.
- Add a student overview service/launcher/viewmodel/view.
- Allow deactivation/reactivation from the student overview.

## Affected Files And Surfaces

- `src/MySmallBusinessLedger.Core/Ports/IStudentRepository.cs`
- `src/MySmallBusinessLedger.Infrastructure/Persistence/SqliteStudentRepository.cs`
- New App service interfaces/classes for student overview and launcher.
- New App display models and ViewModel.
- New WPF view/code-behind.
- `src/MySmallBusinessLedger.App/MainWindow.xaml`
- `src/MySmallBusinessLedger.App/ViewModels/MainWindowViewModel.cs`
- `src/MySmallBusinessLedger.App/App.xaml.cs`
- Tests under `tests/MySmallBusinessLedger.Tests/App/ViewModels/` and `tests/MySmallBusinessLedger.Tests/Infrastructure/Persistence/`.
- Docs/context updates.

## Risks And Mitigations

- Risk: Deactivate/reactivate accidentally rewrites plan or schedule data.
  - Mitigation: Service updates via existing `StudentRecord` snapshot with only `IsActive` changed; add tests.
- Risk: Overview becomes a second editing surface with divergent validation.
  - Mitigation: Reuse the Student editor ViewModel/service behavior in the overview.
- Risk: Large student/invoice lists become slow.
  - Mitigation: Use no-tracking repository reads and basic deterministic ordering; revisit paging only if real data grows.
- Risk: Current plan logic is ambiguous.
  - Mitigation: Document and test the clock-date rule.

## Staged Implementation Plan

### Stage 1: Repository And Service Contracts

Type: implementation / test / database
Status: Complete

Steps:

- Add failing persistence test for `ListStudentsAsync` returning active and inactive students.
- Add failing service/ViewModel tests for loading students and invoices.
- Add repository method to list all students with plans/schedule.
- Add student overview service that composes student and invoice repositories.

Verification:

```powershell
dotnet test tests\MySmallBusinessLedger.Tests\MySmallBusinessLedger.Tests.csproj --filter "FullyQualifiedName~SqlitePersistenceTests|FullyQualifiedName~StudentOverview" -nodeReuse:false
```

### Stage 2: ViewModel, Launcher, And Window

Type: implementation / test / UI
Status: Complete

Steps:

- Add ViewModel tests for selection, current plan display, invoice list, open invoice command, add/edit refresh, and deactivate/reactivate.
- Add launcher and WPF window.
- Add basic WPF construction test and grid column checks.
- Attach window placement and grid column width storage.

Verification:

```powershell
dotnet test tests\MySmallBusinessLedger.Tests\MySmallBusinessLedger.Tests.csproj --filter "FullyQualifiedName~StudentOverview" -nodeReuse:false
```

### Stage 3: Main Menu Wiring, Docs, And Full Verification

Type: implementation / documentation / verification
Status: Complete

Steps:

- Add `Student > Manage Students`.
- Wire composition root.
- Update README, project profile, business requirements, handoff, and this plan completion log.
- Run focused tests, full build, and full tests.

Verification:

```powershell
dotnet build MySmallBusinessLedger.sln -nodeReuse:false
dotnet test MySmallBusinessLedger.sln -nodeReuse:false
```

## Final User Test Instructions

Post-completion user test: mandatory.

1. Build and run the app.
2. Open `Student > Manage Students`.
3. Confirm all students appear, including inactive students.
4. Select a student and confirm details, current plan, and invoices match the existing Student editor/main-window information.
5. Add a test student from the overview and confirm it appears after save.
6. Edit a student from the overview and confirm details refresh after save.
7. Deactivate an active test student and confirm the list/detail state changes without losing plans/invoices.
8. Reactivate that student and confirm the state changes back.
9. Double-click one invoice and confirm the existing invoice viewer opens.
10. Resize the window and grid columns, close/reopen, and confirm placement/column widths are remembered.

Expected result: Student overview supports list/detail/current-plan/invoices and safe activation state changes without mutating invoices or plan snapshots.

## Completion Log

- 2026-06-20:
  - Stage: Plan creation
  - Files changed: `.ai/implementation-plans/active/2026-06-20-student-overview-management.md`
  - Summary: Created the implementation plan for the student overview management window.
  - Tests/verification run: Not run; planning-only change.
  - Results: Ready for implementation.
- 2026-06-20:
  - Stage: Stages 1-3 implementation
  - Files changed: Core student repository port, SQLite student repository, App student overview service/launcher/viewmodel/view, main-window menu/composition, focused tests, README, business requirements, project profile, handoff, and this plan.
  - Summary: Added `Student > Manage Students` with all-student listing, selected student details, current assigned plan snapshot, invoice list with existing viewer double-click, add/edit through the existing Student editor, safe activate/deactivate, and remembered window/grid state.
  - Tests/verification run: `dotnet test tests\MySmallBusinessLedger.Tests\MySmallBusinessLedger.Tests.csproj --filter "FullyQualifiedName~StudentOverview|FullyQualifiedName~StudentsCanBeListedIncludingInactiveStudents|FullyQualifiedName~ManageStudentsCommand" -nodeReuse:false`; `dotnet build MySmallBusinessLedger.sln -nodeReuse:false`; `dotnet test MySmallBusinessLedger.sln -nodeReuse:false`.
  - Results: Focused tests passed 7/7; full build passed with 0 warnings and 0 errors; full solution tests passed 168/168.
