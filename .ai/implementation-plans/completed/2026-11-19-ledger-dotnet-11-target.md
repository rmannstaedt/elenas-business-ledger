# Retarget the Ledger to .NET 11

- Status: Completed
- Created and last updated: 2026-11-19

Use .NET 11 as the Ledger's application and installer target. The earlier dependency work verified this target with EF Core 11.0.0 and the existing migrations.

## Stages

1. Set the application and tests to `net11.0-windows` and the libraries to `net11.0`. Pin EF Core Design and SQLite to 11.0.0. Remove the framework selection logic used to build both .NET versions.
2. Update the installer script, its packaging test, and the build documentation to select .NET 11. Verify the publish in a separate directory so the released installer remains available.
3. Restore and audit the resolved graph, build the solution, and run the full suite with the .NET 11 SDK. Check the published runtime and EF Core versions. Record any verification blocked by missing runtime packs.

## Final user test instructions

On Windows with the .NET 11 SDK, run `dotnet build MySmallBusinessLedger.sln -nodeReuse:false` and `dotnet test MySmallBusinessLedger.sln -nodeReuse:false`. Visual Studio 18.12 or later supports this target in the IDE. Expect the full suite to pass against temporary test data.

Post-completion user test: mandatory.

## Completion

- All projects now target .NET 11 and Infrastructure uses EF Core 11.0.0. The installer script and its packaging test select `net11.0-windows`.
- The .NET 11 build completed with zero warnings or errors. The direct xUnit runner passed all 407 tests. NuGet reported no advisories using the cached advisory data.
- Published a self-contained Windows x64 Release in a verification directory. The publish contains the .NET and Windows Desktop 11.0.0 runtimes and EF Core SQLite 11.0.0.
