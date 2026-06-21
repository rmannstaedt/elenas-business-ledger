# UI Design Standards

Use this file when designing, reviewing, implementing, or refactoring user interfaces on any UI stack.

These standards describe how to make product UIs clear, stable, trustworthy, accessible, and pleasant to work in. They are not a visual theme guide and should not be applied mechanically to force unnecessary redesigns.

Project-specific UI mechanics, platform conventions, WPF/MVVM rules, or product behavior documents may add more specific guidance. Project-specific guidance should not contradict these standards unless the exception is intentional and documented.

## Applying these standards

Apply UI standards by consequence, not by taste.

Review should prioritize user trust, correctness, safety, accessibility, state truthfulness, recoverability, task clarity, maintainability, and performance.

Do not block a change merely because it uses a different visual arrangement if the interface remains clear, accessible, consistent, and fit for the workflow.

Distinguish blocking issues that can mislead users, hide risk, prevent access, lose work, or break the workflow contract; important issues that reduce clarity, accessibility, maintainability, performance, or consistency; polish issues; and theme preferences that belong in a project design system.

## Design purpose

Good UI design helps users understand what they are looking at, what state the system is in, what they can safely do now, what will happen next, what already happened, and what requires attention.

The UI should reduce guessing. It should not make the user infer application state from console output, invisible background work, changing button availability, contradictory status text, or implementation artifacts.

Operational tools should feel calm, compact, and work-focused. Prefer dense but readable information, restrained visual styling, predictable navigation, and controls designed for repeated use.

Consumer or exploratory tools may be more spacious or visually expressive, but they must still keep state, action, risk, and feedback clear.

## Product mental model

Start with the user's mental model, not the implementation's storage model.

- Name concepts the way users think about them.
- Prefer task and workflow concepts over implementation objects when the user is trying to complete work.
- Keep implementation details available for diagnostics, but do not make them primary navigation concepts unless they are the user's actual task.
- Keep the user's current object of attention stable across refreshes, selection changes, and background updates.
- Avoid exposing internal topology as unexplained noise.

If the system has multiple runtime actors, the UI should present their relationship in user-relevant terms. It should show enough state to explain availability, ownership, responsibility, progress, and failure without forcing the user to understand implementation architecture.

## Information architecture and navigation

Navigation should match how users understand the task.

- Put the most common and highest-value paths first.
- Keep primary workflows discoverable without requiring knowledge of internal modules, artifact names, database tables, routes, queues, or worker names.
- Prefer stable navigation locations for stable product concepts.
- Avoid hiding safety-relevant views behind generic menus or diagnostics-only screens.
- Do not make the user navigate away from the current task merely to understand current state, validation errors, or progress.

Back, close, cancel, and exit are different actions.

- Back means navigation unless the screen explicitly says it will cancel or discard work.
- Cancel means stop or abandon a specific operation.
- Close means dismiss the current surface, not necessarily stop durable work.
- Exit means leave the application or product context.
- If any of these actions discard data, stop preparation, abandon a workflow, or leave work running, the UI must say so before the action matters.

Long-running durable work should be owned by the backend service or application workflow, not by the lifetime of a window, tab, dialog, or route.

## State and progress

UI state must be stable, bounded, and truthful.

- Represent bounded internal state with enums or dedicated typed models, not strings.
- Derive display text from typed state.
- Do not use human-readable labels as workflow state.
- Treat missing, stale, partial, or out-of-order data as input to the current state, not as permission to regress the display to an earlier state.
- Once the UI reaches a later workflow stage, do not bounce it back to an earlier stage unless the user explicitly navigates back or the workflow truly restarted.
- If data is temporarily unavailable, show a later-stage waiting message rather than reverting to an earlier visual state.
- Keep status aggregation deliberate and tested.

Progress display should be readable before it is precise.

- Avoid blank screens during long-running work.
- Show useful feedback immediately, even if exact totals are not known yet.
- Use indeterminate progress only while no meaningful measurement is available.
- Replace indeterminate progress with measured progress as soon as stable counters exist.
- Do not show misleading estimates. If ETA is not defensible, omit it or say what is still being discovered.
- Distinguish discovery, preparation, execution, verification, finalization, completion, and cleanup when those phases matter to the user.
- Preserve visible progress while new detail arrives; avoid flicker and disappearing text.
- Keep completed summaries visible long enough for the user to read them.

## Workflow screens

Workflow screens should have simple, governed stage transitions.

- Each screen should have one primary purpose.
- Each stage should have a clear heading, current status, primary action, secondary actions, and escape path.
- Do not create extra transient user-facing states merely because the implementation has intermediate tasks.
- Collapse noisy technical states into a smaller set of meaningful user-facing states.
- Show the current stage even while waiting for the first real data.
- Preserve completed stage summaries long enough for the user to read them.
- Do not auto-advance so quickly that important results flash by.
- When a stage has no work to do, state that clearly and explain whether the user can continue, review, or return.

Empty, loading, error, partial, and success states are part of the design. Empty states explain why there is no data and what action is available. Loading states explain what is being loaded or prepared. Error states say what failed, where practical why it failed, and what can be done. Partial success is partial success, not success.

## Controls and layout

Controls should be sized, placed, and named for the task.

- Size controls to their content, frequency, risk, and role.
- Avoid oversized dropdowns, columns, buttons, and empty panels unless they serve a clear scanning, editing, or touch-interaction purpose.
- Use compact controls in operational surfaces when compactness improves repeated use without harming readability or accessibility.
- Keep primary commands visible.
- Keep secondary technical actions in secondary menus, details panes, command palettes, or diagnostics surfaces.
- Use clear command labels with verbs, such as Run, Pause, Resume, Cancel, Review, View details, Apply changes, or Export report.
- Disable unavailable commands only when the unavailable state is understandable or explained.
- Prefer preventing impossible actions over letting users fail late, but do not hide important actions so completely that the user cannot discover how to become eligible.

Page structure should be steady. Avoid layout shifts caused by changing text, late-arriving data, progress bars, validation messages, or control visibility. Reserve stable space for progress, summaries, validation, and activity text when those elements are expected during a workflow. Use visual grouping to clarify relationships, not as decoration.

Preserve useful user-adjusted layout such as column order, widths, splitters, filters, sorting, density, and window size where repeated use benefits from it. Provide a reset path when persisted layout can become confusing.

Desktop applications should preserve window size and position for main windows and subwindows, then restore them inside the current visible screen area so windows are never stranded off-screen after monitor or resolution changes.

## Visual design and design systems

This document does not define a visual theme. It defines interaction and clarity standards.

When a project has a design system, component library, or visual token set, use it before inventing new component variants. Prefer shared spacing, typography, color, elevation, radius, and motion tokens over one-off values. Do not use visual novelty to solve an information architecture problem.

Visual emphasis should match task importance and risk. Destructive actions should be visually and textually distinct from routine actions. Status colors must be supported by text, icons, shape, position, or other non-color cues.

## Status, language, and feedback

Status language should be direct, human-readable, and derived from truth.

- Avoid contradictory messages.
- Avoid vague status such as `estimating` when the system is really waiting, discovering, preparing, comparing, verifying, blocked, retrying, reconnecting, or finalizing.
- Prefer concrete messages such as `Preparing files`, `Waiting for server response`, `No changes found`, `3 items need review`, or `Verification failed`.
- Use calm language for normal waiting and explicit language for risks or failures.
- Keep diagnostic IDs, artifact paths, raw enum names, internal task names, and implementation types out of the primary user path unless they are needed for action.
- Put technical details in details panes, advanced menus, logs, reports, diagnostics, or copyable error detail sections.

Display text should answer the user's next question: what is happening, what is blocking work, what changed, why a command is disabled, which component is being waited on, and what each choice means.

Use consistent vocabulary across navigation, headings, buttons, dialogs, logs, reports, and help text.

## Forms, input, and validation

Forms are workflows, not just collections of fields.

- Group fields by user task and decision, not by database schema.
- Put required, risky, or high-value fields where users will see them before submitting.
- Use labels, hints, examples, and defaults to prevent errors before validation runs.
- Mark required fields clearly.
- Avoid placeholder-only labels.
- Preserve user input after validation errors unless preserving it would be unsafe.
- Do not clear a form after failure.
- Do not silently correct user input when the correction changes meaning; show the correction or ask for confirmation.

Validation should help the user recover. Put field-specific errors near the field and provide a summary when multiple errors matter. Error text should identify the field, describe the problem, and where possible explain how to fix it. Do not rely on color, icons, or borders alone.

## Data tables, lists, and detail views

Data-dense views should support scanning, comparison, and safe action.

- Choose default columns by user importance, not internal model order.
- Use stable identifiers and meaningful names so rows remain recognizable across refreshes.
- Preserve selection, scroll position, sort, filters, expanded groups, and visible columns where practical.
- Do not let late detail loads overwrite the current selection.
- Show row-level status, warnings, and failures where the user makes row-level decisions.
- Keep bulk actions visibly scoped to the selected or filtered set.
- Make dangerous bulk actions disclose count, scope, and consequence before execution.
- Provide copy/export paths for data users may need in reports or diagnostics.

A detail pane should belong to the current selection. If the selection changes, stale detail responses must not replace visible detail.

## Search, filtering, and sorting

Make it clear whether search is local, server-side, exact, fuzzy, case-sensitive, or scoped. Show active filters and provide a clear way to remove them. Preserve filters when returning to a view if the user is likely continuing the same task.

Avoid hiding important warnings solely because a filter is active; provide a visible indication that hidden warnings or failures exist when that matters.

Sorting should be stable and understandable for numbers, dates, versions, severity, status, names, and mixed values. No-results states should distinguish no data, no matches, unavailable data, and errors.

## Safety-critical UI

Safety-critical UI must make risk visible before action.

Deletion, overwrite, replacement, conflict, unverified mode, suspicious batches, trust failures, permission failures, path-safety failures, financial actions, privacy exposure, and irreversible changes require clear user-visible treatment.

Do not hide dangerous actions behind generic success, progress, or completion messages. Copy success and verification success must remain distinct when verification matters. An item or action that is unstable, unverified, skipped, failed, mismatched, unauthorized, or incomplete must not be displayed as successful.

Destructive or irreversible actions require explicit user intent. Defaults should favor inspection and safety over speed when the consequence of mistake is high. Confirmation text should name the actual action and object, not merely ask `Are you sure?`.

## Async and concurrency UX

The UI should show concurrent work as coordinated progress.

- Let the owning backend/application component aggregate state when it coordinates multiple workers.
- The UI should display aggregate state, not reconstruct it by parsing logs or guessing from independent fragments.
- Avoid flicker caused by alternating between partial snapshots.
- Throttle or coalesce updates enough to be readable while still feeling live.
- Late responses should update only the screen, row, or detail pane they still belong to.
- Selection changes must not allow stale detail loads to overwrite the current selection.
- Stale data should be marked stale, refreshing, or temporarily unavailable rather than silently treated as current.

## Accessibility and keyboard use

Accessibility is not a separate polish pass. It is part of making state and action legible.

A good UI should be usable without precise mouse work.

- Provide keyboard navigation for lists, grids, tree views, menus, dialogs, forms, tabs, and primary commands.
- Preserve visible focus.
- Keep tab order predictable.
- Do not trap keyboard focus unless the user is in a modal surface that deliberately owns focus.
- Use accessible names for important controls.
- Ensure custom controls expose appropriate name, role, value, state, and change notifications.
- Avoid relying on color alone for status, warnings, validation, or selection.
- Use readable contrast and text sizes.
- Support text scaling, zoom, high-contrast modes, reduced-motion preferences, and platform accessibility settings where the platform provides them.

## Responsiveness, density, and device fit

Design for the actual environments where the UI will run. Define expected minimum window sizes, viewport widths, zoom levels, and input modes. Support resizing without hiding primary state or primary actions. Prefer responsive reflow over horizontal scrolling for normal content. Virtualize large lists and tables where appropriate.

## Internationalization and localization

Reusable UI standards should assume that text, format, and reading direction may change. Do not hard-code user-facing strings in reusable UI components when the project supports localization. Leave room for translated text to expand. Use locale-appropriate date, time, number, currency, pluralization, and list formatting. Do not use raw internal enum names as localized UI labels.

## Privacy, security, and trust

Trust-sensitive UI must make boundaries visible. Show account, workspace, environment, endpoint, tenant, device, or destination context when an action could affect the wrong scope. Do not display secrets unless necessary. Mask sensitive values by default. Make external links, downloads, uploads, sharing, publishing, permission grants, and data export explicit. Avoid dark patterns.

## Testing and review

Test UI behavior at the level where the risk lives.

Use unit or ViewModel/presenter tests for state transitions, command availability, selection behavior, progress aggregation, display text decisions, and typed-state mapping. Use integration tests when UI state depends on service contracts, refresh timing, routing, authentication, permissions, or runtime topology. Use UI automation or manual checks for critical workflows, layout, blank waiting states, clipping, focus, keyboard navigation, accessibility, and stale/out-of-order update behavior.

Do not consider a UI change complete merely because it compiles. A UI change is complete when its state, interaction, layout, accessibility, and failure behavior have been checked at the right level of risk.

## Lessons to preserve

- Never make the user stare at a blank screen while the system is working.
- Never bounce the display between earlier and later states because refresh data arrived out of order.
- Never let display text become workflow state.
- Never make the user guess whether work is happening.
- Never expose internal artifacts as primary UI unless they are the user's actual task.
- Never show `success` when the system has only copied, queued, estimated, submitted, or partially completed work.
- Never let convenience blur ownership: UI controls and observes; workflow owners execute and aggregate.
- Never make a dangerous action look routine.
- Never rely on color alone to convey meaning.
- Never treat accessibility as optional polish.
