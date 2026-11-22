# Keep new invoices separate from pending recovery

Status: Completed
Created: 2026-11-22
Last updated: 2026-11-22
Owner/context: Elena's Business Ledger invoice dashboard

## Goal and scope

Disable Write Invoice while a reservation blocks the ledger, regardless of the selected student or month. Keep Pending and Invoice > Resume Invoice available. Base the guard on the stored reservation. Recheck when opening a preview so another session's reservation takes effect even when the dashboard is stale. The new invoice draft query must also exclude exact lesson IDs held by any pending invoice. Normal lesson queries and invoice recovery continue to use their existing records and saved snapshots.

See `AGENTS.md`, the completed pending invoice dashboard plan, and the invoice workflow in `docs/business-requirements-and-logic.md`. This work uses implementation-plan, plan-runner, UI-review, and documentation-refresh. No changes to invoice 003 in Ledger User Test, the schema, or Google Calendar are needed.

## Stages

1. **Block new invoice previews.** Track the ledger's pending state on dashboard refresh, guard the command after activation refresh, and give the disabled button an explanation. Recheck pending state at the workflow entry point. Keep normal recovery and existing invoice viewing available.
2. **Enforce lesson eligibility and verify.** Exclude pending reservations by lesson ID in the invoice-only SQLite query. Verify that discard releases the lessons and restores Write Invoice. Add regression coverage for the command, SQLite query, and stale dashboard guard. Run the full Release suite, refresh the guides, and archive the plan.

No unresolved decisions. Stop for diagnosis if the dashboard blocks recovery, a held lesson appears in a new draft, or a released lesson remains unavailable.

## Final User Test Instructions

Post-completion user test: mandatory.

In Ledger User Test, reopen the dashboard with invoice 003 pending. Verify Write Invoice is disabled for Mira and another student. Open the Pending row or Invoice > Resume Invoice and confirm the three saved October lessons. Cancel to leave the test reservation in place, or discard it while unpublished to confirm Write Invoice becomes available and the released lessons return to a fresh October preview.

## Completion log

- 2026-11-22. Reproduced the cause by reading invoice 003 and tracing the dashboard command and invoice lesson query. No blocking user action.
- 2026-11-22. Stage 1 implemented. The dashboard and preview entry point block a new preview while a reservation is pending. The lesson query excludes exact held IDs, and the guides describe the recovery path. No blocking user action.
- 2026-11-22. Stage 2 completed. All 20 focused invoice tests and all 452 Release tests passed. The WPF test verifies the disabled action, recovery, and re-enablement. The SQLite test verifies that reserving three of four lessons excludes only those three, blocks the normal preview, and restores all four after discard. No blocking user action.
