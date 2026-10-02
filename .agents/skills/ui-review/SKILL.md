---
name: ui-review
description: Review UI, UX, accessibility, state truthfulness, progress, command availability, layout stability, WPF/MVVM boundaries, and user-visible behavior. Use for UI changes in any stack; apply WPF-specific checks only when relevant.
---

# UI Review

Use this skill to review user interfaces for clarity, truthfulness, accessibility, workflow safety, responsiveness, maintainability, and user-visible behavior.

This skill applies to any UI stack. Apply WPF/MVVM-specific checks only when the project profile or source code indicates WPF.

Do not edit UI code unless the user explicitly asks for fixes.

## Purpose

Determine whether the UI helps users understand:

- what they are looking at;
- what state the system is in;
- what they can safely do now;
- what will happen next;
- what already happened;
- what requires attention;
- what failed, was cancelled, was skipped, is partial, or remains unverified.

UI review should prioritize user trust, correctness, accessibility, state truthfulness, recoverability, task clarity, maintainability, and performance. Do not block a change merely because it uses a different visual arrangement if the interface remains clear, accessible, consistent, and fit for the workflow.

## When To Use

Use this skill when changes touch:

- UI screens, dialogs, forms, navigation, layout, grids, lists, detail panes, menus, notifications, or settings;
- ViewModels, commands, binding models, validation display, progress display, selection, filtering, sorting, or state aggregation;
- WPF XAML, code-behind, resources, styles, converters, templates, dispatcher/threading, or FlaUI tests;
- safety-critical UI such as deletion, overwrite, export, permission changes, environment/tenant/account selection, trust boundary crossing, or irreversible work;
- background progress, cancellation, retry, partial success, service/worker state, or runtime availability;
- accessibility, keyboard use, focus, high contrast, text scaling, localization, or screen reader behavior.

Do not use this skill for purely backend changes unless they affect user-visible behavior.

## Read First

Read the relevant subset of:

- `AGENTS.md`;
- `README.md`;
- `.ai/project-profile.md`, if present;
- relevant UI docs under `docs/`;
- `.ai/standards/ui-design-standards.md`;
- `.ai/standards/wpf-mvvm-standards.md` for WPF/MVVM work;
- `.ai/standards/coding-standards.md` when ViewModels, commands, async, cancellation, typed state, or binding contracts are involved;
- `.ai/standards/architecture-principles.md` when UI ownership or backend execution boundaries matter;
- `.ai/standards/security-and-boundaries.md` when trust, secrets, privacy, environment, account, tenant, export, or permission scope is visible;
- `.ai/standards/service-worker-and-job-design.md` when UI controls/observes services, workers, jobs, schedulers, or background work;
- relevant Views, ViewModels, commands, services, display models, resources, converters, styles, tests, and docs;
- current git diff/status when available.

## Core Checks

Check whether:

- the screen explains what is happening now;
- empty, loading, waiting, error, partial-success, cancelled, skipped, blocked, and success states are designed;
- bounded workflow state is typed internally and display text is derived from state;
- display text is not used as workflow state;
- late, stale, partial, or out-of-order refresh data cannot regress visible state or overwrite the current selection;
- progress is useful before it is precise and does not mislead;
- copy, queue acceptance, save, submit, verification, finalization, and true success remain distinct where the distinction matters;
- dangerous or irreversible work is visible before action;
- primary and secondary actions are clear;
- disabled commands have discoverable reasons;
- selection, scroll position, sort, filters, expanded groups, and useful layout preferences are preserved where practical;
- forms are labeled, validated, recoverable, and do not silently lose input;
- status language is concrete and not contradictory;
- technical IDs, internal enum names, artifact paths, raw exceptions, and implementation details stay out of the primary user path unless needed for action;
- errors say what failed, what can be done, and where detail is available;
- privacy, account, environment, tenant, destination, and trust boundaries are visible where relevant;
- the UI remains responsive during background work;
- large lists/tables are virtualized or otherwise bounded where necessary;
- layout survives expected window sizes, zoom levels, density, localization, and text expansion;
- accessibility, keyboard navigation, focus, accessible names, contrast, target size, non-color cues, and platform accessibility settings are adequate.

## WPF/MVVM Checks

For WPF projects, check whether:

- MVVM boundaries are respected;
- ViewModels own UI state, commands, validation state, filters, selection, and display models;
- Application/services own workflow and backend policy;
- Infrastructure owns concrete filesystem, persistence, database, network, OS, external-process, and report implementations;
- code-behind is limited to WPF-specific view mechanics;
- ViewModels do not perform backend execution, durable jobs, transfer, hashing, verification, finalization, database migrations, or long-running infrastructure work;
- bindings are stable and not stringly typed where typed state is needed;
- editable controls distinguish user input from framework synchronization: audit binding modes/update triggers, `ComboBox.Text`, `SelectedItem`/`SelectedValue`, `IsSynchronizedWithCurrentItem`, default `ICollectionView` current position, `ItemsSource` replacement, and `DataContext` changes;
- reusable item sources, collection views, dialogs, or ViewModels do not carry selection/current-item state from one record into another;
- programmatic model loading, record navigation, window reuse, and window reopening cannot execute an edit path or make the form dirty without user input;
- deferred dispatcher callbacks are cancelled, versioned, or identity-guarded so work scheduled for an old record cannot mutate the current one;
- `INotifyPropertyChanged`, `INotifyCollectionChanged`, commands, validation, and dispatcher/threading behavior are correct;
- collection updates occur on the correct thread or through safe synchronization;
- command availability updates when state changes;
- cancellation, progress, and disabled reasons are visible;
- styles/templates/resources do not create brittle visual coupling or inaccessible controls;
- FlaUI or other UI automation targets stable user-visible behavior rather than fragile layout details.

## Safety-Critical UI Checks

For destructive, irreversible, privileged, privacy-sensitive, financial, security-sensitive, file-sensitive, or data-moving workflows, check whether:

- action scope, count, affected object, account/environment/tenant/destination, and consequence are visible before action;
- confirmation text names the actual action and object rather than only asking “Are you sure?”;
- defaults favor inspection and safety over speed when mistakes are costly;
- partial, failed, skipped, cancelled, unverified, unauthorized, unstable, or incomplete states are not displayed as success;
- warnings include concrete reason and affected scope where practical;
- recovery, undo, rollback, restore, report, or audit path is available when the risk model requires it.

## Testing and Verification

Review whether UI behavior is tested at the right level:

- ViewModel tests for state transitions, command availability, validation, selection behavior, progress aggregation, display text mapping, and stale update handling;
- integration tests for UI state that depends on services, refresh timing, authentication, permissions, routing, IPC, database state, or runtime topology;
- UI automation for critical workflows where stable enough;
- visual/screenshot/manual checks for layout, blank waiting states, clipping, overflow, focus, keyboard operation, high contrast, localization/text expansion, and accessibility risk;
- cancellation, repeated runs, refresh, selection changes, and interrupted work.

For state leakage or unintended edits, test the lifecycle that can produce the stale state, not only a freshly opened target record. At minimum, cover: edit record A, save it, open record B, verify untouched fields, close/discard without saving, reopen record B, and verify again. Add other record transitions that exercise shared controls or item sources. Confirm both visible values and the persisted model, and confirm that a programmatic selection/current-item change does not create user intent.

If that sequence is not covered by reliable automation, record it as a mandatory manual acceptance test. A successful build, XAML/source inspection, or isolated binding assertion does not verify the user-visible fix.

Do not consider a UI change complete merely because it compiles.

## Severity Levels

Use:

- **Critical** — UI can cause likely data loss, security/privacy exposure, destructive action confusion, false success, or inaccessible core workflow.
- **High** — UI can mislead users about state, risk, scope, failure, progress, verification, authorization, or command consequences; serious MVVM boundary violation; severe responsiveness/accessibility issue.
- **Medium** — maintainability, clarity, testability, accessibility, layout, state stability, command discoverability, or workflow recovery issue that should be addressed soon.
- **Low** — minor polish, wording, spacing, naming, or consistency issue that does not currently threaten clarity, accessibility, safety, or maintainability.

Do not inflate cosmetic issues.

## Output Format

Report:

1. Overall UI verdict.
2. Findings grouped by severity.
3. For each finding: severity, location/screen/symbol, problem, evidence/scenario, user impact, suggested fix, and whether it is a state, accessibility, layout, wording, MVVM, performance, safety, or test issue.
4. Areas inspected.
5. Areas not inspected.
6. Verification performed or still recommended.
7. Whether `code-review`, `architecture-review`, `refactor-planning`, or `documentation-refresh` is warranted.
8. Open questions.

If no important UI issues are found, say so and list remaining verification gaps.
