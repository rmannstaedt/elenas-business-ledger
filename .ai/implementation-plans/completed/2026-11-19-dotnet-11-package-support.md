# .NET 11 package support

- Status: Completed
- Created and last updated: 2026-11-19

Add a .NET 11 build of the Ledger that uses EF Core 11.0.0. Build the .NET 10 target with EF Core 10.0.15 and retain .NET 10 as the installer target. Publisher metadata confirms that the other direct references are already current.

## Stages

1. Define supported target frameworks for the installed SDK in `Directory.Build.props`. Build Core and Infrastructure for .NET 10 and .NET 11 with SDK 11. Use the corresponding Windows targets for App and Tests. Build the .NET 10 target with SDK 10.
2. Select the same EF Core version for the design package and SQLite provider on each target. Select the existing .NET 10 target explicitly in the installer publish command so the two-target app builds through the established installer entry point.
3. Restore and audit both dependency graphs. Build and run the existing test suite for each target. Pay particular attention to SQLite migration and invoice tests. Check the publish target and update the documented build and installer commands.

The previous framework declarations, package pins, and installer publish arguments provide the rollback point. The two builds use the same application database schema and migrations.

## Final user test instructions

On Windows with the .NET 11 SDK and access to NuGet, run `dotnet build MySmallBusinessLedger.sln -nodeReuse:false`, then `dotnet test MySmallBusinessLedger.sln -nodeReuse:false`. Expect both targets to build and pass the test suite using temporary test data. SDK 10 builds and tests the .NET 10 target.

Post-completion user test: mandatory.

## Completion

- The .NET 11 SDK resolved EF Core Design and SQLite 11.0.0 for the .NET 11 target and 10.0.15 for the .NET 10 target. The .NET 10 SDK selected its single supported target. SDK detection handles evaluations where the SDK version property is empty. Visual Studio reloaded all four projects and restored the required packages.
- Replaced two deprecated `FromSqlInterpolated` calls with the parameterized `FromSql` API. The final SDK 10 and SDK 11 builds reported zero warnings and errors.
- The direct xUnit runner passed all 407 tests on each target. NuGet reported no advisories for either dependency graph using the cached advisory data.
- The installer script selects `net10.0-windows`. A framework-dependent Release publish verified the target selection and EF Core SQLite version. The documentation records both SDK build paths and the supported Visual Studio version.
