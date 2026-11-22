# Implementation plan — Code review fixes

Status: Completed  
Created: 2026-11-21  
Last updated: 2026-11-21  
Owner/context: Elena's Business Ledger, whole-solution review at 4e31149

## Goal and scope

Resolve all eight code-review findings. Keep student ownership, calendar actions, invoice numbering, and dashboard status consistent with the SQLite ledger. Save Settings as one operation and keep installer cleanup inside controlled build directories.

Use the existing WPF, Core, Infrastructure, and Tests projects. Preserve issued PDF filenames, historical invoice snapshots, payment behavior, per-user storage, and x64 distribution. Scope covers verified source changes and updated documentation. Release, versioning, publishing, production data changes, and live calendar changes are separate work.

## Baseline and related material

The reviewed checkout built with zero warnings and all 416 tests passed. Seven disposable checks confirmed failure scenarios affecting identity, calendar writes, numbering, Settings, and dashboard loading. The print and installer findings came from source inspection.

- AGENTS.md and .ai/project-profile.md
- docs/business-requirements-and-logic.md and docs/setup.md
- The domain-data-integrity, database, testing, and WPF MVVM standards under .ai/standards
- .ai/implementation-plans/completed/2026-11-21-ledger-integrity.md
- Implementation-plan for this document, plan-runner for execution, and code-review, ui-review, and documentation-refresh for follow-on work

## Confirmed decisions and proposed implementation safeguards

- Selections carry StudentId through WPF, application requests, and repository queries. ShortName supplies the label and the initial lookup for manually created Google Calendar events. Link a new event only when exactly one student matches. A linked lesson continues to use its saved StudentId.
- SQLite allows one unresolved invoice to occupy the numbering sequence per ledger. Draft previews remain available. A safely unpublished reservation can be released and reused before its publication checkpoint. After the checkpoint, resume the saved invoice or hold issuance for recovery.
- Flag pre-existing gaps as historical discrepancies. Preserve all issued numbers and PDFs. Route legacy pending invoices through explicit recovery in number order before new issuance. Preserve ambiguous files for review.
- Treat a missing calendar event as an observation. Persist an acknowledged cancellation before refreshing, and hold the affected lesson out of billing if the write outcome is uncertain or local persistence is incomplete.
- The plan has enough information to start. If verification reveals another irreversible decision, record it before proceeding with the affected stage.

## Stages

### 1. Sequential invoice numbering and direct recovery — Finding 4

Type: implementation and migration  
Status: Completed

- Scope: IssueInvoiceUseCase, SqliteInvoiceRepository, migrations, invoice workflow launcher, pending invoice UI, and persistence tests.
- In one SQLite write transaction, check for an unresolved reservation before allocating the next number. Add database enforcement so a second process cannot bypass the guard. When blocked, show the pending number and a **Resume invoice** action.
- Resume the saved snapshot independently of the current student, month, and lesson selection. Allow an explicit discard only while the reservation is current, safely unpublished, and free of a publication checkpoint. Enforce those conditions in the repository.
- Preserve older pending records during upgrade, including databases with more than one. Require resolution of that backlog before new allocation. Report historical gaps without attempting to fill them with new invoices. Reuse the pre-upgrade backup and PDF verification already in the project.
- Verify with independent SQLite connections that competing issuers produce one reservation, pre-checkpoint discard reuses the number, post-checkpoint interruption resumes the same artifact, the next issued invoice is consecutive, and legacy history survives intact.
- Gate: pass SQLite, PDF recovery, and pending UI checks before proceeding. Update the recovery and numbering documentation. The rollback point is the stage source baseline and a disposable pre-upgrade database.

### 2. Stable student selection and calendar import — Finding 2

Type: implementation and migration  
Status: Completed

- Scope: dashboard and student navigation, lesson commands, invoice candidate and preview requests, student and lesson repositories, and calendar reconciliation.
- Pass StudentId through the dashboard, editor, lesson, and invoice workflows. Bind the chooser to an ID and display the short name. Use the recorded owner when refreshing an already linked appointment.
- Apply one case-insensitive matching rule when validating student names and importing calendar titles. Reject new collisions. Surface existing collisions during upgrade without merging students or changing ownership. Give the user a way to open each affected record by ID and resolve its label. Keep ambiguous imports unlinked.
- Verify case variants and representative Danish letters with the actual matching rule. Verify rename and reuse across save, close, reopen, calendar refresh, and invoice preview. Upgrade a database containing a collision and check that both records remain accessible.
- Gate: pass the selection, identity, and upgrade tests before proceeding. Update calendar-title and student-management guidance. The rollback point is the stage source baseline and a disposable pre-upgrade database.

### 3. Persist calendar outcomes before refreshing — Findings 1 and 3

Type: implementation and integration tests  
Status: Completed

- Scope: GoogleCalendarGateway, lesson add/edit/delete/populate use cases, local lesson persistence, invoice eligibility, and dashboard refresh.
- Give calendar commands a typed outcome for acknowledged writes, rejected writes, uncertain results, and post-write refresh failures. Check ownership and invoice protection before destructive Google calls. Use a durable local guard for actions that could race with invoicing. Keep network calls outside SQLite transactions.
- Persist an acknowledged deletion locally before requesting a refresh. Preserve records that merely disappear from a list. Keep uncertain or locally uncommitted cancellations out of billing until resolved. If a pending invoice references the lesson, route the user to invoice recovery before performing an incompatible mutation.
- During Populate Calendar, retain each acknowledged event ID and its original request and plan. Persist successful items even when a later item fails. Report partial counts and retain enough state for a retry to recognize earlier successes. Route an ambiguous write through reconciliation before attempting another create.
- Verify delete success with refresh failure, delete of a reserved lesson, a local-write failure, a failure on the second create, and retries. Compare the simulated calendar with real SQLite and open the invoice preview immediately afterwards.
- Gate: pass calendar and invoice-preview integration checks. Document partial outcomes and recovery. The rollback point is the stage source baseline and disposable test data.

### 4. Reflect durable issuance when printing fails — Finding 5

Type: implementation and WPF verification  
Status: Completed

- Scope: IssueInvoiceUseCase, invoice preview and launcher, dashboard refresh, and reprint.
- Return the issuance result once the invoice and lesson locks are committed, even if the print request or print-status write fails. Refresh the dashboard when the user dismisses the print error. Offer reprint of the saved PDF after issuance.
- Verify issue, simulated printer failure, dialog dismissal, dashboard refresh, and reprint as one workflow. Include a failed print-status write. Check that the number, totals, PDF, and lesson locks agree throughout.
- Gate: pass workflow and WPF checks. Update print troubleshooting. The rollback point is the stage source baseline and disposable test data.

### 5. Show appointments with no applicable plan — Finding 8

Type: implementation and dashboard verification  
Status: Completed

- Scope: StartupDashboardQuery, dashboard presentation, plan assignment, and billing eligibility.
- Present an unlinked appointment as needing a plan without writing a placeholder catalog ID. Keep it out of invoice candidates until a valid plan is assigned. Load the other lessons and invoice history normally.
- Verify an event before the first plan period and an event in a plan gap, each alongside valid records. Assign a plan and verify when it becomes eligible to invoice.
- Gate: pass dashboard and invoice-preview checks. Document the user action for completing the link. The rollback point is the stage source baseline and disposable test data.

### 6. Save Settings atomically — Finding 7

Type: implementation and SQLite verification  
Status: Completed

- Scope: CoreAppSettingsService and the SQLite company, calendar, and Economy settings repositories.
- Save the values covered by one Settings submission, including Economy override cleanup, in a single SQLite transaction. Report success after commit. Keep the explicit Google OAuth connection action separate.
- Force a late Economy write failure. Reload the company, calendar, and Economy settings and verify the previous values persist. Retry successfully and verify that a save without Economy changes preserves them.
- Gate: pass fault-injection and normal-save integration checks. Update any outdated save semantics in the docs. The rollback point is the stage source baseline and disposable database.

### 7. Contain installer build cleanup — Finding 6

Type: implementation and build-script verification  
Status: Completed

- Scope: scripts/build-installer.ps1, packaging tests, and build instructions.
- Validate the supported Windows runtime before cleanup. Resolve publish and installer paths under a dedicated build directory and guard against redirection and out-of-root targets. Stage the candidate and retain the previous installer until the replacement is verified.
- Test invalid runtime and destination cases against disposable directories with sentinel files. Verify the normal x64 build path if Inno Setup is available. Preserve per-user installation and existing app data.
- Gate: pass path-containment checks. Record whether a test installer was compiled. Update build instructions if output handling changes. The rollback point is the prior script and artifact.

### 8. Integrated verification and documentation

Type: verification, review, and documentation  
Status: Completed

- Run the focused tests after each stage. Continue automatically after each gate passes. Keep implementation and release acceptance distinct.
- Run a final Windows .NET 11 solution build, the full test suite, and Git's whitespace check. Verify fresh database creation and upgrades from v1.3.2 and the current schema with disposable data. Exercise real SQLite and PDF generation with simulated Google and printer services.
- Use code-review and ui-review on the changed workflows. Use documentation-refresh on the business requirements, setup guide, and project context. Record the outcome of each finding and move this plan to completed after implementation and automated verification.
- Stop when a test reproduces unsafe issuance, lost ownership or data, or a cleanup target outside the approved directory.

## Final User Test Instructions

Post-completion user test: mandatory.

Use the fictional Mira tutorial with a disposable ledger database, a dedicated Google calendar, a separate invoice folder, and a test printer.

1. Use the automated reservation and PDF recovery tests for precise interruption points. In a disposable Ledger build, use **Invoice > Resume Invoice** to inspect a pending reservation. Confirm that a second invoice is blocked, a safely discarded tail number can be reused, and a reservation past the PDF checkpoint can only be resumed. Compare the resumed invoice and the next consecutive number in **Economy > Overview** and in the test invoice folder.
2. Rename Mira and reuse her old short name for a different student. Verify that the original student keeps her invoice history and already-linked lessons. Try a second short name that differs only by case and verify it is rejected.
3. Cancel an unbilled lesson and open the invoice preview immediately. Confirm that it is excluded. Run the automated failed-refresh and partial-population checks. On the dedicated Google calendar, add a matching lesson appointment outside the Student's plan period. Refresh the Ledger, confirm that **Plan needed** appears alongside the valid rows, then assign an active plan.
4. Simulate a failed printer after issue, dismiss the message, confirm that the invoice and locked lesson appear, then reprint the saved PDF.
5. Run `dotnet test ElenasBusinessLedger.sln --filter "FullyQualifiedName~FailedEconomyWriteRollsBackWholeSettingsSave|FullyQualifiedName~InstallerPackagingTests"`. Confirm that prior Settings values and the previous test installer survive forced failures. The installer tests use a temporary Windows junction.

Inspect Economy, Student Invoices, the test PDFs, and the disposable database. Close the trial app and remove the trial calendar and folders when those artifacts are no longer needed.

## Verification commands

```powershell
dotnet restore ElenasBusinessLedger.sln
dotnet build ElenasBusinessLedger.sln --no-restore
dotnet test ElenasBusinessLedger.sln --no-build --no-restore
git diff --check
powershell -ExecutionPolicy Bypass -File .\scripts\build-installer.ps1
```

The installer commands ran in a disposable source directory. The installer in that directory is a verification artifact. Live Google Calendar, printer, and installer acceptance tests are covered by the final user test instructions.

## Completion log

- 2026-11-21 — Created the plan against 4e31149. Planning added only this document.
- Stage 1 — SQLite numbering guard, direct resume and discard, and historical gap reporting verified. All 149 invoice and migration checks passed. No blocking user action.
- Stage 2 — StudentId selection, name collision prevention, and legacy collision reporting verified. All 179 focused checks passed. No blocking user action.
- Stage 3 — Typed calendar receipts, durable billing holds with review, local commit before refresh, and per-event populate saves verified. All 273 calendar, lesson, migration, and invoice checks passed. No blocking user action.
- Stage 4 — Issuance survives printer or status-save failure, the launcher refreshes from durable issuance, and the preview reprints the existing number. All 220 invoice and dashboard checks passed. No blocking user action.
- Stage 5 — An unmatched appointment stays visible without an invalid database row and receives a billing identity on explicit active-plan assignment. All 110 focused checks passed. No blocking user action.
- Stage 6 — Company, calendar, and Economy settings now share one SQLite transaction. Forced a late Economy failure and verified rollback and successful retry. All 62 Settings and Economy checks passed. No blocking user action.
- Stage 7 — All five packaging checks passed, including an unsupported runtime, a redirected destination, and a forced publish failure. Inno Setup compiled a disposable self-contained Windows x64 installer. A second normal build replaced the earlier test artifact and left the staging directory empty. No blocking user action.
- Stage 8 — The Windows .NET 11 solution built with zero warnings and zero errors. All 430 tests passed, including SQLite migrations from the release and preceding development schema, invoice PDF recovery, legacy reservation ordering, student drafts and ownership, and the documented failure paths. Reviewed WPF bindings and view-model transitions, and updated the business requirements, setup guide, and developer context. Google Calendar and printer interactions used test doubles in automated verification. The mandatory hands-on workflow is listed above. No blocking user action.
