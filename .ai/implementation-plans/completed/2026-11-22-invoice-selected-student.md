# Write invoices for the selected student

Status: Completed
Created: 2026-11-22
Last updated: 2026-11-22
Owner/context: Elena's Business Ledger

## Goal and scope

Open Invoice Preview for the student shown in the main window and the displayed billing month. Read the selection after any in-progress activation refresh and use the student ID when present. Disable Write Invoice when no student is selected. Remove the picker, its now-unused candidate lookup, and its tests. Keep existing invoice viewing, reservation, issuance, printing, recovery, and payment flows. This change does not require a schema migration, version bump, or installer.

## Related guidance

See [business requirements](../../../docs/business-requirements-and-logic.md), the [tutorial](../../../docs/tutorial.md), and the [WPF/MVVM standards](../../standards/wpf-mvvm-standards.md). The dashboard owns selection and the invoice launcher owns opening the preview.

## Decisions and risks

The selected student's ID takes precedence over the displayed short name. The current issue date and displayed billing period remain independent. Keep the established shortcut to an existing invoice when the dashboard resolves it. The preview remains a read-only query until the user confirms Save & Print Invoice. There are no open questions.

## Stages

1. Change the dashboard and invoice launcher together. Narrow the workflow service after removing the unused candidate flow, including constructor wiring and dependent test fixtures. Verify using the selected-student and billing regressions. If the preview targets the wrong student or month, correct the routing before proceeding.
2. Update the tutorial and application documentation. Check surviving references and the final PDF render. If the original screenshot shows the removed picker, replace it with a verified invoice preview.
3. Run the full solution tests and verify dashboard routing and the real WPF invoice launcher with fixture data. Verify the preview for the selected student and month, then cancel. Check the working tree before completing the plan.

## Final User Test Instructions

Post-completion user test: mandatory.

1. Start Ledger User Test in Visual Studio. Select Mira and a month with her unbilled lessons, such as October 2026 if the initial fixture is still available.
2. Choose Write Invoice. Verify that Invoice Preview opens immediately for Mira, with the selected billing month and the expected lessons.
3. Cancel and confirm that the invoice list is unchanged. To prepare an invoice for another student, select that student in the main window first.

## Completion log

- 2026-11-22 — Recorded current picker, stable student identity, and the boundary around invoice issuance. No blocking user action.

- 2026-11-22 — Replaced candidate selection with direct preview using the dashboard student and displayed month. Removed the unused picker and service wiring. The focused run passed 218 tests. No blocking user action.
- 2026-11-22 — Updated the business requirements, project guidance, and both tutorials. Visually verified the revised PDF page and retained the invoice-preview screenshot. No blocking user action.
- 2026-11-22 — All 440 tests passed. Automated coverage exercised stable student identity, displayed-month routing, the disabled command when no student is selected, opening the real WPF preview directly, and closing the preview without issuing. The WPF check used fixture records. No blocking user action.
