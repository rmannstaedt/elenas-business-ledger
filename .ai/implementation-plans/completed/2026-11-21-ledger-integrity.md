# Implementation Plan — Ledger integrity and recovery

Status: Completed
Created: 2026-11-21
Last updated: 2026-11-21
Owner/context: Elena's Business Ledger v1.3.2 architecture review

## Goal and scope

Address all five findings within the existing four-project solution. Keep invoice PDFs and recorded amounts consistent across retries, identify invoice owners by stable student ID, preserve billing records when calendar visibility changes, and preserve historical invoice lines on upgrades. This work leaves release packaging outside scope.

## Decisions and safeguards

- The SQLite reservation owns the invoice snapshot, including previously unstored price adjustment details. A recovery action uses that snapshot or revises it at the same invoice number after verifying the PDF has yet to be published.
- Store a file checksum and publication paths before promoting the PDF. A mismatched file or unverifiable legacy publication needs review.
- Derive existing invoice student IDs only from exact linked lessons. Preserve invoice-time recipient labels.
- Preserve local rows when the configured calendar changes or no longer shows an event. Calendar refresh does not authorize deletion. Keep explicit lesson deletion available within the associated calendar source.
- Archive historical invoice lines that cannot be linked without guessing. Back up the current database before a schema upgrade and retain prior backups.

No open decision gates. The conversation establishes the recovery behavior and authorizes execution. The work takes place against a copy of the clean a8617ba baseline, with synthetic SQLite data and a simulated calendar and printer.

## Stages

1. **Invoice snapshot and recovery.** Persist complete snapshots, add a guarded PDF checkpoint, wire a recovery choice into the existing workflow, and test cross-process-equivalent retries against SQLite and the file store.
2. **Student and calendar identity.** Use stable invoice ownership and calendar provenance, preserve missing lessons, and test rename/reuse, calendar switches, and normal lesson commands.
3. **Migration and backup.** Archive unlinked historical line details without assigning a lesson ID. Back up existing SQLite databases before startup migrations. Test a fresh database, a v1.3.2 upgrade, and the pre-lesson-link data path.
4. **Review, documentation and delivery.** Use code-review and documentation-refresh. Run the .NET 11 build and full tests, apply the verified changes to the existing Visual Studio checkout, and check the final diff.

Stages advance after focused verification. The source rollback point is the clean baseline. Production migrations and live calendar changes are outside verification.

## Related materials

AGENTS.md, the project profile, business requirements, setup guide, domain data integrity, database, persistence and test standards. Apply implementation-plan, refactor-planning, plan-runner, code-review and documentation-refresh.

## Final User Test Instructions

Post-completion user test: mandatory.

Use the fictional Mira tutorial under a separate test Windows account with its own calendar and invoice folder. Keep VAT off for the DKK examples.

1. Run the WPF startup project in Visual Studio. Break on `var savedInvoice = reserve.Value!;` in `IssueInvoiceUseCase`. Issue Mira's DKK 320 invoice and stop debugging at the breakpoint.
2. Remove the breakpoint, restart, choose the same lesson, and add a DKK 50 discount with a reason. Select **Use current preview** in **Resume invoice**. The PDF and Economy should both show DKK 270. On a fresh disposable database, repeat and select **Resume saved invoice** to verify both show DKK 320.
3. In the disposable setup, repeat with a breakpoint on `var invoiceNumber = savedInvoice.InvoiceNumber;` after the file store returns its publication result. Stop debugging, restart, and use **Save & Print Invoice** with the same invoice. The existing PDF should be reused and the invoice should appear as issued.
4. Rename Mira and create a different student using her former short name. The invoice history should follow Mira. Switch the configured calendar and switch back. The original unbilled lesson and its saved plan should remain.
5. Close the app when finished and retain or remove the disposable test data through the test Windows account. On an upgrade test, check `Backups` beside `ledger.db` for the pre-upgrade SQLite copy.

## Completion Log

- 2026-11-21 — plan created from the architecture review at a8617ba.
- Stage 1 complete. Added saved snapshot and PDF checkpoint storage, revised invoice issue and recovery, and tested both retry amounts and recovery between the file move and the final SQLite update. The 20 focused invoice tests passed.
- Stage 2 complete. Added stable invoice owner IDs and a backfill from existing lesson links. Namespaced calendar event keys and removed refresh-based deletion. Tested same-ID operations across calendar scopes, moved lessons, and short-name reuse.
- Stage 3 complete. Verified a pre-migration SQLite backup and an archive of unmatched legacy lines. Tested an existing v1.3.2 schema, a pre-lesson-link database, and fresh installation. Earlier versions' previously dropped lines remain recoverable only from their existing backups or original PDFs.
- Stage 4 complete. Reviewed the recovery UI, updated README and setup/business documentation, and built with zero warnings and errors. All 416 tests passed. Full build/test used a disposable SQLite database, a simulated calendar and printer, and real PDF generation. Live OAuth and physical printing are covered by post-completion acceptance.
