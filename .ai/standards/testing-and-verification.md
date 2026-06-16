# Testing and Verification Standards

Use this file when adding, changing, reviewing, or selecting tests, build commands, verification commands, analyzers, formatters, CI checks, UI automation, performance checks, or manual validation.

The goal is confidence, not ritual. Prefer focused verification that catches real regressions over broad tooling that creates noise.

## Recommended default posture

Unless the project profile says otherwise:

- Use xUnit for unit and integration tests.
- Use FlaUI for WPF UI automation only where real UI behavior must be tested end-to-end.
- Use EditorConfig for formatting and basic style consistency.
- Keep nullable enabled where practical.
- Prefer compiler warnings, nullable analysis, targeted analyzers, and meaningful tests over broad style gates.
- Do not require StyleCop, SonarQube, or similar broad analyzer gates by default.
- Treat external analyzers as advisory unless the project profile makes them required.

This is intentionally high-signal. Tools should help maintain readability, maintainability, correctness, and performance. They should not turn harmless style preferences into blocking work.

## Test levels

Use the lowest test level that proves the behavior.

Use unit tests for domain rules, validation, parsing/formatting, result models, state transitions, error mapping, deterministic calculations, and ViewModel command/state behavior.

Use integration tests for behavior crossing important boundaries: filesystem, database, serialization format, HTTP client/server contract, service/worker coordination, dependency injection composition, migrations, import/export, and artifact generation.

Use scenario/end-to-end tests sparingly for high-value workflows where confidence requires multiple layers to work together.

Use UI automation for critical WPF workflows, navigation and command wiring, accessibility/automation identifiers, and high-risk user-visible regressions. Avoid using UI automation for every ViewModel state transition.

## What to test

Add or update tests for non-trivial behavior changes, bug fixes, parsing and validation, data transformation, serialization and migration, retry/resume/cancellation behavior, failure and partial failure paths, authorization/path/trust boundaries, destructive operation safeguards, state machine transitions, public contracts, concurrency behavior where risk is meaningful, and UI state regressions that have happened before.

Do not rely on build success alone for safety-sensitive changes.

## Test quality

Good tests are readable, focused, and behavior-oriented.

- Prefer observable behavior over implementation details.
- Use clear test names that describe the scenario and expected outcome.
- Keep arrange/act/assert obvious.
- Avoid excessive mocking when it hides behavior or makes tests brittle.
- Prefer fakes or in-memory adapters when they express the behavior more clearly.
- Use real filesystem/database/provider tests when provider behavior matters.
- Use characterization tests before refactoring risky legacy behavior.
- Keep fixtures deterministic and isolated.
- Avoid sleeps in tests; use controllable time, explicit signals, or wait conditions.
- Do not make tests depend on local machine paths, user profile data, clock time, culture, network access, or test order unless explicitly intended.

## xUnit guidance

When using xUnit:

- Keep test classes focused by subject or behavior area.
- Use `Theory` for meaningful input variations, not for hiding unrelated cases.
- Use fixtures for expensive shared setup only when isolation remains clear.
- Avoid sharing mutable state across tests.
- Use temporary directories unique to each test or test class where filesystem tests need files.
- Use `IAsyncLifetime` for async setup/cleanup when appropriate.
- Make assertions specific enough to diagnose failure.

## FlaUI guidance for WPF

When using FlaUI:

- Prefer testing critical workflows, not incidental layout.
- Use stable automation ids.
- Wait for observable UI state, not fixed delays.
- Keep tests resilient to harmless visual changes.
- Avoid asserting exact coordinates, incidental control order, or theme details.
- Run UI tests separately from fast unit tests when they are slow or environment-sensitive.
- Capture useful diagnostics on failure when practical: screenshots, window title/state, key visible text, and logs.

## Build and command verification

The project profile should define standard commands. In the absence of project-specific commands, use the narrowest useful command.

Examples:

```powershell
dotnet build <solution-or-project> -nodeReuse:false
dotnet test <solution-or-project> -nodeReuse:false
dotnet format <solution-or-project> --verify-no-changes
```

Rules:

- Run relevant tests after meaningful source changes when possible.
- Run project/solution build after project-file or broad source changes when possible.
- Documentation-only changes do not require a build unless source/project files or generated docs changed.
- Do not claim verification passed unless it was actually run.
- If a command fails because of missing SDK, unavailable NuGet/network, locked files, environment restrictions, or OS constraints, report that clearly.
- Do not retarget the project merely to satisfy the local environment.
- Prefer `-nodeReuse:false` for command-line builds/tests to avoid leaving idle MSBuild worker processes behind.

## Analyzer and formatter policy

Use analyzers to catch real issues. Avoid analyzer noise.

Recommended baseline:

- EditorConfig for formatting and common conventions.
- Nullable reference types where practical.
- Compiler warnings treated seriously.
- Built-in .NET analyzers where they produce high-signal warnings.
- Security/package vulnerability checks when the project profile requires them.

Avoid by default:

- broad style-only gates that block work without improving clarity or correctness;
- analyzer rules that fight the project's intended architecture;
- rules that require noisy suppression comments;
- mandatory SonarQube/StyleCop gates without a project decision.

When enabling an analyzer rule as blocking, it should catch real bugs or security issues, prevent maintainability problems already seen in the project, enforce an architectural boundary, protect performance for realistic workloads, improve accessibility/user-visible correctness, or have a low false-positive rate.

Suppression should be explicit and justified when the warning is meaningful. Do not scatter suppressions to satisfy tooling without understanding the issue.

## Performance verification

Performance work should be evidence-driven.

Avoid premature micro-optimization. Measure realistic workloads when performance is the reason for a change. Watch for unbounded memory use, unbounded concurrency, UI-thread blocking, repeated full scans, N+1 database queries, and unnecessary large allocations. For UI performance, inspect perceived responsiveness, flicker, focus loss, selection loss, and progress smoothness.

## Manual verification

Manual verification is useful when automated tests are too expensive or not yet available, especially for UI, installer, service lifecycle, permissions, and accessibility.

When relying on manual verification, record environment, steps performed, expected behavior, observed behavior, and remaining unverified risks.

Do not describe manual inspection as automated test coverage.

## Verification summary

Every completed coding task should summarize build command run or skipped, test command run or skipped, format/analyzer command run or skipped when relevant, manual verification performed when relevant, why any expected verification could not be run, and what remains unverified.
