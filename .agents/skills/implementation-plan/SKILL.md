---
name: implementation-plan
description: Create or update a persistent implementation plan for non-trivial feature, bug-fix, migration, UI, service/job, database, security, artifact, or multi-step work before coding. Use when planning implementation work that is not primarily a refactor.
---

# Implementation Plan

Use this skill to create or update a persistent implementation plan before non-trivial implementation work.

This skill is for planning. Do not edit source code unless the user explicitly asks for implementation after the plan.

Use `refactor-planning` instead when the main task is structural redesign, moving responsibilities, changing abstraction boundaries, changing dependency direction, or reducing design drift. Use `plan-runner` to execute an existing plan.

## When To Use

Use this skill when:

- the task crosses multiple files, layers, projects, services, workers, UI surfaces, database objects, artifact formats, or runtime boundaries;
- implementation may continue across sessions;
- public behavior, CLI behavior, UI behavior, APIs, durable artifacts, schemas, migrations, configuration, dependencies, security boundaries, or background processing may change;
- failure modes, cancellation, retry, resume, timeout, shutdown, verification, finalization, partial success, or rollback must be explicit;
- the user asks for a plan before coding;
- the safest path requires characterization tests, staged rollout, or manual acceptance checks before changing behavior.

Do not create a persistent plan for a small one-file bug fix, trivial documentation edit, harmless rename, or pure formatting change unless the user asks.

## Read First

Read the relevant subset of:

- `AGENTS.md`;
- `README.md`, if present;
- `.ai/project-profile.md`, if present;
- relevant docs under `docs/`;
- `.ai/context/*`, if present;
- relevant `.ai/standards/*`;
- existing active implementation plans under `.ai/implementation-plans/active/` to avoid overlap;
- relevant source and test files;
- current git status/diff when available.

Do not read everything mechanically. Read enough to define a safe plan.

## Planning Principles

A good plan is:

- scoped to the user's goal;
- clear about non-goals;
- explicit about assumptions;
- behavior-preserving where required;
- honest about behavior that will change;
- staged into buildable, testable, reviewable increments;
- explicit about verification;
- explicit about user/manual testing;
- careful with safety, security, data, persistence, public contracts, performance, and UI truthfulness;
- free of speculative rewrites and unrelated cleanup.

Prefer small stages with clear rollback points. Do not mix unrelated cleanup into feature work.

## Plan Location And Template

Create or update a plan under:

```text
.ai/implementation-plans/active/
```

Use a dated, descriptive filename:

```text
YYYY-MM-DD-short-topic.md
```

Use `.ai/templates/implementation-plan.template.md` when present.

If an active plan already covers the same work, update it instead of creating a competing plan. If a prior plan is obsolete, recommend moving it to `.ai/implementation-plans/superseded/`.

## Plan Contents

A persistent plan should include:

- status;
- created date and last updated date;
- owner/context;
- related docs, standards, and skills;
- goal;
- non-goals;
- assumptions;
- current problem/current state;
- relevant state lifetimes, identities, and mutation paths when the defect involves stale state, unintended edits, reused UI, caches, background work, or deferred callbacks;
- desired end state;
- behavior-preserving boundary;
- intentional behavior changes;
- affected files, projects, commands, docs, schemas, artifacts, tests, or UI surfaces;
- risks and mitigations;
- staged implementation plan;
- verification strategy;
- follow-on reviews and documentation refresh;
- decision points/open questions;
- final user test instructions;
- completion log.

## Stage Requirements

Each stage should include:

- stage type: implementation, test, verification, documentation, review, decision, or cleanup;
- goal;
- affected files/classes/schemas/commands/UI surfaces;
- concrete implementation steps;
- behavior that must remain unchanged;
- tests to add or update;
- verification commands or manual checks;
- risks;
- rollback point;
- stop/review criteria;
- documentation impact;
- follow-on actions;
- whether `plan-runner` may continue automatically after the stage.

Stages should be independently understandable and preferably independently buildable/testable.

## Decision Points

Identify decisions before implementation, especially when they affect:

- public behavior, APIs, CLI behavior, or UI behavior;
- durable artifact formats;
- database schemas or migrations;
- security/trust boundaries;
- destructive actions;
- performance/resource tradeoffs;
- new production dependencies;
- target frameworks;
- compatibility;
- manual/user testing requirements.

For each open question, include a recommended default when evidence supports one, meaningful alternatives, why it matters, and the exact answer needed.

Do not silently choose a default for safety-sensitive, security-sensitive, public-contract, durable-format, persistence-schema, or destructive-behavior decisions unless the answer is already present in the current session or authoritative project docs.

## Verification Strategy

Plan the narrowest useful verification for each stage.

Consider:

- build commands;
- unit tests;
- integration or scenario tests;
- focused test filters;
- UI automation or manual UI checks;
- database migration checks;
- artifact/schema compatibility checks;
- formatting/analyzer checks when useful and not noisy;
- performance/resource checks for realistic workloads.

Build success alone is not enough for safety-sensitive, security, persistence/artifact, service/job, database, migration, verification, or UI behavior changes.

When a regression depends on prior state or an action sequence, define a producer-transition-observer scenario: the action that creates the state, the navigation/reuse/restart boundary, and the later observation that must remain correct. For unintended UI edits, include save and discard/reopen paths and verify the persisted model as well as displayed values.

If the decisive scenario cannot be automated, make the exact manual scenario mandatory and keep artifact/release status separate from implementation status. Packaging may be completed when requested, but the artifact remains an unverified candidate until the required scenario has been exercised; creating an installer or obtaining a clean build is not evidence that the behavioral regression is fixed.

## Follow-On Skills

Include follow-on stages when warranted:

- `code-review` for non-trivial, safety-sensitive, cross-layer, contract-sensitive, persistence/artifact, database, service/job, endpoint, verification, security, or UI changes;
- `documentation-refresh` when public behavior, architecture, rules, project state, CLI/API/UI behavior, safety/security guarantees, runtime topology, durable formats, schema, or project structure may change;
- `architecture-review` when layer ownership, dependency direction, runtime topology, or service boundaries may change;
- `ui-review` when UI state, layout, accessibility, WPF/MVVM boundaries, or user-visible behavior may change.

## Final User Test Instructions

Every persistent implementation plan must include **Final User Test Instructions** before completion.

They should include:

- prerequisites and setup;
- exact user actions or CLI commands;
- expected successful result;
- expected failure/safety result when relevant;
- where to inspect artifacts, reports, logs, database state, or UI state;
- cleanup/reset instructions;
- `Post-completion user test: mandatory.`

If no meaningful manual test exists, say so and explain what automated verification proves and what the user should inspect.

## Output Format

Report:

1. Plan created or updated.
2. Plan location.
3. Goal and non-goals.
4. Assumptions.
5. Key risks.
6. Stages.
7. Verification strategy.
8. Decision points/open questions.
9. Final user test path.
10. Recommended next action: run `plan-runner`, answer decision points, run `refactor-planning`, or proceed with a small direct edit.

Do not begin implementation unless the user explicitly asks for implementation.
