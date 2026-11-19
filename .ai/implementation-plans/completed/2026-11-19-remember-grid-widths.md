# Remember grid widths

- Status: Completed
- Created and completed: 2026-11-19
- Context: My Small Business Ledger desktop usability

## Goal

Keep the widths a user gives the Plan column and other resizable table columns between launches. Remember the divider position between Lessons and Student Invoices so both panes reopen with room for the saved columns.

The dashboard and invoice windows currently have resizable columns with no preference wiring. Manage Plans and Students already save widths. Student Details and Economy Overview use fixed column layouts.

## Scope and decisions

- Keep layout preferences in the App layer and reuse `grid-column-widths.json`. Each window has its own grid keys. Read the existing numeric arrays for Manage Plans and Students.
- Keep fixed-width columns in device-independent pixels and proportional columns flexible as the window resizes. Store the dashboard's preferred lesson pane width, with a current-width limit that leaves room for Student Invoices.
- Apply this to widths and the existing divider. Column order, view selection, and ledger data are outside scope.

## Stages

1. Update the layout store and wire it into resizable windows. Restore before the first layout and save during the window close lifecycle. Fall back to the view defaults for unusable saved settings. Review existing preference compatibility and window resizing.
2. Exercise a WPF resize, close, and reopen with isolated preferences. Check the Plan width, invoice column proportions, lesson pane width, and available space in a narrower window. Build and run relevant tests. Document the layout reset path in the README.

Both stages can proceed with the existing project scope. The source edits and preference file can be rolled back independently of ledger data.

## Final user test

Start the updated build. Widen Plan and move the divider to give Lessons more room. Close the application, then reopen it and confirm the widths and divider are restored. Make the window narrower and confirm both panes remain accessible. To reset column and divider widths, close the Ledger and rename `grid-column-widths.json` in its application data folder.

Post-completion user test: mandatory.

## Completion

- Added width persistence to the dashboard lesson and invoice grids and the two invoice line tables. The Students and Manage Plans grids use their existing keys. The dashboard clamps the effective divider position when the window narrows and recovers the preferred width as space becomes available.
- The WPF regression test moves the Plan and invoice header grippers and the pane divider, closes and reopens the window, and verifies the restored widths at normal and minimum window sizes. It uses isolated preference files.
- The Debug solution build passed with zero warnings and zero errors. `dotnet test` could not connect to its test host in this session. The compiled test assembly ran through xUnit directly with 405 passes and a successful exit after explicit WPF dispatcher shutdown in the main-window tests.
- The README documents how to reset column and divider sizes.
