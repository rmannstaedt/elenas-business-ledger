# Elena VAT & Piano Tutor Accounting Application

This repository contains a small local-first application for independent piano teachers.

Primary goal:

> Make it easy for a teacher to create, print, and store invoices while keeping students, lesson plans, Google Calendar, payments, and VAT/tax tracking consistent.

## Documents

1. `docs/business-requirements-and-logic.md` — product design, scope, architecture, requirements, and key decisions.

## Solution shape

```text
src/
  ElenaSmallBusinessLedger.App
  ElenaSmallBusinessLedger.Core
  ElenaSmallBusinessLedger.Infrastructure

tests/
  ElenaSmallBusinessLedger.Tests
```

## Build and test

```powershell
dotnet build ElenaSmallBusinessLedger.sln -nodeReuse:false
dotnet test ElenaSmallBusinessLedger.sln -nodeReuse:false
```

## Current key assumptions

- Under-30 teaching support remains in the data model because future students may require it (but out of scope for version 1.0).
- Lønsumsafgift is out of scope for version 1.0.
- Google Calendar is the operational lesson schedule, with two-way sync.
- The app stores issued invoice PDFs in a configurable invoices folder.
- The initial UI recommendation is WPF + .NET 10.
- The initial database recommendation is SQLite.
- Database access uses EF Core SQLite.
- The initial PDF recommendation is QuestPDF.
