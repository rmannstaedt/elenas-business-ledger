# Implementation Plan: EF Migrations And Startup Bootstrapper

Status: Complete
Created: 2026-06-22
Last updated: 2026-06-22
Owner/context: Ruben / Codex, My Small Business Ledger local-first WPF app

## Related Docs And Standards

- `AGENTS.md`
- `README.md`
- `.ai/project-profile.md`
- `docs/business-requirements-and-logic.md`
- `.ai/standards/architecture-principles.md`
- `.ai/standards/coding-standards.md`
- `.ai/standards/database-standards.md`
- `.ai/standards/persistence-and-artifacts.md`
- `.ai/standards/wpf-mvvm-standards.md`
- `.ai/standards/testing-and-verification.md`

## Related Skills

- `refactor-planning` for reducing `App.xaml.cs` ownership drift
- `implementation-plan` for the EF migration/schema policy change
- `code-review` after implementation because this touches startup, database, and dependency wiring
- `documentation-refresh` after implementation because persistence policy and startup ownership change

## Goal

Replace the manual SQLite schema initializer with EF Core migrations and move startup/composition work out of `App.xaml.cs` into explicit bootstrapper/startup components.

## Non-Goals

- Do not change domain behavior, UI layout, Google Calendar behavior, invoice numbering, invoice PDF behavior, plan lifecycle rules, or student editing behavior.
- Do not implement the separate invoice issue-finalization/print-status plan here.
- Do not delete or rewrite existing user data.
- Do not introduce a third-party DI container.

## Assumptions

- Existing pilot databases may have the current schema but no EF migrations history table.
- The first EF migration should be safe for fresh databases and current manually-initialized pilot databases.
- Automatic migration at WPF startup is acceptable for this local-first desktop app because the user explicitly requested EF migrations throughout.

## Current Problem / Current State

- `App.xaml.cs` calls `SqliteSchemaInitializer.EnsureCreated()` and then wires every repository, use case, service, launcher, and main window inline.
- `SqliteSchemaInitializer` manually creates/alters/backfills schema at startup.
- Tests use `EnsureCreatedAsync()` or `SqliteSchemaInitializer` directly.

## Desired End State

- EF migrations are the only schema evolution mechanism.
- The WPF app runs `Database.Migrate()` through a named database migration component at startup.
- Tests create SQLite databases by applying EF migrations.
- `App.xaml.cs` delegates to a bootstrapper and only handles WPF startup/window display plus warning/error presentation.
- Composition remains explicit and local; no new container dependency.

## Behavior-Preserving Boundary

- Existing Core/Infrastructure dependency direction remains unchanged.
- Runtime database path remains `%APPDATA%\visualtech.dk\MySmallBusinessLedger\ledger.db`.
- The app still retires due plans on startup and warns if scheduled retirement fails.
- Repositories keep using short-lived `LedgerDbContext` instances.
- UI command behavior remains unchanged.
- Existing pilot data is preserved when migrating.

## Intentional Behavior Changes

- Schema initialization changes from manual `EnsureCreated`/raw SQL to EF migrations.
- Startup wiring moves from `App.xaml.cs` to App-layer startup classes.

## Affected Files, Projects, Commands, Docs, Schemas, Or Artifacts

- `src/MySmallBusinessLedger.Infrastructure/MySmallBusinessLedger.Infrastructure.csproj`
- `src/MySmallBusinessLedger.Infrastructure/Persistence/LedgerDbContext.cs`
- `src/MySmallBusinessLedger.Infrastructure/Persistence/SqliteSchemaInitializer.cs`
- new EF migration files under `src/MySmallBusinessLedger.Infrastructure/Persistence/Migrations/`
- new design-time DbContext factory
- new database migration helper
- `src/MySmallBusinessLedger.App/App.xaml.cs`
- new App startup/bootstrapper classes
- persistence/app service tests
- `README.md`
- `.ai/project-profile.md`
- `docs/business-requirements-and-logic.md`
- `.ai/context/HANDOFF.md`

## Risks And Mitigations

- Risk: first migration fails on an existing manually-created database.
  - Mitigation: make the baseline migration idempotent with `CREATE TABLE IF NOT EXISTS` and `CREATE INDEX IF NOT EXISTS`; tests cover existing-schema migration.
- Risk: EF model snapshot diverges from actual schema.
  - Mitigation: generate/maintain a real EF migration snapshot and run migration-based persistence tests.
- Risk: bootstrapper extraction merely moves the large method.
  - Mitigation: split database migration, service composition, startup jobs, and WPF window creation into named classes/methods with narrow reasons to change.
- Risk: startup migration failure is hidden.
  - Mitigation: surface startup failure through `App.xaml.cs` message box and shutdown rather than showing a half-started app.

## Open Questions

- None. User requested EF migrations throughout and startup/composition cleanup.

## Staged Implementation Plan

### Stage 1: EF Migration Baseline

Type: implementation/test
Status: Complete

Goal:

Add EF migration tooling, a baseline migration, design-time factory, and migration-based test setup.

Affected files/classes/schemas/commands/UI surfaces:

- Infrastructure project file
- `LedgerDbContext`
- new migration/design-time/migrator files
- persistence test database helpers

Concrete steps:

1. Add EF design package to Infrastructure if needed.
2. Add a design-time `LedgerDbContext` factory for `dotnet ef`.
3. Generate or create a baseline EF migration representing the current schema.
4. Make the baseline migration safe for fresh and current manually-created databases.
5. Replace test database `EnsureCreatedAsync()` with `MigrateAsync()`.
6. Replace initializer tests with migration tests.

Behavior that must remain unchanged:

- Repository behavior and schema shape remain equivalent.

Tests to add/update:

- Fresh database migration creates all expected tables/indexes.
- Existing manually-created schema can adopt the baseline migration without data loss.

Verification commands/manual checks:

```powershell
dotnet build MySmallBusinessLedger.sln -nodeReuse:false
dotnet test tests\MySmallBusinessLedger.Tests\MySmallBusinessLedger.Tests.csproj --filter SqlitePersistence -nodeReuse:false
```

Runner may continue automatically after this stage: yes.

### Stage 2: Startup Bootstrapper Extraction

Type: refactor/test
Status: Complete

Goal:

Move database migration, service composition, startup jobs, and main window construction out of `App.xaml.cs`.

Affected files/classes/schemas/commands/UI surfaces:

- `App.xaml.cs`
- new `App/Startup` classes
- app service tests if new seams are testable without WPF windows

Concrete steps:

1. Add a startup database migrator call before repository use.
2. Extract service composition into an App-layer composition class.
3. Extract due-plan retirement startup job into a small runner returning warnings.
4. Keep `App.xaml.cs` responsible for WPF lifecycle, warning display, fatal startup error display, and showing the returned main window.

Behavior that must remain unchanged:

- Startup still creates app data folder, migrates database, retires due plans, wires the same services, and shows the same main window.

Tests to add/update:

- Add focused tests only where practical without launching WPF; otherwise rely on build plus existing ViewModel/service tests.

Verification commands/manual checks:

```powershell
dotnet build MySmallBusinessLedger.sln -nodeReuse:false
```

Runner may continue automatically after this stage: yes.

### Stage 3: Remove Manual Initializer And Refresh Docs

Type: cleanup/documentation/verification
Status: Complete

Goal:

Remove the old manual initializer, update docs/profile/context, and verify the solution.

Affected files/classes/schemas/commands/UI surfaces:

- delete `SqliteSchemaInitializer`
- docs/context listed above
- full solution build/test

Concrete steps:

1. Delete the manual schema initializer after migration tests cover replacement behavior.
2. Update docs to state EF migrations are the schema evolution mechanism and run at startup.
3. Run full build and tests.
4. Record completion log.

Behavior that must remain unchanged:

- No user-facing workflow changes.

Tests to add/update:

- None beyond migration/test helper updates unless failures expose gaps.

Verification commands/manual checks:

```powershell
dotnet build MySmallBusinessLedger.sln -nodeReuse:false
dotnet test MySmallBusinessLedger.sln -nodeReuse:false
```

Runner may continue automatically after this stage: no. Blocking action required: review verification results.

## Verification Strategy

- Focused persistence migration tests after Stage 1.
- Full solution build after Stage 2.
- Full solution build and tests after Stage 3.

## Follow-On Review And Documentation

- Code review required: yes — startup/database behavior changed.
- Architecture review required: optional — the change directly addresses an architecture review finding.
- UI review required: no — no intended UI behavior/layout change.
- Documentation refresh required: yes — database evolution and startup ownership changed.

## Review Stop Points

- Stop if the baseline migration cannot safely run against a current manually-created pilot database.
- Stop if startup extraction changes service wiring behavior.

## Final User Test Instructions

Post-completion user test: mandatory.

Prerequisites/setup:

- Keep a backup copy of `%APPDATA%\visualtech.dk\MySmallBusinessLedger\ledger.db` before testing on real local data.

Exact user actions or commands:

1. Start the app from Visual Studio or `dotnet run --project src\MySmallBusinessLedger.App\MySmallBusinessLedger.App.csproj`.
2. Confirm the app opens normally.
3. Open `Application > Settings` and confirm database/app data paths still point to `%APPDATA%\visualtech.dk\MySmallBusinessLedger`.
4. Open `Plan > Manage Plans` and `Student > Manage Students` and confirm existing pilot plans/students remain.

Expected successful result:

- Existing local data remains available.
- A fresh database is created through EF migrations.
- No manual schema initializer is used.

Expected failure/safety result, if relevant:

- If migration fails, startup shows a fatal error and does not open a partially initialized main window.

Where to inspect artifacts, reports, logs, database state, or UI state:

- `%APPDATA%\visualtech.dk\MySmallBusinessLedger\ledger.db`
- `__EFMigrationsHistory` table in SQLite

Cleanup/reset instructions:

- Restore the pre-test `ledger.db` backup only if manual testing reveals migration trouble.

## Completion Log

- 2026-06-22:
  - Stage: Planning
  - Files changed: `.ai/implementation-plans/active/2026-06-22-ef-migrations-and-startup-bootstrapper.md`
  - Summary: Planned EF migration adoption and App startup/composition extraction.
  - Tests/verification run: Not run; planning only.
  - Results: Ready for implementation.
  - Deviations: None.
  - Risks/open questions: Baseline migration must be safe for current manually-created pilot databases.
- 2026-06-22:
  - Stage: Stages 1-3 implementation, documentation refresh, and verification
  - Files changed: Infrastructure EF migration/design-time/migrator files, App startup bootstrapper/service graph, `App.xaml.cs`, SQLite test helpers/tests, README, project profile, business requirements, handoff.
  - Summary: Replaced manual startup schema initialization with EF Core migrations, added an idempotent baseline migration for fresh/current pilot databases, and moved app startup composition into App-layer bootstrapper classes.
  - Tests/verification run: `dotnet build MySmallBusinessLedger.sln -nodeReuse:false`; `dotnet test tests\MySmallBusinessLedger.Tests\MySmallBusinessLedger.Tests.csproj --filter SqlitePersistence -nodeReuse:false`; `dotnet test MySmallBusinessLedger.sln -nodeReuse:false`.
  - Results: Build passed with 0 warnings and 0 errors; focused SQLite tests passed 14/14; full test suite passed 181/181.
  - Deviations: The baseline migration uses idempotent SQLite DDL so current manually-created pilot databases can adopt EF migrations without dropping data.
  - Risks/open questions: Existing older pre-plan-catalog databases are not explicitly migrated beyond the current pilot-schema adoption path.
