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

## Pilot setup

Run the WPF app from Visual Studio or with:

```powershell
dotnet run --project src\ElenaSmallBusinessLedger.App\ElenaSmallBusinessLedger.App.csproj
```

On first setup, open `Application > Settings` and configure:

- company name, address, VAT status, and invoice folder;
- Google Calendar ID and account label/email;
- Google Calendar login setup file, which is the Google-downloaded OAuth JSON file.

The app copies the selected Google Calendar login setup file to `%LOCALAPPDATA%\ElenaSmallBusinessLedger\google-client-secrets.json`.
Google OAuth access/refresh tokens are stored separately with Windows user data protection.

Runtime data lives under `%LOCALAPPDATA%\ElenaSmallBusinessLedger`:

- `ledger.db` - local SQLite database;
- `google-client-secrets.json` - copied Google Calendar login setup file;
- `GoogleTokens\` - Windows-protected Google OAuth token store.

The invoice folder is configured in Settings and may be outside the app data folder.
The app writes invoice PDFs through a staging/verification/publish flow and does not silently overwrite an existing issued invoice PDF.

## Current key assumptions

- Under-30 teaching support remains in the data model because future students may require it (but out of scope for version 1.0).
- Lønsumsafgift is out of scope for version 1.0.
- Google Calendar is the operational lesson schedule, with two-way sync.
- Google Calendar integration uses the official Google Calendar API client, external browser OAuth, and Windows-protected local token storage.
- Runtime data is stored under `%LOCALAPPDATA%\ElenaSmallBusinessLedger`.
- The WPF app now uses real SQLite/Core/Calendar-backed services; if Google Calendar is not configured, the dashboard reports that instead of showing sample lesson rows.
- Company and Google Calendar setup is available from `Application > Settings`.
- The app stores issued invoice PDFs in a configurable invoices folder.
- Invoice PDFs are generated with QuestPDF, staged and verified before final storage, and never silently overwritten.
- The initial UI recommendation is WPF + .NET 10.
- The initial database recommendation is SQLite.
- Database access uses EF Core SQLite.
- The PDF dependency is QuestPDF.
