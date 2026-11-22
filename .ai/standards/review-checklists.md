# Review Checklists

Use this file as a compact review aid. It does not replace the detailed standards.

Apply checklists by consequence. Do not turn advisory style preferences into blocking review comments.

## Severity triage

Block or escalate when the change can:

- produce false success;
- hide partial failure;
- corrupt, lose, or silently discard user data or durable state;
- apply invoiced, paid, completed, issued, printed, reconciled, finalized, deleted, tax, or audit state to the wrong durable entity;
- bypass security, authorization, path, permission, or trust boundaries;
- leak secrets or unnecessary sensitive data;
- perform destructive operations without explicit intent and appropriate recovery/confirmation;
- move responsibilities across architecture boundaries in a way that makes future changes unsafe;
- make cancellation, retry, resume, shutdown, or recovery dishonest;
- materially regress user-visible behavior or accessibility;
- create unbounded memory, I/O, CPU, concurrency, or UI-thread work.

Treat as important but usually non-blocking: readability and naming issues that affect maintainability, avoidable duplication, weak tests for changed behavior, insufficient logging context, minor layering pressure, and plausible performance concerns.

Treat as advisory: cosmetic preferences, style nits covered by formatting, low-value analyzer suggestions, and visual polish unrelated to clarity/accessibility/safety/workflow.

## General code review

- Is the change scoped to the task?
- Are names accurate about behavior and guarantees?
- Do added or changed types and public methods follow the [commenting guidance](coding-standards.md#comments-and-xml-documentation)?
- Do affected comments and data-flow descriptions still match the implementation?
- Were concrete SOLID issues found during the work reported to the maintainer with the type, principle, and practical impact?
- Are contracts explicit about success, failure, partial success, cancellation, and unsupported input?
- Are bounded states typed rather than internal strings?
- Are nullability annotations honest?
- Are external inputs validated at the boundary?
- Are exceptions handled without hiding failure?
- Are logs useful without leaking sensitive data?
- Are dependencies justified?
- Are tests focused on changed behavior?
- Were relevant build/test commands run or clearly reported as not run?

## Architecture review

- Does each responsibility live in the right layer/component?
- Did Domain/Core stay free of UI, Infrastructure, database, OS, transport, logging, and DI concerns?
- Did Application avoid concrete Infrastructure/Presentation details?
- Did Infrastructure avoid owning business workflow decisions?
- Did Presentation avoid durable backend execution?
- Did executable hosts stay focused on composition and host lifecycle?
- Did a new abstraction improve clarity, safety, testability, or duplication?
- Did decomposition improve cohesion or scatter one concept across too many collaborators?
- Did the runtime authority/trust boundary remain explicit?

## Persistence and artifacts

- Are JSON/JSONL/log/report formats appropriate and inspectable?
- Is there a version field where migration is plausible?
- Are serialization settings treated as part of the contract?
- Are durable references represented by stable identities rather than display text, filenames, dates, names, or amounts?
- Are snapshots and references both preserved when old output must stay historically accurate?
- Can readers mistake incomplete files for complete files?
- Are writes atomic/durable enough for the risk?
- Are failure artifacts preserved?
- Does cleanup avoid deleting recovery/audit/support evidence?
- Are paths and sensitive values redacted where needed?

## Database

- Is the project actually database-bound?
- Is data access in the owning layer?
- Are durable business relationships represented by stable IDs, foreign keys, unique constraints, or documented/enforced natural keys?
- Can two plausible rows with the same descriptive values remain distinct through queries, migrations, and read models?
- Are migrations reviewed and non-destructive unless approved?
- Are transaction boundaries explicit?
- Are queries parameterized?
- Are result sets bounded or streamed where needed?
- Are concurrency conflicts first-class outcomes?
- Are credentials protected?
- Were migration/data-access tests run or skipped with explanation?

## Services, workers, and jobs

- Who owns the job and durable state?
- Can a restart recover or truthfully mark in-progress work?
- Are cancellation, timeout, shutdown, retry, and failure distinct?
- Are retries bounded and idempotent or otherwise replay-safe?
- Can work be claimed or executed twice unsafely?
- Can incomplete staged work be processed or published?
- Is finalization blocked until required verification succeeds?
- Are progress and terminal results truthful?
- Are malformed or unauthorized work items rejected safely?

## UI

- Does the screen explain what is happening now?
- Does it avoid blank or misleading waiting states?
- Are workflow states bounded, typed, and stable?
- Can late data regress the UI, flicker, overwrite selection, or show stale detail?
- Are empty, loading, error, partial-success, and success states designed?
- Are disabled commands understandable?
- Is dangerous work clearly visible before execution?
- Are copy, verification, and final success distinct where they matter?
- Is keyboard navigation adequate for important workflows?
- Are focus, accessible names, contrast, target size, and non-color cues adequate?
- Does the layout survive expected window sizes, zoom levels, density, localization, and text expansion?

## WPF/MVVM

- Did ViewModels stay focused on presentation state and application coordination?
- Did code-behind stay limited to WPF mechanics?
- Are bindings simple and reliable?
- Are commands derived from typed state?
- Can stale async results overwrite current UI state?
- Are UI updates performed on the UI thread?
- Are large lists virtualized?
- Are automation ids stable where UI tests or accessibility need them?
- Are ViewModel tests used instead of brittle UI automation where appropriate?

## Security and boundaries

- What trust boundary is crossed?
- Is the caller/user/process authenticated and authorized where needed?
- Are external inputs parsed and validated at the boundary?
- Are paths normalized, scoped, and revalidated before dangerous use?
- Are secrets kept out of source, logs, reports, telemetry, and UI by default?
- Does the change fail closed for unknown safety-affecting values?
- Can the change execute arbitrary commands or access arbitrary paths?
- Are destructive operations scoped and explicit?
- Is cryptographic language accurate about what is proven?

## Final response checklist

When summarizing work, include what changed, why it changed, verification performed, verification skipped and why, and known risks or follow-up work.

Never claim that a build, test, analyzer, migration, runtime scenario, or manual verification passed unless it was actually performed.
