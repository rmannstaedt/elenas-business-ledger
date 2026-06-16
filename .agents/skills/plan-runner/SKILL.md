---
name: plan-runner
description: Execute an existing persistent implementation plan under .ai/implementation-plans/active stage by stage. Use when the user asks to continue or run an active plan. Preserves decision gates, verification gates, and final user test instructions.
---

# Plan Runner

Use this skill to execute a persistent implementation plan stage by stage.

This skill runs an existing plan from `.ai/implementation-plans/active/`. It should not invent a plan from scratch. Plans are normally created by `implementation-plan`, `refactor-planning`, or manually by the project owner.

The runner must preserve control points: automate implementation work where safe, but stop for open questions, failed verification, stale plans, safety-sensitive decisions, public behavior changes, durable format changes, explicit approval requirements, manual testing gates, unrelated working-tree changes, or unavailable required access.

At the end of every stage, explicitly classify what the user is expected to do as either **User action required** or **No blocking user action**.

## Purpose

Continue or execute staged implementation work without relying on chat memory.

The runner should:

- select the correct active plan;
- verify the plan is still current;
- execute one coherent stage at a time;
- update the plan after each completed stage;
- run or recommend the stage’s verification;
- preserve behavior and safety boundaries declared by the plan;
- keep and surface final user test instructions;
- stop at required decision, approval, manual testing, or verification gates;
- continue automatically to the next stage when no blocking action remains and the next stage is clear;
- run or recommend follow-on skills such as `code-review`, `documentation-refresh`, `architecture-review`, or `ui-review` when the plan calls for them.

## When To Use

Use this skill when:

- the user asks to implement an existing implementation plan;
- the user asks to continue an active plan;
- the user asks to run the next stage of a plan;
- a staged refactor or feature plan exists under `.ai/implementation-plans/active/`;
- prior work needs to continue from durable plan state rather than chat memory.

Do not use this skill when:

- no implementation plan exists;
- the requested work is a simple one-step edit;
- the plan is stale or contradicted by current code and needs replanning;
- the user is asking for planning rather than execution;
- the active directory contains multiple plausible plans and the user has not identified which one to run.

If no suitable plan exists, recommend `implementation-plan` or `refactor-planning` first.

## Read First

Read the relevant subset of:

- `AGENTS.md`;
- `README.md`;
- `.ai/project-profile.md`, if present;
- the selected implementation plan under `.ai/implementation-plans/active/`;
- `.ai/context/architecture.md`, `.ai/context/decisions.md`, `.ai/context/progress.md`, and `.ai/context/HANDOFF.md`, if present;
- `.ai/standards/architecture-principles.md`;
- `.ai/standards/coding-standards.md`;
- `.ai/standards/testing-and-verification.md`;
- `.ai/standards/security-and-boundaries.md` when boundaries are involved;
- `.ai/standards/persistence-and-artifacts.md` when artifacts or durable files are involved;
- `.ai/standards/database-standards.md` when database work is involved;
- `.ai/standards/service-worker-and-job-design.md` when services, workers, schedulers, queues, jobs, or background processing are involved;
- `.ai/standards/ui-design-standards.md` and `.ai/standards/wpf-mvvm-standards.md` when UI/WPF work is involved;
- relevant docs listed by the plan;
- relevant source and test files;
- current git status and diff when available.

Do not read every file mechanically. Read what is needed to validate and execute the current stage.

## Plan Selection

If the user names a specific active plan, use that plan.

If the user asks generally to continue or run implementation plans:

1. Inspect `.ai/implementation-plans/active/`.
2. List active plans found.
3. If exactly one active plan exists, select it.
4. If multiple active plans exist, ask the user which one to run.
5. If no active plans exist, report that no active plan is available and recommend `implementation-plan` or `refactor-planning`.

Do not execute multiple active plans in one run unless the user explicitly asks and the plans are clearly independent.

## Pre-Run Validation

Before implementing any stage, verify the selected plan is usable.

Check:

- plan status is `Active`;
- plan has a clear goal and non-goals;
- stages are defined;
- open questions are identified;
- the next uncompleted stage is clear;
- assumptions still match current code, tests, docs, and git status;
- affected files/classes still exist or have obvious current equivalents;
- prior completed stages appear reflected in code or documentation;
- the plan contains final user test instructions, or the current run can add them before completion;
- the working tree state is understood.

If the plan is stale, ambiguous, contradicted by current code, missing important decisions, or unsafe to continue, stop and report the exact issue before implementing.

## Open Questions and User Input

Open questions are decision gates.

Before implementing a stage, check whether any open question affects the current stage or any later irreversible decision.

If yes:

- stop;
- list the relevant questions in a **Decision Points** section;
- for each decision, state the recommended/default answer from the plan when one exists;
- list meaningful alternatives or say no safe alternative is known;
- explain why each answer matters to safety, architecture, behavior, artifacts, verification, user experience, or maintainability;
- ask for the exact input needed to continue;
- do not silently choose defaults unless the answer is already present in the current session or an authoritative project document.

If implementation discovers a new decision point, add it to the plan and stop before proceeding if it affects behavior, safety, security, architecture, durable formats, public contracts, persistence, migrations, UI behavior, or user-visible results.

## Stage Execution Rules

Execute only one stage at a time unless the plan explicitly marks several stages as safe to run together and none contains a stop condition.

For each stage:

1. Restate the stage goal.
2. Identify affected files/classes.
3. Check whether user input is required.
4. Implement only the planned changes for that stage.
5. Preserve the behavior-preserving boundary.
6. Avoid broad rewrites and unrelated cleanup.
7. Run or recommend the stage’s verification.
8. Update the implementation plan with results.
9. Classify the end-of-stage user action.
10. If this is the final implementation/documentation stage, prepare completed-plan user test instructions.
11. Continue automatically to the next stage when no blocking action remains and the next stage is clear.

Do not skip stages unless the user approves or the plan is updated to explain why a stage became unnecessary. Do not merge unrelated stages merely because they touch nearby files.

## End-of-Stage Action Classification

Use one of these labels in the report:

- **User action required** — execution is stopped until the user does something.
- **No blocking user action** — the runner can continue automatically to the next stage when a next stage exists.

Blocking actions include:

- answering an open question or new decision point;
- approving a material deviation from the plan;
- approving a safety-sensitive behavior, public contract, durable artifact, database schema, migration, or persistence change when approval is not already given;
- choosing what to do after failed or incomplete verification;
- performing manual/user testing required before the next stage;
- granting access, credentials, sandbox permission, process/file-lock intervention, or environment setup required to proceed;
- resolving unexpected unrelated working-tree changes that affect the stage;
- authorizing a follow-on scope when the plan requires a skill such as `code-review`, `architecture-review`, `ui-review`, or `documentation-refresh` and that scope was not already authorized.

Non-blocking actions include:

- reading the summary;
- optional review when verification passed and no user decision is needed;
- mandatory post-completion user acceptance testing after all implementation is complete, because the runner has no remaining implementation stage to unblock;
- awareness of residual risks that do not affect the next stage;
- review stop points whose only purpose is to make completed work visible and contain no explicit approval requirement.

If a plan says the runner may not continue automatically after a stage, translate that into a concrete blocking action. If no concrete action exists, report the review note as non-blocking and continue.

## Stop Conditions

Stop before proceeding when:

- an open question affects the current or next stage;
- build, tests, analyzers, migration checks, UI automation, or other required verification fails;
- verification cannot be run and the stage is risky;
- implementation differs materially from the plan;
- a new design, safety, security, public contract, durable format, persistence, migration, or UI-behavior decision is discovered;
- user-visible behavior may change outside the approved plan;
- runtime topology or dependency direction may change outside the approved plan;
- the stage reaches a review stop point with a concrete blocking action;
- the plan requires manual verification before continuing;
- the working tree contains unexpected unrelated changes.

When stopping, explain what was completed, what was not completed, why execution stopped, the exact user action required, each decision point with recommendation/alternatives/impact, and which stage should run next after approval.

Do not stop merely to ask for a generic OK when verification passed, the plan is updated, no unresolved decision remains, and the next stage is clear.

## Final User Test Instructions

Every active implementation plan should contain a **Final User Test Instructions** section, either from the template or added by the runner before the plan is completed.

Before marking a plan complete or moving it to `completed/`, the runner must:

1. Review final user test instructions against the implemented result.
2. Update instructions if implementation changed the expected path, artifact name, command, setup, cleanup, or expected result.
3. Treat final manual testing as mandatory post-completion user testing, not as a blocking runner gate after all implementation/verification is done.
4. Include a user-facing section titled **How To Test The Completed Plan** in the final report.

Final test instructions must include prerequisites/setup, exact user actions or CLI commands, expected success result, expected failure/safety result when relevant, where to inspect artifacts/logs/UI state, cleanup/reset instructions, and the sentence `Post-completion user test: mandatory.`

If no meaningful manual test exists, say so and explain which automated verification commands prove the result, then state what the user should inspect to accept the work.

## Plan Updates

After each stage, update the implementation plan with:

- stage status;
- files changed;
- implementation summary;
- tests added or updated;
- verification commands run;
- verification results;
- final user test instructions when the plan is complete or they change;
- deviations from the plan;
- new risks or assumptions;
- new open questions;
- next recommended stage;
- date of update.

Do not mark a stage complete unless completion criteria are satisfied or the user explicitly accepts remaining work.

If a plan is finished, move or recommend moving it from `.ai/implementation-plans/active/` to `.ai/implementation-plans/completed/`. If replaced or no longer valid, move or recommend moving it to `.ai/implementation-plans/superseded/`.

When moving implementation plans, update repository bookkeeping only if the repository already tracks documentation/plan files in solution files, manifests, project metadata, or docs indexes. Do not add Visual Studio-specific bookkeeping unless the project already uses it.

## Follow-On Skills

Run or recommend follow-on skills when the plan calls for them:

- `code-review` for non-trivial, safety-sensitive, cross-layer, contract-sensitive, persistence/artifact, service/job, database, verification, or UI-behavior changes.
- `documentation-refresh` when durable architecture, project rules, public behavior, CLI/UI behavior, safety guarantees, runtime topology, project structure, artifact formats, or important project state changed.
- `architecture-review` when layer ownership, dependency direction, runtime topology, service boundaries, or major workflow ownership changed.
- `ui-review` when user-facing UI behavior, accessibility, command state, progress, layout, or WPF/MVVM behavior changed.

If a follow-on skill is required by the plan, do not silently omit it. Either run it if scope is authorized, or stop and request the required scope.

## Verification

Use verification commands listed by the implementation plan when practical. Prefer the narrowest useful verification for the current stage.

For safety-sensitive, persistence/artifact, service/job, database, migration, endpoint, external-process, verification, or UI behavior changes, build success alone is not enough. Run or recommend behavioral tests or scenario checks.

Do not claim verification passed unless it was actually run. If verification cannot be run, record why in the plan and report what remains unverified.

## Output Format

After each stage, report:

1. Plan selected.
2. Stage executed.
3. Files changed.
4. Summary of implementation.
5. Tests added or updated.
6. Verification run and results.
7. Plan updates made.
8. Deviations from the plan, if any.
9. New risks or open questions.
10. End-of-stage user action classification.
11. Exact user action required, if blocked.
12. Whether the runner is continuing automatically and to which stage, if not blocked.
13. Recommended next stage.

If blocked by a decision, approval, or open question, include **Decision Points** before the next-stage note.

When reporting final plan completion, also include **How To Test The Completed Plan** and `Post-completion user test: mandatory.`

If execution cannot begin, report why, what information or decision is needed, and whether `implementation-plan`, `refactor-planning`, `code-review`, `architecture-review`, `ui-review`, or `documentation-refresh` should run first.
