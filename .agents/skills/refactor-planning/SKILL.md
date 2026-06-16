---
name: refactor-planning
description: Evaluate and plan behavior-preserving structural changes before editing: moving responsibilities, splitting or consolidating classes, changing abstractions, altering dependency flow, or reducing design drift. Creates persistent plans when warranted.
---

# Refactor Planning

Use this skill before making structural changes, moving responsibilities, splitting or consolidating classes, changing abstraction boundaries, altering dependency flow, reorganizing projects, or reducing design drift.

This skill is for diagnosis and planning. It should not edit source code unless the user explicitly asks for implementation after the plan.

For non-trivial refactors, multi-stage changes, safety-sensitive work, cross-layer changes, UI behavior changes, database/persistence changes, or work likely to continue across sessions, create or update a persistent implementation plan under `.ai/implementation-plans/active/`.

## Goal

Plan the smallest safe refactor that improves ownership, correctness, testability, readability, maintainability, efficiency where relevant, or change safety without changing behavior unless explicitly requested.

The purpose is controlled structural debt reduction, not beautification. A good refactor plan should make the next intended change safer, simpler, or more obvious.

## When To Use

Use this skill when:

- a code review or architecture review found structural drift;
- a class, service, ViewModel, repository, controller, handler, worker, job, adapter, module, or project has unclear responsibility;
- a bug is persistent, recurring, or difficult to fix locally;
- adding a feature requires changes in too many unrelated places;
- logic is duplicated across layers, services, handlers, validators, projectors, UI code, database code, or adapters;
- ownership is unclear between Domain/Core, Application, Infrastructure, Presentation, CLI, service, worker, job, endpoint, database, or adapter code;
- a contract or abstraction is too weak, broad, misleading, or placed in the wrong layer;
- a workflow is hard to test because orchestration and details are tangled;
- design is over-fragmented into many tiny, overlapping classes;
- several classes always change together and may represent one missing cohesive concept;
- a class has become too broad and needs responsibility extraction;
- inheritance, composition, interfaces, or dependency inversion need reconsideration;
- the obvious place to add new behavior feels wrong;
- changing the code feels unusually risky because side effects are unclear.

Do not refactor merely because code is imperfect, long, unfamiliar, inelegant, or not shaped like a preferred pattern.

## Planning Mode

Default to planning only.

In planning mode:

- do not edit source code;
- do not reorganize source files;
- do not rename symbols;
- do not update formal documentation unless explicitly asked;
- do not run broad automated rewrites;
- produce a clear plan the user can approve, reject, split, or execute later.

Creating or updating a plan file under `.ai/implementation-plans/active/` is allowed when a persistent plan is warranted. That file is a planning artifact, not source code and not authoritative architecture documentation.

Only enter implementation mode if the user explicitly asks for the plan to be implemented.

## Read First

Read the relevant subset of:

- `AGENTS.md`;
- `README.md`;
- `.ai/project-profile.md`, if present;
- relevant project docs under `docs/`;
- `.ai/context/architecture.md`, `.ai/context/decisions.md`, `.ai/context/progress.md`, and `.ai/context/HANDOFF.md`, if present;
- `.ai/standards/architecture-principles.md`;
- `.ai/standards/coding-standards.md`;
- `.ai/standards/testing-and-verification.md`;
- `.ai/standards/security-and-boundaries.md` when trust or boundaries are involved;
- `.ai/standards/persistence-and-artifacts.md` when artifacts or durable files are involved;
- `.ai/standards/database-standards.md` when database work is involved;
- `.ai/standards/service-worker-and-job-design.md` when services, workers, jobs, schedulers, or background processing are involved;
- `.ai/standards/ui-design-standards.md` and `.ai/standards/wpf-mvvm-standards.md` when UI/WPF work is involved;
- relevant source and test files;
- current git status and diff when available;
- existing active implementation plans when work may overlap.

Do not read every document mechanically. Read what is relevant to the refactor question and state scope limits.

## First Question: Is Refactoring Justified?

Refactoring is justified when it will likely improve one or more of:

- correctness or safety;
- ownership clarity;
- testability;
- readability and maintainability;
- performance or resource predictability for realistic workloads;
- change safety;
- reduction of duplicated rules;
- clarity of contracts;
- alignment with accepted architecture;
- ability to implement the next known feature safely.

Refactoring is not justified when:

- the issue is merely stylistic;
- the benefit is speculative;
- the change would add more abstraction than clarity;
- the risk is larger than expected gain;
- a small local bug fix would solve the problem;
- current design is acceptable and only needs tests or docs;
- the proposed refactor would change behavior unnecessarily.

If refactoring is not justified, say so and recommend a normal bug fix, local cleanup, additional investigation, code review, architecture review, documentation refresh, or no action.

Do not create a persistent plan when refactoring is not justified unless the user explicitly asks for an investigation or cleanup plan.

## Planning Principles

Plan refactors that are:

- behavior-preserving unless behavior change is explicitly requested;
- small enough to review;
- reversible where possible;
- buildable and testable in stages;
- aligned with accepted architecture;
- explicit about ownership and boundaries;
- focused on the known problem rather than general redesign;
- safe for current users and existing data/artifacts;
- clear about what must not change.

Prefer incremental migration over large rewrites. Prefer consolidation when many tiny classes represent one concept. Prefer extraction when one class owns unrelated responsibilities. Prefer composition when behavior varies by collaborator or policy. Prefer inheritance only for a real stable “is-a” relationship. Prefer interfaces when they express a meaningful role, boundary, substitution point, or policy/detail separation.

Avoid speculative abstractions. Avoid replacing one unclear structure with a more elaborate unclear structure.

## Responsibility and Ownership Analysis

State:

- what responsibility is misplaced, duplicated, fragmented, or overloaded;
- which layer/component should own it;
- which layer/component currently owns it;
- which callers depend on current behavior;
- which contracts need to change;
- which tests protect behavior;
- which user-visible behavior must remain unchanged.

Classify the target owner when useful: Domain/Core rule, Application policy, Infrastructure detail, Presentation/UI behavior, CLI parsing, service/worker/job orchestration, transport/IPC, database/persistence, artifact/reporting, or diagnostic concern.

## Refactor Shape Checks

Consider these shapes:

- extract cohesive responsibility from a broad class;
- consolidate over-fragmented miniature classes into a coherent component;
- move policy from Infrastructure/UI/CLI/endpoint/host code into Application;
- move technical detail from Application/Core into Infrastructure;
- split orchestration from execution;
- split planning from execution;
- split validation from mutation;
- split calculation from persistence/side effects;
- clarify a weak contract or result model;
- introduce a narrow interface at the owning layer;
- remove a ceremonial interface or abstraction;
- replace duplicated conditionals with a shared policy object, strategy, or domain method;
- replace boolean mode flags with separate methods, options, strategy, or workflow model;
- introduce an adapter around framework, OS, database, network, or external-process concerns;
- move UI mechanics out of business/application logic;
- move backend execution out of Presentation/ViewModels;
- improve DI registration or lifetime boundaries;
- add characterization tests before changing structure.

Do not assume extraction is always better than consolidation. Do not assume more interfaces means better design.

## Behavior-Preserving Boundary

Define before proposing steps:

- observable behavior that must remain unchanged;
- public APIs, CLI behavior, UI behavior, or integration behavior that must remain compatible;
- file formats, artifacts, logs, reports, schemas, migrations, and data behavior that must remain compatible;
- error handling and failure reporting that must remain equivalent;
- performance/resource characteristics that must not regress for realistic workloads;
- tests or scenarios that should pass before and after;
- behavior intentionally allowed to change.

If required behavior is unclear, ask or mark uncertainty before planning risky changes.

## Persistent Implementation Plans

For non-trivial refactors, multi-stage changes, safety-sensitive work, cross-layer changes, or work likely to continue across sessions, create or update a plan under `.ai/implementation-plans/active/`.

Use a dated, descriptive filename such as `YYYY-MM-DD-short-topic.md`.

Persistent plans must include:

- status: `Active`, `Completed`, `Superseded`, or `Abandoned`;
- created date and last updated date;
- owner/context;
- related docs and skills;
- goal and non-goals;
- assumptions;
- current problem;
- desired end state;
- behavior-preserving boundary;
- risks;
- staged implementation plan;
- verification strategy;
- follow-on review and documentation stages;
- review stop points;
- open questions;
- final user test instructions;
- completion log.

Use `.ai/templates/implementation-plan.template.md` when available.

Do not treat implementation plans as authoritative architecture. They are execution guidance. If implementation changes durable architecture, rules, public behavior, safety guarantees, runtime topology, or project state, include a `documentation-refresh` stage or follow-up.

## Verification Planning

For each meaningful step, identify verification:

- existing tests that should remain green;
- characterization tests needed before structural change;
- unit tests for moved/extracted/consolidated logic;
- integration/scenario tests for workflows crossing layers;
- UI or manual checks where automated tests are impractical;
- build commands;
- focused test filters;
- static analysis, nullable warnings, formatting, migration checks, or compiler warnings when useful.

Do not claim a refactor is safe only because it compiles. For safety, security, persistence, artifact, service/job, database, migration, verification, or UI changes, identify behavioral tests or scenario checks.

## Documentation Impact

Identify documentation that may need refresh after implementation:

- `README.md`;
- `AGENTS.md`;
- `.ai/project-profile.md`;
- formal docs under `docs/`;
- `.ai/context/architecture.md`;
- `.ai/context/decisions.md`;
- `.ai/context/progress.md`;
- relevant `.ai/standards/*` if project rules changed.

Do not update docs during planning unless explicitly asked, except for the persistent plan produced by this skill.

## Risk and Rollback Planning

For each plan, call out:

- highest-risk step;
- likely regression points;
- data/artifact/schema compatibility risks;
- runtime topology or DI risks;
- user-visible behavior risks;
- tests that catch regressions;
- rollback strategy;
- good commit boundaries;
- stop points where the user should review before continuing.

Prefer plans where each step can be committed or reverted independently.

## Output Format

Provide:

1. Refactor verdict: justified, not justified, or needs investigation first.
2. Current problem.
3. Why the structure causes friction.
4. Correct ownership/layer/component target.
5. Affected files, classes, interfaces, tests, docs, registrations, schemas, or commands.
6. Behavior-preserving boundary.
7. Implementation plan divided into stages.
8. For each stage: type, goal, affected files/classes, concrete steps, expected behavior-preserving outcome, tests, verification, risks, rollback point, stop/review criteria, documentation impact, and follow-on actions.
9. Smallest safe first step.
10. Alternatives considered.
11. Risks and rollback points.
12. Overall verification strategy.
13. Overall documentation impact.
14. Decisions needed from the user, if any.
15. Whether a persistent implementation plan was created, updated, completed, superseded, or not warranted.
16. Follow-on `code-review`, `documentation-refresh`, `architecture-review`, or `ui-review` stages included or intentionally omitted.

If refactoring is not justified, say so and recommend the smaller appropriate action.
