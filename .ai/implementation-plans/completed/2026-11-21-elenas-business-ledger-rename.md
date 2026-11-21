# Rename the project to Elena's Business Ledger

- Status: Completed
- Created and last updated 2026-11-21
- Owner: Ruben / Codex

## Goal

Use **Elena's Business Ledger** as the product and workspace folder name and `ElenasBusinessLedger` for identifiers in the solution, projects, assemblies, and source. Use the description “A Windows app for managing student lessons, invoices, and payments.”

## Compatibility

The earlier product name is part of the Windows data folder, installer application ID and install directory, installed launcher filename, and Google token protection entropy. Preserve those compatibility identifiers so previous installs and shortcuts can open the same SQLite database, preferences, and saved Calendar authorization. Keep existing EF migration IDs and schema. The new setup executable, Windows metadata, and shortcuts use the new product name. Git history and the configured remote move with the workspace.

## Stages

1. Rename the solution, four project directories and files, project references, managed namespaces, WPF references, and test references. Set the app name and description in the UI and package metadata. Update the OAuth ignore rule before moving its parent directory. Verify the existing storage path and a token protected with the original entropy.
2. Rename and compile the installer and update the README, project profile, handoff, and active command references. Verify the four-project solution builds and the xUnit suite passes on Windows. Build the self-contained installer in staging to preserve earlier setup files. Inspect the assembly and setup metadata and the existing installation identifiers.
3. Verify the Windows destination path, including the apostrophe. Move the workspace as one directory so uncommitted and ignored files travel with it. Check the solution, Git worktree, and ignored OAuth configuration at the destination. Record results in the handoff and this plan.

If build verification fails, correct the renamed references before moving the workspace. If Windows holds the source directory open or denies the move, retain the verified changes in place until the directory can be moved. Preserve the original setup files for rollback.

## Related guidance

Follow `.ai/project-profile.md`, the repository testing and security standards, and the `documentation-refresh` workflow. Review the final diff for WPF, EF Core migration, and installer identity regressions.

## Final User Test Instructions

Post-completion user test: mandatory.

1. Open `ElenasBusinessLedger.sln` under `D:\Projects\Elena's Business Ledger` and start the App project. Check the main window and About dialog for the new name.
2. Open Settings and confirm the existing database path ends in `visualtech.dk\MySmallBusinessLedger\ledger.db`. Verify the expected students and invoices appear. If Google Calendar was connected before, verify it still refreshes without reconnecting.
3. To verify upgrade behavior, use a test Windows installation with a copy of the prior Ledger data. Run the renamed setup and open the app from an existing shortcut and the new Start menu entry. Both should open the renamed app with the existing data. Keep the prior installation and its data available for rollback.

## Completion log

- Inspected the current solution, runtime paths, Google token protection, and installer before editing.
- The four renamed projects built with zero warnings and errors in a folder named Elena's Business Ledger. All 407 tests passed through the direct xUnit runner, including the original DPAPI token fixture and the existing database migration tests. Inno Setup built the renamed v1.31.0 Windows x64 installer. Verified app and installer metadata and moved the installer beside the prior setup files.
- Windows initially returned a sharing violation on both folder rename attempts. The renamed solution and installer were ready for the folder move.
- Ruben moved the workspace to `D:\Projects\Elena's Business Ledger` on 2026-11-21. Verified Git HEAD and worktree access, all four solution projects, the installer checksum, and the ignored local OAuth path at the new location.
