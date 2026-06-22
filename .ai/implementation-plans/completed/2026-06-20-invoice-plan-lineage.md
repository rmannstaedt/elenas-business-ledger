# Implementation Plan: Invoice Plan Lineage

Status: Completed
Created: 2026-06-20
Last updated: 2026-06-23
Owner/context: Ruben / Codex, My Small Business Ledger local-first WPF app

## Related Docs And Standards

- `AGENTS.md`
- `README.md`
- `.ai/project-profile.md`
- `docs/business-requirements-and-logic.md`
- `.ai/standards/database-standards.md`
- `.ai/standards/persistence-and-artifacts.md`
- `.ai/standards/testing-and-verification.md`

## Related Skills

- `implementation-plan` for this cross-layer durable schema change
- `test-driven-development` because invoice contract behavior changes
- `documentation-refresh` after implementation because invoice snapshot/lineage rules change
- `code-review` after implementation because invoice persistence and audit semantics change

## Goal

Store the catalog plan identity on issued invoice lines while preserving the issued-time plan name snapshot. This gives invoices stable lineage to the catalog plan without making old invoice display dependent on reconstructing rename history.

## Non-Goals

- Do not remove `IssuedInvoiceLine.PlanName`.
- Do not derive invoice viewer/PDF plan names from `PlanCatalogId` and `PlanNameChanges`.
- Do not change invoice PDF layout, payment status, Google Calendar lesson matching, or invoice numbering.
- Do not add new dependencies.

## Assumptions

- Existing invoice data is pilot/test data, but issued invoice text should still be treated as a durable snapshot.
- Student plan periods now carry `PlanCatalogId`.
- An invoice line can use `PlanCatalogId = 0` only as legacy fallback when loading older rows that predate this schema field.

## Current Problem / Current State

Issued invoice lines store lesson date/time, plan name, and price, but not the catalog plan identity. The invoice text remains stable, but there is no durable lineage back to the catalog plan that produced the line.

## Desired End State

- `InvoiceLineDraft` and `IssuedInvoiceLine` carry `PlanCatalogId`.
- `InvoicePreviewUseCase` populates invoice line plan identity from the active student plan period.
- `IssuedInvoiceLines` stores `PlanCatalogId` in SQLite with an index.
- Invoice viewer/PDF still display the stored `PlanName`.
- Tests prove issued invoice lines keep both the plan identity and the issued-name snapshot after a catalog rename.

## Behavior-Preserving Boundary

- Issued invoice rendering remains snapshot-based.
- Existing `PlanName` values remain the visible invoice name.
- Invoice issue/save/print ordering and failure behavior remain unchanged.
- Existing app data may be schema-upgraded by the initializer as before.

## Intentional Behavior Changes

- New issued invoice lines include catalog plan identity.
- Loaded issued invoices expose line `PlanCatalogId`.
- SQLite schema includes `IssuedInvoiceLines.PlanCatalogId`.

## Affected Files, Projects, Commands, Docs, Schemas, Or Artifacts

- Core invoice models: `InvoiceLineDraft`, `IssuedInvoiceLine`, `InvoiceDraft`
- Core invoice use case: `InvoicePreviewUseCase`
- Infrastructure persistence: `IssuedInvoiceLineEntity`, `LedgerDbContext`, `SqliteInvoiceRepository`, `SqliteSchemaInitializer`
- Tests: invoice preview, invoice draft, persistence tests, invoice view model tests as needed
- Docs/context: `README.md`, `.ai/project-profile.md`, `docs/business-requirements-and-logic.md`, `.ai/context/HANDOFF.md`
- Database schema: `IssuedInvoiceLines.PlanCatalogId`

## Risks And Mitigations

- Risk: invoice display accidentally follows current catalog names. Mitigation: keep viewer/PDF bound to stored `PlanName`; tests assert rename does not alter invoice name.
- Risk: older invoice rows lack plan IDs. Mitigation: add `PlanCatalogId` with default `0`; treat `0` as legacy/unknown lineage rather than invalid invoice data.
- Risk: line-editing in invoice preview drops the plan ID. Mitigation: update display model/build-draft paths and tests to round-trip `PlanCatalogId`.

## Staged Implementation Plan

### Stage 1: Core invoice line contract

Type: implementation/test
Status: Complete

Goal: Carry plan identity from preview draft to issued invoice line.

Steps:

1. Add a failing test showing invoice preview lines include the active student plan's `PlanCatalogId`.
2. Add `PlanCatalogId` to `InvoiceLineDraft` and `IssuedInvoiceLine`.
3. Update `InvoiceDraft.Issue` to copy the ID.
4. Update all test builders and UI draft rebuild code.

Verification:

- Focused Core invoice/use-case tests.

### Stage 2: SQLite invoice line persistence

Type: implementation/test
Status: Complete

Goal: Persist and load issued invoice line plan identity.

Steps:

1. Add a failing persistence test for issued invoice line `PlanCatalogId` round-trip and post-rename name snapshot stability.
2. Add `PlanCatalogId` to `IssuedInvoiceLineEntity`.
3. Configure and initialize `IssuedInvoiceLines.PlanCatalogId` plus index.
4. Map `PlanCatalogId` in `SqliteInvoiceRepository`.

Verification:

- Focused SQLite persistence tests.

### Stage 3: Documentation and verification

Type: documentation/verification
Status: Complete

Goal: Align durable docs and verify the solution.

Steps:

1. Update docs/profile/context with invoice-line lineage plus snapshot rules.
2. Run full build and full tests.
3. Record completion log.

Verification:

```powershell
dotnet build MySmallBusinessLedger.sln -nodeReuse:false
dotnet test MySmallBusinessLedger.sln -nodeReuse:false
```

## Verification Strategy

- Red/green focused invoice preview and persistence tests.
- Full solution build and test suite after docs.

## Final User Test Instructions

Post-completion user test: mandatory.

1. Run the app.
2. Issue or preview an invoice for a student with an assigned Active plan.
3. Rename that catalog plan.
4. Reopen the issued invoice.

Expected result:

- The invoice still shows the plan name as it was when issued.
- The database `IssuedInvoiceLines` row has the matching `PlanCatalogId` for lineage.

## Completion Log

- 2026-06-20:
  - Stage: Planning
  - Files changed: `.ai/implementation-plans/active/2026-06-20-invoice-plan-lineage.md`
  - Summary: Planned invoice-line catalog plan identity while preserving issued-time plan name snapshots.
  - Tests/verification run: Not run; planning only.
  - Results: Ready for implementation.
- 2026-06-20:
  - Stage: Stages 1-3 implementation, documentation refresh, and verification
  - Files changed: Core invoice line models/use case; SQLite invoice line entity/context/schema initializer/repository; invoice/persistence tests; README/profile/business requirements/handoff.
  - Summary: Added `PlanCatalogId` to draft and issued invoice lines, persisted it in SQLite, and documented that invoice display remains snapshot-based through stored `PlanName`.
  - Tests/verification run: focused invoice lineage tests; invoice-related test slice; full solution build; full solution test suite.
  - Results: Focused lineage tests passed with 5/5 tests; invoice-related tests passed with 45/45 tests; `dotnet build MySmallBusinessLedger.sln -nodeReuse:false` passed with 0 warnings and 0 errors; `dotnet test MySmallBusinessLedger.sln -nodeReuse:false` passed with 174/174 tests.
  - Deviations: Existing invoice lines use `PlanCatalogId = 0` as legacy/unknown lineage if they predate the schema field.
  - Risks/open questions: Manual issued-invoice smoke test still recommended in the running WPF app once real invoice data exists.
- 2026-06-23:
  - Stage: Completion housekeeping
  - Files changed: `.ai/implementation-plans/completed/2026-06-20-invoice-plan-lineage.md`, `MySmallBusinessLedger.sln`
  - Summary: Rechecked the implemented invoice-line plan lineage behavior, confirmed the plan's stages and documented verification are complete, and moved the plan to completed history.
  - Tests/verification run: solution build after moving the plan and updating solution items.
  - Results: Completed.
  - Deviations: None.
  - Risks/open questions: None for the implementation plan.
