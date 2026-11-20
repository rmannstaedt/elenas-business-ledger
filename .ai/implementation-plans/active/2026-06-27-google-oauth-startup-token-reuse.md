# Implementation Plan: Google OAuth Startup Token Reuse

Status: Active - implementation complete, awaiting user acceptance
Created: 2026-06-27
Last updated: 2026-06-27
Owner/context: Ruben and Codex, after diagnosing an unexpected Google OAuth browser prompt at app startup.

## Related Docs And Standards

- `AGENTS.md`
- `README.md`
- `.ai/project-profile.md`
- `.ai/context/HANDOFF.md`
- `.ai/standards/coding-standards.md`
- `.ai/standards/testing-and-verification.md`
- `.ai/standards/security-and-boundaries.md`
- `.ai/standards/persistence-and-artifacts.md`
- `.ai/standards/wpf-mvvm-standards.md`

## Related Skills

- `implementation-plan`
- `test-driven-development`
- `documentation-refresh`

## Goal

Prevent app startup from unexpectedly opening an interactive Google OAuth browser prompt when Google has already granted access, while preserving existing valid local authorizations across the token filename hashing change.

## Non-Goals

- Change requested Google Calendar OAuth scopes.
- Change the publisher OAuth client configuration model.
- Read, log, display, or expose OAuth token payloads.
- Replace the official Google Calendar .NET client.

## Assumptions

- Google's "already has some access" banner can coexist with a missing local refresh token; the app must rely on local token availability before attempting non-interactive startup sync.
- Existing pre-hash token files may be present under the old escaped filename algorithm.
- Explicit `Application > Settings > Connect Google Calendar` remains the intentional interactive OAuth entry point.

## Current Problem / Current State

- `MainWindow.OnLoaded` refreshes the dashboard immediately.
- `StartupDashboardQuery` lists Google Calendar lessons for the previous/current/next month window.
- `GoogleCalendarClientFactory` creates a Calendar client by calling `GoogleWebAuthorizationBroker.AuthorizeAsync`, which may open an external browser when no usable local token is found.
- The current token store uses hashed filenames but no fallback to the previous escaped filename algorithm, so an older valid token file can be ignored.

## Desired End State

- Startup calendar refresh does not trigger interactive OAuth when no current local token exists.
- Settings Connect still triggers OAuth intentionally and can create the saved token.
- Existing legacy escaped token files are read and migrated to hashed filenames on successful lookup.
- Settings authorization status is based on current or legacy local token files without reading token contents.

## Behavior-Preserving Boundary

The following must remain unchanged unless this plan explicitly says otherwise:

- Google Calendar remains the operational schedule source for uninvoiced lessons.
- OAuth tokens remain Windows DPAPI protected and are never stored in SQLite/plaintext config.
- Hashed token filenames remain the canonical write format.
- `Forget Google sign-in` clears local token files from the token store.
- Missing calendar account/settings/configuration still reports Google Calendar as unavailable instead of showing sample data.

## Intentional Behavior Changes

- Startup and automatic dashboard refresh use non-interactive credential creation and fail visibly/inline if authorization is missing, rather than opening OAuth.
- Legacy escaped token files are accepted for backward compatibility and migrated to canonical hashed filenames.

## Affected Files, Projects, Commands, Docs, Schemas, Or Artifacts

- `src/MySmallBusinessLedger.Infrastructure/Calendar/WindowsProtectedFileDataStore.cs`
- `src/MySmallBusinessLedger.Infrastructure/Calendar/GoogleCalendarCredentialFactory.cs`
- `src/MySmallBusinessLedger.App/Services/GoogleCalendarClientFactory.cs`
- `src/MySmallBusinessLedger.App/Services/CoreAppSettingsService.cs`
- Relevant tests under `tests/MySmallBusinessLedger.Tests/`
- `README.md`, `.ai/project-profile.md`, `.ai/context/HANDOFF.md`

No database schema changes are planned.

## Risks And Mitigations

- Risk: accidentally deleting or exposing tokens. Mitigation: tests use temp folders and fake protected payloads; implementation does not log/read token contents outside the existing DPAPI store path.
- Risk: startup silently stops syncing lessons. Mitigation: preserve existing error path so missing authorization surfaces as a dashboard refresh failure.
- Risk: Connect stops opening OAuth when needed. Mitigation: keep explicit Connect on interactive client creation.

## Open Questions

None. Ruben approved the recommended behavior in chat on 2026-06-27.

## Staged Implementation Plan

### Stage 1: Token Store Compatibility

Type: test / implementation
Status: Completed

Goal: Preserve existing local OAuth authorization files written under the old escaped filename algorithm.

Affected files/classes/schemas/commands/UI surfaces:

- `WindowsProtectedFileDataStore`
- infrastructure calendar tests

Concrete steps:

- Add failing filesystem tests proving legacy escaped filenames are detected and migrated to hashed filenames.
- Add/adjust token-store API surface only as needed for local token existence checks.
- Implement legacy read fallback and migration after successful decrypt/deserialize.

Behavior that must remain unchanged:

- New writes use hashed filenames.
- Clear deletes files in the token folder.
- Token payloads remain DPAPI protected.

Tests to add/update:

- Legacy escaped token file can be read through the hashed data store.
- Legacy file is migrated to a hashed file after successful read.
- Token existence reports true for either current hashed or legacy escaped files.

Verification commands/manual checks:

- Focused `dotnet test` filter for calendar token store tests.

Risks:

- Legacy fallback must not reintroduce filename collisions as the canonical format.

Rollback point:

- Revert token-store changes and tests.

Stop/review criteria:

- Token compatibility tests pass.

Documentation impact:

- Document hashed canonical filenames with legacy fallback.

Follow-on actions:

- Continue to Stage 2.

Runner may continue automatically after this stage: yes.

### Stage 2: Non-Interactive Startup Calendar Client Creation

Type: test / implementation
Status: Completed

Goal: Ensure startup/dashboard refresh checks for a saved local token before calling the interactive Google authorization broker.

Affected files/classes/schemas/commands/UI surfaces:

- `GoogleCalendarCredentialFactory`
- `GoogleCalendarClientFactory`
- `CoreAppSettingsService`
- app service tests

Concrete steps:

- Add failing tests for non-interactive client creation when no local token exists.
- Split explicit interactive Connect behavior from startup/background client creation.
- Keep Settings Connect using the interactive path.
- Use token existence checks that include current and legacy filename formats.

Behavior that must remain unchanged:

- Settings Connect validates the calendar by listing events.
- Missing account/calendar/config returns unavailable.
- Existing dashboard error mapping remains truthful.

Tests to add/update:

- Startup-style `CreateAsync` returns null without invoking OAuth when no token exists.
- Explicit `ConnectGoogleCalendarAsync` still uses the interactive path.

Verification commands/manual checks:

- Focused `dotnet test` filter for client factory/settings tests.

Risks:

- Need a small seam around credential creation to test behavior without live Google OAuth.

Rollback point:

- Revert client-factory and settings-service changes and tests.

Stop/review criteria:

- Focused tests pass and code review finds no token exposure.

Documentation impact:

- Update setup docs to state startup never launches OAuth; Connect is the interactive trigger.

Follow-on actions:

- Continue to Stage 3.

Runner may continue automatically after this stage: yes.

### Stage 3: Documentation And Verification

Type: documentation / verification / review
Status: Completed

Goal: Align user and agent docs with the implemented OAuth behavior, then verify the change.

Affected files/classes/schemas/commands/UI surfaces:

- `README.md`
- `.ai/project-profile.md`
- `.ai/context/HANDOFF.md`
- focused and solution test commands

Concrete steps:

- Refresh concise OAuth/token-store wording.
- Run focused tests first, then solution build/test if practical.
- Review diff for scope, security, and startup behavior.

Behavior that must remain unchanged:

- No new durable schema or dependency changes.

Tests to add/update:

- None beyond Stages 1-2 unless doc review reveals a missed behavior.

Verification commands/manual checks:

- `dotnet test MySmallBusinessLedger.sln --filter "<focused filter>" -nodeReuse:false`
- `dotnet build MySmallBusinessLedger.sln -nodeReuse:false`
- `dotnet test MySmallBusinessLedger.sln -nodeReuse:false`

Risks:

- Full test suite may be slower but should remain runnable.

Rollback point:

- Revert doc updates only if implementation remains valid.

Stop/review criteria:

- Verification is complete or any environment limitation is reported clearly.

Documentation impact:

- This stage performs the required documentation refresh.

Follow-on actions:

- Move this plan to completed only after user acceptance or separate plan-housekeeping work.

Runner may continue automatically after this stage: yes.

## Verification Strategy

- Use TDD: add focused failing tests before production code.
- Run focused token/client/settings tests.
- Run full solution build and test when focused verification is green.

## Follow-On Review And Documentation

- Code review required: yes — auth/startup/token persistence are risk-bearing boundaries.
- Architecture review required: no — ownership stays in existing App/Infrastructure boundaries.
- UI review required: no — no layout changes; only startup OAuth trigger behavior changes.
- Documentation refresh required: yes — public setup/startup behavior changes.

## Review Stop Points

- Stop if tests require live Google credentials or real user token files.
- Stop if the fix would require reading or logging token payloads outside existing protected store mechanics.

## Final User Test Instructions

Post-completion user test: mandatory.

Prerequisites/setup:

- Use the normal app data folder and existing Google Calendar configuration.

Exact user actions or commands:

- Start the WPF app normally.
- If the dashboard reports Google authorization is missing, open `Application > Settings`, verify the Google account/calendar fields, and click `Connect Google Calendar`.
- Restart the app.

Expected successful result:

- Startup does not open a Google OAuth browser prompt.
- Explicit Connect opens OAuth only when a connection/reconnection is requested.
- After Connect succeeds, subsequent startups reuse the local authorization.

Expected failure/safety result, if relevant:

- If no usable local authorization exists, startup shows the dashboard refresh failure instead of launching browser consent unexpectedly.

Where to inspect artifacts, reports, logs, database state, or UI state:

- Settings local authorization status.
- `%APPDATA%\visualtech.dk\MySmallBusinessLedger\GoogleTokens` file presence only; do not inspect token contents.

Cleanup/reset instructions:

- Use `Application > Settings > Forget Google sign-in` to clear local authorization and test reconnect flow.

## Completion Log

- 2026-06-27:
  - Stage: Plan created.
  - Files changed:
    - `.ai/implementation-plans/active/2026-06-27-google-oauth-startup-token-reuse.md`
  - Summary: Created scoped implementation plan for legacy OAuth token reuse and non-interactive startup behavior.
  - Tests/verification run: Not yet.
  - Results: Pending implementation.
  - Deviations: None.
  - Risks/open questions: None.
- 2026-06-27:
  - Stage: Stages 1-2 implementation.
  - Files changed:
    - `src/MySmallBusinessLedger.Infrastructure/Calendar/WindowsProtectedFileDataStore.cs`
    - `src/MySmallBusinessLedger.Infrastructure/Calendar/GoogleCalendarCredentialFactory.cs`
    - `src/MySmallBusinessLedger.App/Services/GoogleCalendarClientFactory.cs`
    - `src/MySmallBusinessLedger.App/Services/CoreAppSettingsService.cs`
    - `src/MySmallBusinessLedger.App/Services/ConfiguredGoogleCalendarGateway.cs`
    - `src/MySmallBusinessLedger.App/AssemblyInfo.cs`
    - focused OAuth/app-service tests
  - Summary: Added legacy escaped token-file fallback/migration, saved-token Calendar service creation, non-interactive startup client creation, and explicit interactive Settings Connect behavior.
  - Tests/verification run: `dotnet test MySmallBusinessLedger.sln --filter "FullyQualifiedName~GoogleCalendarGatewayTests|FullyQualifiedName~GoogleCalendarClientFactoryTests|FullyQualifiedName~AppServiceTests" -nodeReuse:false`
  - Results: Passed, 30/30.
  - Deviations: None.
  - Risks/open questions: Full solution verification still pending.
- 2026-06-27:
  - Stage: Stage 3 documentation refresh.
  - Files changed:
    - `README.md`
    - `.ai/project-profile.md`
    - `docs/business-requirements-and-logic.md`
    - `.ai/context/HANDOFF.md`
  - Summary: Documented non-interactive startup OAuth behavior, explicit Settings Connect authorization, and legacy token filename migration.
  - Tests/verification run: Focused OAuth/app-service tests from the prior log entry.
  - Results: Passed, 30/30.
  - Deviations: None.
  - Risks/open questions: Full solution verification still pending.
- 2026-06-27:
  - Stage: Stage 3 verification.
  - Files changed:
    - `.ai/context/HANDOFF.md`
    - `.ai/implementation-plans/active/2026-06-27-google-oauth-startup-token-reuse.md`
  - Summary: Recorded full build/test verification after OAuth startup/token reuse implementation.
  - Tests/verification run:
    - `dotnet build MySmallBusinessLedger.sln -nodeReuse:false`
    - `dotnet test MySmallBusinessLedger.sln -nodeReuse:false`
  - Results:
    - Build passed with 0 warnings and 0 errors.
    - Tests passed, 341/341.
  - Deviations: None.
  - Risks/open questions: Manual app startup smoke test remains the post-completion user test.
