# Group dedicated App tests by feature

Status: Completed
Created: 2026-11-22
Last updated: 2026-11-22
Owner/context: Elena's Business Ledger test project

## Goal and scope

Mirror the App feature folders in `tests/ElenasBusinessLedger.Tests/App/Features` for fixtures owned by a single workflow. Keep test class names and namespaces so existing test filters still resolve. Shared support, mixed fixtures, and the Core, Infrastructure, and Packaging tests retain their locations. See `AGENTS.md` and the completed App feature folders plan for the workflow and source layout.

## Stages

1. **Move the dedicated fixtures.** Move three Invoices files, three Economy files, two Students files, one Plans file, and one Settings file. Check destination paths for collisions and compare the files by SHA-256 before and after each move. Preserve local edits in the invoice fixtures.
2. **Verify and archive.** Build and run the complete solution with `dotnet test ElenasBusinessLedger.sln -c Release -nodeReuse:false`. Compare the test total with the latest full run of 444 and check the scope of the worktree diff before closing this plan.

`EditorViewModelTests.cs` covers student and lesson editing, so the test reorganization has no separate Lessons folder yet. There are no open decisions. If a path collision or hash mismatch occurs, stop and inspect the affected move. If test discovery changes, check compile inputs before making any changes to the test fixtures.

## Final User Test Instructions

In Visual Studio, expand `ElenasBusinessLedger.Tests/App/Features` and check that the five folders appear. The full solution test command above verifies discovery and execution.

## Completion log

- 2026-11-22. Reviewed fixture ownership, existing local edits, and source path usage. No blocking user action.
- 2026-11-22. Stage 1 completed. Moved 10 dedicated files. Verified the contents and expected paths of all 19 App test sources, including the two invoice fixtures with local edits. No blocking user action.
- 2026-11-22. Stage 2 completed. The full Release solution suite passed all 444 tests, with zero failures and zero skips. Git status confirmed the ten expected test-file moves and git diff --check passed. No blocking user action.
