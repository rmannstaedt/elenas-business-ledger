# My Small Business Ledger

This repository contains a small local-first ledger application for independent piano teachers.

Primary goal:

> Make it easy for a teacher to create, print, and store invoices while keeping students, lesson plans, Google Calendar, payments, and VAT/tax tracking consistent.

## Documents

1. `docs/business-requirements-and-logic.md` — product design, scope, architecture, requirements, and key decisions.

## Solution shape

```text
src/
  MySmallBusinessLedger.App
  MySmallBusinessLedger.Core
  MySmallBusinessLedger.Infrastructure

tests/
  MySmallBusinessLedger.Tests
```

## Build and test

```powershell
dotnet build MySmallBusinessLedger.sln -nodeReuse:false
dotnet test MySmallBusinessLedger.sln -nodeReuse:false
```

## Pilot setup

Run the WPF app from Visual Studio or with:

```powershell
dotnet run --project src\MySmallBusinessLedger.App\MySmallBusinessLedger.App.csproj
```

On first setup, open `Application > Settings` and configure:

- company name, address, VAT status, and invoice folder;
- Google Calendar ID and account label/email.

The app uses the publisher Google sign-in configuration bundled with the application. For local development, put the real Desktop OAuth JSON at `src\MySmallBusinessLedger.App\Configuration\google-oauth-client.json`; the file is intentionally ignored by Git and copied to the app output when present. Use `Application > Settings > Connect Google Calendar` to authorize the configured Google account in the browser. Use `Forget Google sign-in` to clear local authorization and force a fresh browser consent flow.

Google OAuth access/refresh tokens are stored with Windows user data protection.

Runtime data lives under `%APPDATA%\visualtech.dk\MySmallBusinessLedger`:

- `ledger.db` - local SQLite database;
- `GoogleTokens\` - Windows-protected Google OAuth token store;
- `window-placement.json` - remembered window sizes and positions.

The invoice folder is configured in Settings and may be outside the app data folder.
The app writes invoice PDFs through a staging/verification/publish flow and does not silently overwrite an existing issued invoice PDF.

## Current key assumptions

- Under-30 teaching support remains in the data model because future students may require it (but out of scope for version 1.0).
- Lønsumsafgift is out of scope for version 1.0.
- Google Calendar is the operational lesson schedule, with two-way sync.
- Google Calendar integration uses the official Google Calendar API client, external browser OAuth, and Windows-protected local token storage.
- The Google OAuth app identity is publisher-owned and supplied from `Configuration\google-oauth-client.json`, not selected by each user in Settings.
- Runtime data is stored under `%APPDATA%\visualtech.dk\MySmallBusinessLedger`.
- The WPF app now uses real SQLite/Core/Calendar-backed services; if Google Calendar is not configured, the dashboard reports that instead of showing sample lesson rows.
- Company and Google Calendar account setup, connection, and authorization reset are available from `Application > Settings`.
- The app stores issued invoice PDFs in a configurable invoices folder.
- Invoice PDFs are generated with QuestPDF, staged and verified before final storage, and never silently overwritten.
- The initial UI recommendation is WPF + .NET 10.
- The initial database recommendation is SQLite.
- Database access uses EF Core SQLite.
- The PDF dependency is QuestPDF.
