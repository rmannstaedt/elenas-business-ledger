# Group the remaining WPF features

Status: Completed
Created: 2026-11-22
Last updated: 2026-11-22
Owner/context: Elena's Business Ledger App project

## Goal and scope

Put each workflow's WPF windows, view models, and dedicated App services together in Solution Explorer. Follow the completed Invoices move and use the existing SDK file discovery, CLR namespaces, and XAML class names. Keep the main window, application composition, common UI helpers, and Google Calendar integration in their current layers. This is a physical reorganization of the App project. UI behavior, data, and the app version are outside scope.

See `AGENTS.md`, `.ai/project-profile.md`, and the completed Invoices plan. Use the refactor-planning, plan-runner, and documentation-refresh skills.

## Stages

1. **Move the features in order.** Start with Students (Manage Students and Student Details, student services, and schedule presentation). Follow with Plans (plan catalog), Economy (overview and CSV export), Settings (editor and settings App service), and Lessons (editor, lesson row/draft helpers, and lesson services). Keep each XAML pair together, check resource references and destination collisions, and compare the moved files against their original SHA-256 hashes.
2. **Verify and document.** Run a non-incremental Release build and the full suite. Check that all six moved windows load, including Settings and Lesson Editor, which currently have view model coverage. Use test doubles or temporary data for WPF checks. Update the project profile and current source references in active plans, verify the worktree scope, and archive this plan.

A hash mismatch, missing compiled WPF resource, or failed test is a stop point for diagnosis. Reverse only the affected path moves if a rollback is needed. There are no open decisions.

## Final User Test Instructions

Post-completion user test: mandatory.

1. Start **Ledger User Test** in Visual Studio and select a fictional student. Open **Student Details**, then close it with **Cancel**. Open **Student > Manage Students** and confirm the student list appears.
2. Open **Plan > Manage Plans**, **Economy > Overview**, and **Application > Settings**. Confirm each window loads and close it.
3. With a lesson displayed, open its inline date and time pickers and dismiss them without choosing a new value.

## Completion log

- 2026-11-22. Confirmed the scope and inspected current window and service references. No blocking user action.
- 2026-11-22. Stage 1 completed. Moved 59 files and verified each by SHA-256. No blocking user action.
- 2026-11-22. Stage 2 completed. The non-incremental Release build passed with zero warnings and errors. All 444 Release tests passed. A separate smoke check opened and closed all six WPF windows with test doubles and temporary preferences and checked every compiled window resource. Rechecked all 59 moves against Git and refreshed the project profile and current paths in the active plans. No blocking user action.
