# Implementation Plan: Plan Identity Foreign Key

Status: Completed
Created: 2026-06-20
Last updated: 2026-06-23
Owner/context: Ruben / Codex, My Small Business Ledger local-first WPF app

## Related Docs And Standards

- `AGENTS.md`
- `README.md`
- `.ai/project-profile.md`
- `.ai/standards/architecture-principles.md`
- `.ai/standards/database-standards.md`
- `.ai/standards/persistence-and-artifacts.md`
- `.ai/standards/ui-design-standards.md`
- `.ai/standards/wpf-mvvm-standards.md`
- `.ai/standards/testing-and-verification.md`

## Related Skills

- `implementation-plan` for this cross-layer schema/domain/UI change
- `code-review` after implementation because persistence and workflow contracts change
- `documentation-refresh` after implementation because project profile and behavior docs change
- `ui-review` after implementation because the Manage Students Current Plan pane changes

## Goal

Change student plan periods to reference catalog plans by stable identity (`PlanCatalogId`) while keeping immutable commercial-term snapshots on each student plan period. Allow plan names to change as catalog display names without rewriting invoice snapshots, and clean up the Manage Students Current Plan pane labels.

## Non-Goals

- Do not change invoice issuing, PDF generation, payment status, Google Calendar sync, or lesson matching.
- Do not preserve pilot/test historical data with a complex compatibility migration; there is no real production data yet.
- Do not introduce a full invoice-time plan-name lookup UI yet. Issued invoices keep their own `PlanName` snapshot.
- Do not add new production dependencies.

## Assumptions

- Existing `ledger.db` data is pilot/test data and may be repaired/reset where needed.
- Plan names should remain user-facing display names, not database identity.
- Student plan periods should continue snapshotting price, lesson cadence, duration, flexibility, student count, payment terms, effective date, and starts/ends dates.
- Issued invoice lines must keep plan-name text as of invoice creation.

## Current Problem / Current State

`StudentPlanPeriods` stores `PlanName` as copied text and has no foreign key to `PlanCatalog`. That makes renaming a catalog plan either require bulk text rewrites or leave students referring to names that no longer exist. The Manage Students Current Plan pane also duplicates the selected plan name by showing both `Active plan` and `Plan name`.

## Desired End State

- `StudentPlanPeriods` contains a required `PlanCatalogId` FK to `PlanCatalog`.
- Domain/application student plan periods expose the plan catalog identity alongside the immutable plan-term snapshot.
- Student overview/editor display the current catalog name through the FK-backed model.
- Plan rename is a controlled catalog-name update that records `OldName`, `NewName`, and `ChangedAt`.
- Issued invoices still store invoice-time `PlanName` snapshots and are not rewritten.
- Manage Students Current Plan pane removes duplicate plan-name display and uses the requested shorter labels.

## Behavior-Preserving Boundary

The following must remain unchanged unless this plan explicitly says otherwise:

- user-visible behavior: student add/edit/save/cancel, activation state, schedule editing, invoice list, and invoice viewer remain as they are.
- public APIs / CLI / UI contracts: standard build/test commands remain unchanged.
- durable file formats / artifacts / logs / reports: invoice PDF and OAuth token behavior remain unchanged.
- database schema / migrations / persistence behavior: invoice tables remain snapshot-based; app startup may initialize/repair SQLite schema as before.
- security / trust boundaries: no secrets or OAuth tokens move into SQLite.
- error handling / failure reporting: save failures must remain surfaced rather than hidden.
- performance/resource characteristics: student listing remains bounded by local SQLite data and avoids N+1 plan lookups in normal UI paths.

## Intentional Behavior Changes

- Student plan periods now refer to a catalog plan identity instead of using plan name as identity.
- Plan name can be changed for Draft, Active, and Retired plans without changing commercial terms.
- Student plan displays follow the catalog name after rename.
- Issued invoice plan names remain as issued.
- Manage Students Current Plan pane labels are simplified.

## Affected Files, Projects, Commands, Docs, Schemas, Or Artifacts

- Core: plan/student records, save student requests/use case, plan rename use case/service contract.
- Infrastructure: EF entities, `LedgerDbContext`, `SqliteSchemaInitializer`, plan and student repositories.
- App: plan catalog service/view model/view for rename, student overview/editor view model/view label cleanup.
- Tests: persistence tests, plan catalog tests, student overview/editor view model tests, invoice snapshot tests as needed.
- Docs/context: `README.md`, `.ai/project-profile.md`, `.ai/context/HANDOFF.md`, related active plan logs.
- Database schema: `StudentPlanPeriods.PlanCatalogId`, `PlanNameChanges`.

## Risks And Mitigations

- Risk: orphaned pilot student plan periods during schema repair. Mitigation: assign matching catalog plan by name where possible; if impossible, create a catalog plan from the snapshot or leave the student period unassigned only if the UI can clearly handle it. Because there is no real production data, prefer a simple deterministic repair.
- Risk: accidental rewrite of invoice snapshots. Mitigation: tests assert issued invoice line names remain unchanged after catalog rename.
- Risk: ViewModels show stale names after rename. Mitigation: reload affected lists after rename and test selection/detail refresh.
- Risk: FK migration breaks existing tests. Mitigation: update persistence fixture builders to create catalog plans before assigning student periods.

## Open Questions

- [x] Question: Should plan names remain globally unique?
  - Recommended/default answer: yes.
  - Alternatives: allow duplicates and rely on IDs only.
  - Why it matters: duplicate user-facing names make assignment and invoices confusing.
  - Exact answer needed: Use uniqueness in UI/domain validation and database index where practical.

## Staged Implementation Plan

### Stage 1: Domain and persistence identity model

Type: implementation/test
Status: Complete

Goal: Add `PlanCatalogId` and plan-name-change history to the domain/persistence model while preserving invoice snapshots.

Affected files/classes/schemas/commands/UI surfaces:

- `Plan`, `StudentPlanPeriod`, save student request/use case
- `IPlanCatalogRepository`, `SqlitePlanCatalogRepository`
- `StudentPlanPeriodEntity`, `PlanCatalogEntity`, new `PlanNameChangeEntity`
- `LedgerDbContext`, `SqliteSchemaInitializer`
- persistence tests

Concrete steps:

1. Add plan catalog identity to the immutable student plan snapshot model.
2. Add FK and plan-name-change entities/configuration.
3. Update student repository to persist/read `PlanCatalogId` and join/load catalog names.
4. Add schema initializer repair for `StudentPlanPeriods.PlanCatalogId` and `PlanNameChanges`.
5. Add/update tests for FK round-trip and invoice snapshot stability.

Behavior that must remain unchanged:

- Student save/load still round-trips all existing commercial plan snapshot fields.
- Issued invoices remain independent snapshots.

Tests to add/update:

- Persistence round-trip stores `PlanCatalogId`.
- Catalog rename changes student display name but not invoice line `PlanName`.
- Schema initializer creates required columns/tables.

Verification commands/manual checks:

- Focused persistence tests.

Risks:

- Existing test builders may need plan IDs.

Rollback point:

- Revert schema/model/repository changes before UI changes.

Stop/review criteria:

- Persistence tests pass and no invoice rewrite occurs.

Documentation impact:

- Project profile must be updated after implementation.

Follow-on actions:

- Stage 2.

Runner may continue automatically after this stage: yes.

### Stage 2: Plan rename workflow

Type: implementation/test
Status: Complete

Goal: Add controlled catalog-name rename behavior with uniqueness validation and name-change history.

Affected files/classes/schemas/commands/UI surfaces:

- plan catalog use cases/service
- Manage Plans ViewModel/View
- plan catalog tests

Concrete steps:

1. Add rename use case/service method that validates non-empty unique names.
2. Persist rename and `PlanNameChanges` in one database save/transaction.
3. Allow name editing for non-Draft plans while keeping all commercial fields immutable.
4. Reload selected plan and list after rename.

Behavior that must remain unchanged:

- Draft plans remain fully editable/deletable until published.
- Active/Retired plans do not allow commercial-term edits.

Tests to add/update:

- Rename rejects blank or duplicate names.
- Rename records old/new name and timestamp.
- Rename does not update issued invoice rows.

Verification commands/manual checks:

- Focused plan catalog tests.

Risks:

- Name editing for Active plans could look like broader editing. Mitigation: keep only name field enabled and keep existing action buttons.

Rollback point:

- Revert plan rename UI/use case while keeping FK model if Stage 1 is good.

Stop/review criteria:

- Rename behavior is tested and visually clear.

Documentation impact:

- Plan lifecycle docs/profile need updated exception for name-only rename.

Follow-on actions:

- Stage 3.

Runner may continue automatically after this stage: yes.

### Stage 3: Manage Students Current Plan pane cleanup

Type: implementation/test
Status: Complete

Goal: Apply requested label and duplicate-field cleanup.

Affected files/classes/schemas/commands/UI surfaces:

- `StudentEditorViewModel`
- `StudentOverviewViewModel`
- `StudentOverviewView.xaml`
- student ViewModel tests

Concrete steps:

1. Remove duplicate `Plan name` display from Manage Students current plan pane.
2. Change labels to `Price without VAT:`, `Lessons:`, `Duration:`, `Number of students:`, `Flexibility:`, `Payment terms:`.
3. Display duration as `<number> minutes`.
4. Remove `Effective date` from the pane.
5. Make blank `Ends on` date display empty rather than `Select a date`.

Behavior that must remain unchanged:

- Active plan dropdown remains the assignment control.
- Student save/cancel remains explicit.

Tests to add/update:

- ViewModel display text tests for duration and blank end date where practical.

Verification commands/manual checks:

- Focused student overview/editor tests and manual UI smoke check if app can run.

Risks:

- DatePicker placeholder behavior may need a small converter/style adjustment.

Rollback point:

- Revert XAML/ViewModel label changes independently.

Stop/review criteria:

- UI compiles, tests pass, no duplicate plan name remains.

Documentation impact:

- Likely no user docs beyond profile/handoff.

Follow-on actions:

- Stage 4.

Runner may continue automatically after this stage: yes.

### Stage 4: Verification and documentation refresh

Type: verification/documentation/review
Status: Complete

Goal: Verify the full solution and update durable project docs/context.

Affected files/classes/schemas/commands/UI surfaces:

- `README.md`
- `.ai/project-profile.md`
- `.ai/context/HANDOFF.md`
- active implementation plan completion log

Concrete steps:

1. Run focused tests.
2. Run full build and full tests.
3. Update docs/profile/context to describe FK identity, name-only rename, rename history, and invoice snapshot behavior.
4. Record completion log.

Behavior that must remain unchanged:

- Build/test commands stay as documented.

Tests to add/update:

- None beyond earlier stages unless verification reveals gaps.

Verification commands/manual checks:

```powershell
dotnet build MySmallBusinessLedger.sln -nodeReuse:false
dotnet test MySmallBusinessLedger.sln -nodeReuse:false
```

Risks:

- Existing app data may need a one-time reset if pilot rows are inconsistent; document if observed.

Rollback point:

- Revert code/docs from this plan.

Stop/review criteria:

- Full tests pass or failures are understood and reported.

Documentation impact:

- Required.

Follow-on actions:

- Code/UI review if time permits.

Runner may continue automatically after this stage: yes.

## Verification Strategy

- Use red/green tests for persistence and use-case changes.
- Run focused persistence/plan/student ViewModel tests during implementation.
- Run `dotnet build MySmallBusinessLedger.sln -nodeReuse:false`.
- Run `dotnet test MySmallBusinessLedger.sln -nodeReuse:false`.
- Manual UI test: open Manage Students, verify Current Plan pane labels and dropdown behavior; rename a plan in Manage Plans and verify student display follows the new name.

## Follow-On Review And Documentation

- Code review required: yes — schema, persistence, and invoice snapshot invariants change.
- Architecture review required: no — layer ownership remains the same.
- UI review required: yes — Manage Students and Manage Plans behavior changes.
- Documentation refresh required: yes — project profile currently describes copied plan names as immutable snapshots.

## Review Stop Points

- Stop after Stage 1 if FK migration cannot deterministically repair existing pilot data.
- Stop after Stage 2 if rename semantics create ambiguity around duplicate names or invoice snapshots.

## Final User Test Instructions

Post-completion user test: mandatory.

Prerequisites/setup:

- Close any running app instance before testing schema changes.
- Keep a copy of `%APPDATA%\visualtech.dk\MySmallBusinessLedger\ledger.db` if you want to preserve pilot data before testing.

Exact user actions or commands:

1. Run the app.
2. Open `Plan > Manage Plans`.
3. Rename an existing plan, save/apply the rename, and close Manage Plans.
4. Open `Student > Manage Students`.
5. Select a student assigned to that plan.
6. Inspect the Current Plan pane.

Expected successful result:

- The student shows the renamed plan in the `Active plan` dropdown.
- Commercial snapshot values remain unchanged.
- The Current Plan pane has no duplicate plan-name label and uses the shorter labels.
- Invoices, if any are later issued, keep the plan name from issuance time.

Expected failure/safety result, if relevant:

- Blank or duplicate plan names are rejected with a clear message.
- Draft/Retired plans remain unassignable.

Where to inspect artifacts, reports, logs, database state, or UI state:

- UI: Manage Plans and Manage Students.
- Database: `%APPDATA%\visualtech.dk\MySmallBusinessLedger\ledger.db`, tables `PlanCatalog`, `StudentPlanPeriods`, `PlanNameChanges`, and `IssuedInvoiceLines`.

Cleanup/reset instructions:

- To reset pilot data, close the app and restore a copied `ledger.db` or delete it intentionally to let the app create a fresh database.

## Completion Log

- 2026-06-20:
  - Stage: Planning
  - Files changed: `.ai/implementation-plans/active/2026-06-20-plan-identity-foreign-key.md`
  - Summary: Created the implementation plan for moving student plan periods to plan catalog identity, adding plan rename history, and cleaning up the Current Plan pane.
  - Tests/verification run: Not run; planning only.
  - Results: Plan ready for implementation.
  - Deviations: The project profile currently states the old copied-name invariant; it will be updated after implementation.
  - Risks/open questions: Plan-name uniqueness will be enforced as the default.
- 2026-06-20:
  - Stage: Stages 1-4 implementation, documentation refresh, and verification
  - Files changed: Core plan/student models and use cases; SQLite entities/context/schema initializer/repositories; App plan/student services, ViewModels, and XAML; focused persistence/ViewModel tests; README/profile/business requirements/handoff.
  - Summary: Added `PlanCatalogId` to student plan periods, added plan-name-change history, implemented catalog name rename without commercial-term edits, updated student plan assignment/display to follow catalog identity, and simplified the Manage Students Current Plan pane.
  - Tests/verification run: focused persistence/plan/student ViewModel tests; full solution build; full solution test suite.
  - Results: Focused slice passed with 64/64 tests; focused SQLite persistence passed with 14/14 tests; `dotnet build MySmallBusinessLedger.sln -nodeReuse:false` passed with 0 warnings and 0 errors; `dotnet test MySmallBusinessLedger.sln -nodeReuse:false` passed with 174/174 tests.
  - Deviations: Legacy `StudentPlanPeriods.PlanName` remains as a fallback physical column for pilot database repair, but catalog identity is now the application model.
  - Risks/open questions: Manual UI smoke test still recommended in the running WPF app.
- 2026-06-23:
  - Stage: Completion housekeeping
  - Files changed: `.ai/implementation-plans/completed/2026-06-20-plan-identity-foreign-key.md`, `MySmallBusinessLedger.sln`
  - Summary: Rechecked the implemented plan identity behavior, confirmed all stages and documented verification are complete, and moved the plan to completed history.
  - Tests/verification run: solution build after moving the plan and updating solution items.
  - Results: Completed.
  - Deviations: None.
  - Risks/open questions: None for the implementation plan.
