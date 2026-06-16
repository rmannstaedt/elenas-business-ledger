---
name: handoff-generation
description: Prepare concise continuation context for a future Codex session after meaningful work, partial progress, blocked work, or before switching tasks. Use to update .ai/context/HANDOFF.md or the project-approved handoff location.
---

# Handoff Generation

Use this skill to prepare continuation context for a future session.

A handoff should help the next session resume without relying on chat memory. It must be concise, truthful, and specific.

## When To Use

Use this skill:

- before ending a meaningful implementation or review session;
- after partial progress where another session may continue;
- when work is blocked by a decision, access issue, failing verification, stale plan, or user testing requirement;
- before running `plan-runner` across sessions;
- after architecture, documentation, project-profile, or standards work that future sessions need to know about.

Do not create a handoff for tiny self-contained edits unless the user asks.

## Read First

Read the relevant subset of:

- `README.md`;
- `AGENTS.md`;
- `.ai/project-profile.md`, if present;
- `.ai/context/architecture.md`, `.ai/context/decisions.md`, `.ai/context/progress.md`, and existing `.ai/context/HANDOFF.md`, if present;
- active implementation plans under `.ai/implementation-plans/active/` when related;
- current git status and diff when available;
- files changed or central to the current task;
- verification logs or command output from the current session.

## Handoff Contents

Update `.ai/context/HANDOFF.md` or the project-approved handoff location with:

- current project state;
- current goal and what prompted the work;
- recent changes completed;
- files changed or most relevant files to inspect next;
- decisions made and where they are documented;
- active implementation plan and current stage, if any;
- build/test/verification commands actually run and results;
- verification skipped and why;
- known risks and unverified areas;
- blockers and exact user actions needed, if any;
- next recommended step;
- docs and standards that must be read next time;
- important pitfalls, invariants, or safety constraints.

## Rules

- Keep it concise but complete enough for restart.
- Do not invent implementation progress.
- Distinguish completed work from proposed work.
- Distinguish verified behavior from unverified assumptions.
- Include commands actually run; do not claim verification passed unless it did.
- Avoid copying long transcripts.
- Avoid sensitive data, secrets, tokens, private keys, connection strings, or unnecessary private paths.
- Preserve user work and do not clean, reset, or reorganize files merely to make the handoff tidy.
- If a handoff contradicts formal docs or an implementation plan, either update the appropriate document through `documentation-refresh` or explicitly mark the inconsistency.

## Suggested Format

Use this shape unless the repository has its own template:

```markdown
# Handoff

## Current state

## Current goal

## Completed this session

## Files changed or important files

## Decisions made

## Verification performed

## Verification skipped or still needed

## Known risks and constraints

## Blockers or user actions required

## Recommended next step

## Read next time
```

## Output

Report:

- handoff file updated;
- key state captured;
- verification status captured;
- remaining blockers or next step;
- anything intentionally left out.
