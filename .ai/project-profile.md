# Project Profile

## Project identity

Working name: Elena's Small Business Ledger

Internal namespace/project prefix:
ElenaSmallBusinessLedger

## Target framework and platform

Target framework: .NET 10
Platform: Windows desktop
UI: WPF
Architecture: local-first desktop app

## Solution shape

src/
  ElenaSmallBusinessLedger.App
  ElenaSmallBusinessLedger.Core
  ElenaSmallBusinessLedger.Infrastructure

tests/
  ElenaSmallBusinessLedger.Tests

## Persistence and storage

Database: SQLite
Database access: EF Core SQLite
Invoice PDFs: stored as files, not BLOBs
Backup: no automated backup in version 1.0; keep persistence design compatible with later manual backup/export

## UI policy

WPF + MVVM
No business logic in code-behind
Danish user-facing invoice text
Elena-facing UI must stay simple

## Test policy

Unit tests: xUnit
UI automation: FlaUI later, where practical
Use IClock for time-sensitive logic
Use fake Google Calendar client for calendar integration tests

## Hard requirements

- Do not mark invoice issued unless PDF storage succeeds.
- Do not silently edit issued invoices.
- Do not charge VAT when business profile says NotRegistered.
- Do not store CPR numbers.
- Ignore Google Calendar entries that do not use the documented first-line `$` student marker.
- Notify the user immediately when a Google Calendar lesson update fails.
- Do not make Google Calendar sync failure corrupt local data.
