---
name: documentation-refresh
description: Update repository documentation and AI context after code, architecture, commands, public behavior, artifacts, service behavior, UI behavior, project structure, or durable decisions change. Use when docs may be stale; do not change source code unless explicitly asked.
---

# Documentation Refresh

Use this skill when code, architecture, requirements, workflow, safety rules, command-line behavior, UI behavior, database behavior, artifact formats, configuration, project structure, or durable project decisions require documentation updates.

This skill keeps documentation aligned with the current implementation. It should update documentation and agent context only. It must not change C# source code, project files, schemas, migrations, generated artifacts, or runtime behavior unless the user explicitly asks for implementation work.

## Purpose

Documentation refresh has two responsibilities:

1. Update documentation after known changes.
2. Correlate documented behavior against the actual codebase so docs describe what the code really does, what is partially implemented, and where known gaps remain.

Do not treat existing documentation as automatically true. Verify important claims against source code, tests, project files, command definitions, artifact formats, configuration, and current repository structure when practical.

## When To Use

Use this skill:

- after meaningful implementation work;
- after architecture, requirements, workflow, safety, security, UI, CLI, database, service, worker, job, artifact, or project-structure changes;
- after public behavior, command-line behavior, artifact formats, configuration, migrations, or generated outputs change;
- after a feature or refactor changes how the system actually works;
- when documentation may have become stale;
- when documentation claims need checking against implementation;
- before handing work over to a future Codex session.

Do not use this skill for every tiny edit. For very small local changes, a short progress or handoff note may be enough.

## Inputs To Inspect

Read the relevant subset of:

- `README.md`;
- `AGENTS.md`;
- `.ai/project-profile.md`, if present;
- authoritative project docs under `docs/`;
- `.ai/context/*`, if present;
- `.ai/standards/*` when standards routing or rule text is affected;
- `.ai/prompts/*` or recurring prompt guidance when onboarding guidance may be affected;
- `.ai/implementation-plans/active/*`, `.ai/implementation-plans/completed/*`, and `.ai/implementation-plans/superseded/*` when related plans exist;
- relevant source files;
- relevant test files;
- relevant project files;
- current git status and diff when available.

Pay special attention to documentation that describes:

- safety, security, authorization, trust, or privacy guarantees;
- verification or finalization guarantees;
- retry, resume, cancellation, shutdown, timeout, and partial-failure behavior;
- destructive action guards;
- CLI commands, options, exit codes, and output;
- configuration and options;
- artifact names, JSON/JSONL formats, report formats, logs, migrations, and retention;
- service, worker, queue, job, scheduler, and background-processing flow;
- UI state, command availability, validation, progress, accessibility, and control-surface behavior;
- database schema, migrations, concurrency, transactions, and data access rules.

Do not convert implementation plans into permanent architecture documentation automatically. Move durable decisions and implemented behavior into formal docs; leave temporary execution history in implementation-plan or context files.

## Documentation Roles

Keep documentation responsibilities separated:

- Root `README.md` is the human-facing repository entry point.
- Root `AGENTS.md` is the agent entry point and navigation hub.
- `docs/` contains formal project documentation when the repository uses it.
- `.ai/project-profile.md` declares project-specific applicability, build/test commands, target frameworks, and stack choices.
- `.ai/context/` contains current agent-oriented project state, architecture memory, decisions, progress, and handoff context.
- `.ai/standards/` contains reusable and project standards.
- `.ai/implementation-plans/` contains temporary or durable execution plans, not authoritative architecture by itself.
- `.agents/skills/` contains Codex skills.

Do not mix these responsibilities unnecessarily.

## Implementation Correlation

When documentation describes behavior that should already exist, inspect implementation enough to classify the claim:

- **Implemented** — code appears to perform the documented behavior.
- **Partially implemented** — code performs some documented behavior but important cases, checks, flows, or guarantees are missing.
- **Not implemented** — docs describe planned or desired behavior that current code does not appear to support.
- **Implemented but undocumented** — code contains behavior, constraints, commands, file formats, safety checks, edge cases, or UX behavior not described in relevant docs.
- **Unclear** — implementation could not be verified confidently from inspected files.

When a documented feature is not implemented or partially implemented, do not silently rewrite docs to pretend it works. Make docs honest by marking the behavior planned, partial, pending, limited, or requiring follow-up, depending on evidence.

When code behavior differs from documentation, prefer truthfulness over aspiration.

## Workflow

1. Identify the requested documentation scope.
2. Inspect the relevant docs before editing.
3. Identify which documents are authoritative for the affected topic.
4. Inspect relevant source, tests, project files, command definitions, schemas, migrations, UI surfaces, or artifacts needed to verify claims.
5. Compare documentation claims against actual implementation.
6. Classify mismatches as stale docs, implementation gaps, undocumented behavior, or unclear evidence.
7. Update only affected documents and context files.
8. Preserve stable rule IDs unless the rule itself changes.
9. If a rule changes meaning, update references and explain the change.
10. Keep root files concise; move detailed guidance into `docs/`, `.ai/context/`, or `.ai/standards/` as appropriate.
11. Keep `.ai/context/*` current enough for future sessions.
12. Check cross-references, filenames, terminology, command names, paths, and duplicated statements.
13. Do not invent requirements, implementation progress, safety guarantees, or design decisions unsupported by codebase or project context.

## Consistency Checks

Search for stale:

- paths and folder names;
- filenames and documentation conventions;
- old project names;
- command names, options, exit codes, and examples;
- target frameworks and SDK claims;
- rule IDs;
- type, namespace, component, or runtime-role names;
- artifact names and schemas;
- status claims such as implemented, complete, supported, guaranteed, validated, verified, final, or deprecated.

Check that `AGENTS.md`, `README.md`, `.ai/project-profile.md`, `docs/`, `.ai/context/`, implementation plans, and standards do not contradict each other.

## Handling Code/Documentation Mismatches

Choose the smallest truthful documentation update:

- If code is correct and docs are stale, update docs.
- If docs describe intended future behavior, mark it planned, pending, or not yet implemented.
- If code has behavior missing from docs, add concise documentation in the right place.
- If code appears wrong or incomplete, document the gap and recommend `code-review`, `architecture-review`, or `refactor-planning` rather than changing code.
- If evidence is uncertain, state uncertainty and recommend the next verification step.

Do not fix code during documentation refresh unless explicitly asked.

## Handoff, Decisions, and Progress

When updating `.ai/context/HANDOFF.md`, include current project state, active goals, important architecture/safety context, files/modules in focus, unresolved issues, recommended next actions, and warnings future sessions should know.

When updating `.ai/context/decisions.md`, record durable decisions only. Include the decision, rationale, tradeoffs, alternatives when relevant, and implications for future development.

When updating `.ai/context/progress.md`, include recent work completed, current status, important discoveries, documentation/implementation mismatches found, blockers/open questions, recommended next steps, and files/modules in focus. Keep it current; do not turn it into a historical dump.

## Code Comments and XML Docs

Documentation refresh should not become a general code-commenting pass.

If documentation review reveals stale or misleading nearby comments or XML docs, call that out. Only update code comments or XML docs if the user explicitly allowed documentation-adjacent code-comment changes.

When comments are updated, follow the coding standards: explain why, preserve non-obvious mechanics and boundaries, keep comments short, remove stale comments, and avoid decorative narration.

## Verification

Documentation-only changes do not require a build unless source files, project files, generated-code inputs, command behavior, artifact formats, schemas, or migrations changed.

Documentation-only changes do require a consistency pass:

- check referenced files exist where practical;
- search for stale paths and names;
- check links and cross-references where practical;
- check documented commands, options, artifacts, schemas, and paths against current source where practical;
- compare important safety, security, verification, workflow, UI, database, and architecture claims against relevant code;
- note claims that could not be verified.

If source files or project files were unexpectedly changed, stop and report that source changes occurred. Run build/test only if source or project edits were authorized and relevant.

## Output

Summarize:

- documents changed;
- why they changed;
- implementation/documentation mismatches found;
- implementation gaps documented;
- implemented-but-undocumented behavior added;
- consistency checks performed;
- rule IDs added, changed, deprecated, or marked not yet implemented;
- source files inspected for correlation;
- verification performed;
- anything intentionally left unchanged;
- unresolved questions or recommended follow-up reviews.

If no documentation updates are needed, say so clearly and include remaining verification gaps.
