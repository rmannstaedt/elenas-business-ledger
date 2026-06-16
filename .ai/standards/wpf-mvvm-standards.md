# WPF/MVVM Standards

Use this file for WPF projects and WPF-specific changes involving XAML, MVVM, ViewModels, commands, bindings, resources, dispatcher/threading, UI automation, WPF accessibility, or FlaUI tests.

Also read `ui-design-standards.md` for generic UI design rules.

These standards apply only when the project uses WPF. Do not impose WPF/MVVM rules on non-WPF UI stacks.

## UI role

A WPF UI is a control and observation surface.

It may collect user intent, validate and display input, request application workflows, display progress/results/reports/errors, and support navigation and interaction.

It must not own durable backend execution, long-running transfer/import/export/verification/finalization/database migration, service or worker recovery, business policy that belongs in Domain/Application, or concrete Infrastructure implementation choices beyond composition root wiring.

## MVVM boundaries

Default ownership:

- View owns visual structure, bindings, WPF controls, templates, resources, and WPF-specific interaction.
- ViewModel owns presentation state, commands, validation state, selection, filters, sorting, and display models.
- Application owns workflows, use cases, validation policy, and result contracts.
- Infrastructure owns concrete filesystem, database, network, persistence, OS, and service implementations.
- Host/composition root wires dependencies.

ViewModels should depend on Application services and abstractions, not concrete Infrastructure implementations.

Do not put business rules in value converters, code-behind, event handlers, or XAML triggers.

## Code-behind

Code-behind is acceptable for WPF-specific mechanics that are awkward or inappropriate in the ViewModel, such as focus behavior, control-specific event bridging, drag/drop mechanics, window placement, scrolling behavior, visual tree interactions, view-specific keyboard shortcuts, and WPF API interop that should not leak into the ViewModel.

Code-behind should not contain backend execution, business workflow, persistence, service calls, hashing, transfer, finalization, database access, or policy decisions.

Keep code-behind narrow and name helper methods by interaction intent rather than control plumbing.

## ViewModels

ViewModels should be readable presentation models, not mini application services.

A ViewModel may own screen state, command availability, field validation display, selected item and detail state, filter/sort/view options, progress presentation, mapping typed application state to display text, and coordination of application service calls.

A ViewModel should not perform long-running I/O itself, access the filesystem directly except through project-approved UI boundaries such as file picker results, open database contexts, construct concrete infrastructure services, parse logs to infer workflow state, use strings as internal workflow state, or hide partial failure as success.

Keep ViewModels testable without starting WPF when practical.

## Commands

- Use clear verb names: `Run`, `Cancel`, `Save`, `Review`, `Export`, `Refresh`, `Apply`, `Retry`.
- Command availability should be derived from typed state.
- Disabled commands should have an understandable reason in the surrounding UI when the user is likely to need them.
- Commands that trigger long-running work should call application/services asynchronously and expose progress/cancellation state.
- Prevent double-submit or reentrancy unless repeated execution is intentionally supported.
- Dangerous commands must disclose scope and consequence before execution.

## Binding and typed state

- Bind to typed state and explicit display properties.
- Do not make raw enum names, internal task names, artifact names, or implementation type names the user-facing label.
- Use display text derived from typed state.
- Keep binding paths stable and readable.
- Avoid complex business logic in XAML bindings, converters, or triggers.
- Converters should be small, deterministic, UI-specific formatting helpers.
- Binding failures are bugs; do not ignore noisy binding output in workflows under active development.

## Threading and dispatcher use

WPF UI state must be updated on the UI thread.

- Use async commands or equivalent patterns for UI-triggered async work.
- Do not block the UI thread with `.Result`, `.Wait()`, long loops, file I/O, network calls, database calls, sleeps, or synchronous waits.
- Use dispatcher marshaling only at UI boundaries.
- Keep application services UI-framework-agnostic where practical.
- Use cancellation tokens for long-running application calls.
- Throttle progress updates enough to keep the UI responsive.
- Protect against stale or out-of-order responses overwriting current selection or newer state.

## Observable collections and refresh

- Update `ObservableCollection<T>` on the UI thread.
- Prefer replacing snapshots for simpler refresh flows when item identity does not matter.
- Preserve selection, scroll position, expanded state, filters, and sort when the user is likely continuing the same task.
- Use stable row identifiers where background refresh updates a list.
- Virtualize large lists and grids.
- Do not let late detail loads overwrite the current detail pane after selection changes.

## Validation

Use WPF validation mechanisms intentionally: `INotifyDataErrorInfo`, validation rules, or project-approved validation patterns. Keep business validation in Application/Domain when it is not purely presentation-level. Surface field-level errors near the field and provide summaries when multiple errors matter. Preserve user input after validation failure unless unsafe. Do not silently correct user input when correction changes meaning.

## Navigation, windows, and dialogs

Define whether close dismisses a surface, navigates back, cancels work, or exits the application. Do not use window lifetime as backend job lifetime unless the project explicitly defines that contract. Dialogs should be proportional to consequence. Avoid modal dialogs for routine information that can be inline. Keep ownership of navigation state explicit.

## Resources, styles, and templates

Use project design resources before introducing one-off styling. Prefer shared styles/templates for repeated controls. Keep resource names meaningful and scoped. Do not encode product behavior in styles in a way that hides workflow logic. Avoid fragile visual tree assumptions. Preserve readability of XAML; split resources when files become hard to scan.

## Accessibility

WPF accessibility is part of correctness for important workflows.

- Provide accessible names for important controls.
- Preserve visible focus.
- Keep tab order predictable.
- Support keyboard navigation for lists, grids, trees, menus, dialogs, and primary commands.
- Do not rely on color alone for status, warnings, validation, or selection.
- Ensure custom controls expose appropriate automation name, role, value, state, and change notifications.
- Test high DPI, text scaling, and high-contrast modes when the project profile requires them.
- Use automation properties intentionally when FlaUI or assistive technology needs stable identifiers.

## Performance

Keep UI thread work short. Virtualize large lists/grids. Avoid unnecessary property change storms. Coalesce high-frequency progress updates. Load expensive detail on demand while preserving layout and context. Avoid repeated re-rendering that causes flicker, lost focus, lost selection, or scroll jumps. Use asynchronous data loading with cancellation/versioning to prevent stale updates.

## UI automation and FlaUI

When the project uses FlaUI or similar UI automation:

- Prefer stable automation ids for important screens and controls.
- Test high-value user workflows rather than brittle visual details.
- Keep UI automation tests few, meaningful, and resilient.
- Use ViewModel tests for state logic where possible; reserve FlaUI for integration paths that need real WPF behavior.
- Avoid tests that depend on exact timing, incidental control order, or minor styling.
- When timing matters, use explicit wait conditions tied to state, not fixed sleeps.

## Testing posture

Prefer tests in this order when practical:

1. Pure unit tests for Domain/Application logic.
2. ViewModel tests for state transitions, command availability, validation display, selection safety, and progress mapping.
3. Integration tests for service/application/UI contract boundaries.
4. FlaUI tests for critical end-to-end WPF workflows.
5. Visual/manual checks for layout, accessibility, focus, high DPI, and perceived performance.

Do not consider a WPF change complete merely because it compiles. Check state, interaction, layout, accessibility, and failure behavior at the level of risk.

## Review checklist

Before accepting a WPF change, ask:

- Did ViewModels stay focused on presentation state and application coordination?
- Did code-behind stay limited to WPF mechanics?
- Are bounded states typed rather than strings?
- Can stale async results overwrite current UI state?
- Are commands reentrancy-safe?
- Does the UI remain responsive during work?
- Are progress, partial success, cancellation, and failure represented truthfully?
- Are binding errors avoided?
- Are selection, filters, sort, and layout preserved where useful?
- Can important workflows be used with the keyboard?
- Are automation ids stable where UI tests or accessibility need them?
