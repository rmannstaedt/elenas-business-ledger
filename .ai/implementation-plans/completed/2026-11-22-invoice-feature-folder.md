# Group the WPF invoice feature

Status: Completed
Created: 2026-11-22
Last updated: 2026-11-22
Owner/context: Elena's Business Ledger App project

## Goal and scope

Group the invoice preview, viewer, and recovery XAML and code-behind, their view models, and invoice-specific App services and contracts under `src/ElenasBusinessLedger.App/Features/Invoices`. Keep the existing CLR namespaces and XAML class names so the first pass is a physical regrouping. Shared window infrastructure, Economy, Core and Infrastructure billing, application behavior, and the release version are outside the move.

## Stages

1. **Move the feature.** Verify the file list and caller references, move each XAML pair together, and validate that the moved source bytes match their originals. Refresh the project profile and current plan paths where they describe these files. If the SDK does not include a moved file or a relative XAML reference breaks, correct the reference before continuing.
2. **Verify the move.** Use the Release build and the full solution tests. Exercise the actual preview, viewer, and recovery WPF windows with fictional fixtures and temporary layout paths. Extend coverage specifically where a moved window was previously tested only as a view model or by construction. Verify the expected scope and close this plan.

## Decisions and gates

The App project remains the composition host. Invoice-specific printer and PDF launch adapters belong with the invoice workflow. Economy and general WPF helpers retain their current ownership. No open decisions. Continue after successful verification.

## Final User Test Instructions

Post-completion user test: mandatory.

1. Start **Ledger User Test** in Visual Studio. With a fictional student selected and a month with uninvoiced lessons displayed, choose **Write Invoice**, verify the expected student and lessons, then cancel.
2. Open a saved test invoice from **Student Invoices** and verify its number and PDF reference in the viewer.
3. If the test ledger has a pending reservation, choose **Invoice > Resume Invoice**, verify the recovery choices, then cancel the dialog. Retain any test records you want for later testing.

## Completion log

- 2026-11-22. Identified the WPF invoice windows and invoice-specific App files to move. No blocking user action.
- 2026-11-22. Stage 1 completed. Moved 22 App files to Features/Invoices and verified each by SHA-256. Updated the project profile and the UI review plan’s current file paths. No blocking user action.
- 2026-11-22. Stage 2 completed. The non-incremental Release build passed with zero warnings and errors. Verified the three compiled WPF pages in Features/Invoices. All 444 Release tests passed, including opening preview, viewer, and recovery windows using fixture data. No blocking user action.
