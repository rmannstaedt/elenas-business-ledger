---
name: code-review
description: Review changed or existing C#/.NET code for bugs, regressions, unsafe edge cases, weak contracts, SOLID/local design issues, coding-standard violations, performance risks, and verification gaps. Use for code-level review, not broad architecture review.
---

# Code Review

Use this skill to review the codebase for bugs, regressions, unsafe edge cases, weak contracts, coding-standard violations, SOLID/design problems, verification gaps, and local structural issues.

This is a code-level and code-structure review skill. It is not a full architecture review, but it should catch architectural pressure when it appears locally in code: unclear ownership, dependency-direction problems, responsibilities drifting into the wrong layer, overly broad contracts, unsafe abstractions, or SOLID violations.

Do not edit code unless the user explicitly asks for fixes.

## Purpose

Determine whether the current implementation is correct, safe, readable, maintainable, efficient enough for expected workloads, and aligned with the repository’s standards and project profile.

Catch issues such as:

- likely bugs and behavioral regressions;
- incorrect edge-case handling;
- unsafe filesystem, database, network, external process, or UI behavior;
- weak verification or false success conditions;
- mismatches between the state a command mutates and the source later used by billing, reporting, audit, permissions, previews, exports, synchronization, destructive actions, or UI refresh;
- overly loose, misleading, or under-specified contracts;
- cancellation, retry, timeout, resume, idempotency, shutdown, or partial-failure bugs;
- missing failure recording or poor error reporting;
- resource lifetime, disposal, concurrency, async, or thread-affinity problems;
- inefficient unbounded memory, I/O, rendering, database, network, or UI-thread work;
- SOLID/local ownership drift;
- unclear responsibility boundaries;
- over-broad interfaces or abstractions;
- implementation choices that make future change harder;
- missing or inadequate tests.

## When To Use

Use this skill:

- after meaningful implementation changes;
- before committing or merging;
- after Codex changed source files;
- after a bug fix;
- after a feature touches multiple files or subsystems;
- when changes affect safety, security, persistence, artifacts, services, jobs, UI behavior, databases, CLI behavior, external processes, or public contracts;
- when changed code may have consequences outside the files directly edited;
- when the user asks for a whole-solution review;
- when the user asks whether code adheres to standards.

Do not use this skill for trivial documentation-only edits, harmless renames, or pure formatting unless the user explicitly asks.

## Review Mode vs Fix Mode

Default to review-only.

In review mode:

- do not edit source code;
- do not silently fix findings;
- report findings clearly enough that the user can decide what to fix;
- recommend the smallest safe fix for each finding.

Only enter fix mode if the user explicitly asks to fix issues.

In fix mode:

- fix only the requested findings or severity levels;
- keep changes small and reviewable;
- do not start broad refactoring unless the finding cannot be fixed safely without it;
- preserve current behavior except where the finding is itself incorrect behavior;
- run or recommend relevant verification after changes.

## Read First

Read the relevant subset of:

- `AGENTS.md`;
- `README.md`;
- `.ai/project-profile.md`, if present;
- relevant project docs under `docs/`;
- `.ai/standards/coding-standards.md`;
- `.ai/standards/review-checklists.md`;
- `.ai/standards/architecture-principles.md` when ownership or boundaries matter;
- `.ai/standards/domain-data-integrity.md` when durable business entities, financial records, audit history, cross-record state, external-system synchronization, identity, lineage, references, or snapshots are involved;
- `.ai/standards/security-and-boundaries.md` when trust, paths, external inputs, secrets, IPC, network, processes, installers, or privacy are involved;
- `.ai/standards/persistence-and-artifacts.md` when files, JSON, JSONL, logs, reports, import/export, artifacts, or durable state are involved;
- `.ai/standards/database-standards.md` when SQL, SQLite, EF Core, migrations, transactions, or database persistence are involved;
- `.ai/standards/service-worker-and-job-design.md` when services, workers, queues, schedulers, jobs, retry, resume, shutdown, staging, or progress are involved;
- `.ai/standards/ui-design-standards.md` and `.ai/standards/wpf-mvvm-standards.md` when UI, WPF, commands, state, binding, selection, accessibility, or ViewModels are involved;
- relevant source and test files;
- current git diff/status when available.

Also read `references/code-review-checks.md` in this skill for a comprehensive checklist. Use it as a prompt, not as a mandatory output section.

Do not read every document mechanically. Read what is relevant to the review scope and explain important scope limits.

## Review Scope

Default to a whole-solution impact review when the user asks for a general code review.

If the user provides a specific diff, file, or concern, focus there but follow consequences through callers, implementations, tests, contracts, DTOs, persistence/artifact paths, DI registrations, UI/CLI entry points, database queries/migrations, service/job paths, verification/finalization paths, and error-reporting paths when relevant.

When code mutates state that is also represented in a UI projection, cache, external system, local database, generated artifact, queue, report, or read model, trace both the mutation path and the next consequential read path. Consequential readers include billing/previews, reports, exports, audits, permissions, finalization, cleanup, synchronization, destructive actions, and any UI that claims the operation succeeded.

For very large solutions, prioritize high-risk paths first and clearly state what was not reviewed. Prefer depth on high-risk paths over shallow commentary on every file.

## Domain Identity, Lineage, And Reference Integrity

When reviewing durable business data, do an explicit identity pass.

Trace important identifiers through:

- UI/view models, commands, requests, and use-case inputs;
- domain drafts, final/issued records, snapshots, and result models;
- repositories, persistence entities, migrations, and query projections;
- external API payloads and local mapping records;
- generated PDFs, reports, exports, logs, and read models when they support later actions.

Check whether:

- each state change targets an exact durable entity;
- each successful state-changing command updates the source that later consequential readers actually query before it reports success;
- IDs needed downstream are preserved rather than dropped because the immediate display does not need them;
- financial, tax, invoice, payment, receipt, credit, audit, import/export, reconciliation, printed, completed, locked, or finalized state is never inferred from names, dates, times, titles, prices, filenames, formatted text, or other descriptive fields;
- snapshots are used for historical display while stable references are used for ownership and state application;
- natural keys are documented, validated, constrained, immutable enough for the domain, and tested against collision/edit cases;
- missing, stale, deleted, or ambiguous references fail safe instead of falling back to broad matching;
- cleanup, refresh, reconciliation, cache invalidation, startup repair, or background work is a recovery fallback and not the only path that makes a just-completed user action safe;
- database schema, EF entities, domain models, DTOs, and tests agree about required identities and null/default behavior;
- tests include duplicate descriptive values such as same name/date/time/category/amount where state must remain distinct.

When a workflow spans multiple state representations, make a small mutation/read matrix: command, source(s) mutated, UI/read-model state changed, source queried by the next consequential workflow, recovery/cleanup path, and failure window between them. Report gaps where a just-completed command can be followed immediately by a stale consequential read.

Failure scenarios to actively look for:

- a command removes or changes one representation and updates visible state, but a later billing/report/export/audit/permission/destructive workflow still reads a stale row, cache entry, queue item, artifact, or projection;
- a command writes local durable state and shows success, but the external item, generated artifact, queue message, cache, search index, or read model used by later workflows was not created or updated;
- tests cover the mutation and downstream reader independently but not the immediate sequence `mutate -> downstream read`.

Treat a lost or weak identity in financial, billing, payment, tax, issued-document, or audit code as a likely Critical finding. It is not enough that the current data set has no duplicate values; review against plausible domain data and future edits.

## Review Priorities

Prioritize findings in this order:

1. Data loss, corruption, false success, unsafe destructive behavior, stale split-source state reaching a consequential reader, state attached to the wrong durable entity, or security/trust-boundary failures.
2. Authorization, path traversal, external input, process execution, secret/privacy, tenant/user/workspace boundary risks.
3. Verification correctness, proof, finalization, idempotency, and durable state truthfulness.
4. Persistence, database, artifact, migration, queue, cache, identity, lineage, or durable-reference consistency risks.
5. Cancellation, retry, timeout, resume, shutdown, and partial-failure recovery correctness.
6. Runtime bugs, exceptions, races, deadlocks, resource leaks, and thread/UI-affinity problems.
7. Contract correctness, identity preservation, reference soundness, and interface soundness.
8. Unbounded memory, CPU, I/O, database, network, rendering, or concurrency cost that can hurt realistic workloads.
9. Layer boundaries and local ownership drift.
10. SOLID/design-standard adherence.
11. Test coverage and verification gaps.
12. Simplicity, readability, and maintainability.

Do not inflate minor style issues above correctness or safety.

## General Code Review Checks

Read `references/code-review-checks.md`. Apply only the sections relevant to the project, changed code, and review scope.

## Severity Levels

Classify findings as:

- **Critical** — likely data loss, corruption, security issue, false success, destructive action without proper guard, broken build, severe correctness failure, stale split-source state reaching a financial/audit/destructive/permission reader, or financial/audit state being applied to the wrong durable entity.
- **High** — likely runtime bug, serious safety/workflow/security bug, important contract flaw, major test gap around risky behavior, or significant local ownership/design drift.
- **Medium** — plausible edge-case failure, maintainability issue, incomplete verification, unclear contract, SOLID/coding-standard concern, performance risk for realistic workloads, or missing tests that should be addressed soon.
- **Low** — minor clarity, naming, style, or small cleanup issue worth fixing only if nearby.

Do not inflate severity. Do not report purely subjective style preferences as findings.

## Review Rules

- Findings come first.
- Do not manufacture findings to satisfy a format.
- Use concrete file, line, type, method, configuration, schema, command, or symbol references where possible.
- Distinguish confirmed problems from plausible risks.
- Distinguish bugs from design concerns.
- Distinguish local code-structure issues from full architecture issues.
- Preserve user-visible behavior unless a behavior change was explicitly requested.
- Prefer small targeted fixes over broad rewrites.
- Do not propose speculative abstractions.
- Do not recommend changing source code during review unless the user asked for fixes.
- Group repeated symptoms under one root cause.
- If a finding is architectural rather than local, recommend `architecture-review`.
- If a finding suggests structural friction, recommend `refactor-planning` before editing.
- If documentation appears inconsistent with code, recommend `documentation-refresh`.
- If no important issues are found, say so clearly and mention remaining verification gaps.

## Output Format

Group findings by severity.

For each finding include:

- severity;
- file/location;
- problem;
- evidence and failure scenario;
- likely impact;
- suggested fix;
- classification: bug, contract issue, SOLID/design issue, coding-standard issue, performance issue, test gap, documentation mismatch, or architecture warning.

Then include:

- review scope;
- files/areas inspected;
- important areas not inspected, if any;
- verification status;
- remaining test gaps;
- mutation/read-source gaps in split-source workflows, when relevant;
- whether documentation refresh is warranted;
- whether architecture review is warranted;
- whether refactor planning is warranted;
- recommended next actions.

If no important findings are found, say so clearly and include scope reviewed, verification performed, remaining blind spots, and tests still recommended.
