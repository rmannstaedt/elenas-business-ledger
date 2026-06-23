# Implementation Plan: Populate Calendar From Student Schedule

Status: Active
Created: 2026-06-23
Last updated: 2026-06-23
Owner/context: Ruben / My Small Business Ledger WPF pilot

## Related Docs And Standards

- `AGENTS.md`
- `README.md`
- `.ai/project-profile.md`
- `docs/business-requirements-and-logic.md`
- `.ai/standards/architecture-principles.md`
- `.ai/standards/coding-standards.md`
- `.ai/standards/testing-and-verification.md`
- `.ai/standards/security-and-boundaries.md`
- `.ai/standards/ui-design-standards.md`
- `.ai/standards/wpf-mvvm-standards.md`

## Related Skills

- `implementation-plan`
- `documentation-refresh`
- `code-review`
- `ui-review`
- `plan-runner` for staged execution if this work is continued later

## Goal

Add a main-window `Populate Calendar` action that creates Google Calendar lesson entries for the selected student from that student's saved standard weekday/time schedule.

## Non-Goals

- No conflict detection beyond exact duplicate prevention for the same student/date/time.
- No local Lessons table.
- No recurring Google Calendar series; create normal lesson events that match existing lesson behavior.
- No automatic completion state changes.
- No seeding of past months.

## Assumptions

- The selected student is active and has saved schedule entries in SQLite.
- Google Calendar remains the source of truth for future lessons.
- Existing single-lesson add infrastructure creates correctly marked calendar events.
- The current date is supplied by the UI clock so tests can be deterministic.
- Approved duplicate behavior: skip exact same-student same-date/time lessons; ignore all other calendar conflicts in v1.

## Current Problem / Current State

The main window can add a single blank lesson row and save it to Google Calendar, but it cannot seed a month from a student's stored schedule. Empty months require repetitive manual lesson creation.

## Desired End State

The selected student's toolbar includes `Populate Calendar`. For the current month it seeds scheduled occurrences from today inclusive through month end; for a future month it seeds the full month; for a past month it is unavailable and refuses to run. The command writes immediately to Google Calendar, skips exact duplicates, refreshes the dashboard afterward, and reports Google Calendar failures immediately.

## Behavior-Preserving Boundary

The following must remain unchanged unless this plan explicitly says otherwise:

- Existing manual `Add Lesson`, edit, delete, completion toggle, invoice writing, invoice viewing, and payment toggles keep their behavior.
- Google Calendar entries are still identified by `$ <student short name>` or `$<student short name>` on the first summary line.
- Student schedules remain edited and saved through Manage Students.
- Google Calendar remains the durable source for future lessons; local SQLite does not receive a Lessons table.
- Calendar update failures remain user-visible immediately and must not pretend local state changed.
- Invoice and payment behavior must not be changed by calendar population.

## Intentional Behavior Changes

- Add `Populate Calendar` to the main lesson toolbar.
- Add immediate Google Calendar write workflow for schedule-based monthly population.
- Skip exact duplicate lessons for the selected student at the same date/time.
- Document v1 limitation that other conflicts are not detected or handled.

## Affected Files, Projects, Commands, Docs, Schemas, Or Artifacts

- `docs/business-requirements-and-logic.md`
- `README.md` and `.ai/project-profile.md` if public/setup/current behavior needs a concise mention
- `src/MySmallBusinessLedger.Core/UseCases/Lessons/*`
- `src/MySmallBusinessLedger.Core/Ports/IGoogleCalendarGateway.cs`
- `src/MySmallBusinessLedger.App/Services/ILessonEditorService.cs`
- `src/MySmallBusinessLedger.App/Services/CoreLessonEditorService.cs`
- `src/MySmallBusinessLedger.App/ViewModels/MainWindowViewModel.cs`
- `src/MySmallBusinessLedger.App/MainWindow.xaml`
- `src/MySmallBusinessLedger.Infrastructure/Calendar/GoogleCalendarGateway.cs`
- `src/MySmallBusinessLedger.Infrastructure/Calendar/IGoogleCalendarEventClient.cs` only if a batch API is needed
- Relevant Core, App ViewModel, Infrastructure Calendar, and docs tests

## Risks And Mitigations

- Risk: accidental duplicate event creation on repeated clicks. Mitigation: exact duplicate detection against existing Google Calendar lessons for the selected student and generated starts.
- Risk: partial Google Calendar writes if one create succeeds and a later create fails. Mitigation: report failure immediately and refresh from Google Calendar; v1 does not roll back already-created external calendar events.
- Risk: misleading conflict behavior. Mitigation: document and surface that v1 skips exact duplicates only and does not detect other conflicts.
- Risk: ViewModel grows workflow policy. Mitigation: keep schedule/date generation and duplicate selection in a Core use case/service; ViewModel triggers and displays results.

## Open Questions

- [x] Duplicate behavior:
  - Recommended/default answer: skip exact same-student same-date/time lessons.
  - Alternatives: create all occurrences; refuse non-empty months.
  - Why it matters: controls accidental duplicate risk.
  - Exact answer: user approved exact duplicate skipping on 2026-06-23.

## Staged Implementation Plan

### Stage 1: Documentation And Core Population Contract

Type: documentation/test/implementation
Status: Complete

Goal:

Document the business rules, add a Core use case contract, and test date-window generation plus duplicate skipping.

Affected files/classes/schemas/commands/UI surfaces:

- `docs/business-requirements-and-logic.md`
- Core lesson population use case/result/request files
- Core use case tests

Concrete steps:

1. Update business rules documentation before source implementation.
2. Add a schedule population request/result model.
3. Add a Core use case that loads the student record, validates schedule/month rules, lists existing calendar lessons in the target window, generates candidate occurrences, skips exact duplicates, and requests creation of missing lessons.
4. Return a result with created count, skipped duplicate count, and refreshed lessons.

Behavior that must remain unchanged:

- Existing single lesson add use case behavior.
- Student schedule storage and edit behavior.

Tests to add/update:

- Current month starts at today inclusive.
- Future month seeds full month.
- Past month fails without writing.
- No selected/missing student or no schedule fails clearly.
- Existing exact duplicates are skipped.

Verification commands/manual checks:

```powershell
dotnet test MySmallBusinessLedger.sln --filter PopulateCalendar -nodeReuse:false
```

Risks:

- Date/time offset conversion around DST must use the existing lesson time-zone pattern.

Rollback point:

- Remove new Core use case/files and doc paragraph.

Stop/review criteria:

- Stop if schedule data cannot be loaded without changing dashboard ownership or persistence shape.

Documentation impact:

- Required before implementation.

Follow-on actions:

- Wire App service and UI after Core tests pass.

Runner may continue automatically after this stage: yes.

### Stage 2: Calendar Gateway And App Service Wiring

Type: implementation/test
Status: Complete

Goal:

Expose schedule population through existing lesson editor service and Google Calendar gateway.

Affected files/classes/schemas/commands/UI surfaces:

- `IGoogleCalendarGateway`
- `GoogleCalendarGateway`
- `ConfiguredGoogleCalendarGateway`
- `ILessonEditorService`
- `CoreLessonEditorService`
- App service tests and calendar infrastructure tests

Concrete steps:

1. Add a gateway method for creating multiple lesson events or call existing add behavior safely from the use case.
2. Keep event summaries and duration consistent with single lesson creation.
3. Ensure successful writes refresh the relevant month.
4. Ensure failures are reported as Google Calendar update failures without false local success.

Behavior that must remain unchanged:

- Existing `AddLessonAsync`, edit, delete, and state-update contracts.

Tests to add/update:

- Calendar gateway creates marked events for population requests.
- Failure on create returns update failure.
- Refresh failure remains distinct after successful write.

Verification commands/manual checks:

```powershell
dotnet test MySmallBusinessLedger.sln --filter "PopulateCalendar|GoogleCalendarGateway|AppService" -nodeReuse:false
```

Risks:

- Partial external writes are possible if Google Calendar fails mid-run. This must be visible.

Rollback point:

- Remove new gateway/service methods while keeping Core generation tests if desired.

Stop/review criteria:

- Stop if batch creation requires a new Google dependency or API scope.

Documentation impact:

- Note partial-write limitation if implementation exposes it.

Follow-on actions:

- Wire WPF command after service tests pass.

Runner may continue automatically after this stage: yes.

### Stage 3: Main Window UI Command

Type: implementation/test
Status: Complete

Goal:

Add `Populate Calendar` to the main window and wire it to the population service.

Affected files/classes/schemas/commands/UI surfaces:

- `MainWindowViewModel`
- `MainWindow.xaml`
- Main window ViewModel tests

Concrete steps:

1. Add `PopulateCalendarCommand`.
2. Enable for selected student and current/future month; refuse past month defensively.
3. Execute the service, show immediate errors, and refresh dashboard on success.
4. Keep layout compact and consistent with existing toolbar.

Behavior that must remain unchanged:

- Existing lesson list selection/edit/delete and invoice pane behavior.

Tests to add/update:

- Command availability for current/future/past month.
- Successful population calls service and refreshes.
- Failure leaves current rows and shows error.

Verification commands/manual checks:

```powershell
dotnet test MySmallBusinessLedger.sln --filter MainWindowViewModel -nodeReuse:false
```

Risks:

- Button layout may become crowded at minimum width.

Rollback point:

- Remove command/button and App service method call.

Stop/review criteria:

- Stop if UI wording suggests conflict detection beyond exact duplicate skipping.

Documentation impact:

- Required if wording changes from documented behavior.

Follow-on actions:

- Run focused UI review.

Runner may continue automatically after this stage: yes.

### Stage 4: Verification, Review, And Context Refresh

Type: verification/review/documentation
Status: Implemented; awaiting user manual Google Calendar population smoke test

Goal:

Verify the feature end to end, refresh context, and review the Google Calendar write path.

Affected files/classes/schemas/commands/UI surfaces:

- Full solution build/test
- `.ai/context/HANDOFF.md`
- active plan completion log

Concrete steps:

1. Run focused tests and full build/test.
2. Run focused code review for Core/App/Infrastructure calendar population.
3. Run focused UI review for button availability, layout, and failure messaging.
4. Update `.ai/context/HANDOFF.md` and this plan.

Behavior that must remain unchanged:

- Invoice issue/print changes from the active invoice plan remain intact.

Tests to add/update:

- Any gaps identified in review.

Verification commands/manual checks:

```powershell
dotnet build MySmallBusinessLedger.sln -nodeReuse:false
dotnet test MySmallBusinessLedger.sln -nodeReuse:false
```

Risks:

- Manual Google Calendar verification needs a safe test calendar/month.

Rollback point:

- Keep work uncommitted until manual smoke test is accepted.

Stop/review criteria:

- Stop if automated tests show possible duplicate creation or false success after Google Calendar failure.

Documentation impact:

- Required.

Follow-on actions:

- Move plan to completed only after user manual smoke test passes.

Runner may continue automatically after this stage: no. Blocking action required: user manual Google Calendar population smoke test.

## Verification Strategy

- Core unit tests for date-window generation, missing/no-schedule validation, past-month refusal, duplicate skipping, and successful create counts.
- Infrastructure Calendar tests for event creation and refresh/failure behavior.
- MainWindow ViewModel tests for command availability, refresh, and error behavior.
- Full solution build/test before completion.
- Manual WPF smoke test against a safe Google Calendar/test student.

## Follow-On Review And Documentation

- Code review required: yes — Google Calendar writes and duplicate prevention are behavior-sensitive.
- Architecture review required: no — intended ownership fits existing Core/App/Infrastructure layering.
- UI review required: yes — new main-window action and command availability.
- Documentation refresh required: yes — public business rules and handoff context change.

## Review Stop Points

- After Stage 1 if schedule loading or generation requires wider dashboard reshaping.
- Before completion if Google Calendar write failure can look like success.

## Final User Test Instructions

Post-completion user test: mandatory.

Prerequisites/setup:

1. Use a safe Google Calendar account/calendar and test database or test student.
2. Select a student with at least one saved schedule entry.
3. Choose a current or future month with few or no lessons for that student.

Exact user actions or commands:

1. Start the WPF app.
2. Select the target student.
3. Navigate to the current month or a future month.
4. Click `Populate Calendar`.
5. Refresh/open Google Calendar.
6. Repeat `Populate Calendar` for the same student/month.
7. Navigate to a past month and inspect the button availability.

Expected successful result:

- Current month creates scheduled lessons from today through month end.
- Future month creates scheduled lessons for the whole month.
- Re-running the command skips exact duplicate same-student same-date/time lessons and does not create duplicates.
- The main lesson list refreshes to show the created lessons.

Expected failure/safety result, if relevant:

- Past months cannot be seeded.
- Missing schedule or Google Calendar write failure shows an immediate error.
- Non-exact conflicts are not detected in v1 and must be resolved manually in Google Calendar.

Where to inspect artifacts, reports, logs, database state, or UI state:

- Main WPF lesson list.
- Google Calendar for the selected account/calendar.
- `.ai/implementation-plans/active/2026-06-23-populate-calendar-from-student-schedule.md` for completion evidence.

Cleanup/reset instructions:

- Delete only test-created Google Calendar lesson entries.
- Do not delete real lessons or invoices.

## Completion Log

- 2026-06-23:
  - Stage: Planning
  - Files changed: `.ai/implementation-plans/active/2026-06-23-populate-calendar-from-student-schedule.md`
  - Summary: Planned schedule-based monthly Google Calendar population for selected student, with current/future month rules and exact duplicate skipping.
  - Tests/verification run: Not run; planning only.
  - Results: Ready for documentation-first, test-first implementation.
  - Deviations: None.
  - Risks/open questions: Partial external writes remain possible if Google Calendar fails mid-run; must be visible and documented.
- 2026-06-23:
  - Stage: Stages 1-3 - Documentation, Core/App/Infrastructure implementation, and WPF command
  - Files changed: `docs/business-requirements-and-logic.md`, `README.md`, `.ai/project-profile.md`, `.ai/context/HANDOFF.md`, `src/MySmallBusinessLedger.Core/UseCases/Lessons/PopulateCalendarRequest.cs`, `src/MySmallBusinessLedger.Core/UseCases/Lessons/PopulateCalendarResult.cs`, `src/MySmallBusinessLedger.Core/UseCases/Lessons/PopulateCalendarUseCase.cs`, `src/MySmallBusinessLedger.Core/Ports/IGoogleCalendarGateway.cs`, `src/MySmallBusinessLedger.Core/UseCases/Startup/StartupDashboardResult.cs`, `src/MySmallBusinessLedger.Core/UseCases/Startup/StartupDashboardQuery.cs`, `src/MySmallBusinessLedger.App/Services/ILessonEditorService.cs`, `src/MySmallBusinessLedger.App/Services/CoreLessonEditorService.cs`, `src/MySmallBusinessLedger.App/Services/ConfiguredGoogleCalendarGateway.cs`, `src/MySmallBusinessLedger.App/Startup/ApplicationServiceGraph.cs`, `src/MySmallBusinessLedger.App/ViewModels/MainWindowViewModel.cs`, `src/MySmallBusinessLedger.App/MainWindow.xaml`, `src/MySmallBusinessLedger.Infrastructure/Calendar/GoogleCalendarGateway.cs`, `tests/MySmallBusinessLedger.Tests/Core/UseCases/UseCaseTests.cs`, `tests/MySmallBusinessLedger.Tests/Infrastructure/Calendar/GoogleCalendarGatewayTests.cs`, `tests/MySmallBusinessLedger.Tests/App/ViewModels/MainWindowViewModelTests.cs`, `tests/MySmallBusinessLedger.Tests/App/ViewModels/EditorViewModelTests.cs`, and `tests/MySmallBusinessLedger.Tests/App/Services/AppServiceTests.cs`
  - Summary: Added schedule-based calendar population for selected student/month. Core generates current/future month occurrences from saved schedule entries and skips exact same-student same-date/time duplicates. Infrastructure creates normal marked Google Calendar lesson events with the configured default duration. Main window exposes `Populate Calendar` and refreshes after success.
  - Tests/verification run: `dotnet test tests\MySmallBusinessLedger.Tests\MySmallBusinessLedger.Tests.csproj --filter PopulateCalendar -nodeReuse:false`; `dotnet test MySmallBusinessLedger.sln --filter "PopulateCalendar|GoogleCalendarGateway" -nodeReuse:false`; `dotnet test MySmallBusinessLedger.sln --filter MainWindowViewModel -nodeReuse:false`; `dotnet test MySmallBusinessLedger.sln --filter "PopulateCalendar|GoogleCalendarGateway|MainWindowViewModel" -nodeReuse:false`.
  - Results: Core populate slice passed 5/5; combined Core/Calendar slice passed 17/17; MainWindowViewModel passed 30/30; combined feature/UI slice passed 47/47.
  - Deviations: No batch Google API endpoint was introduced; the gateway creates individual normal lesson events in sequence and then refreshes.
  - Risks/open questions: If Google Calendar fails mid-run, some events may already exist externally and the user must inspect/refresh Google Calendar. This matches the v1 no-conflict/manual-resolution posture.
- 2026-06-23:
  - Stage: Stage 4 - Verification, review, and context refresh
  - Files changed: `.ai/context/HANDOFF.md`, `.ai/implementation-plans/active/2026-06-23-populate-calendar-from-student-schedule.md`
  - Summary: Ran focused code/UI review against the new population path. No blocking findings found; ownership remains Core for schedule/duplicate policy, Infrastructure for Google event creation, and WPF ViewModel for command state/error display. Recorded final verification and manual smoke-test gate.
  - Tests/verification run: `dotnet build MySmallBusinessLedger.sln -nodeReuse:false`; `dotnet test MySmallBusinessLedger.sln -nodeReuse:false`.
  - Results: Build passed with 0 warnings and 0 errors; full test suite passed 213/213.
  - Deviations: Manual Google Calendar smoke test remains pending, so the plan stays active.
  - Risks/open questions: User must verify against a safe calendar/month before this plan should be moved to completed.
