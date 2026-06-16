# Code Review Checks

Use these checklists as prompts, not as mandatory output sections. Apply only the parts relevant to the project, changed code, and review scope.

Do not apply file, UI, database, network, distributed-system, installer, or domain-specific checks to code where those concerns are absent.

## General Code Review Checks

Check for:

- null handling, invalid-input behavior, missing guards, and incorrect default handling;
- mismatches between names, contracts, return values, docs, tests, and actual behavior;
- unclear preconditions, postconditions, invariants, ownership, or lifetime expectations;
- off-by-one, empty collection, duplicate, missing, already-exists, permission-denied, locked, unavailable, malformed, unsupported, unknown, and future-value cases;
- incorrect state transitions, missing state validation, invalid repeated execution, and hidden lifecycle ordering requirements;
- idempotency problems where work may be retried, resumed, repeated, cancelled, timed out, crashed, or partially completed;
- fragile assumptions about ordering, timing, current directory, path casing, culture, environment, clock, timezone, duration, timeout, machine configuration, machine-specific behavior, external service behavior, installed SDKs, Windows version, DPI, or network state;
- equality, comparison, sorting, hashing, dictionary/set-key, version, and numeric precision mistakes;
- boundary conversion mistakes between UI, application, domain, infrastructure, persistence, serialization, CLI, database, network, or external API models;
- configuration/default-value mistakes, especially where missing configuration silently creates unsafe, expensive, or misleading behavior;
- serialization/deserialization, file format, schema, migration, version-tolerance, or backward/forward compatibility problems;
- exception handling that hides failure, reports success incorrectly, catches too broadly, or loses diagnostic context;
- async/await misuse, blocking waits, unobserved tasks, fire-and-forget work, cancellation-token neglect, or conflation of timeout/cancellation/failure;
- resource lifetime, disposal, stream ownership, database context, transaction, file handle, lock, timer, event subscription, process, or unmanaged-resource leaks;
- dependency-lifetime problems, including captive scoped services, unintended singletons, or services retaining mutable operation/request/user/test state;
- unnecessary buffering of large or unbounded data;
- non-streaming file, network, or database operations where streaming, batching, paging, or limits are required;
- race conditions around shared state, files, directories, markers, manifests, migrations, persistence, caches, background jobs, queues, finalization, UI selection, or progress refresh;
- concurrency issues such as unsafe shared mutable state, missing synchronization, deadlocks, lock ordering, unbounded fan-out, and assumptions that code is single-threaded when it may not be;
- mutable shared state leaking across operations, requests, sessions, tenants, users, tests, or jobs;
- missing validation before irreversible, destructive, externally visible, privileged, or expensive work;
- logging, diagnostics, telemetry, or reporting that omit context needed to diagnose failure;
- secret, credential, token, private data, PII, sensitive path, or file content leakage in logs, errors, reports, UI, or telemetry;
- tests that assert implementation details instead of observable behavior;
- tests missing important success, edge, failure, cancellation, retry, resume, authorization, migration, UI, or boundary conditions;
- code that is hard to read, reason about, or change correctly;
- code logic that happens to work now but is brittle, under-specified, or likely to cause bugs when the next feature is added;
- dead code, duplicated logic, confusing control flow, or unnecessary complexity when it affects correctness, readability, maintainability, efficiency, or change safety;
- brittle logic patterns such as duplicated conditionals, inverted boolean meaning, mixed inclusive/exclusive bounds, fall-through defaults, hidden side effects, stringly typed state, boolean mode flags, or partially updated state;
- compiler, analyzer, nullable, security, or code-style warnings that indicate real correctness, contract, maintainability, efficiency, or safety issues.

## C#/.NET-Specific Checks

Check for:

- nullable annotations that do not match runtime behavior;
- `default` values for enums/structs becoming valid business state accidentally;
- inappropriate `async void` outside event handlers;
- `.Result`, `.Wait()`, or synchronous blocking on async work;
- missing `ConfigureAwait` discipline in general-purpose libraries where the project expects it;
- hidden deferred execution or repeated enumeration of expensive `IEnumerable<T>` sequences;
- LINQ queries that re-run I/O, database, or expensive computation unexpectedly;
- mutable `static` state or singleton state that leaks between operations/tests/users;
- incorrect `IDisposable`/`IAsyncDisposable` ownership;
- event subscriptions that retain ViewModels, services, controls, or background objects longer than intended;
- `CancellationToken` accepted but not passed through;
- `Task.Run` used to hide blocking I/O or CPU work in the wrong layer;
- culture-sensitive string or number behavior in machine-readable paths, tokens, JSON, IDs, or durable formats;
- `DateTime.Now` or local time used as durable workflow state where UTC/`DateTimeOffset` is required;
- unbounded `ToList()`, `ReadAllText`, `ReadAllBytes`, buffering, or materialization of user-supplied/large data;
- incorrect DI lifetimes, especially singleton services capturing scoped services or mutable per-operation state;
- overly broad NuGet dependencies for a small task;
- public XML docs that promise behavior the implementation does not provide.

## Contract and API Checks

A contract includes names, signatures, nullability, exceptions, result models, comments, XML docs, tests, and caller assumptions.

Check whether:

- interface, method, parameter, and result names accurately describe behavior and guarantees;
- method signatures make invalid or unsafe use difficult;
- preconditions, postconditions, invariants, ownership, and lifetime expectations are clear;
- return values distinguish success, partial success, failure, cancellation, timeout, skipped work, validation failure, conflict, unauthorized, forbidden, retry-exhausted, not-found, and no-op cases where those states matter;
- error handling is consistent across related APIs;
- failure information is durable and detailed enough for later diagnosis;
- exceptions versus result objects are used deliberately and consistently;
- cancellation behavior is explicit, including whether cancellation is best-effort, immediate, leaves partial state, or creates durable cancellation records;
- retry and idempotency behavior is clear for repeated, resumed, or partially completed work;
- contracts define behavior for null, empty, missing, duplicate, malformed, unsupported, unauthorized, unknown, or future values;
- nullable values, empty values, and missing values are handled explicitly;
- enum/default values are safe and cannot silently become valid business state by accident;
- DTOs and result models preserve information needed by later stages;
- contracts are too loose for safe implementation;
- contracts are too narrow or over-specific in ways that force callers to know implementation details;
- abstractions hide implementation details without hiding necessary guarantees;
- callers rely on assumptions not expressed by the contract;
- multiple implementations of the same interface provide compatible behavior;
- names such as `Verify`, `Ensure`, `Commit`, `Finalize`, `Publish`, `Persist`, `Atomic`, `Safe`, or `Complete` do not overpromise;
- boolean return values hide important result states;
- optional parameters, default values, overloads, or mode flags can accidentally select unsafe behavior;
- returned streams, buffers, contexts, handles, entities, or disposable objects have clear ownership/lifetime;
- returned collections are live views, snapshots, ordered, unordered, distinct, mutable, immutable, or caller-owned as promised;
- async methods do not hide fire-and-forget work behind a completed task;
- API boundaries do not leak lower-layer concerns into higher layers;
- serialization, persistence, CLI, UI, database, network, or external API models are not mistaken for validated domain contracts unless intentional;
- versioning and future extension have been considered for durable or externally visible contracts.

If a contract permits unsafe, ambiguous, misleading, or under-specified implementation, report it as a contract weakness even if the current implementation happens to work.

## Local Design and SOLID Checks

This is not a full architecture review. It may report local architecture pressure, ownership drift, or dependency-direction problems, but should not propose broad architectural rewrites. Recommend `architecture-review` or `refactor-planning` when the issue is larger than local code review.

Check whether:

- classes, methods, and modules have focused responsibilities;
- classes or services are becoming junk drawers for unrelated behavior;
- orchestration, validation, persistence, transformation, reporting, UI mechanics, and side effects are mixed in ways that obscure ownership or make testing fragile;
- dependencies are stable, purposeful, and aligned with the class’s responsibility;
- dependencies are at the appropriate abstraction level for the layer and responsibility;
- large DI constructors reveal mixed responsibilities, unclear ownership, hidden orchestration, missing collaborators, or difficult testing;
- interfaces are narrow, meaningful, role-focused, and not created only for ceremony or mocking;
- abstractions clarify ownership, seams, policy/detail boundaries, or substitution needs;
- high-level policy does not depend on low-level details;
- low-level infrastructure does not make application/domain policy decisions;
- domain or application code is not forced to understand infrastructure, UI, serialization, persistence, CLI, database, or framework concerns;
- UI code does not perform backend execution work;
- ViewModels do not accumulate unrelated backend, persistence, orchestration, or infrastructure responsibilities;
- repositories remain persistence-focused;
- services remain cohesive and do not become broad `Manager` or `Helper` objects;
- inheritance represents a real stable “is-a” relationship;
- composition would be safer, clearer, or more flexible than inheritance;
- base classes do not hide important control flow, state, or lifecycle assumptions;
- boolean flags, optional parameters, or mode arguments are causing one method to perform several jobs;
- public methods require hidden call order;
- side effects are explicit and occur at the expected layer;
- duplicated logic encodes subtly different rules in multiple places;
- local design makes the next likely change safer and easier.

Prefer findings that affect correctness, testability, maintainability, change safety, or clarity of ownership.

## File, Content, Backup, Restore, Publishing, Import/Export, or Verification Systems

Apply only when relevant.

Check whether:

- checksum/hash/signature proof is weakened;
- algorithm choice is explicit and appropriate for the threat model;
- hash/checksum contracts state what was proven and what was not;
- work can be reported verified, committed, finalized, published, imported, exported, or complete before required proof occurred;
- source-changed-during-read, partial-write, interrupted-write, stale-cache, or incomplete payload cases can produce false success;
- staged payloads, ready markers, manifests, plans, reports, or result artifacts can become inconsistent or visible before required data is durable;
- paths, URIs, resource identifiers, allowlists, symlinks, junctions, reparse points, casing, and relative paths are handled safely;
- large or unbounded payloads are streamed or batched;
- destructive behaviors are guarded by explicit approval and correct scope display;
- per-item/per-action failures are recorded and visible;
- retry, resume, cancellation, timeout, and crash recovery preserve truthful state.

## Security, Tenant, Account, Workspace, or Privilege-Sensitive Code

Apply only when relevant.

Check whether:

- authentication and authorization are enforced at the correct boundary;
- callers can access, mutate, delete, export, or infer data outside allowed scope;
- trust boundaries are explicit for files, network, CLI, plugins, scripts, external processes, users, tenants, workspaces, services, and jobs;
- secrets, credentials, tokens, private keys, PII, or sensitive paths are exposed in logs, errors, UI, telemetry, artifacts, or exceptions;
- inputs are validated before use in SQL, command execution, paths, URLs, templates, serialization, deserialization, reflection, dynamic dispatch, or process arguments;
- privilege changes, destructive operations, external sharing, data export, and permission grants are explicit and auditable;
- default configuration fails closed where safety or security depends on it.

## Database, Persistence, Cache, Queue, Event, or Migration Code

Apply only when relevant.

Check whether:

- transactions, concurrency tokens, unique constraints, ordering, idempotency, retries, and partial failure are handled correctly;
- migration code can lose data, mishandle existing rows, break rollback assumptions, or silently change durable meaning;
- EF Core tracking/no-tracking, context lifetime, lazy loading, query composition, and transaction boundaries are intentional;
- queries can cause N+1 roundtrips, unbounded result sets, client-side evaluation, missing indexes, or avoidable table scans in realistic workloads;
- outbox/inbox, message acknowledgement, duplicate delivery, poison-message, and retry semantics are explicit;
- cache invalidation, stale reads, write-through/write-behind, and eventual consistency are represented honestly;
- durable IDs and ordering are stable across restarts and migrations.

## Services, Workers, Schedulers, Jobs, and Background Processing

Apply only when relevant.

Check whether:

- job ownership, state transitions, retry, resume, cancellation, shutdown, and recovery are explicit;
- background exceptions are observed and recorded;
- work is not tied to UI/window/request lifetime unless intended;
- progress and status are truthful and bounded;
- jobs can be reconstructed after restart when durability is required;
- duplicate execution or replay is safe or prevented;
- queues and scheduled tasks handle poison work, backoff, locking, leases, timeouts, and clock drift where relevant;
- shutdown leaves state recoverable and honest;
- per-item failures are visible without unnecessarily aborting unrelated work.

## UI Code

Apply only when relevant.

Check whether:

- UI state remains typed, truthful, and stable;
- late or out-of-order data can regress visible state or overwrite current selection;
- UI code starts doing backend execution, persistence, verification, migration, or workflow ownership;
- dangerous, destructive, partial, skipped, failed, cancelled, unauthorized, or unverified states are visible before and after action;
- command availability, cancellation, progress, disabled reasons, and errors are understandable;
- UI thread remains responsive;
- WPF collection and property notifications occur on the appropriate thread;
- binding failures, converter behavior, validation errors, and accessibility names are considered;
- tests cover ViewModel state transitions, stale update handling, command availability, and critical UI workflows.

## External Process, Installer, Network, and IPC Code

Apply only when relevant.

Check whether:

- command arguments are passed structurally rather than through shell concatenation;
- exit code, stdout/stderr, timeout, cancellation, and diagnostics are captured;
- external process output is bounded and sanitized before logging/display;
- network calls have timeout, cancellation, retry, idempotency, and result semantics;
- `HttpClient` usage follows project conventions and avoids per-request socket exhaustion;
- IPC/named pipe/server endpoints authenticate/authorize callers where required;
- installers/updaters avoid unsafe elevation, path, rollback, and partial-install states.

## Tests and Verification Checks

Check whether tests cover behavior at the level where risk lives:

- observable behavior rather than implementation details;
- edge, empty, invalid, duplicate, malformed, unknown, authorization, and future-value cases;
- failure, exception, cancellation, timeout, retry, resume, partial success, and repeated-run paths;
- boundary tests for serialization, persistence, paths, network, database, CLI, UI, and external APIs;
- integration or scenario tests when behavior crosses important layers;
- characterization tests before risky refactoring;
- tests proving false success, hidden partial failure, and misleading durable state cannot occur.

Do not claim verification passed unless it was actually run. If tests cannot be run, explain why and state what remains unverified.

For safety, security, persistence, artifacts, services, jobs, migrations, verification, or UI changes, ordinary build success is not enough. Identify the behavioral tests or scenario checks still needed.
