# NuGet package updates

- Status: Completed
- Created and last updated: 2026-11-19
- Context: Ledger dependency maintenance on .NET 10

Update the Infrastructure and Tests package references to stable releases compatible with their .NET 10 targets. Follow the dependency and verification policies in the project profile and standards.

## Stages

1. Update production package references. Use EF Core 10.0.15 for both design and runtime. Pair SQLitePCLRaw 3.0.5 with its native `SQLite` package at 3.54.1. The updated bundle selects the renamed native package, so replace the older `SourceGear.sqlite3` reference to keep one provider of `e_sqlite3`. Update Google Calendar, QuestPDF and ProtectedData. Check the revised references against publisher metadata.
2. Update the existing test toolchain as a set. Pair xUnit v3 4.0.3 with its VSTest adapter 4.0.1, and update the test SDK and Coverlet. Use the `xunit.v3.mtp-off` package for the VSTest and Coverlet entry point. Preserve serial execution with the new xUnit assembly attribute in `AssemblyInfo.cs`.
3. Restore, build, run the test suite, and audit direct and transitive packages. Review database migration and PDF test outcomes for regressions from native dependency updates. If the feed is unavailable, record the restore result and leave verification pending. Prior versions in Git provide the rollback point.

## Final user test instructions

Use a Windows machine with the .NET 10 SDK and access to the configured NuGet feed. Run `dotnet build MySmallBusinessLedger.sln -nodeReuse:false` and `dotnet test MySmallBusinessLedger.sln -nodeReuse:false`. Expect a warning-free build and the full suite to pass, including the SQLite migration and PDF tests. Review the NuGet audit for dependency advisories. The verification commands run against test data and temporary files.

Post-completion user test: mandatory.

## Completion log

- Confirmed the package versions and the EF Core 11 target framework requirement against publisher metadata.
- Updated the production and test package declarations and migrated the xUnit parallelisation attribute.
- The first full test run exposed QuestPDF's new default of disabling system fonts. Set font discovery and fallback explicitly in the invoice writer to retain its Segoe UI layout.
- The default xUnit v3 package selects Microsoft.Testing.Platform v2, which rejected the VSTest target on the .NET 10 SDK. Selected the xUnit package variant for the existing VSTest workflow.
- The final restore and solution build on SDK 10.0.404 passed with zero warnings and errors. NuGet's audit of direct and transitive dependencies reported no advisories using its cached NuGet.org vulnerability data.
- The direct xUnit runner passed all 407 tests, including the PDF and SQLite tests. The sandbox denied the VSTest test host's socket connection and the coverage run timed out connecting to its collector. Coverage remains unverified in this environment.
