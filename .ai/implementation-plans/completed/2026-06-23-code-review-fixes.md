# Implementation Plan: Code Review Fixes

Status: Completed
Created: 2026-06-23
Last updated: 2026-06-23
Owner/context: Ruben / Codex, My Small Business Ledger WPF pilot

## Goal

Fix the accepted code-review findings around student identity, lesson timezone offsets, invoice candidate preselection, Manage Students live-selection ordering, and Google Calendar paging.

## Non-Goals

- Do not implement the separate invoice issue/print-status plan here.
- Do not redesign student plan history editing beyond preserving current behavior.
- Do not introduce new production dependencies.

## Desired End State

- Students have a durable database identity, and editing a student's short name updates the selected student instead of inserting a second row.
- Lesson date/time edits calculate the correct local timezone offset for the selected date.
- Invoice candidate preselection chooses the current lesson only when the student has no invoice for that month, otherwise the next eligible lesson within 30 minutes.
- Manage Students live-selection refreshes cannot apply stale main-window state after a newer selection.
- Google Calendar event listing follows all result pages.

## Staged Implementation Plan

### Stage 1: Durable Student Identity

- Add student id to Core student records, save requests, editor snapshots, overview snapshots, and SQLite repository mapping.
- Keep existing student ids stable; new students are inserted with generated SQLite ids.
- Add tests proving short-name edits update the existing row.

### Stage 2: Lesson Timezone Offset

- Build lesson edit timestamps from a timezone-aware date/time conversion rather than a stored offset.
- Add tests covering cross-DST date edits.

### Stage 3: Invoice Candidate Preselection

- Use lesson intervals and invoice status when choosing preselected candidate.
- Prefer a currently running eligible lesson; otherwise choose the next eligible lesson within 30 minutes.
- Add tests for in-lesson selection, already-invoiced exclusion, and next-lesson fallback.

### Stage 4: Manage Students Selection Ordering

- Version or serialize main-window selection refreshes so older refreshes cannot overwrite newer selections.
- Add ViewModel/launcher tests where practical.

### Stage 5: Google Calendar Paging

- Follow `NextPageToken` in the Google Calendar events client.
- Add client tests for multi-page listing.

## Verification Strategy

Run focused tests after each stage where practical, then:

```powershell
dotnet build MySmallBusinessLedger.sln -nodeReuse:false
dotnet test MySmallBusinessLedger.sln -nodeReuse:false -p:UseSharedCompilation=false
```

## Final User Test Instructions

Post-completion user test: mandatory.

1. Open Manage Students, edit an existing student's short name, save, close, and confirm the student appears once with existing plan/invoice data still attached.
2. Edit or add a lesson on a date across a DST boundary and confirm Google Calendar shows the intended wall-clock time.
3. Start Write Invoice near a lesson time and confirm the intended eligible student is preselected; confirm a student already invoiced for the month is skipped.
4. Select several students quickly in Manage Students and confirm the main window ends on the most recent selection.
5. Use a calendar with enough events to page, or rely on automated tests for paging.

## Completion Log

- 2026-06-23:
  - Stage: Planning
  - Files changed: `.ai/implementation-plans/active/2026-06-23-code-review-fixes.md`
  - Summary: Created plan for accepted code-review fixes.
  - Tests/verification run: Not run; planning only.
- 2026-06-23:
  - Stage: Implementation
  - Files changed: Core student records/save requests, SQLite student repository, student editor snapshots, lesson view models, invoice candidate use case, Google Calendar event client, and regression tests.
  - Summary: Implemented durable student IDs, date-aware local timezone offsets, invoice candidate interval/eligibility preselection, stale live-selection protection, and Google Calendar paging.
  - Tests/verification run: `dotnet test MySmallBusinessLedger.sln -nodeReuse:false -p:UseSharedCompilation=false`; `dotnet build MySmallBusinessLedger.sln -nodeReuse:false -p:UseSharedCompilation=false`.
