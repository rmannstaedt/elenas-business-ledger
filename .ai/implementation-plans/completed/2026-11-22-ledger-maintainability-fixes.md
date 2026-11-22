# Ledger maintainability fixes

Status: Completed
Created: 2026-11-22
Last updated: 2026-11-22
Owner/context: Elena's Business Ledger, current v1.4.2 source

## Goal and boundaries

Fix Calendar event duration, invoice PDF plan-change notes, and WPF test isolation before the three cleanup findings. Add regression coverage for both reproduced bugs. Keep student IDs and invoice snapshots authoritative, retain the existing Calendar write and recovery ordering, preserve all-day event behavior, and keep the current installer identity and filenames. This plan covers source, tests, and relevant documentation. It does not include migrations, a version bump, a replacement release installer, or GitHub publication.

## Related guidance

Use the implementation-plan, plan-runner, code-review, and documentation-refresh skills. Apply the project coding, domain data integrity, WPF, and testing standards. See the [business requirements](../../../docs/business-requirements-and-logic.md) and [project profile](../../project-profile.md).

## Stages

1. **Calendar duration.** Add failing tests for the manual and batch creation paths, checking the duration carried by the resolved plan and received by the Google event adapter. Have Core supply the plan duration and share the gateway's event insertion. Keep existing refresh, per-event persistence, fallback, and all-day semantics. Gate on the focused Calendar and lesson tests. Roll back this contract and its callers together if the gate fails.
2. **Invoice plan-change notes.** Add regressions for A → B → A and distinct IDs with the same saved plan label. Compare adjacent dated invoice lines by saved plan identity and display their saved descriptions. Use an explicit same-label note. Use the positive plan ID already required by invoice lines. Keep service totals, line grouping, and issued snapshot data intact. Gate on the PDF model and invoice tests.
3. **WPF test isolation.** Consolidate the STA runners into one test helper that uses temporary paths for window and grid preferences, restores thread state, and cleans up. Preserve synchronous and asynchronous test behavior. Verify the WPF tests against a timestamp and hash check on the workstation layout files. Do not reset the existing layout.
4. **Shared business rules.** Reuse a Core schedule expansion in Calendar population and dashboard navigation. Centralize plan field validation while keeping draft versus publication requirements and existing error order. Gate on schedule, plan, and dashboard tests.
5. **Draft autosave ownership.** Move draft autosave tracking, stale-request suppression, and saves in progress into a focused App collaborator. Keep observable rows, selection, and command wiring in the main view model. Verify the existing edit, selection-change, refresh, draft-replacement, and rapid-update scenarios.
6. **Release version ownership.** Use the app project's version as the source for assembly and installer versions. Derive the output name and pass the version to Inno Setup. Keep staging guards, previous-file preservation, and the canonical build entry point. Verify packaging tests and compile an installer in a disposable source copy for metadata checks.
7. **Integrated verification.** Review the changed contracts and user-facing behavior, refresh relevant docs, and run the full Release suite plus a whitespace check. Close this plan once all stages pass.

## Decisions and execution gates

The app continues using the selected catalog plan for manual creation and each scheduled occurrence's effective plan for population. Existing Calendar events and issued invoices retain their saved values. User acceptance follows implementation. All stages may continue automatically in order after their gates pass. There are no open decisions.

## Final User Test Instructions

Post-completion user test: mandatory.

Use Ledger User Test, the dedicated test calendar, and a temporary invoice folder. Keep the app on fictional data.

1. Add a timed lesson for an active plan with a 45-minute duration. Inspect the same event in the test calendar and confirm it ends 45 minutes after it starts. Populate a month with a weekly test schedule and check the durations of the newly created appointments.
2. For one fictional student, add three dated lessons in the same month. Use Add Lesson and the row plan selector to choose plan A, plan B, and plan A in date order. Issue a test invoice using the trial printer and check the PDF has both plan-change dates in order.
3. Begin an incomplete draft, switch students, then return and finish entering the missing fields. Verify it saves once for the intended student and displays the chosen plan after refreshing.
4. Close the test app. Remove the temporary Calendar events and invoice artifacts through the test setup when finished.

## Completion log

- 2026-11-22. Created the plan from the six maintainability findings. The prior Release run passed 440 tests, and the two identified defects were reproduced with fictional data. No blocking user action.
- 2026-11-22. Stage 1 completed. The new duration cases failed with null or 60-minute durations on the pre-fix implementation. AddLessonUseCase and PopulateCalendarUseCase now send the resolved plan duration and GoogleCalendarGateway shares its event creation. The focused Calendar and lesson run passed 39 tests. No blocking user action.
- 2026-11-22. Stage 2 completed. The PDF notes now compare consecutive saved plan IDs. The two reproductions failed before the fix and passed afterward. The focused PDF and invoice infrastructure run passed 20 tests. No blocking user action.
- 2026-11-22. Stage 3 completed. All WPF tests now use one dispatcher-aware STA helper with temporary window and grid preference paths. The App suite passed 238 tests, and before/after hashes and write times matched for installed and user-test layout files. No blocking user action.
- 2026-11-22. Stage 4 completed. Calendar population and dashboard navigation now use the same weekly schedule expansion. Draft and publish validation share field checks and retain their required-field policies and error order. All 73 focused schedule, dashboard, and plan tests passed. No blocking user action.
- 2026-11-22. Stage 5 completed. DraftLessonAutosave owns delayed requests, cancellation of removed rows, and in-flight saves. MainWindowViewModel retains persistence callbacks and selection. The focused main-window tests passed 93 cases. No blocking user action.
- 2026-11-22. Stage 6 completed. The SDK app Version now supplies assembly metadata, the staged and final installer name, and Inno metadata. All eight focused packaging and About tests passed. A real disposable installer compiled and reported 1.4.2 in both app and setup metadata. The existing release installer hash matched before and after the check. Inno rejected the first disposable path during compression, so the successful rerun used a shorter temporary directory. No blocking user action.
- 2026-11-22. Stage 7 completed. Reviewed the Calendar and invoice contracts, draft save ownership, WPF test paths, and installer staging. Refreshed the affected business requirements, README, project profile, and handoff. All 443 Release tests passed, and the full suite preserved both layout profiles. Git whitespace verification passed. No blocking user action.
