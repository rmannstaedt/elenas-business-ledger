# AGENTS.md

Read this file before making changes.

This repository uses `AGENTS.md` for compact, always-on Codex guidance. Detailed standards live under `.ai/standards/`.
Project-specific applicability lives in `.ai/project-profile.md` when present. Repeatable Codex workflows live under `.agents/skills/`.

## Read first

For most code tasks, read:

- `README.md`, if it exists;
- `AGENTS.md`;
- `.ai/project-profile.md`;
- relevant project docs under `docs/`;
- the directly relevant source and test files.

## Standards routing

Use only the standards that match the task and the project profile:

- `.ai/standards/architecture-principles.md` — ownership, layering, dependency direction, runtime boundaries, and design drift.
- `.ai/standards/coding-standards.md` — C#/.NET code, contracts, async, cancellation, resources, logging, configuration, comments, and maintainability.
- `.ai/standards/testing-and-verification.md` — tests, build verification, xUnit/FlaUI defaults, analyzers, and completion criteria.
- `.ai/standards/security-and-boundaries.md` — trust boundaries, secrets, path safety, external input, process execution, network calls, IPC, installers, and privacy.
- `.ai/standards/tooling-and-dependencies.md` — target framework policy, SDK/global.json, EditorConfig, analyzers, NuGet, and dependency governance.
- `.ai/standards/persistence-and-artifacts.md` — JSON, JSON Lines, logs, reports, durable files, schema evolution, cleanup, and traceability.
- `.ai/standards/domain-data-integrity.md` — stable identities, durable references, snapshots, lineage, financial/audit state ownership, and natural-key risks. Apply when durable business data or cross-record state is involved.
- `.ai/standards/database-standards.md` — SQL, SQLite, EF Core, migrations, transactions, concurrency, and database testing. Apply only when a database component exists.
- `.ai/standards/service-worker-and-job-design.md` — Windows services, hosted workers, schedulers, durable jobs, queues, resumability, staging, verification, shutdown, and progress. Apply only when those components exist.
- `.ai/standards/ui-design-standards.md` — generic UI design, state, transitions, progress, accessibility, safety-critical UI, layout, and review.
- `.ai/standards/wpf-mvvm-standards.md` — WPF-specific MVVM, XAML, binding, threading, dialogs, validation, accessibility, and FlaUI posture. Apply only to WPF projects/components.
- `.ai/standards/review-checklists.md` — severity model and focused review checklists.

Do not apply platform-, UI-, service-, storage-, database-, network-, installer-, or domain-specific standards to projects where those concerns are not present.

## Codex skills

When `.agents/skills/` exists, use matching skills for repeatable workflows:

- `architecture-review` — assess architecture, layering, ownership, topology, boundaries, or drift.
- `code-review` — review changed or existing code for bugs, regressions, unsafe edge cases, weak contracts, local design issues, performance risks, and verification gaps.
- `implementation-plan` — create or update a persistent implementation plan for non-trivial feature/bug/migration/UI/service/database work before coding.
- `plan-runner` — execute a persistent implementation plan under `.ai/implementation-plans/active/` stage by stage.
- `refactor-planning` — evaluate and plan behavior-preserving structural changes before editing.
- `documentation-refresh` — update documentation and AI context after implemented behavior, commands, artifacts, architecture, or project state changes.
- `handoff-generation` — prepare continuation context for a later session.
- `ui-review` — review UI/UX, accessibility, state truthfulness, WPF/MVVM boundaries, and user-visible behavior.

Skills use the current Codex structure: `.agents/skills/<skill-name>/SKILL.md`.
If a relevant skill or standard file does not exist, follow this `AGENTS.md`, the project documentation, and the user's request directly.

## Enforcement model

Use a tiered standard:

1. **Hard requirements**: correctness, safety, data integrity, security, truthful state, cancellation/retry semantics, non-destructive repository behavior, and declared architecture boundaries. Do not knowingly violate these without explicit user approval or a documented architecture decision.
2. **Project-profile requirements**: choices declared in `.ai/project-profile.md`, such as UI stack, target frameworks, test stack, storage model, service model, supported Windows versions, and build/test commands.
3. **Strong defaults**: preferred maintainability, readability, and efficiency practices. Follow them unless the existing project design or a documented decision has a better reason.
4. **Advisory guidance**: style, polish, naming nuance, and tool preferences. Apply by consequence, not by taste.

Do not turn advisory guidance into noisy review comments. Prefer high-signal findings that prevent bugs, misleading state, data loss, security exposure, unreadable code, inefficient realistic workloads, or expensive future maintenance.

## Core guardrails

- Preserve existing user-visible behavior unless explicitly asked to change it.
- Keep changes scoped to the request.
- Prefer simple, readable, maintainable code over cleverness.
- Follow the [commenting guidance](.ai/standards/coding-standards.md#comments-and-xml-documentation) whenever adding or changing C# code.
- Prefer efficient-enough designs for realistic workloads; avoid both premature cleverness and avoidable unbounded memory/I/O/database/UI-thread work.
- Prefer cohesive design over both god-classes and excessive micro-class fragmentation.
- Add abstractions only when they clarify ownership, isolate dependencies, protect boundaries, create useful test seams, or remove real duplication.
- Preserve declared architecture boundaries. In Clean/Layered projects, keep Core/Domain policy independent of Infrastructure, UI, transport, persistence, database, and OS details.
- Keep UI code focused on presentation state and user interaction. Durable work, backend execution, policy, persistence, external calls, database migrations, hashing, transfer, verification, and finalization belong outside UI code unless the project profile explicitly says otherwise.
- Keep durable state truthful. Do not report success for cancelled, partial, unverified, failed, queued-only, or merely attempted work.
- Prefer inspectable artifacts for durable state and diagnostics: JSON, JSON Lines, Markdown, HTML, and structured logs as appropriate.
- Use streaming, batching, paging, or bounded processing for unbounded or user-supplied payloads.
- Use cancellation tokens for long-running I/O, network, database, import/export, scan, hash, copy, verification, finalization, service, worker, and job operations.
- Treat external input as untrusted until validated, parsed, authorized, and converted at the boundary.
- Do not add, remove, or upgrade production dependencies without explaining why they are needed and what alternatives were considered.

## Target framework policy

Use the target framework and SDK policy declared in `.ai/project-profile.md` or the project files.

For new projects without a profile, prefer the current approved .NET LTS or STS for the organization/project. Do not retarget or downgrade a project merely to make a local build pass. If the required SDK is unavailable, report that clearly.

## Persistent implementation plans

Persistent implementation plans live under `.ai/implementation-plans/`:

- `.ai/implementation-plans/active/` — plans currently intended for execution.
- `.ai/implementation-plans/completed/` — finished plans retained for history.
- `.ai/implementation-plans/superseded/` — abandoned or replaced plans.

Use `implementation-plan` to create plans for non-trivial feature or bug work.
Use `refactor-planning` for structural refactors.
Use `plan-runner` to execute active plans stage by stage.

Open questions in implementation plans are decision gates. Do not implement behavior that depends on them until answered by the user or an authoritative project document.

## Workflow

- Inspect current files before editing.
- Check `git status --short` before editing and before summarizing when the repository is under Git.
- Treat modified, deleted, or untracked files you did not create as user work.
- Do not revert user work.
- Do not run `git reset --hard`, `git checkout --`, `git clean -fdx`, broad recursive deletes, or similarly destructive commands unless explicitly asked.
- Do not modify generated build output.
- Do not commit unless the user asks.
- Before adding or moving code, identify which layer/component owns the responsibility.
- After structural changes, check for design drift: dependency direction, ownership, state truthfulness, UI boundaries, persistence boundaries, security boundaries, and test coverage.
- Use `rg` or equivalent fast search when available.

## Build, test, and verification

Use the narrowest useful build/test/verification command for the change. Prefer commands declared in `.ai/project-profile.md`.

Common examples:

```powershell
dotnet build <solution-or-project> -nodeReuse:false
dotnet test <solution-or-project> -nodeReuse:false
dotnet format <solution-or-project> --verify-no-changes
```

Documentation-only changes do not require a build unless source, project, generated-code inputs, command behavior, artifact formats, schemas, migrations, or cross-references changed.

Do not claim a build, test, analyzer, migration, UI automation run, or manual verification passed unless it was actually performed. If verification cannot be run, state why and what remains unverified.

## Output expectations

When completing work, summarize:

- what changed;
- why it changed;
- verification performed or skipped;
- risks, assumptions, or follow-up work, if any.
