# Elena's Business Ledger

A Windows app for managing student lessons, invoices, and payments.

The Ledger is a WPF desktop application for independent teachers. It schedules lessons through Google Calendar, keeps billing and payment records in a local SQLite database, and generates Danish invoice PDFs. The Economy overview shows issued invoice totals and exports them to CSV.

## Get started

- [How to set up the Ledger](docs/setup.md) covers installation, company settings, Google Calendar, and creating a Google OAuth client for a source build.
- [Illustrated tutorial (PDF)](docs/tutorial.pdf) follows fictional student Mira from her first lesson to a paid invoice. [Text version](docs/tutorial.md).
- [Business requirements and logic](docs/business-requirements-and-logic.md) describes the domain rules for scheduling, invoicing, and reporting.

The current release is **v1.3.2**.

## Solution layout

| Project | Responsibility |
| --- | --- |
| `ElenasBusinessLedger.App` | WPF views, view models, and application startup |
| `ElenasBusinessLedger.Core` | Domain models, business rules, and use cases |
| `ElenasBusinessLedger.Infrastructure` | EF Core SQLite, Google Calendar, QuestPDF, and Windows printing |
| `ElenasBusinessLedger.Tests` | xUnit tests for business rules, app behavior, persistence, and packaging |

The WPF app wires the services at startup. Google Calendar provides appointment timing and attendance state. SQLite holds the durable student, lesson, and financial records. The saved invoice snapshot supplies both the PDF and the amounts used by Economy. The business requirements describe how the current source handles retries and upgrades.

## Build and test

On Windows, install the .NET 11 SDK. For IDE builds, use Visual Studio 18.12 or later with the .NET desktop development workload. Open `ElenasBusinessLedger.sln`, or run the following from the repository root in PowerShell.

```powershell
dotnet build .\ElenasBusinessLedger.sln -nodeReuse:false
dotnet test .\ElenasBusinessLedger.sln -nodeReuse:false
```

## Build the installer

Install Inno Setup 6, then run this from the repository root.

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\build-installer.ps1
```

The script publishes a self-contained Windows x64 build and writes `artifacts\installer\ElenasBusinessLedger-1.3.2-Setup.exe`. It uses `C:\Program Files (x86)\Inno Setup 6\ISCC.exe` by default. Pass `-InnoSetupCompiler` to use a different compiler path. Each build stages its files separately. A successful build replaces only the installer with the same filename after Inno Setup produces a nonempty candidate. A failed build leaves the previous installer in place.

To bundle Google sign-in for your build, place a Desktop OAuth client file in the app project before packaging. See [Set up a Google OAuth client](docs/setup.md#set-up-a-google-oauth-client) for the exact file location and Google Cloud steps.
