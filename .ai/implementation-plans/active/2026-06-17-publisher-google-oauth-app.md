# Publisher Google OAuth App Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `plan-runner` for project-stage execution. If using Superpowers execution workflows, use `superpowers:subagent-driven-development` or `superpowers:executing-plans` task-by-task. Steps use checkbox syntax for tracking.

**Goal:** Replace the current user-supplied Google OAuth client JSON setup with an app-owned publisher OAuth client so every installed copy can ask the current user to authorize their own Google Calendar.

**Architecture:** Keep Google OAuth access behind the existing Infrastructure Calendar adapter and WPF App composition root. Add a small app-owned OAuth client configuration loader and Calendar client factory, remove the manual "browse for Google login setup file" Settings flow, and keep user OAuth access/refresh tokens in the existing Windows-protected token store.

**Tech Stack:** .NET 10, WPF/MVVM, Google.Apis.Calendar.v3, Google.Apis.Auth.OAuth2, Windows DPAPI token storage, xUnit.

---

Status: Active
Created: 2026-06-17
Last updated: 2026-06-17
Owner/context: Ruben and Codex; Ruben wants the product to use a publisher-owned Google OAuth app immediately rather than continuing with per-user/manual Google developer credential setup.

## Related Docs And Standards

- `README.md`
- `AGENTS.md`
- `.ai/project-profile.md`
- `docs/business-requirements-and-logic.md`
- `.ai/standards/security-and-boundaries.md`
- `.ai/standards/persistence-and-artifacts.md`
- `.ai/standards/testing-and-verification.md`
- `.ai/standards/ui-design-standards.md`
- `.ai/standards/wpf-mvvm-standards.md`

## Related Skills

- `implementation-plan` for this persistent plan.
- `plan-runner` to execute this plan stage by stage.
- `code-review` after the OAuth config, token reset, and Calendar connection stages.
- `ui-review` after the Settings UI changes.
- `documentation-refresh` after behavior and setup docs change.

## Goal

Use a visualtech.dk / publisher-owned Google Cloud OAuth Desktop client as the application identity. Installed users authorize their own Google account at runtime, but they do not create or supply Google Cloud credentials.

## Non-Goals

- Do not complete Google app verification inside code. Verification is a Google Cloud Console / publisher process.
- Do not build an installer or update system in this plan.
- Do not add a web backend or relay service.
- Do not move OAuth access/refresh tokens to SQLite, plaintext files, or app configuration.
- Do not broaden the Google Calendar scope unless a later explicit decision approves it.
- Do not implement multi-account switching beyond the current configured Google account email and token folder.
- Do not automate Google Cloud Console setup from the app.

## Assumptions

- The Google Cloud project will be owned by Ruben/visualtech.dk for the shareware product.
- The OAuth client type is **Desktop app**.
- During immediate private testing, the Google OAuth app may remain in Testing mode, with Ruben and Elena added as test users.
- The app continues to request `https://www.googleapis.com/auth/calendar.events` through `CalendarService.Scope.CalendarEvents`, because it must read, create, move, delete, and mark lesson events.
- A Desktop app OAuth client secret cannot be kept truly secret once distributed, but it should still not be logged, displayed, pasted into chat, or stored in SQLite.
- The actual publisher OAuth JSON should be supplied to local/release builds as an untracked or release-injected file, not committed casually to public source control.
- Existing Windows-protected token storage remains the correct storage for user OAuth access/refresh tokens.

## Current Problem / Current State

- `src/MySmallBusinessLedger.App/App.xaml.cs` loads `GoogleClientSecrets` from `%APPDATA%\visualtech.dk\MySmallBusinessLedger\google-client-secrets.json`.
- `src/MySmallBusinessLedger.App/Services/CoreAppSettingsService.cs` copies a user-selected Google OAuth JSON file into app data.
- `src/MySmallBusinessLedger.App/ViewModels/SettingsViewModel.cs` exposes `GoogleClientSecretsSourcePath`, `HasGoogleClientSecrets`, and `GoogleLoginSetupFileStatusText`.
- `src/MySmallBusinessLedger.App/Views/SettingsView.xaml` shows a "Google Calendar login setup file" textbox and Browse button.
- `src/MySmallBusinessLedger.App/Views/SettingsView.xaml.cs` owns an `OpenFileDialog` for the Google login setup JSON file.
- The current setup is acceptable for a private pilot but wrong for shareware, because every user would have to create Google developer credentials.

## Desired End State

- The app loads a publisher OAuth client configuration from the app installation/output, not from user-selected app data.
- Settings no longer asks for a Google Calendar login setup file.
- Settings clearly shows whether the installed app has Google sign-in configured.
- Users configure only the Calendar ID and Google account email, then authorize their own Google account through the browser OAuth flow.
- A user can force reconnection by clearing the Windows-protected Google token store from Settings.
- Missing publisher OAuth configuration fails closed with a clear Settings/dashboard message and does not pretend Calendar is available.
- Immediate testing is possible by placing the visualtech.dk Desktop OAuth JSON into a known local file path before running the app.
- Public/shareware distribution can package the same publisher OAuth client configuration without changing user setup flow.

## Behavior-Preserving Boundary

The following must remain unchanged unless this plan explicitly says otherwise:

- User-visible business behavior: lesson filtering, editing, completion state, invoice preview, invoice issue, PDF storage, payment status, and dashboard behavior remain unchanged.
- Calendar ownership marker: only first-line `$ <student short name>` or `$<student short name>` events are app-owned lessons.
- Google Calendar lesson write failures remain immediately visible and do not corrupt local data.
- OAuth access/refresh tokens remain Windows-protected and are not stored in SQLite or plaintext config.
- Company profile, invoice folder, Calendar ID, and Google account email remain persisted through existing app settings.
- SQLite database path remains `%APPDATA%\visualtech.dk\MySmallBusinessLedger\ledger.db`.
- Build and test commands remain `dotnet build MySmallBusinessLedger.sln -nodeReuse:false` and `dotnet test MySmallBusinessLedger.sln -nodeReuse:false`.

## Intentional Behavior Changes

- Remove the user-facing "Google Calendar login setup file" Browse/copy flow.
- Add app-owned publisher OAuth client configuration resolution.
- Add a Settings-visible Google sign-in configuration status.
- Add a user action to clear/reconnect Google authorization.
- Stop writing or requiring `%APPDATA%\visualtech.dk\MySmallBusinessLedger\google-client-secrets.json` for normal setup.
- Allow immediate pilot testing with a local, untracked publisher OAuth JSON file copied into the app output at build time.

## Affected Files, Projects, Commands, Docs, Schemas, Or Artifacts

Expected source/project files:

- Modify: `src/MySmallBusinessLedger.App/MySmallBusinessLedger.App.csproj`
- Modify: `src/MySmallBusinessLedger.App/App.xaml.cs`
- Modify: `src/MySmallBusinessLedger.App/Services/AppPaths.cs`
- Modify: `src/MySmallBusinessLedger.App/Services/CoreAppSettingsService.cs`
- Modify: `src/MySmallBusinessLedger.App/Services/IAppSettingsService.cs`
- Modify: `src/MySmallBusinessLedger.App/ViewModels/SettingsViewModel.cs`
- Modify: `src/MySmallBusinessLedger.App/Views/SettingsView.xaml`
- Modify: `src/MySmallBusinessLedger.App/Views/SettingsView.xaml.cs`
- Modify: `src/MySmallBusinessLedger.Infrastructure/Calendar/GoogleCalendarCredentialFactory.cs` only if token clearing or configuration injection needs a narrower seam.
- Create: `src/MySmallBusinessLedger.App/Services/GoogleCalendarClientFactory.cs`
- Create: `src/MySmallBusinessLedger.App/Services/GoogleOAuthClientConfigurationProvider.cs`
- Create: `src/MySmallBusinessLedger.App/Configuration/google-oauth-client.template.json`
- Create or modify: `.gitignore` entry for `src/MySmallBusinessLedger.App/Configuration/google-oauth-client.json`

Expected tests:

- Create: `tests/MySmallBusinessLedger.Tests/App/Services/GoogleOAuthClientConfigurationProviderTests.cs`
- Create: `tests/MySmallBusinessLedger.Tests/App/Services/GoogleCalendarClientFactoryTests.cs`
- Modify: `tests/MySmallBusinessLedger.Tests/App/ViewModels/SettingsViewModelTests.cs`
- Modify or add Settings view smoke tests only as needed for removed Browse UI.

Expected docs/context:

- Modify: `README.md`
- Modify: `.ai/project-profile.md`
- Modify: `.ai/context/HANDOFF.md`
- Modify: `.ai/implementation-plans/completed/2026-06-16-v1-invoice-printer.md` only if stale manual OAuth setup references remain discoverable and misleading.

Runtime/config artifacts:

- Local developer/publisher-supplied file, not committed by default: `src/MySmallBusinessLedger.App/Configuration/google-oauth-client.json`
- Build output copy: `bin/<Configuration>/net10.0-windows/Configuration/google-oauth-client.json`
- Existing protected token folder: `%APPDATA%\visualtech.dk\MySmallBusinessLedger\GoogleTokens`

## Risks And Mitigations

- Risk: The app ships without the publisher OAuth client JSON and Calendar silently fails.
  - Mitigation: Load configuration through a dedicated provider that reports `Missing`, `Invalid`, or `Ready`; surface missing/invalid status in Settings and dashboard failure messages.
- Risk: OAuth client ID/secret are logged, displayed, or accidentally committed to public source.
  - Mitigation: Add `.gitignore` for the real local JSON file, commit only a template with dummy values, and keep UI status boolean/textual rather than exposing raw values.
- Risk: Existing Windows-protected tokens were minted for a different OAuth client and fail after switching to the publisher client.
  - Mitigation: Add a "Reconnect Google Calendar" / "Forget Google sign-in" path that clears the token store so the next connection uses the publisher OAuth client.
- Risk: Google OAuth app in Testing mode rejects unknown public users.
  - Mitigation: Document that public/shareware distribution requires publishing/verification; immediate testing requires adding Ruben/Elena as test users.
- Risk: Scope choice triggers Google verification requirements.
  - Mitigation: Keep the current narrowest workable scope, `calendar.events`; document why read/write event access is required.
- Risk: Settings UI becomes misleading by saying Google is connected when only client configuration exists.
  - Mitigation: Use separate statuses: "App sign-in configuration: ready/missing" and "Google account authorization: connect/reconnect required/authorized" if implemented in UI.
- Risk: Refactoring `App.xaml.cs` introduces composition drift.
  - Mitigation: Move only Calendar client construction into a focused app service; keep Core and Infrastructure boundaries unchanged.

## Open Questions

- [x] Question: Should the app use a publisher-owned OAuth app going forward, including immediate pilot testing?
  - Recommended/default answer: Yes.
  - Alternatives: Keep manual per-user OAuth JSON setup for pilot only; require each customer to create credentials; use a service account.
  - Why it matters: This decides whether Settings should collect Google developer JSON or the app should ship with its own OAuth client identity.
  - Exact answer received: Use the publisher OAuth app immediately.

- [x] Question: Where should the real publisher OAuth JSON be supplied during local development and release packaging?
  - Recommended/default answer: Put the real JSON at `src/MySmallBusinessLedger.App/Configuration/google-oauth-client.json`, keep it untracked, copy it to output when present, and commit only `google-oauth-client.template.json`.
  - Alternatives: Commit the real OAuth Desktop client JSON to the private repo; store it in an environment-specific build artifact; paste client id/secret into MSBuild properties at release time.
  - Why it matters: The app needs the file to run Calendar OAuth immediately, but the repo should not casually expose credential material.
  - Exact answer received: Use the untracked local JSON path.

- [x] Question: Should the app keep a temporary fallback to the old app-data `google-client-secrets.json` file?
  - Recommended/default answer: No fallback after this plan, because the user explicitly wants to move away from the manual procedure and avoid misleading setup paths.
  - Alternatives: Keep fallback for one release to avoid breaking existing pilot installs.
  - Why it matters: A fallback lowers migration friction but keeps obsolete behavior alive and can confuse Settings/status.
  - Exact answer received: No fallback.

## Staged Implementation Plan

### Stage 0: Prepare Publisher OAuth App Outside The Codebase

Type: decision / setup
Status: Completed

Goal:

Create or confirm the Google Cloud OAuth Desktop client that represents the application.

Affected files/classes/schemas/commands/UI surfaces:

- Google Cloud Console project owned by Ruben/visualtech.dk.
- Downloaded OAuth Desktop client JSON file.
- No source files changed in this stage.

Concrete steps:

- [ ] In Google Cloud Console, use the `My Small Business Ledger` project or create a visualtech.dk-owned project.
- [ ] Enable Google Calendar API for the project.
- [ ] Configure the OAuth consent screen with app name `My Small Business Ledger`, support/contact email, and external audience if public users are expected.
- [ ] During private testing, add Ruben and Elena as test users if the app remains in Testing mode.
- [ ] Create an OAuth client with Application type `Desktop app` and name `My Small Business Ledger Desktop`.
- [ ] Download the OAuth client JSON.
- [ ] Save the file outside source control until Stage 1 defines the exact local path.

Behavior that must remain unchanged:

- The current app can still build and run before code stages begin.
- No Google access/refresh token handling changes in this stage.

Tests to add/update:

- None; this is external setup.

Verification commands/manual checks:

- In Google Cloud Console, confirm Calendar API status is enabled.
- In Google Cloud Console, confirm the OAuth client type is Desktop app.
- Open the downloaded JSON locally and confirm it contains an `installed` object with `client_id`, `project_id`, and `client_secret` fields. Do not paste the values into chat or logs.

Risks:

- Choosing Web application instead of Desktop app will produce the wrong OAuth flow for this WPF app.
- Forgetting test users will block Elena/Ruben while the OAuth app is in Testing mode.

Rollback point:

- Delete the incorrect OAuth client in Google Cloud Console and create a new Desktop app client.

Stop/review criteria:

- Stop before Stage 1 if the OAuth client JSON is not available.
- Stop before public distribution if Google verification/publishing is not complete.

Documentation impact:

- README and project profile will be updated in later stages after code behavior changes.

Follow-on actions:

- Proceed to Stage 1 after the local JSON supply path is confirmed.

Runner may continue automatically after this stage: yes. Completed with strategy confirmed; the real JSON file is still required before Stage 6 manual OAuth testing.

### Stage 1: Add Publisher OAuth Client Configuration Provider

Type: implementation / test
Status: Completed

Goal:

Add a focused provider that loads the publisher OAuth Desktop client JSON from app installation/output configuration and reports missing/invalid/ready state without exposing raw credential values.

Affected files/classes/schemas/commands/UI surfaces:

- Create: `src/MySmallBusinessLedger.App/Services/GoogleOAuthClientConfigurationProvider.cs`
- Create: `src/MySmallBusinessLedger.App/Configuration/google-oauth-client.template.json`
- Modify: `src/MySmallBusinessLedger.App/MySmallBusinessLedger.App.csproj`
- Modify: `.gitignore`
- Create: `tests/MySmallBusinessLedger.Tests/App/Services/GoogleOAuthClientConfigurationProviderTests.cs`

Concrete steps:

- [ ] Write a failing test `LoadsDesktopOAuthClientSecretsFromJsonFile` in `GoogleOAuthClientConfigurationProviderTests`.
  - Test writes a temporary JSON file with an `installed` object containing fake `client_id`, `project_id`, `auth_uri`, `token_uri`, and `client_secret`.
  - Test asserts the provider returns `Ready`, a non-null `ClientSecrets`, and expected fake `ClientId` / `ClientSecret`.
- [ ] Run:

  ```powershell
  dotnet test tests\MySmallBusinessLedger.Tests\MySmallBusinessLedger.Tests.csproj --filter "FullyQualifiedName~GoogleOAuthClientConfigurationProviderTests" -nodeReuse:false
  ```

  Expected: fail because the provider does not exist.
- [ ] Implement `GoogleOAuthClientConfigurationProvider` with:
  - constructor accepting a `string configurationPath`;
  - `Load()` or `LoadAsync(CancellationToken)` returning a typed result such as `GoogleOAuthClientConfigurationResult`;
  - result states for `Ready`, `Missing`, and `Invalid`;
  - no raw JSON or client secret in error messages.
- [ ] Add tests for missing file:
  - provider returns `Missing`;
  - no exception escapes;
  - message is user-safe.
- [ ] Add tests for malformed or wrong JSON:
  - provider returns `Invalid`;
  - no client secret appears in the returned message.
- [ ] Add `src/MySmallBusinessLedger.App/Configuration/google-oauth-client.template.json` with dummy placeholder values that cannot be used against Google.
- [ ] Add `.gitignore` entry for `src/MySmallBusinessLedger.App/Configuration/google-oauth-client.json`.
- [ ] Modify `MySmallBusinessLedger.App.csproj` to copy `Configuration\google-oauth-client.json` to output when the file exists:

  ```xml
  <ItemGroup>
    <None Include="Configuration\google-oauth-client.json"
          CopyToOutputDirectory="PreserveNewest"
          Condition="Exists('Configuration\google-oauth-client.json')" />
  </ItemGroup>
  ```

- [ ] Re-run the focused test command and confirm it passes.

Behavior that must remain unchanged:

- The app must still build without the real local OAuth JSON file.
- Missing OAuth config must not crash app startup.
- Existing Google token storage remains untouched.

Tests to add/update:

- `GoogleOAuthClientConfigurationProviderTests` for ready, missing, invalid, and no-secret-leak behavior.

Verification commands/manual checks:

```powershell
dotnet test tests\MySmallBusinessLedger.Tests\MySmallBusinessLedger.Tests.csproj --filter "FullyQualifiedName~GoogleOAuthClientConfigurationProviderTests" -nodeReuse:false
dotnet build MySmallBusinessLedger.sln -nodeReuse:false
```

Risks:

- `GoogleClientSecrets.FromStream` may accept JSON shapes beyond Desktop app JSON.
- An invalid file might throw a low-level exception that includes path details.

Rollback point:

- Remove the provider, tests, template file, `.gitignore` entry, and csproj content item.

Stop/review criteria:

- Stop if invalid/missing config cannot be represented without crashing.
- Stop if the real OAuth JSON must be committed to make the app build.

Documentation impact:

- Document the local untracked JSON path in README after runtime wiring is complete.

Follow-on actions:

- Stage 2 wires the provider into Calendar client creation.

Runner may continue automatically after this stage: yes.

### Stage 2: Centralize Google Calendar Client Creation

Type: implementation / test
Status: Completed

Goal:

Move Calendar client creation out of `App.xaml.cs` into a focused factory that uses the publisher OAuth client configuration provider and existing Windows-protected token store.

Affected files/classes/schemas/commands/UI surfaces:

- Create: `src/MySmallBusinessLedger.App/Services/GoogleCalendarClientFactory.cs`
- Modify: `src/MySmallBusinessLedger.App/App.xaml.cs`
- Modify: `src/MySmallBusinessLedger.App/Services/AppPaths.cs` if a token-clear helper path needs to be exposed more explicitly.
- Create: `tests/MySmallBusinessLedger.Tests/App/Services/GoogleCalendarClientFactoryTests.cs`

Concrete steps:

- [ ] Write a failing test `CreateAsyncReturnsNullWhenCalendarSettingsAreMissing`.
  - Use a fake/in-memory calendar settings repository or a test SQLite context consistent with existing persistence tests.
  - Assert no OAuth configuration is loaded when settings are missing.
- [ ] Write a failing test `CreateAsyncReturnsNullWhenPublisherOAuthConfigurationIsMissing`.
  - Arrange valid `CalendarSettings`.
  - Arrange provider result `Missing`.
  - Assert the factory returns null or a typed failure without creating a Google service.
- [ ] Write a failing test `CreateAsyncCreatesConfiguredClientWhenSettingsAndOAuthConfigurationAreReady`.
  - Use a seam/factory delegate around `GoogleCalendarCredentialFactory` or `IGoogleCalendarServiceFactory` so the test does not open a browser.
  - Assert selected calendar ID and account email are passed through.
- [ ] Run:

  ```powershell
  dotnet test tests\MySmallBusinessLedger.Tests\MySmallBusinessLedger.Tests.csproj --filter "FullyQualifiedName~GoogleCalendarClientFactoryTests" -nodeReuse:false
  ```

  Expected: fail because the factory does not exist.
- [ ] Implement `GoogleCalendarClientFactory` with dependencies:
  - `SqliteCalendarSettingsRepository`;
  - `GoogleOAuthClientConfigurationProvider`;
  - `AppPaths` for `GoogleTokenFolderPath`;
  - `TimeZoneInfo`;
  - `string applicationName`.
- [ ] Keep `WindowsProtectedFileDataStore` as the token store.
- [ ] Keep `GoogleCalendarCredentialFactory` as the Infrastructure adapter that calls `GoogleWebAuthorizationBroker.AuthorizeAsync`.
- [ ] Modify `App.xaml.cs` so `ConfiguredGoogleCalendarGateway` calls the new factory instead of `LoadConfiguredCalendarClientAsync`.
- [ ] Delete the old `LoadConfiguredCalendarClientAsync` method from `App.xaml.cs` after equivalent behavior is covered.
- [ ] Re-run focused tests and build.

Behavior that must remain unchanged:

- Missing Calendar ID/account still disables Calendar-backed dashboard/edit actions truthfully.
- Missing OAuth configuration disables Calendar-backed actions truthfully.
- The browser OAuth flow still uses the configured Google account email as `userId`.
- Tokens remain in `%APPDATA%\visualtech.dk\MySmallBusinessLedger\GoogleTokens`.

Tests to add/update:

- Factory tests for missing settings, missing OAuth config, invalid OAuth config, and ready config.
- Existing Calendar gateway tests remain unchanged unless seams move.

Verification commands/manual checks:

```powershell
dotnet test tests\MySmallBusinessLedger.Tests\MySmallBusinessLedger.Tests.csproj --filter "FullyQualifiedName~GoogleCalendarClientFactoryTests|FullyQualifiedName~GoogleOAuthClientConfigurationProviderTests" -nodeReuse:false
dotnet build MySmallBusinessLedger.sln -nodeReuse:false
```

Risks:

- A factory seam could become a second composition root if it starts constructing unrelated services.
- Tests must not open a real browser or hit Google.

Rollback point:

- Restore `App.xaml.cs` to direct loading from app-data `google-client-secrets.json` and remove the new factory/provider wiring.

Stop/review criteria:

- Stop if unit tests require real Google network/browser authorization.
- Stop if Core or Infrastructure starts depending on WPF or app-specific Settings UI types.

Documentation impact:

- Later docs should describe app-owned OAuth client configuration and no manual user JSON.

Follow-on actions:

- Stage 3 removes obsolete Settings UI.

Runner may continue automatically after this stage: yes.

### Stage 3: Remove Manual Google Login Setup File From Settings

Type: implementation / test / UI
Status: Completed

Goal:

Change Settings so users configure Calendar ID and Google account email only; the app-owned OAuth configuration status is read-only.

Affected files/classes/schemas/commands/UI surfaces:

- Modify: `src/MySmallBusinessLedger.App/Services/IAppSettingsService.cs`
- Modify: `src/MySmallBusinessLedger.App/Services/CoreAppSettingsService.cs`
- Modify: `src/MySmallBusinessLedger.App/ViewModels/SettingsViewModel.cs`
- Modify: `src/MySmallBusinessLedger.App/Views/SettingsView.xaml`
- Modify: `src/MySmallBusinessLedger.App/Views/SettingsView.xaml.cs`
- Modify: `tests/MySmallBusinessLedger.Tests/App/ViewModels/SettingsViewModelTests.cs`

Concrete steps:

- [ ] Write a failing test `LoadsOAuthAppConfigurationStatus`.
  - Fake app settings service returns `GoogleOAuthAppConfigured: true`.
  - ViewModel exposes display text `Google sign-in app: Ready`.
- [ ] Write a failing test `SaveDoesNotRequireOrPassGoogleClientSecretsSourcePath`.
  - Fake service records only company profile and calendar settings.
  - ViewModel has no need to set `GoogleClientSecretsSourcePath`.
- [ ] Run:

  ```powershell
  dotnet test tests\MySmallBusinessLedger.Tests\MySmallBusinessLedger.Tests.csproj --filter "FullyQualifiedName~SettingsViewModelTests" -nodeReuse:false
  ```

  Expected: fail because current ViewModel/service still uses manual source path.
- [ ] Replace `HasGoogleClientSecrets` in `AppSettingsSnapshot` with a name that matches the new behavior, such as `HasGoogleOAuthAppConfiguration`.
- [ ] Remove `GoogleClientSecretsSourcePath` from `SettingsViewModel`.
- [ ] Replace `GoogleLoginSetupFileStatusText` with `GoogleOAuthAppStatusText`.
- [ ] Change `IAppSettingsService.SaveAsync` signature to remove `googleClientSecretsSourcePath`.
- [ ] Remove file-copy logic from `CoreAppSettingsService.SaveAsync`.
- [ ] Change `CoreAppSettingsService.LoadAsync` / snapshot creation to use the new provider status rather than `File.Exists(appPaths.GoogleClientSecretsPath)`.
- [ ] Update `SettingsView.xaml`:
  - remove "Google Calendar login setup file" label, textbox, Browse button, and old saved-file status;
  - add read-only status text for app-owned Google sign-in configuration;
  - keep Calendar ID and Google account email fields.
- [ ] Update `SettingsView.xaml.cs`:
  - remove `Microsoft.Win32` using;
  - remove `OnBrowseGoogleClientSecrets`.
- [ ] Re-run Settings tests.

Behavior that must remain unchanged:

- Company profile validation remains unchanged.
- Invoice folder remains required.
- Calendar ID and Google account email remain editable and saved to SQLite.
- App data/database read-only path display remains unchanged.

Tests to add/update:

- Settings ViewModel tests for new status text and Save signature.
- Existing Settings view smoke test should still instantiate the window.

Verification commands/manual checks:

```powershell
dotnet test tests\MySmallBusinessLedger.Tests\MySmallBusinessLedger.Tests.csproj --filter "FullyQualifiedName~SettingsViewModelTests" -nodeReuse:false
dotnet build MySmallBusinessLedger.sln -nodeReuse:false
```

Risks:

- Removing the Browse button could leave no visible clue when app-owned OAuth config is missing.
- Terminology could confuse users by implying the Google account is already connected.

Rollback point:

- Restore old Settings fields and `CoreAppSettingsService` file-copy path.

Stop/review criteria:

- Stop if Settings no longer shows why Google Calendar is unavailable.
- Stop for UI review if the status wording is ambiguous.

Documentation impact:

- README setup instructions must remove the manual downloaded JSON step.

Follow-on actions:

- Stage 4 adds a user-visible connect/reconnect path.

Runner may continue automatically after this stage: yes.

### Stage 4: Add Connect/Reconnect Google Calendar Flow

Type: implementation / test / UI
Status: Completed

Goal:

Let the user intentionally start Google authorization and clear stale tokens from Settings, so switching to the publisher OAuth app is testable and recoverable.

Affected files/classes/schemas/commands/UI surfaces:

- Modify: `src/MySmallBusinessLedger.App/Services/IAppSettingsService.cs`
- Modify: `src/MySmallBusinessLedger.App/Services/CoreAppSettingsService.cs`
- Modify: `src/MySmallBusinessLedger.App/ViewModels/SettingsViewModel.cs`
- Modify: `src/MySmallBusinessLedger.App/Views/SettingsView.xaml`
- Modify: `src/MySmallBusinessLedger.Infrastructure/Calendar/WindowsProtectedFileDataStore.cs` if a safer scoped clear helper is needed.
- Modify: `tests/MySmallBusinessLedger.Tests/App/ViewModels/SettingsViewModelTests.cs`
- Add: service tests for token clearing if implemented outside ViewModel.

Concrete steps:

- [ ] Write a failing ViewModel test `ConnectGoogleCalendarReportsMissingCalendarSettings`.
  - Calendar ID or Google account email is blank.
  - Command sets a clear error message and does not call the service.
- [ ] Write a failing ViewModel test `ReconnectGoogleCalendarClearsTokenStoreAndReportsNextAuthorization`.
  - Fake service records `ClearGoogleAuthorizationAsync` call.
  - ViewModel status text tells user the next Calendar action will ask them to sign in.
- [ ] Write a failing service test for clearing only the configured Google token folder.
  - Use a temp folder that contains token files.
  - Assert only files under that exact folder are removed.
- [ ] Add `ConnectGoogleCalendarCommand` or `TestGoogleCalendarConnectionCommand`.
  - It should require Calendar ID and Google account email.
  - It should call a service method that creates a configured Calendar client and performs a safe, bounded read such as listing lessons for the current month through the existing gateway/client path.
  - It should surface success/failure as Settings status text, not a raw exception.
- [ ] Add `ForgetGoogleAuthorizationCommand`.
  - It should clear the Windows-protected token folder through a scoped service method.
  - It should not delete database, invoice PDFs, or app-owned OAuth configuration.
- [ ] Add Settings buttons:
  - `Connect Google Calendar`;
  - `Forget Google sign-in` or `Reconnect`.
- [ ] Keep command methods async and reentrancy-safe.
- [ ] Re-run focused tests.

Behavior that must remain unchanged:

- A failed connection test must not alter Calendar lesson data.
- Clearing authorization must not delete Calendar settings, company settings, invoices, database, OAuth app configuration, or invoice PDFs.
- Calendar write behavior remains unchanged after successful authorization.

Tests to add/update:

- ViewModel command availability/status tests.
- Token clear scoping test.
- Service test for missing OAuth app config producing a safe failure.

Verification commands/manual checks:

```powershell
dotnet test tests\MySmallBusinessLedger.Tests\MySmallBusinessLedger.Tests.csproj --filter "FullyQualifiedName~SettingsViewModelTests|FullyQualifiedName~GoogleCalendarClientFactoryTests|FullyQualifiedName~WindowsProtectedFileDataStore" -nodeReuse:false
dotnet build MySmallBusinessLedger.sln -nodeReuse:false
```

Risks:

- A connection test can open a browser, which is not suitable for automated tests.
- Token clearing is destructive to authorization state and must be tightly scoped.

Rollback point:

- Remove the new commands/buttons and keep implicit authorization on first Calendar use.

Stop/review criteria:

- Stop if the clear-token implementation can delete outside `%APPDATA%\visualtech.dk\MySmallBusinessLedger\GoogleTokens`.
- Stop if automated tests require real browser OAuth or network access.

Documentation impact:

- README should explain Connect/Reconnect behavior and token location.

Follow-on actions:

- Stage 5 updates docs and performs review.

Runner may continue automatically after this stage: yes.

### Stage 5: Documentation Refresh And Review

Type: documentation / review
Status: Completed

Goal:

Make documentation match the publisher OAuth app behavior and review the security/UI implications before final manual testing.

Affected files/classes/schemas/commands/UI surfaces:

- Modify: `README.md`
- Modify: `.ai/project-profile.md`
- Modify: `.ai/context/HANDOFF.md`
- Modify completed implementation-plan notes only where stale current-behavior OAuth setup references are misleading.

Concrete steps:

- [ ] Update README "Pilot setup":
  - remove "choose Google Calendar login setup file";
  - explain that the app ships with visualtech.dk Google sign-in configuration;
  - explain that users configure Calendar ID and Google account email;
  - explain Connect/Reconnect if Stage 4 adds it.
- [ ] Update runtime data list:
  - remove `google-client-secrets.json`;
  - keep `GoogleTokens\` as Windows-protected user authorization token storage.
- [ ] Update `.ai/project-profile.md`:
  - replace "Google Calendar login setup file path" with "Publisher Google OAuth client configuration";
  - keep hard requirement that OAuth access/refresh tokens are not plaintext.
- [ ] Update `.ai/context/HANDOFF.md` with current OAuth topology and manual test status.
- [ ] Search for stale manual setup references:

  ```powershell
  rg "Google Calendar login setup file|google-client-secrets|GoogleClientSecrets|client secrets|BrowseGoogleClientSecrets" README.md .ai docs src tests
  ```

- [ ] Run a focused `code-review` on OAuth config/token clearing and a focused `ui-review` on Settings.

Behavior that must remain unchanged:

- Docs must not claim Google app verification is complete unless Ruben has completed it in Google Cloud Console.
- Docs must distinguish app OAuth configuration from user OAuth tokens.

Tests to add/update:

- None for docs, unless stale references require code/test renames.

Verification commands/manual checks:

```powershell
rg "Google Calendar login setup file|google-client-secrets|GoogleClientSecrets|client secrets|BrowseGoogleClientSecrets" README.md .ai docs src tests
dotnet build MySmallBusinessLedger.sln -nodeReuse:false
dotnet test MySmallBusinessLedger.sln -nodeReuse:false
```

Risks:

- Documentation could overstate public readiness before Google verification.

Rollback point:

- Restore prior docs if code stages are rolled back.

Stop/review criteria:

- Stop if docs still tell users to create their own Google credentials.
- Stop if reviews find security or UI blockers.

Documentation impact:

- This is the documentation update stage.

Follow-on actions:

- Stage 6 runs final manual user test.

Runner may continue automatically after this stage: no. Blocking action required: Ruben places the real OAuth JSON and performs Stage 6 manual browser consent testing.

### Stage 6: Final Build, Test, And Real OAuth User Test

Type: verification
Status: Not started

Goal:

Prove the new publisher OAuth path works locally with a real Google account and Calendar before continuing feature work.

Affected files/classes/schemas/commands/UI surfaces:

- Local app output under `src/MySmallBusinessLedger.App/bin/...`
- `%APPDATA%\visualtech.dk\MySmallBusinessLedger\GoogleTokens`
- Settings window
- Dashboard Calendar refresh path

Concrete steps:

- [ ] Place the real publisher OAuth Desktop client JSON at `src\MySmallBusinessLedger.App\Configuration\google-oauth-client.json`.
- [ ] Confirm the file is untracked or intentionally release-injected:

  ```powershell
  git status --short -- src\MySmallBusinessLedger.App\Configuration\google-oauth-client.json
  ```

- [ ] Build:

  ```powershell
  dotnet build MySmallBusinessLedger.sln -nodeReuse:false
  ```

- [ ] Test:

  ```powershell
  dotnet test MySmallBusinessLedger.sln -nodeReuse:false
  ```

- [ ] Run the app:

  ```powershell
  dotnet run --project src\MySmallBusinessLedger.App\MySmallBusinessLedger.App.csproj
  ```

- [ ] Open `Application > Settings`.
- [ ] Confirm Google sign-in app status is ready.
- [ ] Enter Calendar ID and Google account email for the account/calendar being tested.
- [ ] Click Connect Google Calendar or trigger dashboard refresh if Stage 4 keeps implicit authorization.
- [ ] In the browser, sign in as the target user and grant Calendar access.
- [ ] Confirm Settings/dashboard reports success or displays real Calendar-backed lesson data.
- [ ] If prior tokens interfere, use Forget/Reconnect and repeat the sign-in flow.

Behavior that must remain unchanged:

- No test should write invoice data unless the user deliberately runs the invoice flow.
- No Calendar events should be edited during connection test; the safe test should read only.
- Token storage remains local and Windows-protected.

Tests to add/update:

- No additional automated tests; this stage is manual integration verification with Google.

Verification commands/manual checks:

```powershell
dotnet build MySmallBusinessLedger.sln -nodeReuse:false
dotnet test MySmallBusinessLedger.sln -nodeReuse:false
```

Manual checks:

- Browser OAuth opens under the publisher app name.
- Consent uses the expected Google account.
- The app uses the configured Calendar ID.
- `%APPDATA%\visualtech.dk\MySmallBusinessLedger\GoogleTokens` contains protected token files after authorization.
- `%APPDATA%\visualtech.dk\MySmallBusinessLedger\google-client-secrets.json` is not required for the successful run.

Risks:

- Google consent screen may show "unverified app" until verification is complete.
- Testing mode will reject users not listed as test users.
- Local browser/account state can authorize the wrong Google account if the user is signed into multiple accounts.

Rollback point:

- Remove or rename `src\MySmallBusinessLedger.App\Configuration\google-oauth-client.json`, clear `GoogleTokens`, and restore old code only if previous stages are explicitly rolled back.

Stop/review criteria:

- Stop if OAuth opens under the wrong app/project.
- Stop if the app authorizes Ruben's account when Elena's account was intended, or vice versa.
- Stop if token files appear plaintext.

Documentation impact:

- Record successful or failed manual OAuth test in `.ai/context/HANDOFF.md`.

Follow-on actions:

- After success, proceed with normal product work.
- Before public shareware release, complete Google OAuth app publishing/verification and installer packaging.

Runner may continue automatically after this stage: no. Blocking action required: real Google OAuth/browser consent must be performed by Ruben/Elena.

## Verification Strategy

- Use TDD for provider/factory/ViewModel/token-clear changes.
- Run focused tests after each implementation stage.
- Run full solution build/test before manual OAuth testing:

  ```powershell
  dotnet build MySmallBusinessLedger.sln -nodeReuse:false
  dotnet test MySmallBusinessLedger.sln -nodeReuse:false
  ```

- Do not automate browser OAuth in unit tests.
- Manual verification is required for the real Google consent flow.
- Search for stale manual OAuth setup references before completion.

## Follow-On Review And Documentation

- Code review required: yes — OAuth config loading, token clearing, and failure reporting are security/trust-boundary work.
- Architecture review required: no by default — dependency direction should remain App composition root plus Infrastructure Calendar adapter; request architecture review only if implementation starts moving OAuth policy into UI or Core.
- UI review required: yes — Settings behavior and status wording changes are user-visible and trust-sensitive.
- Documentation refresh required: yes — setup, runtime topology, and Google OAuth behavior change.

## Review Stop Points

- After Stage 0: confirm the publisher OAuth JSON strategy before code execution.
- After Stage 2: review provider/factory ownership before deleting manual setup UI.
- After Stage 4: review Settings UI wording and token reset behavior.
- Before Stage 6: confirm the correct Google account/calendar will be used for the real test.

## Final User Test Instructions

Post-completion user test: mandatory.

Prerequisites/setup:

- Google Cloud project has Calendar API enabled.
- OAuth Desktop client exists under the intended publisher project.
- If the app is in Testing mode, the test Google account is listed as a test user.
- Real OAuth JSON is available locally at `src\MySmallBusinessLedger.App\Configuration\google-oauth-client.json` or equivalent release-injected path chosen before implementation.
- Safe Calendar ID and Google account email are known.

Exact user actions or commands:

1. Run:

   ```powershell
   dotnet build MySmallBusinessLedger.sln -nodeReuse:false
   dotnet test MySmallBusinessLedger.sln -nodeReuse:false
   dotnet run --project src\MySmallBusinessLedger.App\MySmallBusinessLedger.App.csproj
   ```

2. Open `Application > Settings`.
3. Confirm Google sign-in app status is ready.
4. Enter Calendar ID and Google account email.
5. Save Settings.
6. Click Connect/Reconnect Google Calendar if available, or refresh the dashboard.
7. Complete browser consent with the intended Google account.
8. Return to the app and refresh the dashboard.

Expected successful result:

- The consent screen identifies the publisher app.
- The app no longer asks for a Google developer/client-secrets JSON file in Settings.
- Calendar refresh succeeds or truthfully reports no matching `$` lesson entries.
- Token files appear under `%APPDATA%\visualtech.dk\MySmallBusinessLedger\GoogleTokens` and are not plaintext.

Expected failure/safety result, if relevant:

- If `google-oauth-client.json` is missing, Settings/dashboard reports that Google sign-in app configuration is missing and does not open a broken OAuth flow.
- If the user is not a test user while the OAuth app is in Testing mode, Google rejects consent; the app reports a connection failure without changing local lesson or invoice data.
- If Calendar ID is wrong, connection/read fails truthfully without local data corruption.

Where to inspect artifacts, reports, logs, database state, or UI state:

- Settings window Google sign-in status.
- `%APPDATA%\visualtech.dk\MySmallBusinessLedger\GoogleTokens`
- `%APPDATA%\visualtech.dk\MySmallBusinessLedger\ledger.db`
- Dashboard lesson list after refresh.

Cleanup/reset instructions:

- Use the Settings reconnect/forget action if implemented.
- Otherwise close the app and delete only `%APPDATA%\visualtech.dk\MySmallBusinessLedger\GoogleTokens` to force a fresh Google consent flow.
- Do not delete `ledger.db` unless intentionally resetting all local app data.

## Completion Log

- 2026-06-17:
  - Stage: Plan creation
  - Files changed: `.ai/implementation-plans/active/2026-06-17-publisher-google-oauth-app.md`
  - Summary: Created plan to replace per-user Google OAuth client JSON setup with publisher-owned OAuth app configuration and Settings-based connect/reconnect behavior.
  - Tests/verification run: Not run; planning-only change.
  - Results: Plan ready for review.
  - Deviations: None.
  - Risks/open questions: Need exact strategy for supplying real publisher OAuth JSON during local development/release packaging; recommended untracked local JSON copied to output.
- 2026-06-17:
  - Stage: Stages 0-5 implementation
  - Files changed: `.gitignore`, `README.md`, `.ai/project-profile.md`, `.ai/context/HANDOFF.md`, `.ai/implementation-plans/completed/2026-06-16-v1-invoice-printer.md`, `src/MySmallBusinessLedger.App/MySmallBusinessLedger.App.csproj`, `src/MySmallBusinessLedger.App/App.xaml.cs`, `src/MySmallBusinessLedger.App/Configuration/google-oauth-client.template.json`, `src/MySmallBusinessLedger.App/Services/AppPaths.cs`, `src/MySmallBusinessLedger.App/Services/CoreAppSettingsService.cs`, `src/MySmallBusinessLedger.App/Services/GoogleCalendarClientFactory.cs`, `src/MySmallBusinessLedger.App/Services/GoogleOAuthClientConfigurationProvider.cs`, `src/MySmallBusinessLedger.App/Services/IAppSettingsService.cs`, `src/MySmallBusinessLedger.App/ViewModels/SettingsViewModel.cs`, `src/MySmallBusinessLedger.App/Views/SettingsView.xaml`, `src/MySmallBusinessLedger.App/Views/SettingsView.xaml.cs`, `tests/MySmallBusinessLedger.Tests/App/Services/AppPathsTests.cs`, `tests/MySmallBusinessLedger.Tests/App/Services/GoogleCalendarClientFactoryTests.cs`, `tests/MySmallBusinessLedger.Tests/App/Services/GoogleOAuthClientConfigurationProviderTests.cs`, `tests/MySmallBusinessLedger.Tests/App/ViewModels/SettingsViewModelTests.cs`, `tests/MySmallBusinessLedger.Tests/Infrastructure/Calendar/GoogleCalendarGatewayTests.cs`.
  - Summary: Added app-owned publisher OAuth Desktop configuration loading from `Configuration/google-oauth-client.json`, rejects missing/invalid/non-Desktop OAuth JSON safely, moved Calendar client creation into `GoogleCalendarClientFactory`, removed the Settings manual JSON browse/copy flow, added Connect Google Calendar and Forget Google sign-in commands, and refreshed setup/runtime docs.
  - Tests/verification run: red/green provider, factory, Settings ViewModel, and token-store focused tests; focused combined run `dotnet test tests\MySmallBusinessLedger.Tests\MySmallBusinessLedger.Tests.csproj --filter "FullyQualifiedName~SettingsViewModelTests|FullyQualifiedName~GoogleCalendarGatewayTests|FullyQualifiedName~GoogleCalendarClientFactoryTests|FullyQualifiedName~GoogleOAuthClientConfigurationProviderTests" -nodeReuse:false`.
  - Results: Focused combined run passed 25/25 tests.
  - Reviews: Focused code/UI review found and fixed two issues: Web OAuth JSON was accepted by the Google parser, so the provider now requires an `installed` object; Connect success text now says to save settings so users do not mistake browser authorization for persisted Calendar settings.
  - Final verification: `dotnet build MySmallBusinessLedger.sln -nodeReuse:false` passed with 0 warnings and 0 errors; `dotnet test MySmallBusinessLedger.sln -nodeReuse:false` passed with 110/110 tests.
  - Deviations: Build output keeps the `Configuration\google-oauth-client.json` relative folder instead of copying the JSON to output root.
  - Risks/open questions: Stage 6 remains open and requires the real untracked publisher OAuth JSON plus manual browser consent with the intended Google account.
