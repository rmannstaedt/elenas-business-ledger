# Show pending invoice reservations on the dashboard

Status: Completed
Created: 2026-11-22
Last updated: 2026-11-22
Owner/context: Elena's Business Ledger, Student Invoices

## Goal and scope

Show the ledger's blocking invoice reservation in Student Invoices for the student who owns it. Mark the row Pending and let its status button open the existing recovery dialog. If another student is selected, show the reservation number and saved student name beside the list. Resolve ownership by StudentId so renames and reused short names keep the correct invoice history.

The dashboard currently loads only issued rows. Keep issued history queries, lesson locks, payment accounting, Economy totals, publication, and numbering behavior intact. Use the oldest reservation, which is already the recovery target for legacy ledgers with more than one pending item. See the invoice numbering and recovery sections in `docs/business-requirements-and-logic.md`.

## Stages

1. **Project and display the pending reservation.** Read the existing recovery repository query during dashboard refresh. Use stored dates and recipient details, keep pending rows out of paid and PDF actions, and use the same recovery action for the Pending button and menu. Refresh after an issuance attempt or successful discard so the row reflects the stored outcome.
2. **Review and verify.** Cover ownership, row actions, and transitions after recovery with focused regression tests, including a WPF binding check. Run the full Release suite. Update the setup and invoice behavior notes and archive this plan.

No open decisions. Stop for diagnosis if a pending row can affect issued history or payment, or if tests show stale state after recovery. The implementation-plan, plan-runner, UI-review, and documentation-refresh skills apply.

## Final User Test Instructions

Post-completion user test: mandatory.

In **Ledger User Test**, open a ledger containing a pending reservation. Select its student and confirm the Pending row appears at the top. Select another student and confirm the notice points to **Invoice > Resume Invoice** while that student's own rows stay in the list. Click Pending for the reservation's student and cancel to verify recovery opens without changing the test ledger. With a disposable reservation, finish issuing it or discard it while that option is available and confirm the pending indication clears.

## Completion log

- 2026-11-22. Traced dashboard projection, issued history, and the recovery commands. No blocking user action.
- 2026-11-22. Stage 1 completed. Projected the blocking reservation by student ID and wired its Pending status to existing recovery. The seven focused Release tests passed, including the WPF button and unchanged payment action. No blocking user action.
- 2026-11-22. Stage 2 completed. The final Release run passed all 450 tests with zero failures or skips. WPF checks cover the Pending action, refresh after resolution, and the existing payment controls. Updated the setup and business logic guides. Git diff --check passed. No blocking user action.
