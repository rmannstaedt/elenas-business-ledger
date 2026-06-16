# C# Coding Standards

Use this file when writing, reviewing, or refactoring C# source code, tests, comments, and implementation-adjacent documentation.

These standards are reusable across C#/.NET projects. They describe expectations for correctness, safety, maintainability, observability, performance, and future change.

## Applying these standards

Apply these standards by consequence, not mechanically.

Do not apply platform-, UI-, storage-, network-, database-, service-, or domain-specific rules outside their relevant scope.

Correctness, safety, data integrity, truthful state, cancellation semantics, security boundaries, and user-visible behavior take priority over style preferences.

A change should be blocked when it can produce false success, hide partial failure, lose diagnostic information, corrupt durable state, bypass path or trust boundaries, leak operation state across jobs/users/tests, expose sensitive data, or make retry/resume/cancellation dishonest.

Mechanical style issues should be handled by formatting, EditorConfig, targeted analyzers, or focused cleanup. Do not mix broad stylistic rewrites into unrelated correctness changes.

## Platform and framework

- Use the project profile's approved SDK and target framework.
- Prefer the current approved .NET LTS/STS target for new projects unless the project profile says otherwise.
- Existing projects should keep their approved target unless retargeting is explicitly part of the task.
- Use Windows-specific TFMs such as `netX.0-windows` only for projects that require Windows APIs or Windows UI frameworks.
- Keep reusable libraries on portable TFMs where practical.
- Keep nullable enabled for new projects and new code unless the project profile says otherwise.
- Treat nullable warnings as meaningful contract signals.
- Do not silence nullable warnings without understanding the underlying contract.
- Prefer current .NET APIs and project conventions.
- Do not add, remove, or upgrade production dependencies without explaining why they are needed and what alternatives were considered.
- If the required SDK is unavailable, report that clearly. Do not retarget or downgrade the project merely to pass a local build.

## Code shape

- Prefer classes, methods, and modules with one clear reason to change.
- Prefer cohesive design over both god-classes and excessive micro-class fragmentation.
- Prefer composition over inheritance unless inheritance expresses a real and stable `is-a` relationship.
- Keep interfaces narrow, meaningful, and use-case focused.
- Add abstractions only when they clarify ownership, isolate dependencies, protect a policy/detail boundary, create a meaningful substitution point, or remove real duplication.
- Do not add interfaces, factories, base classes, or helper layers merely because a pattern suggests them.
- Preserve existing patterns unless there is a clear correctness, safety, testability, maintainability, performance, or ownership reason to change them.
- Avoid boolean mode flags, broad optional-parameter sets, or vague `Manager`/`Helper` objects when they cause one method or class to perform several different jobs.
- Keep orchestration, validation, persistence, transformation, reporting, UI mechanics, and side effects separated when mixing them would obscure ownership or make testing fragile.
- Keep code easy to read, reason about, and change correctly.

## Names, contracts, and result models

- Names should accurately describe behavior and guarantees.
- Avoid names that overpromise, such as `Verify`, `Ensure`, `Commit`, `Finalize`, `Safe`, `Atomic`, or `Complete`, unless the implementation actually provides that guarantee.
- Prefer explicit result models over ambiguous boolean return values when multiple outcomes matter.
- Distinguish success, partial success, failure, cancellation, skipped work, validation failure, unauthorized, conflict, retry-exhausted, and not-found cases where those states matter.
- Contracts should define what happens for null, empty, missing, duplicate, malformed, unsupported, unknown, or future values.
- Failure information should be detailed enough for later diagnosis.
- DTOs and result models must preserve information needed by later stages.
- Multiple implementations of the same interface must provide compatible behavior.
- If an operation may be retried, resumed, repeated, cancelled, or partially completed, the contract should make those semantics clear.

## Mutability and public API shape

- Prefer immutable result models and value objects unless mutation is part of the explicit responsibility.
- Do not expose mutable collections from public contracts unless callers are intended to mutate them.
- Prefer `IReadOnlyList<T>`, `IReadOnlyDictionary<TKey,TValue>`, immutable collections, or defensive copies as appropriate.
- Records are appropriate for immutable data and value-like results.
- Classes are preferred when identity, lifecycle, resource ownership, or controlled mutation matters.
- DTOs used at serialization, UI, database, or transport boundaries should not be mistaken for validated domain models.
- Convert boundary DTOs into validated application/domain types before safety-sensitive use.

## Bounded signals, enums, and state

- Use enums, discriminated result models, or dedicated value types for bounded signals: status, kind, mode, role, phase, severity, action, direction, policy, and result category.
- Do not represent bounded internal signals as strings.
- Do not compare bounded states using string literals in domain, application, service, or presentation logic.
- Human-readable display text should be derived from typed state, not used as state itself.
- Serialized JSON, logs, CLI text, database rows, and UI labels may contain strings, but parsing/formatting must happen at the boundary.
- Persisted or wire-format enum strings must be parsed intentionally. Unknown safety-affecting values must fail closed.
- Do not let `null`, empty string, missing fields, or `default(TEnum)` silently mean a valid business state unless the contract explicitly defines that meaning.
- If an enum can be default-constructed accidentally, define an explicit `Unspecified`/`Unknown` guard value or require explicit construction and test that default values cannot leak into behavior.
- State transitions should be explicit for workflow, job, task, endpoint, database migration, and UI state machines.

## Strings, culture, and time

- Use explicit `StringComparison` or `StringComparer` values for non-trivial string matching, lookup, sorting, and prefix/suffix checks.
- Use ordinal comparisons for non-linguistic tokens such as IDs, JSON names, enum text, hashes, protocol values, internal keys, and safety-sensitive boundary checks.
- Use current-culture behavior for user-visible linguistic sorting and display.
- Use invariant culture for durable machine-readable formatting/parsing.
- Use user culture for UI display.
- Prefer UTC or `DateTimeOffset` for durable timestamps.
- Do not use local time as durable workflow state unless the contract explicitly requires it.
- Use `TimeProvider` or an equivalent abstraction for time-dependent logic that must be tested deterministically.

## Nullability and validation

- Use nullable annotations honestly.
- Validate external input at boundaries.
- Validate internal invariants where failure would otherwise create misleading state.
- Prefer guard clauses for invalid inputs when they clarify flow.
- Do not use `null`, empty strings, default enum values, or missing values as hidden control signals unless the contract explicitly defines them.
- Handle unknown or future enum values safely.
- Treat raw string status/kind/mode values from JSON, HTTP, CLI, files, database rows, or external sources as untrusted until parsed.

## Async, cancellation, and long-running work

- Async code must preserve truthful completion, failure, cancellation, exception observation, resource disposal, and synchronization-context behavior.
- Avoid blocking on asynchronous work with `.Result`, `.Wait()`, or equivalent patterns.
- Avoid fire-and-forget work unless its lifetime, error handling, and cancellation behavior are explicit.
- Use cancellation tokens for long-running I/O, network, database, import/export, scan, hash, copy, verification, finalization, service, worker, and UI-triggered operations.
- Pass cancellation tokens through relevant call chains.
- Cancellation should leave durable state truthful and recoverable where durable state exists.
- Do not report success for cancelled, partially completed, or unverified work.
- Avoid unobserved tasks and swallowed background exceptions.
- Async methods should return `Task`, `Task<T>`, `ValueTask`, `ValueTask<T>`, or `IAsyncEnumerable<T>` as appropriate. Use `async void` only for event handlers.
- Do not use `Task.Run` to hide blocking I/O inside application services. Prefer true async APIs or make blocking behavior explicit at the boundary.
- General-purpose, app-model-agnostic library code should avoid capturing synchronization context unless the contract requires it.
- UI code should preserve or explicitly return to the UI context when updating UI state.
- Timeout, cancellation, retry, and shutdown are related but distinct concerns. Do not represent all of them as generic failure.

## Concurrency and thread safety

- Make thread safety explicit. A type that is safe for concurrent use should say so in its contract; otherwise callers should assume instances are not thread-safe.
- Avoid shared mutable state.
- Prefer immutable snapshots, per-operation state, channels, queues, or explicit synchronization when work crosses threads or tasks.
- Do not use unbounded concurrency for file, network, hashing, verification, database, UI refresh, or worker operations.
- Concurrency limits should reflect resource ownership and failure behavior.
- Do not hold locks across `await`.
- Background work must have explicit ownership, cancellation, exception observation, and shutdown behavior.
- Progress and UI refresh events should be ordered, throttled, or versioned when stale or out-of-order updates could regress visible state.

## Dependency injection and service lifetimes

- Use dependency injection as the normal composition mechanism when the project uses DI.
- Constructor injection is preferred for required dependencies.
- Large DI constructors are design signals, not automatic findings.
- A large constructor is acceptable when dependencies are coherent, stable, at the right abstraction level, and aligned with one clear responsibility.
- A large constructor is suspicious when dependencies span unrelated concerns, reveal hidden orchestration, or make the class difficult to test meaningfully.
- Avoid captive scoped services, unintended singletons, or services holding mutable operation state.
- Prefer stateless services unless state is part of the service's explicit responsibility.
- Operation-specific state must not leak between calls, jobs, sessions, requests, users, tenants, tests, or UI screens.
- Register services with lifetimes that match their state, resource ownership, and intended usage.

## Resource, file, path, and stream handling

- Use streaming APIs for large file hashing, copying, comparison, parsing, and verification.
- Do not load unbounded or user-supplied file payloads into memory in production paths.
- Small, explicitly bounded metadata files and test fixtures may be read into memory when doing so is simpler and safe.
- Treat external resources as fallible and mutable unless the contract proves otherwise.
- Handle missing files, missing directories, already-existing paths, permission-denied cases, locked files, long paths, transient I/O failures, and partial writes where relevant.
- Consider casing, symlinks, junctions, reparse points, and network-share behavior where relevant to safety.
- Capture file size and modified timestamp before and after reads where file stability matters.
- Treat files that change during scan, hash, transfer, import, or verification as unstable and not verified.
- Dispose streams, file handles, locks, timers, transactions, and other resources correctly.
- Make ownership of streams and disposable objects clear.
- Normalize and validate paths before safety-sensitive use.
- Do not allow path traversal, unsafe relative paths, unexpected root changes, or bypass of allowlists/restricted roots.

## Verification and finalization

These rules apply when a workflow distinguishes attempted, copied, queued, verified, committed, published, or finalized work.

- Keep those states distinct.
- Do not report verified, committed, finalized, published, or successful work unless the contractually required proof actually happened.
- Copy completion, queue acceptance, API acceptance, database write success, or process exit does not automatically prove end-to-end success unless the contract explicitly says so.
- Hash/checksum algorithm choice must be explicit where safety depends on it.
- Hash/checksum contracts must state what was proven.
- Destination-local verification must occur before finalization where required by the workflow.
- Source-changed-during-read cases must not create false success.
- Staged payloads must not be finalized before required payload and metadata are durable.
- Ready markers must not make incomplete work appear complete.
- Finalization must not hide partial failure.
- Retry, resume, cancellation, and shutdown must preserve truthful durable state.
- A hash proves only the property stated by the contract. A content hash can prove that two byte streams match the hash input. It does not prove authenticity unless protected by a trusted channel, signature, HMAC, or equivalent trust mechanism.
- Use SHA-256 or stronger for safety-relevant content hashing unless a compatibility contract requires otherwise.
- Do not use MD5 or SHA-1 for safety or security decisions.

## Errors, logging, and privacy

- Prefer explicit failure reporting over silent success.
- Do not swallow exceptions in ways that hide failure or produce false success.
- Avoid overly broad catch blocks unless they add useful context and preserve truthful outcome reporting.
- Record per-item errors where practical.
- Do not allow one per-item failure to abort an entire job unless the failure is structurally blocking.
- Keep error/result artifacts inspectable when durable operations are involved.
- Include enough diagnostic context to understand what failed, where, and why.
- Logs and reports should not contradict durable result artifacts.
- Partial success must be represented as partial success, not success.
- Use structured logs for operation id, request id, correlation id, user/account/tenant id, resource id, job id, path/URI, operation, phase, attempt, duration, size, algorithm, and result category where relevant.
- Do not log secrets, credentials, access tokens, private keys, recovery codes, or file contents.
- Treat file paths and file names as potentially sensitive.

## Serialization and durable formats

- Use JSON for durable machine-readable state when a stable, inspectable format is needed.
- Use JSON Lines for appendable event streams or high-volume per-item records.
- Use Markdown or HTML for human-readable reports, with machine-readable JSON alongside when later automation matters.
- Durable artifacts should include a schema or format version when future migration is plausible.
- Parsing of durable or external JSON should be strict where ambiguity can affect safety.
- Unknown required fields, duplicate properties, malformed values, and unsupported enum values must not silently produce valid business state.
- Serialization settings are part of the contract. Changing naming, enum representation, case sensitivity, null handling, duplicate-property handling, or required-field behavior is a format change and should be reviewed as such.

## Network, external processes, and configuration

- Network calls are fallible and must have explicit timeout, cancellation, retry, and result semantics.
- Do not create and dispose `HttpClient` per request. Use typed/named clients, `IHttpClientFactory`, or long-lived clients with an appropriate connection lifetime.
- Retries must be safe for the operation. Do not retry non-idempotent operations unless the contract makes replay safe.
- External process execution must capture exit code, stdout/stderr as appropriate, cancellation behavior, timeout behavior, and diagnostic context.
- Do not invoke a shell or concatenate command strings when structured argument passing is available.
- Treat external tool output as untrusted input.
- Configuration is external input and must be validated before use.
- Prefer strongly typed options for related settings.
- Validate options at startup or before first use when invalid settings would cause misleading behavior, data loss, security bypass, or late job failure.
- Do not pass raw configuration objects deep into domain or workflow logic.
- Defaults must be explicit. Do not let missing configuration silently imply risky behavior.

## Comments and XML documentation

- Keep comments selective and useful.
- Explain invariants, safety decisions, framework pitfalls, non-obvious intent, and architectural boundaries.
- Prefer comments that explain why, not comments that narrate obvious code.
- Remove or correct stale comments.
- Public contracts should have XML documentation when the guarantee is non-obvious, safety-relevant, durable, or relied on by other layers.
- XML documentation must not promise behavior the implementation does not provide.
