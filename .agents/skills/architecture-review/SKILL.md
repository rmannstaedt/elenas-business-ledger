---
name: architecture-review
description: Review architecture, layering, ownership, runtime topology, dependency direction, design drift, maintainability, and long-term change safety in C#/.NET/Windows repositories. Use for broad structural questions, not ordinary local bug review.
---

# Architecture Review

Use this skill to review architecture, layering, ownership, runtime topology, dependency direction, design drift, maintainability, testability, and long-term change safety.

This skill is diagnostic by default. It should identify architectural strengths, risks, unclear ownership, drift, and recommended next steps. Do not edit code, reorganize files, or rewrite architecture unless the user explicitly asks for implementation work.

## Purpose

Determine whether the system architecture remains understandable, coherent, safe, testable, efficient enough for its expected workload, and changeable.

Answer questions such as:

- Does each layer, project, module, service, worker, UI surface, repository, adapter, controller, handler, job, and ViewModel own the right responsibilities?
- Are dependency directions still valid and intentional?
- Is business, workflow, security, validation, persistence, or UI policy leaking into the wrong layer?
- Are infrastructure, framework, database, filesystem, Windows, UI, transport, or serialization details leaking upward?
- Are runtime roles and trust boundaries clear?
- Are safety, verification, finalization, retry, resume, cancellation, and recovery guarantees owned by the correct components?
- Are abstractions helping, or are they hiding important guarantees behind indirection?
- Has the design become either too broad or falsely decomposed into too many overlapping small parts?
- Does the documented architecture still match the code?

Use `code-review` for concrete bugs, weak contracts, unsafe implementations, local SOLID/design issues, and test gaps. Use this skill when the concern is architectural shape, ownership, boundaries, topology, workflow design, or design drift.

## When To Use

Use this skill:

- after a major feature has been added;
- after several changes may have caused design drift;
- before implementing a feature that crosses layers, projects, runtimes, services, workers, databases, or UI surfaces;
- when ownership is unclear between Domain/Core, Application, Infrastructure, Presentation, CLI, service, worker, job, controller, handler, repository, adapter, database, or external integration code;
- when a class, service, worker, repository, ViewModel, controller, handler, job, adapter, presenter, or code-behind file is becoming large or difficult to reason about;
- when business logic may be leaking into UI, infrastructure, persistence, transport, or host code;
- when infrastructure or framework concerns may be leaking into Domain/Core or Application code;
- when runtime topology, deployment boundaries, IPC, local/remote execution, background processing, or trust boundaries may be unclear;
- when safety, verification, finalization, retry, resume, cancellation, idempotency, or recovery behavior crosses multiple components;
- when a code review identifies structural friction larger than a local code issue;
- before running `refactor-planning` for a broad structural change.

Do not use this skill for every small edit. For small local changes, use normal verification or `code-review`.

## Read First

Read only the relevant subset of:

- `AGENTS.md`;
- `README.md`;
- `.ai/project-profile.md`, if present;
- relevant project docs under `docs/`;
- `.ai/context/architecture.md`, `.ai/context/decisions.md`, `.ai/context/progress.md`, and `.ai/context/HANDOFF.md`, if present;
- `.ai/standards/architecture-principles.md`;
- `.ai/standards/coding-standards.md` when coding standards affect structure;
- `.ai/standards/service-worker-and-job-design.md` when services, workers, schedulers, queues, jobs, IPC, or background processing are involved;
- `.ai/standards/persistence-and-artifacts.md` when durable files, manifests, reports, logs, import/export, or artifact formats are involved;
- `.ai/standards/database-standards.md` when SQL, SQLite, EF Core, transactions, migrations, or database-backed persistence are involved;
- `.ai/standards/security-and-boundaries.md` when trust, paths, secrets, IPC, external processes, network, installers, permissions, or privacy are involved;
- `.ai/standards/ui-design-standards.md` and `.ai/standards/wpf-mvvm-standards.md` when UI, WPF, commands, view state, or ViewModels are involved;
- relevant source and test files;
- current git status and diff when available.

Do not read every document mechanically. Read what is relevant to the architecture question and state any important scope limits.

## Review Scope

Default to reviewing the architectural impact of the current change or the user’s stated concern.

If the user asks for a general architecture review, inspect the solution at a high level first, then follow high-risk paths in depth. Use the current diff, recent changes, or stated concern as the entry point, but do not treat changed files as the full boundary. Architectural drift often appears in callers, registrations, tests, runtime wiring, configuration, persistence, or adjacent services that were not directly edited.

Inspect relevant:

- solution and project structure;
- project references and dependency direction;
- namespaces and folder organization;
- composition roots and dependency-injection registration;
- public interfaces, major abstractions, and ownership of interfaces;
- Domain/Core, Application, Infrastructure, Presentation, host, and test boundaries;
- CLI, service, worker, job, controller, handler, repository, adapter, database, and UI entry points;
- runtime topology and communication boundaries;
- workflow orchestration, state transitions, and policy ownership;
- persistence, artifact, logging, and reporting boundaries;
- verification, finalization, destructive action, retry, resume, and cancellation paths;
- tests around architectural seams;
- documentation when it claims architectural guarantees.

For large solutions, prefer depth on high-risk architectural paths over shallow commentary on every file. If one pass is not enough, divide the review into explicit passes such as layering, runtime topology, workflow ownership, persistence/artifacts, UI boundaries, database boundaries, security boundaries, and testability.

## Core Architecture Checks

Check whether:

- the intended architecture is visible in the codebase;
- project and namespace boundaries reflect real ownership;
- dependency direction is valid and intentional;
- Domain/Core remains free of infrastructure, UI, framework, persistence, serialization, transport, logging, dependency-injection, and runtime-hosting concerns;
- Application owns use-case orchestration, workflow policy, validation coordination, result models, and application decisions;
- Infrastructure implements technical details without owning business or workflow policy;
- Presentation owns UI state, user interaction, commands, display concerns, view mechanics, and user-facing validation, but not backend execution;
- host entry points compose and run the system without becoming hidden workflow-policy owners;
- CLI entry points translate user intent into application calls without duplicating application workflow logic;
- repositories remain persistence-focused and do not become application workflow coordinators;
- services and workers remain cohesive and do not become broad `Manager` or `Helper` objects;
- ViewModels do not accumulate application, persistence, infrastructure, database, external-process, or backend-execution responsibilities;
- code-behind remains limited to framework-specific mechanics where applicable;
- validators, projectors, planners, executors, reporters, repositories, adapters, controllers, handlers, presenters, workers, and jobs have distinct responsibilities where those distinctions matter;
- abstractions clarify ownership, boundaries, substitution, lifecycle, policy/detail separation, or test seams rather than hiding important guarantees;
- interfaces are placed in the layer that owns the policy or abstraction, not merely near one implementation;
- dependency inversion protects policy from details rather than adding ceremony;
- cross-cutting concerns such as logging, clocks, configuration, telemetry, retry, cancellation, and correlation are handled consistently;
- tests exercise important seams rather than requiring fragile end-to-end coverage for every policy;
- decomposition creates coherent components rather than a proliferation of overlapping miniature services;
- runtime topology and communication boundaries are explicit, intentional, and safe, including which components may call, command, observe, or mutate which other components.

## Ownership and Policy Checks

For each important workflow or rule, identify the owner:

- Domain/Core rule;
- Application policy;
- Infrastructure implementation detail;
- Presentation/UI behavior;
- CLI parsing or translation concern;
- runtime-hosting concern;
- service, worker, job, queue, or scheduler concern;
- persistence, artifact, database, cache, or migration concern;
- transport, endpoint, IPC, or external integration concern;
- reporting or diagnostic concern.

Report unclear or misplaced ownership.

Examples of architecture drift include:

- Infrastructure deciding workflow outcome, validation policy, business rules, authorization policy, or product behavior;
- UI code invoking low-level backend operations directly;
- Domain/Core models depending on storage, serialization, UI, logging, DI, OS, or runtime services;
- repositories performing orchestration instead of persistence;
- application services accumulating unrelated workflows;
- ViewModels coordinating backend execution instead of calling application services or abstractions;
- CLI handlers duplicating application workflow logic;
- tests needing excessive setup because responsibilities are poorly separated;
- repeated rule logic appearing in multiple layers;
- host startup code becoming the only place a workflow can be understood.

## Runtime Topology and Trust Checks

When the system has multiple processes, services, workers, machines, endpoints, queues, scheduled tasks, plugins, external processes, or runtime roles, check whether:

- runtime roles are clearly named and documented;
- each role has a clear responsibility;
- communication direction is intentional;
- trust boundaries are explicit;
- authorization, allowlists, capability boundaries, and path boundaries are enforced where required;
- remote execution boundaries are respected;
- runtime state is not split ambiguously across roles;
- failure, retry, resume, cancellation, timeout, shutdown, and recovery semantics are defined at the right layer;
- durable artifacts truthfully represent what happened across runtime boundaries;
- UI/control-surface responsibilities are separated from backend execution responsibilities;
- arbitrary remote command execution is absent unless explicitly intended, constrained, authorized, logged, and documented.

For file-sensitive, content-sensitive, security-sensitive, remote-execution, backup, restore, migration, publishing, import/export, synchronization, or verification systems, use the topology lens: control surface, coordinator/planner, worker/executor, staged workflow, plan-before-execution where appropriate, trust/capability boundaries, verification/finalization safety, and durable reporting of partial failures.

## Workflow and Safety Checks

For staged, durable, long-running, destructive, or safety-sensitive workflows, check whether:

- workflow stages are explicit;
- planning, validation, execution, verification, finalization, cleanup, and reporting remain distinct where they need to be;
- no component can skip required safety stages;
- success cannot be reported without sufficient proof;
- finalization cannot happen before required checks complete;
- partial failure is represented truthfully;
- cancellation and interruption leave the system recoverable and honest;
- retry and resume behavior is owned by the correct layer;
- safety rules are enforced by code, not only documented;
- destructive actions require intended authorization, confirmation, or policy approval;
- durable artifacts reflect actual outcomes rather than desired outcomes.

## SOLID, Decomposition, and Structural Drift

Report SOLID and local design concerns only when they reveal architectural pressure.

Check whether:

- classes or services have accumulated multiple architectural responsibilities;
- constructor dependencies suggest mixed ownership across layers;
- broad abstractions hide important architectural guarantees;
- inheritance or base classes obscure ownership or lifecycle;
- shared helpers erase domain meaning;
- adapters, repositories, services, jobs, workers, handlers, and ViewModels remain cohesive;
- dependency inversion protects policy from details;
- abstractions are stable enough to justify their existence;
- concrete implementation details are not leaking across boundaries.

Also check for false decomposition:

- many small classes that are not independently meaningful;
- several classes with nearly identical responsibilities, names, inputs, outputs, or lifecycle;
- responsibilities fragmented in a way that makes the workflow harder to understand;
- interfaces created only for ceremony or mocking;
- miniature services that always change together and cannot be understood independently;
- orchestration complexity caused mainly by stitching together too many trivial collaborators;
- code navigation that requires following many indirections before reaching meaningful behavior.

Do not recommend merging classes merely because they are small. Small classes are acceptable when they have clear names, stable responsibilities, useful substitution points, independent tests, or meaningful architectural boundaries.

## Documentation and Code Correlation

Do not assume architecture documentation is automatically correct. Compare documented architecture against current code when practical.

Classify mismatches as:

- documentation is stale;
- code has drifted from accepted architecture;
- implementation is partial;
- architecture decision is unclear;
- planned architecture has not yet been implemented;
- code implements behavior that is not documented.

If documentation claims guarantees that code does not enforce, report that clearly. If the architecture is acceptable but documentation is stale, recommend `documentation-refresh`.

## Severity Levels

Use these severities:

- **Critical** — architecture permits or causes likely data loss, corruption, false success, unsafe destructive behavior, serious security/trust-boundary failure, or severe dependency-direction failure that invalidates core guarantees.
- **High** — significant boundary violation, misplaced policy ownership, unsafe runtime topology, serious workflow/safety drift, or structural issue likely to block near-term change.
- **Medium** — maintainability concern, unclear ownership, responsibility accumulation, weak abstraction, testability problem, or drift that should be planned before it becomes costly.
- **Low** — minor naming, organization, documentation, or clarity issue that does not currently threaten correctness or change safety.

Do not inflate severity. If something is acceptable but worth monitoring, mark it Medium or Low.

## Review Rules

- Lead with the most important architectural risks.
- Use concrete file, project, namespace, type, method, configuration, or symbol references where possible.
- Distinguish confirmed issues from suspicions.
- Distinguish architecture findings from code-level bugs.
- Distinguish accepted architecture from proposed architecture.
- Do not recommend broad rewrites unless clearly justified.
- Prefer small, staged corrections.
- Preserve current user-visible behavior unless the user explicitly wants behavior changes.
- Call out tradeoffs and uncertainty.
- Do not propose speculative abstractions.
- Do not report purely subjective design preferences as architecture findings.
- Group repeated symptoms under one root cause.
- Do not manufacture findings to satisfy the format.
- If a finding is local and code-level, recommend `code-review`.
- If a refactor appears justified, recommend `refactor-planning` before editing.
- If documentation is stale or inconsistent, recommend `documentation-refresh`.
- If the architecture is acceptable, say so clearly.

## Verification Expectations

Architecture review does not require changing or running code by default.

When practical, verify claims by inspecting project references, DI registrations, namespace/folder boundaries, public interfaces, important call paths, tests around seams, and documentation that claims guarantees.

If build or test commands are relevant and practical, recommend them or run them only if the user requested verification work and the environment permits it. Do not claim verification passed unless it was actually run.

## Output Format

Report:

1. Overall verdict.
2. Architecture strengths.
3. Findings grouped by severity.
4. For each finding: severity, location, problem, evidence, architectural impact, smallest safe change, and whether this should be documented only, code-reviewed, refactor-planned, or fixed immediately.
5. Affected files, projects, layers, runtime areas, or durable formats.
6. Documentation/code mismatches, if any.
7. Verification performed or still recommended.
8. Whether `code-review`, `refactor-planning`, or `documentation-refresh` is warranted.
9. Open questions.

Do not rewrite architecture as part of the review unless explicitly asked.
