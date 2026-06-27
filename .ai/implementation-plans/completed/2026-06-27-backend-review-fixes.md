# Implementation Plan: Backend Review Fixes

Status: Complete
Created: 2026-06-27
Last updated: 2026-06-27
Owner/context: Ruben / Codex, following backend code-review findings.

## Related Docs And Standards

- `AGENTS.md`
- `README.md`
- `docs/business-requirements-and-logic.md`
- `.ai/project-profile.md`
- `.ai/standards/coding-standards.md`
- `.ai/standards/testing-and-verification.md`
- `.ai/standards/database-standards.md`
- `.ai/standards/domain-data-integrity.md`
- `.ai/standards/persistence-and-artifacts.md`
- `.ai/standards/security-and-boundaries.md`

## Related Skills

- `implementation-plan`
- `test-driven-development`
- `systematic-debugging`
- `documentation-refresh`

## Goal

Fix the remaining backend review findings around invoice issue recovery, calendar/student case matching, OAuth token filename collisions, and bounded lesson date queries.

## Non-Goals

- No UI redesign.
- No new production dependencies.
- No credit-note, invoice correction, backup/export, or manual recovery UI workflow.
- No change to the supported Google OAuth scope or token protection mechanism.

## Assumptions

- Current runtime is the local WPF app with SQLite under `%APPDATA%\visualtech.dk\MySmallBusinessLedger`.
- EF Core migrations are applied at startup before repositories are used.
- Issued invoice lines must continue to reference exact local lessons through `LessonId`.
- If a final PDF has already been published, it is safer to preserve the in-progress invoice row than to delete the database reservation and risk an orphaned PDF with no durable invoice evidence.

## Current Problem / Current State

- `IssueInvoiceUseCase` releases `Issuing` reservations after final PDF publication if database finalization fails, including cases where published-PDF deletion fails.
- Dashboard calendar lesson filtering accepts active student names case-insensitively, then compares later with case-sensitive equality.
- `WindowsProtectedFileDataStore` maps escaped percent signs to underscores, allowing different token keys to collide on the same filename.
- `SqliteLessonRepository` loads all lessons before applying date-window filters in memory.

## Desired End State

- Pre-publication failures still discard staging and release the invoice reservation so retries reuse the number.
- Once a final PDF may exist, finalization failure leaves the `Issuing` reservation in place and records the published path when known; no print request is made.
- Calendar lessons are canonicalized to the stored student short name once matched, and dashboard/invoice queries use consistent case-insensitive matching.
- Token filenames are collision-resistant hashes of the original key.
- Lesson month/window queries are bounded in SQLite using the existing `StartsAt` index.

## Behavior-Preserving Boundary

- User-visible successful invoice issue/print flow stays the same.
- Published issued invoice snapshots remain immutable.
- Issued invoice lists still hide `Issuing` rows.
- OAuth token bytes remain Windows DPAPI protected.
- Google Calendar `$` marker semantics stay unchanged.
- No broad formatting or unrelated cleanup.

## Intentional Behavior Changes

- Finalization failures after final PDF publication no longer delete the `Issuing` invoice reservation.
- Dashboard matching treats calendar/student short-name casing consistently and displays canonical stored student casing.
- Token storage uses hashed filenames for newly written tokens.
- SQLite lesson repositories filter date windows in the database instead of after loading all rows.

## Affected Files, Projects, Commands, Docs, Schemas, Or Artifacts

- Core invoice use case and invoice repository port.
- Infrastructure SQLite invoice/lesson repositories.
- Infrastructure Windows protected token data store.
- Core startup dashboard query.
- Core and Infrastructure tests.
- README, business requirements, project profile, and handoff context.

## Risks And Mitigations

- Risk: preserving `Issuing` rows may require later manual/support recovery.
  - Mitigation: keep them hidden from normal issued invoice lists but visible through existing `GetIssuingInvoiceAsync` and future recovery paths.
- Risk: hashed token filenames break lookup for tokens already stored under the old filename.
  - Mitigation: v1 pilot risk is acceptable; Connect Google Calendar can recreate tokens. Do not delete old files automatically.
- Risk: DateTimeOffset translation differs by provider.
  - Mitigation: verify against real SQLite integration tests.

## Open Questions

- None blocking. The user explicitly requested the recommended fixes.

## Staged Implementation Plan

### Stage 1: Regression Tests

Type: test
Status: Complete

Goal: Capture the reviewed failure modes before production edits.

Affected files/classes/schemas/commands/UI surfaces:
- `tests/MySmallBusinessLedger.Tests/Core/UseCases/UseCaseTests.cs`
- `tests/MySmallBusinessLedger.Tests/Infrastructure/Persistence/SqlitePersistenceTests.cs`
- `tests/MySmallBusinessLedger.Tests/Infrastructure/Calendar/GoogleCalendarGatewayTests.cs`

Concrete steps:
1. Add invoice finalization failure test proving post-publish reservations are kept and printing is skipped.
2. Add dashboard case-canonicalization test for `$ anna` with saved `Anna`.
3. Add token filename collision regression test for `a%b@example.com` and `a_25b@example.com`.
4. Add SQLite lesson query boundary test with out-of-window rows.

Behavior that must remain unchanged:
- Pre-publish PDF write/verify/publish failures still release the invoice number.

Tests to add/update:
- Focused xUnit tests listed above.

Verification commands/manual checks:
- Run focused test filters and confirm RED before implementation.

Risks:
- Existing fakes may need small contract additions.

Rollback point:
- Revert test-only edits before production changes.

Stop/review criteria:
- Stop if a test already passes for the wrong reason.

Documentation impact:
- None yet.

Follow-on actions:
- Implement only the behavior needed to turn tests green.

Runner may continue automatically after this stage: yes.

### Stage 2: Invoice Recovery Contract

Type: implementation
Status: Complete

Goal: Preserve recoverable invoice reservations once final PDF publication has succeeded or may have succeeded.

Affected files/classes/schemas/commands/UI surfaces:
- `IssueInvoiceUseCase`
- `IInvoiceRepository`
- `SqliteInvoiceRepository`
- test fakes

Concrete steps:
1. Add a repository operation to record a published PDF path on an `Issuing` reservation without marking it `Issued`.
2. Use that operation before finalization.
3. On finalization failure after publication, return failure without deleting the published PDF or releasing the reservation.
4. Keep staging cleanup and reservation release for pre-publication failures.

Behavior that must remain unchanged:
- Successful issue flow still finalizes and prints in order.
- Normal invoice lists/viewers still show only `Issued` invoices.

Tests to add/update:
- Core use-case test.
- SQLite persistence tests around reserved published path and next number.

Verification commands/manual checks:
- Focused Core use-case and SQLite persistence tests.

Risks:
- Leaving `Issuing` rows can block duplicate lesson references; this is intentional pending future recovery UI.

Rollback point:
- Revert port/use-case/repository changes together.

Stop/review criteria:
- Stop if post-publish failure can still delete the only durable invoice evidence.

Documentation impact:
- Update invoice safety wording.

Follow-on actions:
- Consider a future explicit recovery/retry UI or support tool.

Runner may continue automatically after this stage: yes.

### Stage 3: Matching, Token Store, And Query Boundaries

Type: implementation
Status: Complete

Goal: Fix lower-risk backend defects without changing product workflow.

Affected files/classes/schemas/commands/UI surfaces:
- `StartupDashboardQuery`
- `SqliteInvoiceRepository`
- `WindowsProtectedFileDataStore`
- `SqliteLessonRepository`

Concrete steps:
1. Canonicalize calendar lesson student names to stored student short names after active-student match.
2. Use case-insensitive comparisons for invoice list/month lookups.
3. Hash token keys to SHA-256 base64url filenames.
4. Push lesson date-window filtering into EF/SQLite queries.

Behavior that must remain unchanged:
- Unknown calendar student names remain ignored.
- Token payload protection remains DPAPI current-user protection.
- Lesson results stay sorted by `StartsAt`.

Tests to add/update:
- Core dashboard query test.
- Token store collision test.
- SQLite lesson repository tests.

Verification commands/manual checks:
- Focused tests, then full solution test.

Risks:
- Existing old token filenames will not be found after hashing.

Rollback point:
- Each change is isolated enough to revert independently.

Stop/review criteria:
- Stop if canonicalization causes unknown/inactive students to be reconciled.

Documentation impact:
- Update docs/context for matching and invoice recovery behavior.

Follow-on actions:
- Optional later token migration/cleanup if pilot data needs it.

Runner may continue automatically after this stage: yes.

### Stage 4: Documentation And Verification

Type: documentation / verification
Status: Complete

Goal: Keep durable docs and AI context aligned with implemented backend behavior.

Affected files/classes/schemas/commands/UI surfaces:
- `README.md`
- `docs/business-requirements-and-logic.md`
- `.ai/project-profile.md`
- `.ai/context/HANDOFF.md`

Concrete steps:
1. Update safety invariants for post-publish invoice finalization failure.
2. Update context with verified commands and remaining follow-up.
3. Run focused tests, full build, and full test suite.

Behavior that must remain unchanged:
- Docs must not claim a recovery UI exists.

Tests to add/update:
- None beyond stages above.

Verification commands/manual checks:
- `dotnet build MySmallBusinessLedger.sln -nodeReuse:false`
- `dotnet test MySmallBusinessLedger.sln -nodeReuse:false`

Risks:
- Broad format checks may be noisy; do not run `dotnet format` unless requested.

Rollback point:
- Revert docs/context edits if implementation changes.

Stop/review criteria:
- Stop if docs overstate recovery or token migration.

Documentation impact:
- This stage is documentation refresh.

Follow-on actions:
- Recommend a later explicit invoice recovery/support workflow if needed.

Runner may continue automatically after this stage: yes.

## Verification Strategy

- RED focused tests for each bug before production edits.
- GREEN focused tests after each stage.
- Full solution build and test at the end.
- No manual UI test required for these backend-only changes, but a later pilot smoke test should issue a test invoice and reconnect Google Calendar if token files were affected.

## Follow-On Review And Documentation

- Code review required: yes — invoice issue recovery and token persistence are safety-sensitive.
- Architecture review required: no — ownership remains Core workflow + Infrastructure adapters.
- UI review required: no — no UI layout/state changes.
- Documentation refresh required: yes — invoice safety and startup/recovery context changes.

## Review Stop Points

- Stop if preserving post-publish reservations breaks successful invoice issue flow.
- Stop if SQLite cannot translate bounded date filters.
- Stop if hashed token filename migration must preserve existing real user tokens in this cycle.

## Final User Test Instructions

Post-completion user test: mandatory.

Prerequisites/setup:
- Use a safe test student, test invoice folder, and test printer/PDF print target.

Exact user actions or commands:
- Run `dotnet test MySmallBusinessLedger.sln -nodeReuse:false`.
- In the app, issue one safe test invoice and confirm it appears once in invoice lists.
- If Google Calendar needs reconnecting, use `Application > Settings > Connect Google Calendar`.

Expected successful result:
- Test invoice gets a PDF, database issued record, and print status without duplicate numbers.
- Google Calendar token storage works after reconnect.

Expected failure/safety result, if relevant:
- If invoice finalization fails after PDF publication, normal issued invoice lists do not show a false issued invoice and the reservation remains available for support/recovery.

Where to inspect artifacts, reports, logs, database state, or UI state:
- Invoice PDF folder configured in Settings.
- `%APPDATA%\visualtech.dk\MySmallBusinessLedger\ledger.db`.
- `%APPDATA%\visualtech.dk\MySmallBusinessLedger\GoogleTokens`.

Cleanup/reset instructions:
- Delete only safe test invoice files and test data intentionally created for the manual smoke test.

## Completion Log

- 2026-06-27:
  - Stage: 1-3
  - Files changed: Core invoice startup use cases/ports, SQLite invoice and lesson repositories, Windows protected token store, Core/Infrastructure/App tests.
  - Summary: Added failing regressions for the review findings, then implemented retry of matching `Issuing` reservations, preservation of post-publish invoice reservations, canonical dashboard student matching, case-insensitive SQLite invoice student lookup, hashed token filenames, and bounded SQLite lesson date queries.
  - Tests/verification run: `dotnet test MySmallBusinessLedger.sln --filter "FullyQualifiedName~IssueInvoiceKeepsPublishedReservationWhenFinalizationFails|FullyQualifiedName~IssueInvoiceRetriesPublishedIssuingReservationWithoutRewritingPdf|FullyQualifiedName~StartupDashboardCanonicalizesMatchedCalendarStudentNameAndInvoiceLookup|FullyQualifiedName~ReserveInvoiceReusesMatchingIssuingReservationWithoutPublishedPdf|FullyQualifiedName~ReserveInvoiceReturnsMatchingPublishedIssuingReservationForRetry|FullyQualifiedName~InvoiceSummaryMatchesStudentShortNameCaseInsensitively|FullyQualifiedName~HasInvoiceForStudentInMonthMatchesStudentShortNameCaseInsensitively|FullyQualifiedName~LessonRepositoryFiltersStudentMonthByStartsAtInSqlite|FullyQualifiedName~LessonRepositoryFiltersWindowByStartsAtInSqlite|FullyQualifiedName~WindowsProtectedFileDataStoreUsesDistinctFilesForDistinctKeysThatEscapeSimilarly" -nodeReuse:false`
  - Results: 10/10 focused tests passed after initially failing on the old behavior.
  - Deviations: Bounded lesson queries use parameterized raw SQLite against the existing `StartsAt` TEXT date prefix because EF Core SQLite does not translate `DateTimeOffset` comparisons.
  - Risks/open questions: Existing OAuth token files under the old escaped filename scheme are not migrated; reconnecting Google Calendar recreates tokens under hashed filenames.
- 2026-06-27:
  - Stage: 4
  - Files changed: `README.md`, `docs/business-requirements-and-logic.md`, `.ai/project-profile.md`, `.ai/context/HANDOFF.md`, `.ai/implementation-plans/active/2026-06-27-backend-review-fixes.md`
  - Summary: Refreshed documentation for post-publish invoice recovery behavior, case-insensitive student matching, hashed token filenames, bounded lesson queries, and verification evidence.
  - Tests/verification run:
    - `dotnet test MySmallBusinessLedger.sln --filter "FullyQualifiedName~IssueInvoiceReleasesReservedInvoiceNumberWhenPdfPublishFailsBeforeFinalPathExists|FullyQualifiedName~IssueInvoiceKeepsReservationWhenFinalPdfAlreadyExistsOnPublishFailure|FullyQualifiedName~IssueInvoiceKeepsPublishedReservationWhenFinalizationFails|FullyQualifiedName~IssueInvoiceRetriesPublishedIssuingReservationWithoutRewritingPdf" -nodeReuse:false`
    - `dotnet build MySmallBusinessLedger.sln -nodeReuse:false`
    - `dotnet test MySmallBusinessLedger.sln -nodeReuse:false`
  - Results: Focused invoice recovery edge tests passed with 4/4 tests; full build passed with 0 warnings and 0 errors; full test suite passed with 325/325 tests.
  - Deviations: None.
  - Risks/open questions: Existing OAuth token files under the old escaped filename scheme are not migrated; reconnecting Google Calendar recreates tokens under hashed filenames.
