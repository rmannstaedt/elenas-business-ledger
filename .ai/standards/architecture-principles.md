# Architecture Principles

Use this file when making or reviewing changes that affect structure, layer boundaries, ownership, dependencies, runtime topology, maintainability, safety guarantees, or broad refactoring.

These principles are reusable across C#/.NET projects. Apply them proportionally. A small console utility does not need the same layout as a large WPF application with services, but both need clear ownership and code that can be changed safely.

## Purpose

The goal is not architectural ceremony. The goal is a system whose responsibilities are clear, whose safety and correctness guarantees are enforceable, and whose future changes do not require guessing where behavior belongs.

Good architecture should:

- make ownership obvious;
- make unsafe behavior hard to express;
- keep policy above implementation details;
- keep implementation details below policy;
- keep UI mechanics separate from backend execution;
- preserve security and trust boundaries;
- make tests meaningful rather than fragile;
- make the next likely change safer.

## Architecture style

Use the project profile as the source of truth for architecture style.

For non-trivial business applications, Clean/Layered Architecture is the default unless the project profile says otherwise:

```text
Domain/Core has no inward dependencies.
Application -> Domain/Core.
Infrastructure -> Application + Domain/Core.
Presentation -> Application + Domain/Core.
Executable hosts compose Application + Infrastructure + Presentation as needed.
```

Small tools may collapse these into fewer projects or folders, but responsibility boundaries still apply. Do not put business policy, workflow decisions, persistence details, UI mechanics, and OS integration into one class merely because the project is small.

Vertical-slice or feature-folder architectures are acceptable when the project profile chooses them, provided that business rules, external integrations, persistence details, UI mechanics, and host composition remain intentionally separated.

## Layer ownership

### Domain/Core

Domain/Core contains core concepts, business rules, value objects, domain services, and invariants.

Domain/Core must not depend on WPF, WinUI, WinForms, ASP.NET, filesystem APIs, registry APIs, network APIs, database APIs, operating-system APIs, JSON/XML/SQL/EF Core/HTTP/named-pipe transport details, logging frameworks, dependency-injection frameworks, CLI, host, installer, or deployment concerns.

Domain/Core should remain understandable without knowing how the application is hosted, displayed, persisted, transported, or deployed.

### Application

Application owns use cases and workflow policy.

Application may own command/query/use-case orchestration, validation coordination, business workflow sequencing, result models, status models, application-level security policy, retry/resume/cancellation semantics for application workflows, and abstractions for external capabilities.

Application decides what should happen and in what order. It may define interfaces for external capabilities, but it must not depend on concrete Infrastructure or Presentation implementations.

### Infrastructure

Infrastructure implements technical details: filesystem/path access, serialization, database implementations and migrations, EF Core DbContexts, HTTP clients, cryptography integration, Windows services, registry, event log, named pipes, OS APIs, external processes, logging sinks, artifact writers, report writers, and caches.

Infrastructure may enforce low-level safety mechanics, such as path validation and atomic write behavior, but it must not become the hidden owner of business policy or workflow decisions.

### Presentation

Presentation owns user interaction and presentation state: views, screens, dialogs, navigation, ViewModels or presenters, commands, validation display, selection, filtering, sorting, progress display, and UI-specific interaction mechanics.

Presentation may request work, display work, and control interaction. It must not perform durable backend execution such as transfer, hashing, verification, finalization, database migrations, long-running jobs, or service-owned processing.

### Executable hosts

Executable hosts compose and run the system. Hosts include console apps, WPF applications, Windows services, ASP.NET hosts, worker services, test harnesses, and installer/bootstrapper entry points.

Hosts may reference Application, Infrastructure, Domain/Core, and Presentation as needed for composition. They should not become owners of workflow policy, business rules, or persistence semantics.

## Runtime boundaries

Runtime boundaries must remain explicit, intentional, and safe.

When the project has multiple runtime actors, identify which component may accept user intent, authorize work, coordinate work, execute work, observe progress, mutate durable state, publish results, and recover after interruption.

A component must not silently gain authority to command another component, mutate another component's data, bypass trust checks, or treat an implementation artifact as proof of success.

## Workflow and state ownership

Workflow state should have one clear owner.

- UI state belongs to Presentation.
- Use-case state belongs to Application.
- Durable job state belongs to the component responsible for job execution and recovery.
- Database schema state belongs to the persistence component and migration policy.
- External system state belongs behind an adapter boundary.

State transitions should be explicit for workflows, jobs, tasks, imports, exports, background operations, UI screens, and database migrations.

Do not represent bounded workflow state as arbitrary strings inside the application. Use enums, discriminated result models, or dedicated value types.

## Design principles

Use SOLID to clarify ownership, not to add ceremony.

Prefer simple components with one clear reason to change, cohesive design over both god-classes and excessive micro-class fragmentation, composition over inheritance unless inheritance expresses a stable `is-a` relationship, explicit dependencies over service locators, domain/application types over raw transport/persistence/UI DTOs, and meaningful abstractions at real boundaries.

Avoid broad managers that mix policy, persistence, UI, and side effects; helper classes that become dumping grounds; premature interfaces for every class; factories or base classes with no real ownership value; pass-through layers; and moving code into Infrastructure merely because it touches a technical API while the decision itself is business policy.

## Abstraction and decomposition

Before introducing a new abstraction, ask what responsibility it owns, which layer owns it, what policy/boundary/lifecycle/guarantee it protects, what substitution or test seam it provides, and whether it makes the code clearer or only more indirect.

Before splitting a class, ask whether there are multiple reasons to change, a real boundary between responsibilities, an independently meaningful extracted part, simpler callers, and a clearer workflow.

Before consolidating classes, ask whether they represent one concept that was falsely decomposed, always change together, have nearly identical inputs/outputs/lifecycles/names, or create orchestration complexity mainly because too many tiny collaborators must be stitched together.

Small classes are good when they have clear names, stable responsibilities, useful boundaries, independent tests, or meaningful substitution points. Large classes are acceptable only when they represent one coherent responsibility and do not mix unrelated policy, implementation, UI, persistence, security, and orchestration concerns.

## Dependency injection

Dependency injection is the normal composition mechanism, but it does not remove responsibility concerns.

Large constructors are design signals, not automatic findings. A large constructor is acceptable when the class is an explicit orchestrator/composition-facing service, dependencies are at appropriate abstraction levels, dependencies align with one coherent responsibility, collaborators are meaningful and stable, and tests remain understandable.

A large constructor is suspicious when dependencies come from unrelated layers or concerns, some dependencies are only needed by one unrelated method, the class is coordinating work that belongs elsewhere, the dependency set reveals mixed ownership, or the class is hard to test because it has too many behavior axes.

## Architecture decisions

Document non-obvious architecture decisions in the repository's chosen location, usually `docs/decisions/` or `.ai/context/decisions.md`.

Document decisions when they affect dependency direction, project layout, runtime topology, persistence format, database technology, security boundary, job/workflow ownership, UI architecture, public contracts, migration, or compatibility.

Do not turn local code smells into architecture findings unless they point to a larger boundary or ownership problem. Do not turn architecture concerns into broad rewrites without a refactor plan.

## Design drift checklist

Before accepting a structural change, check:

- Did Domain/Core gain infrastructure, OS, UI, transport, JSON, logging, DI, or filesystem concerns?
- Did Application gain concrete Infrastructure, UI, database, or host implementation details?
- Did Infrastructure start making business, validation, security, or workflow-outcome decisions?
- Did Presentation gain long-running execution, persistence, transfer, verification, finalization, database, or service-owned logic?
- Did a host become a hidden owner of business workflow?
- Did a new abstraction make the code clearer, safer, or more testable, or only more indirect?
- Did decomposition create coherent components or a proliferation of overlapping miniature services?
- Did consolidation improve cohesion or create a broader mixed-responsibility class?
- Did the change preserve runtime authority, trust, and communication boundaries?
- Did the change make the next likely change easier and safer?
