# V1 Invoice Printer Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `plan-runner` for project-stage execution. If using Superpowers execution workflows, use `superpowers:subagent-driven-development` or `superpowers:executing-plans` task-by-task. Steps use checkbox syntax for tracking.

**Goal:** Build the first usable WPF/.NET 10 local app for Elena to maintain students and plans, read/edit relevant Google Calendar lessons, write immutable Danish invoices, save invoice PDFs, print them, and mark invoices paid/outstanding.

**Architecture:** Use a small three-project shape: WPF `App`, business `Core`, and technical `Infrastructure`. Keep invoice, VAT, plan, lesson, and payment rules in Core; keep WPF, SQLite, Google Calendar, PDF generation, printing, and filesystem details outside Core.

**Tech Stack:** .NET 10, WPF/MVVM, xUnit, SQLite, Google Calendar API, QuestPDF or an approved PDF alternative.

Status: Completed
Created: 2026-06-16
Last updated: 2026-06-16
Owner/context: Ruben and Codex; stages 1-9 are implemented/refined, and the WPF startup path now uses real Core/SQLite/Calendar-backed services instead of sample dashboard/editor services. Stage 10 remains next for the invoice printer/viewer flow.

## Related Docs And Standards

- `README.md`
- `AGENTS.md`
- `.ai/project-profile.md`
- `docs/business-requirements-and-logic.md`
- `.ai/standards/architecture-principles.md`
- `.ai/standards/coding-standards.md`
- `.ai/standards/testing-and-verification.md`
- `.ai/standards/security-and-boundaries.md`
- `.ai/standards/tooling-and-dependencies.md`
- `.ai/standards/persistence-and-artifacts.md`
- `.ai/standards/database-standards.md`
- `.ai/standards/ui-design-standards.md`
- `.ai/standards/wpf-mvvm-standards.md`

## Related Skills

- `implementation-plan` for this persistent plan.
- `plan-runner` to execute this plan stage by stage.
- `code-review` after non-trivial Core, Infrastructure, or workflow stages.
- `ui-review` after WPF dashboard and invoice workflow stages.
- `documentation-refresh` after project structure, commands, behavior, or durable artifacts change.
- `architecture-review` if project boundaries or runtime topology change.

## Goal

Create a focused v1 application that supports Elena's real workflow:

1. Open app.
2. See current student and lessons for previous/current/next month.
3. Edit uninvoiced lessons through Google Calendar.
4. Write an invoice from relevant Google Calendar entries.
5. Save a PDF copy in the configured invoice folder.
6. Save immutable invoice data.
7. Print a hard copy when a printer is available.
8. Track invoices as paid or outstanding.

## Non-Goals

- No VAT-exempt student support in v1.
- No VAT reporting in v1.
- No payment due dates or payment terms in v1.
- No credit notes or correction invoices in v1.
- No MobilePay/bank/payment-provider integration in v1.
- No tax reserve estimate in v1.
- No automated backup in v1.
- No generated month planner, holiday/closure engine, or calendar review queue in v1.
- No separate plan-template/agreement architecture in v1.
- No multi-user, remote sync, cloud database, or web app in v1.

## Assumptions

- The active product source is `docs/business-requirements-and-logic.md`.
- The archived documents are historical context only.
- Solution shape should be simplified to:

  ```text
  src/
    ElenaSmallBusinessLedger.App
    ElenaSmallBusinessLedger.Core
    ElenaSmallBusinessLedger.Infrastructure

  tests/
    ElenaSmallBusinessLedger.Tests
  ```

- The app is a single-user Windows desktop app.
- Google Calendar is the source of truth for future lessons and uninvoiced past lessons.
- Local lesson records are created only from issued invoices.
- Relevant Google Calendar entries are identified only by first line:

  ```text
  $ <student short name>
  $<student short name>
  ```

- All non-matching Google Calendar entries are ignored.
- Plan prices are stored without VAT.
- VAT is charged only when company VAT status is registered and the invoice date is on or after the VAT registration date.
- Current v1 students are adult/standard students; under-30 VAT exemption is out of scope.
- Invoice text/PDF language is Danish.
- The workspace is currently not a Git repository, so rollback is by file review and deletion/recreation rather than commits.

## Current Problem / Current State

- The repository contains a buildable `.sln`, App/Core/Infrastructure projects, and an xUnit test project.
- `.ai/project-profile.md` has been aligned to the App/Core/Infrastructure solution shape and current runtime data locations.
- There is no active implementation plan other than this file.
- Build and test commands are verified for the current solution.
- The WPF app startup path now uses real SQLite repositories, Core use cases, and the configured Google Calendar gateway. If Calendar settings/client secrets are missing, the dashboard fails truthfully instead of showing sample lesson rows.
- Completed-but-uninvoiced lesson state is represented in Google Calendar by preserving the first-line `$` student marker and adding/removing a later `Status: Completed` summary line.

## Desired End State

- The repository contains a buildable .NET 10 solution with App/Core/Infrastructure projects and one xUnit test project.
- Core contains the v1 business model and use cases for students, plans, lessons, invoices, VAT calculation, invoice issue, and payment status.
- Infrastructure persists students, plans, company settings, issued invoices, invoice lessons, and paid/outstanding state in SQLite.
- Infrastructure reads and updates Google Calendar entries using the strict first-line `$ short-name` rule and preserves that marker when updating app-owned lesson status.
- Infrastructure saves invoice PDFs to the configured invoice folder and prints only after the PDF is saved.
- The WPF UI supports the startup dashboard, student add/edit, lesson list, invoice writing, invoice viewer, and paid/outstanding toggle.
- The test suite covers the business rules and safety-critical workflows.

## Behavior-Preserving Boundary

The following must remain unchanged unless this plan explicitly says otherwise:

- User-visible behavior: follow `docs/business-requirements-and-logic.md`.
- UI contract: v1 has one main window centered on current student, lesson list, invoice list, student menu, and Write Invoice.
- Durable artifacts: issued invoice PDF files are evidence and must not be silently overwritten.
- Database behavior: issued invoices are immutable after successful save.
- Security/trust boundaries: no CPR numbers; Google OAuth tokens are secrets; arbitrary Google Calendar entries are ignored unless marked by the first-line `$` convention.
- Error handling: failed Google Calendar updates notify the user immediately; failed PDF save prevents invoice data save and print.
- Performance/resource characteristics: small local data set, no unbounded calendar/database/UI work.

## Intentional Behavior Changes

- Replace the older four-project solution shape with App/Core/Infrastructure.
- Treat old archived planning, data-model, and broad accounting requirements as non-authoritative.
- Treat Google Calendar as the lesson source of truth before invoice issue instead of generating local planned lessons.

## Affected Files, Projects, Commands, Docs, Schemas, Or Artifacts

Expected files and directories:

- Modify: `.ai/project-profile.md`
- Create: `ElenaSmallBusinessLedger.sln`
- Create: `src/ElenaSmallBusinessLedger.App/ElenaSmallBusinessLedger.App.csproj`
- Create: `src/ElenaSmallBusinessLedger.Core/ElenaSmallBusinessLedger.Core.csproj`
- Create: `src/ElenaSmallBusinessLedger.Infrastructure/ElenaSmallBusinessLedger.Infrastructure.csproj`
- Create: `tests/ElenaSmallBusinessLedger.Tests/ElenaSmallBusinessLedger.Tests.csproj`
- Create Core model/use-case files under `src/ElenaSmallBusinessLedger.Core/`
- Create WPF views/ViewModels under `src/ElenaSmallBusinessLedger.App/`
- Create SQLite, Calendar, PDF, printing, and file-storage implementations under `src/ElenaSmallBusinessLedger.Infrastructure/`
- Create tests under `tests/ElenaSmallBusinessLedger.Tests/`
- Update: `README.md` with final build/test/run commands after commands are verified.
- Update: `.ai/context/HANDOFF.md` or create it if project handoff context is needed after implementation.

Likely commands after scaffolding:

```powershell
dotnet build ElenaSmallBusinessLedger.sln -nodeReuse:false
dotnet test ElenaSmallBusinessLedger.sln -nodeReuse:false
dotnet format ElenaSmallBusinessLedger.sln --verify-no-changes
```

## Risks And Mitigations

- Risk: Reintroducing the old broad accounting architecture.
  - Mitigation: Keep v1 non-goals explicit in every stage and review against `business-requirements-and-logic.md`.
- Risk: PDF save succeeds but invoice database save fails.
  - Mitigation: Use a staged issue workflow and preserve/announce failures truthfully. Final design must be settled before invoice issue implementation.
- Risk: Calendar edits fail after UI changes the displayed lesson.
  - Mitigation: UI updates only after Calendar update succeeds, or shows immediate failure and refreshes from Calendar.
- Risk: Google Calendar matching accidentally imports irrelevant/private events.
  - Mitigation: Only parse entries whose first line matches the `$ short-name` marker exactly.
- Risk: WPF ViewModels accumulate persistence/calendar/PDF logic.
  - Mitigation: ViewModels call Core use cases/application services; Infrastructure implements Core ports.
- Risk: .NET 10 SDK or package restore is unavailable locally.
  - Mitigation: Check installed SDK first; do not retarget. Report missing SDK/package access clearly.
- Risk: QuestPDF licensing or package access blocks PDF implementation.
  - Mitigation: Confirm package/license before production dependency; keep PDF generation behind a Core port.

## Open Questions

- [x] Confirm project profile solution shape update.
  - Recommended/default answer: Use App/Core/Infrastructure as described in this plan.
  - Alternatives: Keep App/Application/Domain/Infrastructure, or collapse to a single WPF project plus tests.
  - Why it matters: This decides dependency boundaries and file organization before scaffolding.
  - Exact answer needed: "Use App/Core/Infrastructure" or the exact alternative shape.

- [x] Choose SQLite access approach.
  - Recommended/default answer: EF Core with SQLite provider for v1 because migrations, mapping, and test setup are straightforward for a small relational app.
  - Alternatives: Direct `Microsoft.Data.Sqlite` repositories for less abstraction and more explicit SQL.
  - Why it matters: This affects dependencies, migrations, repository tests, and schema evolution.
  - Exact answer received: Use EF Core SQLite.

- [x] Confirm PDF dependency.
  - Recommended/default answer: Use QuestPDF if the license is acceptable for this app.
  - Alternatives: PdfSharp/MigraDoc or another approved PDF library.
  - Why it matters: Invoice PDF generation is safety-critical and production dependency choice must be deliberate.
  - Exact answer received: Use QuestPDF; license accepted.

- [x] Decide invoice issue staging semantics.
  - Accepted answer: Render PDF to a staging path, verify it, publish/move the PDF to the final path, persist issued invoice metadata only after final PDF storage succeeds, then request print. If any pre-persistence step fails, show failure and do not display the invoice as issued.
  - Original recommended/default answer: Render PDF to a staging path, verify it, persist issued invoice metadata, then publish/move the PDF to final path; if any step fails, show failure and do not display the invoice as issued.
  - Alternatives: Save final PDF first, then persist invoice data; or persist draft first, then save final PDF, then mark issued.
  - Why it matters: SQLite transactions cannot include filesystem writes, so false success and orphan PDFs must be handled deliberately.
  - Exact answer received: Use the staged workflow; implementation orders final PDF publish before issued metadata persistence to preserve the hard requirement that a final stored PDF exists before the invoice is marked issued.

- [x] Decide how much Google Calendar UI is embedded.
  - Recommended/default answer: Open Google Calendar in the user's browser for v1; revisit embedded pane later only if technically easy and reliable.
  - Alternatives: Embedded WebView2 pane inside the WPF app.
  - Why it matters: Embedded browser adds dependency, auth/session, layout, and support complexity.
  - Exact answer received: Use the external browser for v1, with app activation refresh when focus returns to the ledger plus a manual Refresh button.

## Staged Implementation Plan

### Stage 1: Align Project Profile And Tooling Baseline

Type: documentation / decision
Status: Completed 2026-06-16

Goal:

Update project guidance so future coding follows the simplified v1 solution shape.

Affected files/classes/schemas/commands/UI surfaces:

- `.ai/project-profile.md`
- `README.md` if it still contradicts the simplified scope

Concrete steps:

- [x] Replace the solution shape in `.ai/project-profile.md` with App/Core/Infrastructure/Tests.
- [x] Update persistence and storage notes to remove invoice/report backup copy as a v1 expectation unless retained as manual future work.
- [x] Keep hard requirements for PDF-before-issued, invoice immutability, no CPR, no VAT before registration, and safe Calendar updates.
- [x] Add likely commands only after Stage 2 verifies them.
- [x] Run a read-only check for stale references to archived docs:

  ```powershell
  rg "02-business-rules|03-requirements|04-data-model|05-implementation-plan|06-open-questions|07-ui-design|08-backend-design|archived" README.md AGENTS.md .ai docs
  ```

Behavior that must remain unchanged:

- Active product behavior remains defined by `docs/business-requirements-and-logic.md`.

Tests to add/update:

- None; documentation-only.

Verification commands/manual checks:

```powershell
Get-Content -Raw .ai\project-profile.md
rg "02-business-rules|03-requirements|04-data-model|05-implementation-plan|06-open-questions|07-ui-design|08-backend-design|archived" README.md AGENTS.md .ai docs
```

Risks:

- Stale docs can steer implementation back to the old architecture.

Rollback point:

- Revert the profile/doc edits manually before scaffolding.

Stop/review criteria:

- Stop if the user does not confirm the App/Core/Infrastructure shape.

Documentation impact:

- Project profile becomes authoritative for implementation.

Follow-on actions:

- Proceed to scaffolding after the shape is accepted.

Runner may continue automatically after this stage: no. Blocking action required: user confirms solution shape and open decisions that affect scaffolding.

### Stage 2: Scaffold Solution, Projects, And Test Baseline

Type: implementation / test
Status: Completed 2026-06-16

Goal:

Create a buildable, testable .NET 10 solution with the simplified project structure.

Affected files/classes/schemas/commands/UI surfaces:

- `ElenaSmallBusinessLedger.sln`
- `src/ElenaSmallBusinessLedger.App/`
- `src/ElenaSmallBusinessLedger.Core/`
- `src/ElenaSmallBusinessLedger.Infrastructure/`
- `tests/ElenaSmallBusinessLedger.Tests/`
- `.editorconfig`
- optional `global.json` if the installed .NET 10 SDK version should be pinned

Concrete steps:

- [x] Check installed SDKs:

  ```powershell
  dotnet --list-sdks
  ```

- [x] Create the solution:

  ```powershell
  dotnet new sln -n ElenaSmallBusinessLedger
  ```

- [x] Create projects:

  ```powershell
  dotnet new wpf -n ElenaSmallBusinessLedger.App -o src\ElenaSmallBusinessLedger.App -f net10.0-windows
  dotnet new classlib -n ElenaSmallBusinessLedger.Core -o src\ElenaSmallBusinessLedger.Core -f net10.0
  dotnet new classlib -n ElenaSmallBusinessLedger.Infrastructure -o src\ElenaSmallBusinessLedger.Infrastructure -f net10.0
  dotnet new xunit -n ElenaSmallBusinessLedger.Tests -o tests\ElenaSmallBusinessLedger.Tests -f net10.0
  ```

- [x] Add projects to solution:

  ```powershell
  dotnet sln ElenaSmallBusinessLedger.sln add src\ElenaSmallBusinessLedger.App\ElenaSmallBusinessLedger.App.csproj
  dotnet sln ElenaSmallBusinessLedger.sln add src\ElenaSmallBusinessLedger.Core\ElenaSmallBusinessLedger.Core.csproj
  dotnet sln ElenaSmallBusinessLedger.sln add src\ElenaSmallBusinessLedger.Infrastructure\ElenaSmallBusinessLedger.Infrastructure.csproj
  dotnet sln ElenaSmallBusinessLedger.sln add tests\ElenaSmallBusinessLedger.Tests\ElenaSmallBusinessLedger.Tests.csproj
  ```

- [x] Add references:

  ```powershell
  dotnet add src\ElenaSmallBusinessLedger.App\ElenaSmallBusinessLedger.App.csproj reference src\ElenaSmallBusinessLedger.Core\ElenaSmallBusinessLedger.Core.csproj
  dotnet add src\ElenaSmallBusinessLedger.App\ElenaSmallBusinessLedger.App.csproj reference src\ElenaSmallBusinessLedger.Infrastructure\ElenaSmallBusinessLedger.Infrastructure.csproj
  dotnet add src\ElenaSmallBusinessLedger.Infrastructure\ElenaSmallBusinessLedger.Infrastructure.csproj reference src\ElenaSmallBusinessLedger.Core\ElenaSmallBusinessLedger.Core.csproj
  dotnet add tests\ElenaSmallBusinessLedger.Tests\ElenaSmallBusinessLedger.Tests.csproj reference src\ElenaSmallBusinessLedger.Core\ElenaSmallBusinessLedger.Core.csproj
  dotnet add tests\ElenaSmallBusinessLedger.Tests\ElenaSmallBusinessLedger.Tests.csproj reference src\ElenaSmallBusinessLedger.Infrastructure\ElenaSmallBusinessLedger.Infrastructure.csproj
  ```

- [x] Add `.editorconfig` based on `.ai/templates/editorconfig.recommended`.
- [x] Enable nullable and implicit usings in all project files.
- [x] Add a simple smoke test in `tests/ElenaSmallBusinessLedger.Tests/Core/SolutionSmokeTests.cs` asserting a Core type can be referenced.
- [x] Build and test the empty solution.

Behavior that must remain unchanged:

- No user-visible behavior exists yet.

Tests to add/update:

- `SolutionSmokeTests` verifies test project wiring.

Verification commands/manual checks:

```powershell
dotnet build ElenaSmallBusinessLedger.sln -nodeReuse:false
dotnet test ElenaSmallBusinessLedger.sln -nodeReuse:false
```

Risks:

- Missing .NET 10 SDK or WPF template blocks scaffolding.

Rollback point:

- Delete created solution/project files before adding feature code.

Stop/review criteria:

- Stop if the solution does not build or tests do not run.

Documentation impact:

- Update `README.md` with verified commands.

Follow-on actions:

- Begin Core model and use-case tests.

Runner may continue automatically after this stage: yes.

### Stage 3: Build Core Domain Model And Pure Business Rules

Type: implementation / test
Status: Completed 2026-06-16

Goal:

Implement the v1 business model and deterministic rules without WPF, SQLite, Google Calendar, PDF, or filesystem dependencies.

Affected files/classes/schemas/commands/UI surfaces:

- `src/ElenaSmallBusinessLedger.Core/Students/Student.cs`
- `src/ElenaSmallBusinessLedger.Core/Plans/Plan.cs`
- `src/ElenaSmallBusinessLedger.Core/Plans/StudentPlanPeriod.cs`
- `src/ElenaSmallBusinessLedger.Core/Lessons/Lesson.cs`
- `src/ElenaSmallBusinessLedger.Core/Company/CompanyProfile.cs`
- `src/ElenaSmallBusinessLedger.Core/Invoices/Invoice.cs`
- `src/ElenaSmallBusinessLedger.Core/Invoices/InvoiceLine.cs`
- `src/ElenaSmallBusinessLedger.Core/Invoices/InvoiceDraft.cs`
- `src/ElenaSmallBusinessLedger.Core/Invoices/InvoiceNumber.cs`
- `src/ElenaSmallBusinessLedger.Core/Invoices/PaymentStatus.cs`
- `src/ElenaSmallBusinessLedger.Core/Money/Money.cs`
- `src/ElenaSmallBusinessLedger.Core/Calendar/CalendarLessonEntry.cs`
- `src/ElenaSmallBusinessLedger.Core/Time/IClock.cs`
- matching tests under `tests/ElenaSmallBusinessLedger.Tests/Core/`

Concrete steps:

- [x] Write tests for student age bracket and mandatory birthdate when age bracket is young.
- [x] Implement `Student`, `AgeBracket`, and validation result types.
- [x] Write tests for plan periods: only one active plan for a date, future plan starts later, old plan remains historical.
- [x] Implement `Plan` and `StudentPlanPeriod`.
- [x] Write tests for Google Calendar marker parsing:

  ```text
  "$ Anna" -> "Anna"
  "$Anna" -> "Anna"
  "Anna" -> ignored
  "Piano Anna" -> ignored
  ```

- [x] Implement `CalendarLessonEntry` parsing.
- [x] Write tests for VAT calculation: no VAT when company is not registered; VAT added only when registered and invoice date is on or after VAT registration date.
- [x] Implement `CompanyProfile`, `VatStatus`, `Money`, and invoice total calculation.
- [x] Write tests for invoice immutability shape: issued invoice has number, date, company snapshot, student snapshot, lesson lines, total, and cannot be edited through domain methods.
- [x] Implement invoice draft and issued invoice types.
- [x] Write tests for price adjustment requiring a reason.
- [x] Implement price adjustment validation.
- [x] Write tests for paid/outstanding toggle rules: outstanding can become paid directly; paid to outstanding requires confirmation at use-case level.

Behavior that must remain unchanged:

- Core must follow `business-requirements-and-logic.md` and v1 limitations.

Tests to add/update:

- Pure Core unit tests for each business rule listed above.

Verification commands/manual checks:

```powershell
dotnet test tests\ElenaSmallBusinessLedger.Tests\ElenaSmallBusinessLedger.Tests.csproj --filter "FullyQualifiedName~Core" -nodeReuse:false
dotnet build ElenaSmallBusinessLedger.sln -nodeReuse:false
```

Risks:

- Naming `Plan` as both general offer and student agreement can become confusing.

Rollback point:

- Core can be refactored before persistence depends on it.

Stop/review criteria:

- Stop if the Core model starts requiring Infrastructure types.

Documentation impact:

- If names diverge from the business doc, update the doc or plan before continuing.

Follow-on actions:

- Add Core ports and use cases.

Runner may continue automatically after this stage: yes.

### Stage 4: Add Core Use Cases And Ports

Type: implementation / test
Status: Completed 2026-06-16

Goal:

Define application workflows in Core while keeping technical details behind interfaces.

Affected files/classes/schemas/commands/UI surfaces:

- `src/ElenaSmallBusinessLedger.Core/UseCases/Students/`
- `src/ElenaSmallBusinessLedger.Core/UseCases/Lessons/`
- `src/ElenaSmallBusinessLedger.Core/UseCases/Invoices/`
- `src/ElenaSmallBusinessLedger.Core/Ports/IStudentRepository.cs`
- `src/ElenaSmallBusinessLedger.Core/Ports/ICompanyProfileRepository.cs`
- `src/ElenaSmallBusinessLedger.Core/Ports/IInvoiceRepository.cs`
- `src/ElenaSmallBusinessLedger.Core/Ports/IGoogleCalendarGateway.cs`
- `src/ElenaSmallBusinessLedger.Core/Ports/IInvoicePdfWriter.cs`
- `src/ElenaSmallBusinessLedger.Core/Ports/IInvoicePrinter.cs`
- `src/ElenaSmallBusinessLedger.Core/Ports/IInvoiceFileStore.cs`
- matching tests under `tests/ElenaSmallBusinessLedger.Tests/Core/UseCases/`

Concrete steps:

- [x] Write tests for startup dashboard query using fake repositories and fake calendar gateway.
- [x] Implement query result for current student, previous/current/next month lessons, and student invoices.
- [x] Write tests for editing an uninvoiced lesson: calendar gateway succeeds, returned lesson list reflects Calendar state.
- [x] Write tests for editing an uninvoiced lesson: calendar gateway fails, result reports failure and no local lesson is written.
- [x] Implement lesson edit/add/delete use cases.
- [x] Write tests for Write Invoice candidate selection: students with lessons today and no invoice this month are listed; current-time student is preselected.
- [x] Implement invoice candidate selection.
- [x] Write tests for invoice preview creation from Google Calendar entries.
- [x] Implement invoice preview use case.
- [x] Write tests for invoice issue safety: if PDF save fails, invoice repository is not called and printer is not called.
- [x] Write tests for invoice issue success: PDF is saved, invoice data is saved, print is requested, result includes invoice id/status.
- [x] Implement invoice issue use case with current staged workflow decision.
- [x] Write tests for paid/outstanding toggle including confirmation requirement for paid to outstanding.
- [x] Implement invoice payment status use case.

Behavior that must remain unchanged:

- Core use cases must not open files, call Google APIs, print, use WPF types, or use SQLite directly.

Tests to add/update:

- Core use-case tests with fake ports.

Verification commands/manual checks:

```powershell
dotnet test tests\ElenaSmallBusinessLedger.Tests\ElenaSmallBusinessLedger.Tests.csproj --filter "FullyQualifiedName~UseCases" -nodeReuse:false
dotnet build ElenaSmallBusinessLedger.sln -nodeReuse:false
```

Risks:

- Invoice issue workflow can accidentally imply end-to-end success before both PDF and invoice data are durable.

Rollback point:

- Use-case contracts can still change before UI and Infrastructure depend on them.

Stop/review criteria:

- Stop for code review if result models cannot express failure, partial failure, or user notification states.

Documentation impact:

- Document any confirmed deviation from the "Print Invoice" sequence in the business doc.

Follow-on actions:

- Implement persistence and technical adapters.

Runner may continue automatically after this stage: no. Blocking action required: code-review recommended before Infrastructure binds to Core contracts.

### Stage 5: Implement SQLite Persistence

Type: implementation / test
Status: Completed 2026-06-16

Goal:

Persist company settings, students, plans, issued invoices, invoice lesson history, and paid/outstanding state.

Affected files/classes/schemas/commands/UI surfaces:

- `src/ElenaSmallBusinessLedger.Infrastructure/Persistence/`
- `tests/ElenaSmallBusinessLedger.Tests/Infrastructure/Persistence/`
- package references selected by the SQLite decision

Concrete steps:

- [x] Apply the SQLite access decision from Open Questions.
- [x] Add approved SQLite packages to `ElenaSmallBusinessLedger.Infrastructure`.
- [x] Create persistence models/schema for company profile, students, plan periods, issued invoices, invoice lines, and payment status.
- [x] Implement repository interfaces from Core.
- [x] Add deterministic test database setup using a per-test temporary directory.
- [x] Write save/load tests for company profile.
- [x] Write save/load tests for student with plan history.
- [x] Write save/load tests for issued invoice with lines and PDF path.
- [x] Write tests that issued invoice data is not updated by student/plan edits.
- [x] Write tests for paid/outstanding state persistence.

Behavior that must remain unchanged:

- Persistence must not decide business rules; it stores and retrieves Core-approved state.

Tests to add/update:

- SQLite integration tests.

Verification commands/manual checks:

```powershell
dotnet test tests\ElenaSmallBusinessLedger.Tests\ElenaSmallBusinessLedger.Tests.csproj --filter "FullyQualifiedName~Persistence" -nodeReuse:false
dotnet build ElenaSmallBusinessLedger.sln -nodeReuse:false
```

Risks:

- Database schema may overfit early UI and make Core harder to change.

Rollback point:

- Persistence can be deleted/recreated while no production data exists.

Stop/review criteria:

- Stop if database model leaks into Core or WPF ViewModels.

Documentation impact:

- Add a short persistence note to README or project context after schema choice is verified.

Follow-on actions:

- Implement Calendar adapter and PDF adapter.

Runner may continue automatically after this stage: no. Blocking action required: confirm Google Calendar dependency/authentication/token-storage approach before Stage 6.

### Stage 6: Implement Google Calendar Gateway

Type: implementation / test
Status: Completed 2026-06-16

Goal:

Read and edit relevant Google Calendar entries using the strict first-line marker rule.

Affected files/classes/schemas/commands/UI surfaces:

- `src/ElenaSmallBusinessLedger.Infrastructure/Calendar/`
- `tests/ElenaSmallBusinessLedger.Tests/Infrastructure/Calendar/`
- WPF settings fields later for selected Google account/calendar

Concrete steps:

- [x] Add approved Google Calendar packages after dependency review.
- [x] Implement OAuth/token storage using Windows secure credential storage or an approved secure local mechanism.
- [x] Implement selected-calendar configuration storage through existing settings/persistence.
- [x] Implement calendar read for previous/current/next month window.
- [x] Ignore every event whose first line does not match `$ <short name>` or `$<short name>`.
- [x] Implement event update for date/time changes.
- [x] Implement event delete for lesson cancellation.
- [x] Implement event create for new lesson.
- [x] Map Calendar/API failures into Core result failures that the UI can display immediately.
- [x] Add fake gateway tests in Core and adapter tests around marker parsing/request mapping.

Behavior that must remain unchanged:

- Non-marked Calendar entries are always ignored.
- Invoiced lessons cannot be edited.

Tests to add/update:

- Marker filtering tests.
- Adapter mapping tests.
- Use-case failure tests for Calendar update failure.

Verification commands/manual checks:

```powershell
dotnet test tests\ElenaSmallBusinessLedger.Tests\ElenaSmallBusinessLedger.Tests.csproj --filter "FullyQualifiedName~Calendar" -nodeReuse:false
dotnet build ElenaSmallBusinessLedger.sln -nodeReuse:false
```

Risks:

- Real Google integration may require manual browser auth and cannot be fully automated.

Rollback point:

- Keep a fake calendar gateway usable by the app until real auth is stable.

Stop/review criteria:

- Stop before storing OAuth tokens if secure storage approach is not confirmed.

Documentation impact:

- Document Google Calendar marker convention and auth setup.

Follow-on actions:

- Wire Calendar data into WPF dashboard.

Runner may continue automatically after this stage: no. Blocking action required: manual Google Calendar setup may be needed.

### Stage 7: Implement PDF Storage, Invoice Rendering, And Printing

Type: implementation / test
Status: Completed 2026-06-16

Goal:

Generate Danish invoice PDFs, save them safely, and print only after a successful save.

Affected files/classes/schemas/commands/UI surfaces:

- `src/ElenaSmallBusinessLedger.Infrastructure/Invoices/Pdf/`
- `src/ElenaSmallBusinessLedger.Infrastructure/Invoices/Printing/`
- `tests/ElenaSmallBusinessLedger.Tests/Infrastructure/Invoices/`
- configured invoice folder setting

Concrete steps:

- [x] Apply the PDF dependency decision from Open Questions.
- [x] Add approved PDF package.
- [x] Implement Danish invoice rendering from immutable invoice preview/snapshot data.
- [x] Implement invoice folder validation.
- [x] Implement stable file naming with sanitized student short/full name and invoice number.
- [x] Implement no-silent-overwrite behavior.
- [x] Implement staged PDF save according to the confirmed issue workflow.
- [x] Implement printer adapter that prints only saved PDFs.
- [x] Write tests for PDF save failure blocking invoice save.
- [x] Write tests for file name collision handling.
- [x] Write a PDF smoke test that verifies a file is created and non-empty.

Behavior that must remain unchanged:

- PDF save failure prevents invoice data save and printing.
- Issued invoice data is immutable after successful save.

Tests to add/update:

- File storage tests using temporary directories.
- PDF rendering smoke tests.
- Core issue workflow tests if contracts change.

Verification commands/manual checks:

```powershell
dotnet test tests\ElenaSmallBusinessLedger.Tests\ElenaSmallBusinessLedger.Tests.csproj --filter "FullyQualifiedName~Invoice" -nodeReuse:false
dotnet build ElenaSmallBusinessLedger.sln -nodeReuse:false
```

Manual check:

- Generate a sample invoice PDF in a temporary invoice folder.
- Open the PDF and confirm Danish labels, company information, student information, lesson list, VAT fields when applicable, and total.

Risks:

- Printing is environment-dependent and may require manual verification.

Rollback point:

- Keep invoice issue use case returning "PDF unavailable" until rendering is accepted.

Stop/review criteria:

- Stop if PDF output lacks required invoice content or if save/print ordering cannot be made truthful.

Documentation impact:

- Document invoice folder behavior and PDF save/print safety.

Follow-on actions:

- Wire invoice writing into WPF UI.

Runner may continue automatically after this stage: no. Blocking action required: manual PDF review.

### Stage 8: Implement WPF Shell And Startup Dashboard

Type: implementation / test / UI
Status: Completed 2026-06-16

Goal:

Create the main WPF window around Elena's everyday workflow.

Affected files/classes/schemas/commands/UI surfaces:

- `src/ElenaSmallBusinessLedger.App/App.xaml`
- `src/ElenaSmallBusinessLedger.App/MainWindow.xaml`
- `src/ElenaSmallBusinessLedger.App/ViewModels/MainWindowViewModel.cs`
- `src/ElenaSmallBusinessLedger.App/ViewModels/StartupDashboardViewModel.cs`
- `src/ElenaSmallBusinessLedger.App/Views/`
- `tests/ElenaSmallBusinessLedger.Tests/App/ViewModels/`

Concrete steps:

- [x] Add dependency injection composition in App startup.
- [x] Create main window with current student short name.
- [x] Show lesson list for previous/current/next month.
- [x] Show each lesson's date/time, planned/completed state, invoiced flag, and paid/outstanding when invoiced.
- [x] Add Previous/Next student commands based on the dashboard student list.
- [x] Add Previous/Next month commands.
- [x] Add Student menu with Add Student and Edit Student.
- [x] Add Student Invoices pane with Paid/Outstanding flag.
- [x] Implement Outstanding to Paid direct toggle.
- [x] Implement Paid to Outstanding confirmation dialog.
- [x] Add double-click invoice open command.
- [x] Add Write Invoice button.
- [x] Add inline error display for Google Calendar update failures.
- [x] Add ViewModel tests for command availability and state transitions.

Behavior that must remain unchanged:

- UI must not edit invoiced lessons.
- All uninvoiced lesson edits are saved through Google Calendar use cases.

Tests to add/update:

- ViewModel tests for lesson selection, month navigation, payment toggle, and error display.

Verification commands/manual checks:

```powershell
dotnet test tests\ElenaSmallBusinessLedger.Tests\ElenaSmallBusinessLedger.Tests.csproj --filter "FullyQualifiedName~ViewModels" -nodeReuse:false
dotnet build ElenaSmallBusinessLedger.sln -nodeReuse:false
```

Manual check:

- Run the WPF app with fake/sample data.
- Confirm dashboard loads without a blank screen and commands are visible.

Risks:

- UI can become too accounting-like or too complex.

Rollback point:

- Keep UI bound to fake/sample Core data until persistence/calendar wiring is stable.

Stop/review criteria:

- Stop for `ui-review` after the dashboard is usable with sample data.

Documentation impact:

- Capture any UI behavior deviations in business requirements.

Follow-on actions:

- Add student editing and invoice writing forms.

Runner may continue automatically after this stage: no. Blocking action required: UI review recommended.

### Stage 9: Implement Student Add/Edit And Lesson Editing UI

Type: implementation / test / UI
Status: Completed 2026-06-16

Goal:

Allow Ruben/Elena to maintain students, plan history, schedule, and uninvoiced Google Calendar lessons.

Affected files/classes/schemas/commands/UI surfaces:

- `src/ElenaSmallBusinessLedger.App/Views/StudentEditorView.xaml`
- `src/ElenaSmallBusinessLedger.App/ViewModels/StudentEditorViewModel.cs`
- `src/ElenaSmallBusinessLedger.App/Views/LessonEditorView.xaml`
- `src/ElenaSmallBusinessLedger.App/ViewModels/LessonEditorViewModel.cs`
- matching ViewModel tests

Concrete steps:

- [x] Add student create/edit dialog with short name, full name, address, phone, email, age bracket, optional/required birthdate, active flag.
- [x] Add plan fields: name, price without VAT, lessons per week, duration, flexibility, number of students, effective date.
- [x] Add schedule fields: weekday/time list.
- [x] Enforce one active plan per student at any date through Core use case.
- [x] Add direct lesson edit for uninvoiced lessons.
- [x] Add lesson cancellation/delete for uninvoiced lessons.
- [x] Add new lesson command for Google Calendar.
- [x] Display immediate failure when Calendar update fails.
- [x] Preserve user input after validation failure.
- [x] Refine student editor to use date selectors, read-only Plan-owned fields, human-readable plan flexibility, and fixed schedule placement rows.
- [x] Replace double-click lesson editor with inline planned-lesson date/time editing and row delete action.
- [x] Add dismissible inline message banner and clear it on row selection changes.
- [x] Persist main and subwindow size/position inside the visible screen area.
- [x] Polish inline lesson controls: quieter delete action, first-click date selector behavior, editable time selector, and manual Planned/Completed state toggling.
- [x] Stop treating past uninvoiced lessons as automatically completed; completion is a manual user action.

Behavior that must remain unchanged:

- Invoiced lessons cannot be edited.
- Completed lessons cannot be edited or deleted; the only allowed completed-lesson action is confirmation-gated return to Planned.
- Calendar remains source of truth before invoicing.

Tests to add/update:

- ViewModel validation tests.
- Use-case tests for plan period conflicts and Calendar update failure.

Verification commands/manual checks:

```powershell
dotnet test tests\ElenaSmallBusinessLedger.Tests\ElenaSmallBusinessLedger.Tests.csproj -nodeReuse:false
dotnet build ElenaSmallBusinessLedger.sln -nodeReuse:false
```

Manual check:

- Create a student, assign a plan, add a schedule, add/edit/delete an uninvoiced lesson, and confirm the main list refreshes.

Risks:

- Direct Calendar editing can create user confusion if refresh timing is unclear.

Rollback point:

- Disable lesson editing commands while keeping read-only dashboard.

Stop/review criteria:

- Stop if Calendar failure can leave the UI showing an unsaved lesson change as successful.

Documentation impact:

- Document lesson edit semantics if they are refined.

Follow-on actions:

- Implement invoice writing UI.

Runner may continue automatically after this stage: yes.

### Stage 10: Implement Write Invoice Flow And Invoice Viewer

Type: implementation / test / UI
Status: Completed 2026-06-16

Goal:

Let the user choose a student, preview an invoice, edit lessons/adjustment, save PDF, save invoice data, print, and view saved invoices.

Affected files/classes/schemas/commands/UI surfaces:

- `src/ElenaSmallBusinessLedger.App/Views/InvoiceCandidateDialog.xaml`
- `src/ElenaSmallBusinessLedger.App/ViewModels/InvoiceCandidateDialogViewModel.cs`
- `src/ElenaSmallBusinessLedger.App/Views/InvoicePreviewView.xaml`
- `src/ElenaSmallBusinessLedger.App/ViewModels/InvoicePreviewViewModel.cs`
- `src/ElenaSmallBusinessLedger.App/Views/InvoiceViewerView.xaml`
- `src/ElenaSmallBusinessLedger.App/ViewModels/InvoiceViewerViewModel.cs`
- Core invoice issue use case
- Infrastructure PDF/persistence/printer adapters
- matching tests

Concrete steps:

- [x] Implement candidate dialog listing students with lessons today who have not had an invoice printed this month.
- [x] Preselect the current-time student when one exists.
- [x] Allow selecting any student in the database.
- [x] Implement invoice preview using company info, student info, matching Google Calendar lessons, VAT when relevant, and price information.
- [x] Allow editing invoice lesson list before issue.
- [x] Allow price adjustment only with mandatory reason.
- [x] Implement Cancel so no invoice/PDF/local lesson data is saved.
- [x] Implement Print Invoice command through the Core issue use case.
- [x] Display PDF save failure and keep invoice unissued.
- [x] Save invoice data and local invoice lessons only after PDF save succeeds according to confirmed workflow.
- [x] Request print after successful save.
- [x] Close form and refresh main view after successful save.
- [x] Open saved invoice in invoice viewer on double-click.
- [x] Prevent editing saved invoice data.

Behavior that must remain unchanged:

- Once an invoice has been successfully saved, it cannot be edited.
- The Lessons table/local lesson history is updated only from invoice issue.

Tests to add/update:

- ViewModel tests for candidate selection and preview editing.
- Core use-case tests for cancel/no side effects.
- Integration test for successful issue with fake PDF/printer.
- Integration test for PDF failure/no invoice saved.

Verification commands/manual checks:

```powershell
dotnet test tests\ElenaSmallBusinessLedger.Tests\ElenaSmallBusinessLedger.Tests.csproj -nodeReuse:false
dotnet build ElenaSmallBusinessLedger.sln -nodeReuse:false
```

Manual check:

- Use sample data to write an invoice.
- Confirm PDF exists in configured folder.
- Confirm invoice appears in main invoice pane as outstanding.
- Confirm invoice lessons become invoiced and uneditable.
- Confirm print is attempted only after PDF save.

Risks:

- This is the primary safety-critical workflow.

Rollback point:

- Keep invoice preview available but disable Print Invoice command until issue safety passes.

Stop/review criteria:

- Stop for code review and UI review before treating this workflow as complete.

Documentation impact:

- Update README and business docs if final issue sequence differs from current wording.

Follow-on actions:

- Add hardening, formatting, and final checks.

Runner may continue automatically after this stage: no. Blocking action required: code review and UI review.

### Stage 11: Hardening, Packaging Readiness, And Documentation Refresh

Type: verification / review / documentation / cleanup
Status: Completed 2026-06-16; final Ruben/Elena pilot test pending

Goal:

Make the first usable app coherent, verifiable, and ready for Ruben/Elena pilot testing.

Affected files/classes/schemas/commands/UI surfaces:

- `README.md`
- `.ai/project-profile.md`
- `.ai/context/HANDOFF.md`
- source and tests changed in prior stages

Concrete steps:

- [x] Run full build.
- [x] Run full test suite.
- [x] Attempt formatter check and document existing repository-wide line-ending/whitespace noise.
- [x] Review package dependencies and licenses.
- [x] Review logs/errors for secrets and sensitive data.
- [x] Run code review skill for Core/Infrastructure safety.
- [x] Run UI review skill for WPF flows.
- [x] Update README with verified commands and app setup notes.
- [x] Update project profile if commands, solution shape, or dependency decisions changed.
- [x] Create or update `.ai/context/HANDOFF.md`.
- [x] Record remaining limitations and manual test notes.

Behavior that must remain unchanged:

- v1 limitations remain explicit.

Tests to add/update:

- Add regression tests for any bug found during review.

Verification commands/manual checks:

```powershell
dotnet build ElenaSmallBusinessLedger.sln -nodeReuse:false
dotnet test ElenaSmallBusinessLedger.sln -nodeReuse:false
dotnet format ElenaSmallBusinessLedger.sln --verify-no-changes
```

Manual check:

- Complete the final user test path below.

Risks:

- Environment-specific WPF, printer, Google auth, or PDF behavior may not be fully automated.

Rollback point:

- Mark incomplete stage in completion log and keep plan active.

Stop/review criteria:

- Stop if final user test cannot issue and save one invoice with sample data.

Documentation impact:

- Documentation refresh required.

Follow-on actions:

- Decide whether to create a follow-up plan for automated backup, VAT reporting, credit notes, or payment due terms.

Runner may continue automatically after this stage: no. Blocking action required: final user test.

## Verification Strategy

Use the narrowest useful command during each stage and full verification at the end.

Build:

```powershell
dotnet build ElenaSmallBusinessLedger.sln -nodeReuse:false
```

Tests:

```powershell
dotnet test ElenaSmallBusinessLedger.sln -nodeReuse:false
```

Formatting:

```powershell
dotnet format ElenaSmallBusinessLedger.sln --verify-no-changes
```

Manual verification:

- WPF dashboard starts and shows sample data.
- Calendar marker filtering ignores unmarked entries.
- Calendar update failure is visible immediately.
- Invoice preview can be cancelled without side effects.
- PDF save failure blocks invoice save and print.
- Successful invoice issue saves PDF, saves invoice data, prints after save, refreshes dashboard, and prevents invoice/lesson edits.
- Paid/outstanding toggle behaves as specified.

## Follow-On Review And Documentation

- Code review required: yes, because invoice issue, persistence, Calendar writes, and PDF storage are safety-sensitive.
- Architecture review required: yes after scaffolding and after invoice workflow, because App/Core/Infrastructure boundaries must stay clean.
- UI review required: yes after dashboard and invoice workflow, because WPF state truthfulness and command availability matter.
- Documentation refresh required: yes, because project structure, commands, dependencies, and final behavior will change.

## Review Stop Points

- After Stage 1: confirm solution shape and dependency decisions.
- After Stage 4: review Core contracts before Infrastructure depends on them.
- After Stage 7: manually review generated PDF.
- After Stage 8: UI review for dashboard.
- After Stage 10: code review and UI review for invoice issue.
- After Stage 11: final user test before calling v1 complete.

## Final User Test Instructions

Post-completion user test: mandatory.

Prerequisites/setup:

- .NET 10 SDK installed.
- A writable test invoice folder.
- A test Google Calendar or safe calendar entries using the `$ short-name` first-line convention.
- At least one sample student with a plan and schedule.
- Company profile configured with invoice folder and VAT status.

Exact user actions or commands:

1. Start the app.
2. Confirm the dashboard shows the current student and lessons for previous/current/next month.
3. Add or edit one uninvoiced lesson and confirm Google Calendar updates.
4. Try editing an invoiced lesson and confirm the UI blocks it.
5. Click `Write Invoice`.
6. Select a student from the candidate dialog.
7. Review the invoice preview.
8. Add a price adjustment with a reason.
9. Cancel and confirm no invoice/PDF/local lesson record was saved.
10. Repeat `Write Invoice`.
11. Click `Print Invoice`.
12. Confirm a PDF is saved in the configured invoice folder.
13. Confirm the hard-copy print is attempted only after the PDF exists.
14. Return to the main view.
15. Confirm the invoice appears as outstanding.
16. Toggle outstanding to paid.
17. Toggle paid to outstanding and confirm the dialog appears before the change.
18. Double-click the invoice and confirm it opens read-only in the invoice viewer.

Expected successful result:

- Invoice PDF exists.
- Invoice record exists.
- Invoice lessons are stored locally and marked invoiced.
- Invoiced lessons cannot be edited.
- Invoice appears in the main invoice pane.
- Payment flag toggles according to the confirmation rule.

Expected failure/safety result, if relevant:

- If PDF save fails, no invoice data is saved and no print is attempted.
- If Google Calendar update fails, the user is notified immediately and the lesson list does not pretend the change succeeded.
- If the invoice preview is cancelled, no PDF, invoice, or local lesson data is saved.

Where to inspect artifacts, reports, logs, database state, or UI state:

- Configured invoice folder for PDF.
- SQLite database file in the configured app data location.
- App log file if logging is implemented.
- Main WPF dashboard invoice and lesson lists.

Cleanup/reset instructions:

- Delete sample Google Calendar entries from the test calendar.
- Delete sample PDFs from the test invoice folder.
- Delete the test SQLite database or use an app reset command if implemented.

## Completion Log

- 2026-06-16:
  - Stage: Stage 11 - Hardening, Packaging Readiness, And Documentation Refresh
  - Files changed: `README.md`, `.ai/project-profile.md`, `.ai/context/HANDOFF.md`, `.ai/implementation-plans/active/2026-06-16-v1-invoice-printer.md`, `src/ElenaSmallBusinessLedger.Core/Invoices/InvoiceDraft.cs`, `src/ElenaSmallBusinessLedger.Core/Ports/ICompanyProfileRepository.cs`, `src/ElenaSmallBusinessLedger.Infrastructure/Persistence/SqliteCompanyProfileRepository.cs`, `src/ElenaSmallBusinessLedger.App/App.xaml.cs`, `src/ElenaSmallBusinessLedger.App/MainWindow.xaml`, `src/ElenaSmallBusinessLedger.App/Services/IAppSettingsService.cs`, `src/ElenaSmallBusinessLedger.App/Services/CoreAppSettingsService.cs`, `src/ElenaSmallBusinessLedger.App/Services/ISettingsLauncher.cs`, `src/ElenaSmallBusinessLedger.App/Services/SettingsWindowLauncher.cs`, `src/ElenaSmallBusinessLedger.App/ViewModels/SettingsViewModel.cs`, `src/ElenaSmallBusinessLedger.App/ViewModels/InvoiceLineDisplayModel.cs`, `src/ElenaSmallBusinessLedger.App/ViewModels/InvoicePreviewViewModel.cs`, `src/ElenaSmallBusinessLedger.App/Views/SettingsView.*`, `tests/ElenaSmallBusinessLedger.Tests/App/ViewModels/SettingsViewModelTests.cs`, `tests/ElenaSmallBusinessLedger.Tests/App/ViewModels/MainWindowViewModelTests.cs`, `tests/ElenaSmallBusinessLedger.Tests/App/ViewModels/InvoiceViewModelTests.cs`, and `tests/ElenaSmallBusinessLedger.Tests/Core/Invoices/InvoiceDraftTests.cs`.
  - Summary: Added the minimal pilot setup surface under `Application > Settings` for company profile, invoice folder, Google Calendar ID/account label, and Google client secrets import. Hardened invoice draft creation so empty invoices are rejected in Core, made missing company profile errors user-readable, and refreshed invoice preview totals when included lines or price adjustments change. Updated README, project profile, and handoff with runtime paths, setup steps, and pilot-test notes.
  - Tests/verification run: Focused settings/main-window tests during implementation; focused invoice regression tests; `dotnet build ElenaSmallBusinessLedger.sln -nodeReuse:false`; `dotnet test ElenaSmallBusinessLedger.sln -nodeReuse:false`; `dotnet format ElenaSmallBusinessLedger.sln --verify-no-changes --no-restore`.
  - Results: Full build passed with 0 warnings and 0 errors. Full test suite passed: 96 passed, 0 failed. Formatter verification did not pass because it reports broad existing line-ending/whitespace differences across the repository, including files outside this final change; no sweeping format rewrite was performed.
  - Dependencies added: None.
  - Review notes: Dependency/license review found only the already accepted/expected production dependencies: Google Calendar API client, EF Core SQLite, QuestPDF, and Windows ProtectedData. Secrets review confirmed OAuth tokens remain in Windows-protected storage and are not stored in SQLite; the selected Google client secrets JSON is copied to the documented app-data path. Focused Core/Infrastructure review found and fixed empty-invoice acceptance and missing-company-profile messaging. Focused UI review found and fixed stale invoice-preview totals after include/exclude or adjustment edits.
  - Deviations: Stage 11 made a small setup UI addition because a full pilot would otherwise require manual database/file setup. The real end-to-end pilot was not run in this automated session because it requires Ruben/Elena's Google Calendar OAuth setup, safe Calendar data, invoice folder, and local print/PDF association.
  - Risks/open questions: Ruben/Elena still need to run the final user test below. PDF layout polish, admin overview, plan administration, backup/export, VAT reporting, credit notes, and payment due terms remain follow-up work.

- 2026-06-16:
  - Stage: Stage 10 - Implement Write Invoice Flow And Invoice Viewer
  - Files changed: `src/ElenaSmallBusinessLedger.Core/Ports/IInvoiceRepository.cs`, `src/ElenaSmallBusinessLedger.App/App.xaml.cs`, `src/ElenaSmallBusinessLedger.App/Services/IInvoiceWorkflowService.cs`, `src/ElenaSmallBusinessLedger.App/Services/CoreInvoiceWorkflowService.cs`, `src/ElenaSmallBusinessLedger.App/Services/IInvoiceWorkflowLauncher.cs`, `src/ElenaSmallBusinessLedger.App/Services/InvoiceWorkflowWindowLauncher.cs`, `src/ElenaSmallBusinessLedger.App/ViewModels/InvoiceCandidateDialogViewModel.cs`, `src/ElenaSmallBusinessLedger.App/ViewModels/InvoiceLineDisplayModel.cs`, `src/ElenaSmallBusinessLedger.App/ViewModels/InvoicePreviewViewModel.cs`, `src/ElenaSmallBusinessLedger.App/ViewModels/InvoiceViewerViewModel.cs`, `src/ElenaSmallBusinessLedger.App/ViewModels/MainWindowViewModel.cs`, `src/ElenaSmallBusinessLedger.App/Views/InvoiceCandidateDialog.*`, `src/ElenaSmallBusinessLedger.App/Views/InvoicePreviewView.*`, `src/ElenaSmallBusinessLedger.App/Views/InvoiceViewerView.*`, `tests/ElenaSmallBusinessLedger.Tests/App/ViewModels/InvoiceViewModelTests.cs`, `tests/ElenaSmallBusinessLedger.Tests/App/ViewModels/MainWindowViewModelTests.cs`, and existing test fakes updated for `GetIssuedInvoiceAsync`.
  - Summary: Added the WPF Write Invoice workflow: candidate student dialog, invoice preview with include/exclude lesson rows and mandatory-reason price adjustment, issue command through the Core staged PDF/save/print use case, main-window refresh after successful issue, and read-only saved invoice viewer on invoice double-click. The real startup composition now includes company profile repository, QuestPDF writer, saved-PDF printer, invoice workflow service, and invoice launcher.
  - Tests/verification run: Red check: `dotnet test tests\ElenaSmallBusinessLedger.Tests\ElenaSmallBusinessLedger.Tests.csproj --filter "FullyQualifiedName~InvoiceViewModelTests" -nodeReuse:false` failed before invoice ViewModels existed. Red check: focused main-window invoice command tests failed before `IInvoiceWorkflowLauncher` existed. Review-driven red check: empty invoice and print-failure tests failed before the preview safeguards were added. Green checks: focused invoice ViewModel/main-window tests; `dotnet build ElenaSmallBusinessLedger.sln -nodeReuse:false`; `dotnet test ElenaSmallBusinessLedger.sln -nodeReuse:false`.
  - Results: Full build passed with 0 warnings and 0 errors. Full test suite passed: 90 passed, 0 failed.
  - Dependencies added: None.
  - Review notes: Focused code/UI review found and fixed two issues before completion: every lesson could be excluded, producing an empty invoice attempt; and a post-save print request failure closed the preview before showing the warning. The preview now blocks empty invoice issue and stays open with an error when the PDF is saved but print request fails.
  - Deviations: The invoice viewer is a read-only WPF invoice detail view, not an embedded PDF renderer. The saved PDF path is shown so the generated PDF remains inspectable. Real end-to-end issue still requires company profile, Google Calendar settings/client secrets, and a printer/PDF shell association in the local environment.
  - Risks/open questions: Manual visual testing is still needed for the three invoice windows and the real PDF/print path. Stage 11 remains next for hardening, setup notes, documentation refresh, final review, and pilot-test readiness.

- 2026-06-16:
  - Stage: Stage 9 course correction - replace sample runtime wiring with real composition
  - Files changed: `README.md`, `.ai/project-profile.md`, `docs/business-requirements-and-logic.md`, `src/ElenaSmallBusinessLedger.Core/Calendar/*`, `src/ElenaSmallBusinessLedger.Core/Ports/IGoogleCalendarGateway.cs`, `src/ElenaSmallBusinessLedger.Core/UseCases/Lessons/ToggleLessonStateUseCase.cs`, `src/ElenaSmallBusinessLedger.Core/UseCases/Startup/StartupDashboardQuery.cs`, `src/ElenaSmallBusinessLedger.Infrastructure/Calendar/*`, `src/ElenaSmallBusinessLedger.App/App.xaml.cs`, `src/ElenaSmallBusinessLedger.App/Services/Core*Service.cs`, `src/ElenaSmallBusinessLedger.App/Services/ConfiguredGoogleCalendarGateway.cs`, `src/ElenaSmallBusinessLedger.App/Services/SystemUiClock.cs`, deleted `src/ElenaSmallBusinessLedger.App/Services/Sample*Service.cs`, `tests/ElenaSmallBusinessLedger.Tests/App/Services/AppServiceTests.cs`, `tests/ElenaSmallBusinessLedger.Tests/Core/Calendar/CalendarLessonEntryTests.cs`, `tests/ElenaSmallBusinessLedger.Tests/Core/UseCases/UseCaseTests.cs`, `tests/ElenaSmallBusinessLedger.Tests/Infrastructure/Calendar/GoogleCalendarGatewayTests.cs`, `.ai/implementation-plans/active/2026-06-16-v1-invoice-printer.md`
  - Summary: Removed sample services from the real WPF startup path and deleted the dead sample service files. App startup now creates the local SQLite database under `%LOCALAPPDATA%\ElenaSmallBusinessLedger`, composes SQLite repositories, Core use cases, Core-backed student/lesson/payment services, and a configured Google Calendar gateway. Missing Calendar settings or client secrets now produce truthful failure results instead of invented dashboard rows. Manual Completed/Planned state for uninvoiced lessons is now durable in Google Calendar by preserving the first-line `$` marker and adding/removing a later `Status: Completed` summary line.
  - Tests/verification run: Red check: `dotnet test tests\ElenaSmallBusinessLedger.Tests\ElenaSmallBusinessLedger.Tests.csproj --filter "FullyQualifiedName~CalendarLessonEntry|FullyQualifiedName~LessonStateToggle|FullyQualifiedName~GoogleCalendarGateway" -nodeReuse:false` failed before state-aware Calendar APIs existed. Red check: `dotnet test tests\ElenaSmallBusinessLedger.Tests\ElenaSmallBusinessLedger.Tests.csproj --filter "FullyQualifiedName~AppServiceTests" -nodeReuse:false` failed before the real app service adapters/configured gateway existed. Green checks: same focused commands; `dotnet build ElenaSmallBusinessLedger.sln -nodeReuse:false`; `dotnet test ElenaSmallBusinessLedger.sln -nodeReuse:false`.
  - Results: Focused Calendar/state tests passed: 18 passed, 0 failed. App service tests passed: 2 passed, 0 failed. Full build passed with 0 warnings and 0 errors. Full test suite passed: 83 passed, 0 failed.
  - Dependencies added: None.
  - Deviations: The real startup path currently requires later setup UI/tooling for `CalendarSettings` and `%LOCALAPPDATA%\ElenaSmallBusinessLedger\google-client-secrets.json`; until then, Calendar-backed dashboard/edit actions fail closed with a configuration message. This is intentional and preferable to sample data masquerading as persistence.
  - Risks/open questions: Stage 10 remains the next planned implementation stage for Write Invoice and invoice viewing. A small setup/admin workflow is still needed so Ruben/Elena can choose the Google calendar/account and supply OAuth client secrets without manual database/file setup.

- 2026-06-16:
  - Stage: Stage 9 feedback refinement - inline Add Lesson row
  - Files changed: `docs/business-requirements-and-logic.md`, `src/ElenaSmallBusinessLedger.App/ViewModels/LessonDisplayModel.cs`, `src/ElenaSmallBusinessLedger.App/ViewModels/MainWindowViewModel.cs`, `tests/ElenaSmallBusinessLedger.Tests/App/ViewModels/MainWindowViewModelTests.cs`, `.ai/implementation-plans/active/2026-06-16-v1-invoice-printer.md`
  - Summary: Changed Add Lesson so it no longer opens the old Lesson dialog. The command now inserts an editable draft row for the current student with empty date/time fields. When the user fills in a valid date and time, the inline save path calls the Calendar add service and marks the draft as a saved lesson.
  - Tests/verification run: Red check: `dotnet test tests\ElenaSmallBusinessLedger.Tests\ElenaSmallBusinessLedger.Tests.csproj --filter "FullyQualifiedName~AddLessonAddsEditableDraftRowWithoutOpeningDialog|FullyQualifiedName~SavingCompleteDraftLessonAddsCalendarLesson" -nodeReuse:false` failed before the draft-row model existed. Green checks: same command; `dotnet test tests\ElenaSmallBusinessLedger.Tests\ElenaSmallBusinessLedger.Tests.csproj --filter "FullyQualifiedName~MainWindowViewModelTests" -nodeReuse:false`; `dotnet build ElenaSmallBusinessLedger.sln -nodeReuse:false`; `dotnet test ElenaSmallBusinessLedger.sln -nodeReuse:false`.
  - Results: Add Lesson focused tests passed: 2 passed, 0 failed. MainWindowViewModel tests passed: 18 passed, 0 failed. Full build passed with 0 warnings and 0 errors. Full test suite passed: 76 passed, 0 failed.
  - Dependencies added: None.
  - Risks/open questions: Visual/manual confirmation is still useful to ensure the new draft row is easy to spot and edit in the WPF grid.

- 2026-06-16:
  - Stage: Stage 9 bug fix - lesson delete visibility
  - Files changed: `src/ElenaSmallBusinessLedger.App/ViewModels/MainWindowViewModel.cs`, `tests/ElenaSmallBusinessLedger.Tests/App/ViewModels/MainWindowViewModelTests.cs`, `.ai/implementation-plans/active/2026-06-16-v1-invoice-printer.md`
  - Summary: Fixed the row delete interaction reported by Ruben. A successful delete now removes the lesson from the visible dashboard collection instead of immediately refreshing from the sample dashboard and reintroducing the hard-coded row.
  - Tests/verification run: Red check: `dotnet test tests\ElenaSmallBusinessLedger.Tests\ElenaSmallBusinessLedger.Tests.csproj --filter "FullyQualifiedName~SuccessfulLessonDeleteRemovesVisibleLesson" -nodeReuse:false` failed with the deleted row still visible. Green checks: same command; `dotnet test tests\ElenaSmallBusinessLedger.Tests\ElenaSmallBusinessLedger.Tests.csproj --filter "FullyQualifiedName~MainWindowViewModelTests" -nodeReuse:false`; `dotnet build ElenaSmallBusinessLedger.sln -nodeReuse:false`; `dotnet test ElenaSmallBusinessLedger.sln -nodeReuse:false`.
  - Results: Targeted delete regression passed: 1 passed, 0 failed. MainWindowViewModel tests passed: 16 passed, 0 failed. Full build passed with 0 warnings and 0 errors. Full test suite passed: 74 passed, 0 failed.
  - Dependencies added: None.
  - Risks/open questions: This keeps the sample WPF shell visually truthful after deletion. Real Google Calendar composition should eventually refresh from actual Calendar data, where the deleted event should naturally be absent.

- 2026-06-16:
  - Stage: Stage 9 bug fix - lesson state race and DatePicker popup behavior
  - Files changed: `src/ElenaSmallBusinessLedger.App/MainWindow.xaml`, `src/ElenaSmallBusinessLedger.App/MainWindow.xaml.cs`, `src/ElenaSmallBusinessLedger.App/ViewModels/MainWindowViewModel.cs`, `tests/ElenaSmallBusinessLedger.Tests/App/ViewModels/MainWindowViewModelTests.cs`, `.ai/implementation-plans/active/2026-06-16-v1-invoice-printer.md`
  - Summary: Fixed two Stage 9 interaction bugs reported by Ruben. Confirmation dialogs now suppress activation refresh while the command is in progress, preventing a refresh from replacing the visible lesson row before Completed -> Planned is applied. The DatePicker no longer has a broad first-click mouse handler, so clicking the date field should focus/edit the field without opening the calendar popup, while the native calendar button owns popup opening.
  - Tests/verification run: Red check: `dotnet test tests\ElenaSmallBusinessLedger.Tests\ElenaSmallBusinessLedger.Tests.csproj --filter "FullyQualifiedName~CompletedLessonReturnToPlannedIsNotLostWhenConfirmationReactivatesWindow" -nodeReuse:false` failed with visible row still `Completed`; green checks: same command, `dotnet test tests\ElenaSmallBusinessLedger.Tests\ElenaSmallBusinessLedger.Tests.csproj --filter "FullyQualifiedName~MainWindowViewModelTests" -nodeReuse:false`, `dotnet build ElenaSmallBusinessLedger.sln -nodeReuse:false`, `dotnet test ElenaSmallBusinessLedger.sln -nodeReuse:false`.
  - Results: Targeted regression passed: 1 passed, 0 failed. MainWindowViewModel tests passed: 15 passed, 0 failed. Full build passed with 0 warnings and 0 errors. Full test suite passed: 73 passed, 0 failed.
  - Dependencies added: None.
  - Risks/open questions: The DatePicker popup behavior still needs Ruben-side visual confirmation in the running WPF app because it is control-template/input-device behavior, not fully covered by ViewModel tests.

- 2026-06-16:
  - Stage: Stage 9 feedback refinement - lesson state and inline picker polish
  - Files changed: `docs/business-requirements-and-logic.md`, `src/ElenaSmallBusinessLedger.Core/UseCases/Startup/StartupDashboardQuery.cs`, `src/ElenaSmallBusinessLedger.App/App.xaml.cs`, `src/ElenaSmallBusinessLedger.App/MainWindow.xaml`, `src/ElenaSmallBusinessLedger.App/MainWindow.xaml.cs`, `src/ElenaSmallBusinessLedger.App/Services/IConfirmationService.cs`, `src/ElenaSmallBusinessLedger.App/Services/MessageBoxConfirmationService.cs`, `src/ElenaSmallBusinessLedger.App/Services/ILessonStateService.cs`, `src/ElenaSmallBusinessLedger.App/Services/SampleLessonStateService.cs`, `src/ElenaSmallBusinessLedger.App/ViewModels/LessonDisplayModel.cs`, `src/ElenaSmallBusinessLedger.App/ViewModels/MainWindowViewModel.cs`, `tests/ElenaSmallBusinessLedger.Tests/App/ViewModels/MainWindowViewModelTests.cs`, `tests/ElenaSmallBusinessLedger.Tests/Core/UseCases/UseCaseTests.cs`, `.ai/implementation-plans/active/2026-06-16-v1-invoice-printer.md`
  - Summary: Applied Ruben's follow-up UI feedback: the row delete action is now a quiet icon-style button; inline date controls open their selector on first click; time editing uses an editable selector with quarter-hour choices; Planned/Completed state is now a manual row action; completed lessons may return to Planned only after confirmation; completed lessons remain locked for date/time edits and deletion; uninvoiced past lessons are no longer automatically marked Completed by the startup query.
  - Tests/verification run: Red check: `dotnet test tests\ElenaSmallBusinessLedger.Tests\ElenaSmallBusinessLedger.Tests.csproj --filter "FullyQualifiedName~MainWindowViewModelTests" -nodeReuse:false` failed before `ILessonStateService` existed. Focused green check: `dotnet test tests\ElenaSmallBusinessLedger.Tests\ElenaSmallBusinessLedger.Tests.csproj --filter "FullyQualifiedName~MainWindowViewModelTests|FullyQualifiedName~StartupDashboardDoesNotCompleteUninvoicedLessonsByDate" -nodeReuse:false`. Full checks: `dotnet build ElenaSmallBusinessLedger.sln -nodeReuse:false`; `dotnet test ElenaSmallBusinessLedger.sln -nodeReuse:false`.
  - Results: Focused tests passed: 15 passed, 0 failed. Full build passed with 0 warnings and 0 errors. Full test suite passed: 72 passed, 0 failed.
  - Dependencies added: None.
  - Deviations: `Write Invoice` still remains Stage 10 scope. Superseded note: manual lesson-state persistence was initially represented by an app-level seam; the Stage 9 course correction above now persists Completed/Planned state through the Google Calendar status marker.
  - Review notes: The business requirements now distinguish schedule changes saved to Google Calendar from manual Planned/Completed state changes.
  - Risks/open questions: Visual/manual confirmation is still needed for the first-click date selector, time selector, and state toggle behavior in the running WPF app.

- 2026-06-16:
  - Stage: Stage 9 feedback refinement - Student/lesson UI corrections
  - Files changed: `docs/business-requirements-and-logic.md`, `.ai/project-profile.md`, `.ai/standards/ui-design-standards.md`, `src/ElenaSmallBusinessLedger.App/MainWindow.xaml`, `src/ElenaSmallBusinessLedger.App/MainWindow.xaml.cs`, `src/ElenaSmallBusinessLedger.App/ViewModels/MainWindowViewModel.cs`, `src/ElenaSmallBusinessLedger.App/ViewModels/LessonDisplayModel.cs`, `src/ElenaSmallBusinessLedger.App/ViewModels/StudentEditorViewModel.cs`, `src/ElenaSmallBusinessLedger.App/ViewModels/SchedulePlacementViewModel.cs`, `src/ElenaSmallBusinessLedger.App/Views/StudentEditorView.xaml`, `src/ElenaSmallBusinessLedger.App/Services/IConfirmationService.cs`, `src/ElenaSmallBusinessLedger.App/Services/MessageBoxConfirmationService.cs`, `src/ElenaSmallBusinessLedger.App/Services/WindowPlacementStore.cs`, `src/ElenaSmallBusinessLedger.App/App.xaml.cs`, `src/ElenaSmallBusinessLedger.App/Views/StudentEditorView.xaml.cs`, `src/ElenaSmallBusinessLedger.App/Views/LessonEditorView.xaml.cs`, `tests/ElenaSmallBusinessLedger.Tests/App/ViewModels/EditorViewModelTests.cs`, `tests/ElenaSmallBusinessLedger.Tests/App/ViewModels/MainWindowViewModelTests.cs`, `.ai/implementation-plans/active/2026-06-16-v1-invoice-printer.md`
  - Summary: Applied Ruben's Stage 9 UI feedback: student and plan dates now use date selectors; Plan-owned fields are displayed read-only except student-specific Starts on/Ends on; plan flexibility is human-readable; schedule editing uses fixed placement rows with an Unspecified option instead of Add/Remove; main/subwindow placement is persisted and clamped to the visible desktop; lesson editing moved from a separate double-click dialog to inline planned-lesson date/time controls plus a row-level delete button with confirmation; completed/invoiced lessons cannot be edited or deleted; the inline message banner can be dismissed and clears when row selection changes.
  - Tests/verification run: `dotnet test tests\ElenaSmallBusinessLedger.Tests\ElenaSmallBusinessLedger.Tests.csproj --filter "FullyQualifiedName~EditorViewModel|FullyQualifiedName~MainWindowViewModel|FullyQualifiedName~StudentUseCase" -nodeReuse:false`; `dotnet build ElenaSmallBusinessLedger.sln -nodeReuse:false`; `dotnet test ElenaSmallBusinessLedger.sln -nodeReuse:false`.
  - Results: Focused tests passed: 18 passed, 0 failed. Full build passed with 0 warnings and 0 errors. Full test suite passed: 68 passed, 0 failed.
  - Dependencies added: None.
  - Deviations: `Add Plan` is present as a visible command but currently reports that plan administration will be implemented with the plan administration workflow; a real implementation needs a Plan catalog/selection workflow so Plan remains the owner of Plan fields. `Write Invoice` remains Stage 10 scope.
  - Review notes: The reusable UI standard now records Ruben's default rule that desktop main windows and subwindows preserve size/position while restoring inside the current visible screen area.
  - Risks/open questions: Visual/manual confirmation still needed for the refined student editor layout and inline lesson-edit behavior in Visual Studio.

- 2026-06-16:
  - Stage: Stage 9 - Implement Student Add/Edit And Lesson Editing UI
  - Files changed: `docs/business-requirements-and-logic.md`, `src/ElenaSmallBusinessLedger.Core/Students/StudentScheduleEntry.cs`, `src/ElenaSmallBusinessLedger.Core/Ports/IStudentRepository.cs`, `src/ElenaSmallBusinessLedger.Core/Ports/StudentRecord.cs`, `src/ElenaSmallBusinessLedger.Core/UseCases/Students/*`, `src/ElenaSmallBusinessLedger.Infrastructure/Persistence/Entities.cs`, `src/ElenaSmallBusinessLedger.Infrastructure/Persistence/LedgerDbContext.cs`, `src/ElenaSmallBusinessLedger.Infrastructure/Persistence/SqliteStudentRepository.cs`, `src/ElenaSmallBusinessLedger.App/App.xaml.cs`, `src/ElenaSmallBusinessLedger.App/MainWindow.xaml`, `src/ElenaSmallBusinessLedger.App/MainWindow.xaml.cs`, `src/ElenaSmallBusinessLedger.App/Services/*Editor*`, `src/ElenaSmallBusinessLedger.App/ViewModels/*Editor*`, `src/ElenaSmallBusinessLedger.App/Views/*Editor*`, `tests/ElenaSmallBusinessLedger.Tests/App/ViewModels/EditorViewModelTests.cs`, `tests/ElenaSmallBusinessLedger.Tests/App/ViewModels/MainWindowViewModelTests.cs`, `tests/ElenaSmallBusinessLedger.Tests/Core/UseCases/StudentUseCaseTests.cs`, `tests/ElenaSmallBusinessLedger.Tests/Core/UseCases/UseCaseTests.cs`, `tests/ElenaSmallBusinessLedger.Tests/Infrastructure/Persistence/SqlitePersistenceTests.cs`, `.ai/implementation-plans/active/2026-06-16-v1-invoice-printer.md`
  - Summary: Added Core student save request/use case with one-active-plan-period validation, student schedule entries, SQLite persistence for schedule entries, student add/edit and lesson add/edit/cancel ViewModels, WPF editor dialogs, sample editor services for the current shell, Core-backed editor service adapters, main-window Add Lesson and double-click lesson editing, and documented the deliberate no-prices-on-main-screen decision.
  - Tests/verification run: Red check: `dotnet test tests\ElenaSmallBusinessLedger.Tests\ElenaSmallBusinessLedger.Tests.csproj --filter "FullyQualifiedName~StudentUseCase|FullyQualifiedName~EditorViewModel" -nodeReuse:false` failed because the planned student use-case contracts, editor services, and editor ViewModels did not exist. Green checks: same targeted command; `dotnet test tests\ElenaSmallBusinessLedger.Tests\ElenaSmallBusinessLedger.Tests.csproj --filter "FullyQualifiedName~StudentUseCase|FullyQualifiedName~EditorViewModel|FullyQualifiedName~MainWindowViewModel" -nodeReuse:false`; `dotnet build ElenaSmallBusinessLedger.sln -nodeReuse:false`; `dotnet test ElenaSmallBusinessLedger.sln -nodeReuse:false`.
  - Results: Targeted Stage 9 tests passed: 17 passed, 0 failed. Full build passed with 0 warnings and 0 errors. Full test suite passed: 67 passed, 0 failed.
  - Dependencies added: None.
  - Deviations: The current app startup still uses sample editor services so Ruben can safely exercise dialogs without real Google Calendar writes. Core-backed editor adapters are present for real composition in a later wiring stage. The main screen continues not to show prices by design; price fields appear only in the student editor/admin-style surface and invoice flow.
  - Review notes: Tests cover student save validation, overlapping plan rejection, schedule persistence round trip, editor validation input preservation, Calendar update failure truthfulness, blocked invoiced lesson editing, and dashboard refresh after a saved student dialog.
  - Risks/open questions: Visual/manual check still needed in Visual Studio for the new dialogs: Add Student, Edit Student, Add Lesson, and double-click an uninvoiced lesson. Stage 10 remains the next implementation stage.

- 2026-06-16:
  - Stage: Stage 8 - Implement WPF Shell And Startup Dashboard
  - Files changed: `src/ElenaSmallBusinessLedger.App/App.xaml`, `src/ElenaSmallBusinessLedger.App/App.xaml.cs`, `src/ElenaSmallBusinessLedger.App/MainWindow.xaml`, `src/ElenaSmallBusinessLedger.App/MainWindow.xaml.cs`, `src/ElenaSmallBusinessLedger.App/Commands/*`, `src/ElenaSmallBusinessLedger.App/Services/*`, `src/ElenaSmallBusinessLedger.App/ViewModels/*`, `src/ElenaSmallBusinessLedger.Core/UseCases/Startup/StartupDashboardResult.cs`, `src/ElenaSmallBusinessLedger.Core/UseCases/Startup/StartupDashboardQuery.cs`, `tests/ElenaSmallBusinessLedger.Tests/ElenaSmallBusinessLedger.Tests.csproj`, `tests/ElenaSmallBusinessLedger.Tests/App/ViewModels/MainWindowViewModelTests.cs`, `tests/ElenaSmallBusinessLedger.Tests/Core/UseCases/UseCaseTests.cs`, `.ai/implementation-plans/active/2026-06-16-v1-invoice-printer.md`
  - Summary: Added the WPF main shell with sample dashboard composition, current student/month header, Google Calendar open/refresh buttons, lesson list, student navigation, month navigation, student menu entries, student invoice pane, paid/outstanding toggle behavior, paid-to-outstanding confirmation, inline refresh/update errors, app-activation refresh debounce, and a self-contained main-window resource setup for tests.
  - Tests/verification run: Red check: `dotnet test tests\ElenaSmallBusinessLedger.Tests\ElenaSmallBusinessLedger.Tests.csproj --filter "FullyQualifiedName~ViewModels" -nodeReuse:false` failed before planned App ViewModel/window types existed. Additional red check: the WPF smoke test failed because `MainWindow` depended on an app-level `BooleanToVisibilityConverter`; fixed by making the window resource self-contained. Green checks: `dotnet test tests\ElenaSmallBusinessLedger.Tests\ElenaSmallBusinessLedger.Tests.csproj --filter "FullyQualifiedName~ViewModels" -nodeReuse:false`; `dotnet test tests\ElenaSmallBusinessLedger.Tests\ElenaSmallBusinessLedger.Tests.csproj --filter "FullyQualifiedName~StartupDashboard" -nodeReuse:false`; `dotnet build ElenaSmallBusinessLedger.sln -nodeReuse:false`; `dotnet test ElenaSmallBusinessLedger.sln -nodeReuse:false`.
  - Results: ViewModel/window tests passed: 9 passed, 0 failed. StartupDashboard filtered tests passed: 1 passed, 0 failed. Full build passed with 0 warnings and 0 errors. Full test suite passed: 59 passed, 0 failed.
  - Dependencies added: None.
  - Deviations: Previous/Next student navigation now uses an explicit active-student list from the startup dashboard result instead of inferring students from currently visible lesson rows. This is more reliable once the dashboard filters lessons to the selected student. The test project now targets `net10.0-windows` and references the WPF App project so ViewModel/window tests can compile and instantiate WPF surfaces.
  - Review notes: Focused UI/state review found and fixed the weak student navigation contract and the window resource dependency. Superseded note: the dashboard was initially sample-backed; the Stage 9 course correction above now wires the real Core/SQLite/Calendar-backed runtime path.
  - Risks/open questions: The WPF executable was launched and the process stayed responding, but the tool session could not enumerate a visible top-level desktop window for it. Ruben should visually confirm the dashboard shell opens with sample data before moving past the Stage 8 UI-review gate.

- 2026-06-16:
  - Stage: Stage 7 - Implement PDF Storage, Invoice Rendering, And Printing
  - Files changed: `src/ElenaSmallBusinessLedger.Infrastructure/ElenaSmallBusinessLedger.Infrastructure.csproj`, `src/ElenaSmallBusinessLedger.Infrastructure/Invoices/Pdf/*`, `src/ElenaSmallBusinessLedger.Infrastructure/Invoices/Printing/*`, `tests/ElenaSmallBusinessLedger.Tests/Infrastructure/Invoices/InvoiceInfrastructureTests.cs`, `.ai/project-profile.md`, `README.md`, `.ai/implementation-plans/active/2026-06-16-v1-invoice-printer.md`
  - Summary: Added QuestPDF invoice rendering, local staged invoice PDF storage, staged PDF verification, stable sanitized final filenames, fail-closed no-overwrite publishing, and a printer adapter that only requests printing for an existing saved PDF.
  - Tests/verification run: Red check: `dotnet test tests\ElenaSmallBusinessLedger.Tests\ElenaSmallBusinessLedger.Tests.csproj --filter "FullyQualifiedName~Invoice" -nodeReuse:false` failed because planned PDF/file/printing Infrastructure types did not exist. Green checks: same invoice-filtered command; `dotnet build ElenaSmallBusinessLedger.sln -nodeReuse:false`; `dotnet test ElenaSmallBusinessLedger.sln -nodeReuse:false`; targeted sample-PDF smoke test with `ELENA_LEDGER_KEEP_SAMPLE_PDF`; rendered `tmp\pdfs\stage7-sample-invoice.pdf` to `tmp\pdfs\stage7-sample-invoice.png` using `pypdfium2` and visually inspected the output.
  - Results: Invoice-filtered tests passed: 26 passed, 0 failed. Full build passed with 0 warnings and 0 errors. Full test suite passed: 50 passed, 0 failed. Visual PDF check showed required Danish invoice sections, company/student information, lesson row, VAT/totals, and footer with no clipping or overlap.
  - Dependencies added: `QuestPDF` 2026.6.0.
  - Deviations: The printer adapter can prove only that the OS print request was launched for an existing PDF; it cannot prove paper was physically printed. Final file collision handling fails closed instead of generating a suffix, because issued invoice PDFs must not be silently overwritten.
  - Review notes: Focused code-review pass found no blocking artifact-safety issues after implementation. Current tests cover staged PDF verification, final filename sanitization, no-overwrite behavior, PDF smoke generation, missing-PDF print refusal, and print request dispatch for an existing PDF.
  - Risks/open questions: Manual PDF review by Ruben/Elena is still the Stage 7 gate before proceeding to WPF wiring. The sample files are under `tmp\pdfs\` and can be deleted after review.

- 2026-06-16:
  - Stage: Stage 6 - Implement Google Calendar Gateway
  - Files changed: `src/ElenaSmallBusinessLedger.Infrastructure/ElenaSmallBusinessLedger.Infrastructure.csproj`, `src/ElenaSmallBusinessLedger.Infrastructure/Calendar/*`, `src/ElenaSmallBusinessLedger.Infrastructure/Persistence/Entities.cs`, `src/ElenaSmallBusinessLedger.Infrastructure/Persistence/LedgerDbContext.cs`, `src/ElenaSmallBusinessLedger.Infrastructure/Persistence/CalendarSettings.cs`, `src/ElenaSmallBusinessLedger.Infrastructure/Persistence/SqliteCalendarSettingsRepository.cs`, `tests/ElenaSmallBusinessLedger.Tests/Infrastructure/Calendar/GoogleCalendarGatewayTests.cs`, `tests/ElenaSmallBusinessLedger.Tests/Infrastructure/Persistence/SqlitePersistenceTests.cs`, `.ai/project-profile.md`, `README.md`, `.ai/implementation-plans/active/2026-06-16-v1-invoice-printer.md`
  - Summary: Added the Google Calendar Infrastructure adapter behind the Core gateway port. The implementation uses the official `Google.Apis.Calendar.v3` package, external browser OAuth through Google's installed-app flow, Windows DPAPI-protected local token storage, SQLite storage for selected calendar/account labels, strict first-line `$` marker filtering, event create/update/delete mapping, and Core failure results for Calendar write failures.
  - Tests/verification run: Red check: `dotnet test tests\ElenaSmallBusinessLedger.Tests\ElenaSmallBusinessLedger.Tests.csproj --filter "FullyQualifiedName~Calendar|FullyQualifiedName~CalendarSettings" -nodeReuse:false` failed because the planned Stage 6 Infrastructure types did not exist. Additional red check: `dotnet test tests\ElenaSmallBusinessLedger.Tests\ElenaSmallBusinessLedger.Tests.csproj --filter "FullyQualifiedName~RefreshFailureAfterSuccessfulCalendarWriteIsReportedTruthfully" -nodeReuse:false` failed because a successful Google write followed by refresh failure was reported as `Calendar.UpdateFailed`. Green checks: same targeted refresh-failure test; Calendar/CalendarSettings filtered tests; `dotnet build ElenaSmallBusinessLedger.sln -nodeReuse:false`; `dotnet test ElenaSmallBusinessLedger.sln -nodeReuse:false`.
  - Results: Calendar/CalendarSettings filtered tests passed: 20 passed, 0 failed. Full build passed with 0 warnings and 0 errors. Full test suite passed: 44 passed, 0 failed.
  - Dependencies added: `Google.Apis.Calendar.v3` 1.74.0.4154; `System.Security.Cryptography.ProtectedData` 10.0.9.
  - Deviations: Real Google account authorization was not manually exercised in this stage. Event creation uses a configurable default lesson duration because the current Core add-lesson request carries student/start time but not a plan-derived duration.
  - Review notes: Code-review pass found and fixed one truthful-state issue: if Google accepts a write but the subsequent refresh fails, the result is now `Calendar.RefreshFailed` rather than falsely claiming the Calendar update failed.
  - Risks/open questions: Stage 7 is blocked by the PDF dependency decision. Google Calendar read/list failures still surface as exceptions through the current Core list contract; before WPF wiring, consider whether startup/invoice-preview read failures should use explicit Core failure results.

- 2026-06-16:
  - Stage: Stage 5 - Implement SQLite Persistence
  - Files changed: `src/ElenaSmallBusinessLedger.Infrastructure/ElenaSmallBusinessLedger.Infrastructure.csproj`, `src/ElenaSmallBusinessLedger.Infrastructure/Persistence/*`, `tests/ElenaSmallBusinessLedger.Tests/Infrastructure/Persistence/SqlitePersistenceTests.cs`, `.ai/project-profile.md`, `README.md`, `.ai/implementation-plans/active/2026-06-16-v1-invoice-printer.md`
  - Summary: Added EF Core SQLite persistence for company profile, students, plan periods, issued invoice snapshots, invoice lines, PDF paths, and paid/outstanding state. The invoice lesson timestamp schema was cleaned up to one ISO 8601 round-trip text value instead of storing both a readable value and duplicate UTC ticks.
  - Tests/verification run: Red check: `dotnet test tests\ElenaSmallBusinessLedger.Tests\ElenaSmallBusinessLedger.Tests.csproj --filter "FullyQualifiedName~Persistence" -nodeReuse:false` failed before persistence types existed. Additional red checks caught SQLite cleanup friction from pooled test connections, SQLite/EF translation limits for `DateTimeOffset` filtering, and the unwanted duplicate `LessonDateTimeUtcTicks` schema field. Green checks: `dotnet test tests\ElenaSmallBusinessLedger.Tests\ElenaSmallBusinessLedger.Tests.csproj --filter "FullyQualifiedName~Persistence" -nodeReuse:false`; `dotnet build ElenaSmallBusinessLedger.sln -nodeReuse:false`; `dotnet test ElenaSmallBusinessLedger.sln -nodeReuse:false`.
  - Results: Persistence tests passed: 6 passed, 0 failed. Full build passed with 0 warnings and 0 errors. Full test suite passed: 36 passed, 0 failed.
  - Deviations: No EF migration was generated yet; the schema is verified through EF model configuration and per-test SQLite databases while there is no production data. Lesson date/time values are stored as a single round-trip text value and parsed back to the original `DateTimeOffset`.
  - Review notes: Code-review pass found no required Stage 5 fixes after the timestamp cleanup. Remaining low-risk note: invoice number allocation is suitable for the current single-user desktop shape, but would need stronger serialization if multiple app instances could issue invoices concurrently.
  - Risks/open questions: Stage 6 is now blocked on the Google Calendar package/authentication/token-storage approach. PDF dependency and Google Calendar UI approach remain open for their later stages.

- 2026-06-16:
  - Stage: Stage 4 - Add Core Use Cases And Ports
  - Files changed: `src/ElenaSmallBusinessLedger.Core/Common/OperationResult.cs`, `src/ElenaSmallBusinessLedger.Core/Calendar/CalendarLesson.cs`, `src/ElenaSmallBusinessLedger.Core/Lessons/LessonState.cs`, `src/ElenaSmallBusinessLedger.Core/Time/IClock.cs`, `src/ElenaSmallBusinessLedger.Core/Ports/*`, `src/ElenaSmallBusinessLedger.Core/UseCases/*`, `src/ElenaSmallBusinessLedger.Core/Invoices/InvoiceLessonStatus.cs`, `src/ElenaSmallBusinessLedger.Core/Invoices/IssuedInvoiceSummary.cs`, `tests/ElenaSmallBusinessLedger.Tests/Core/UseCases/UseCaseTests.cs`, `.ai/implementation-plans/active/2026-06-16-v1-invoice-printer.md`
  - Summary: Added Core ports and use-case contracts for startup dashboard data, Google Calendar-backed lesson add/edit/delete, invoice candidate selection, invoice preview creation, staged invoice issue, and paid/outstanding status changes. The staged invoice issue contract creates a staging path, writes PDF, verifies staged PDF, publishes to final path, saves issued invoice metadata only after final PDF storage succeeds, and then requests print.
  - Tests/verification run: Red check: `dotnet test tests\ElenaSmallBusinessLedger.Tests\ElenaSmallBusinessLedger.Tests.csproj --filter "FullyQualifiedName~UseCases" -nodeReuse:false` failed because planned Core ports/use-case namespaces/types did not exist. Additional red check: `dotnet test tests\ElenaSmallBusinessLedger.Tests\ElenaSmallBusinessLedger.Tests.csproj --filter "FullyQualifiedName~InvoicePreviewFailsWhenCalendarLessonHasNoActivePlan" -nodeReuse:false` failed because invoice preview silently succeeded when a lesson had no active plan. Green checks: `dotnet test tests\ElenaSmallBusinessLedger.Tests\ElenaSmallBusinessLedger.Tests.csproj --filter "FullyQualifiedName~UseCases" -nodeReuse:false`; `dotnet test tests\ElenaSmallBusinessLedger.Tests\ElenaSmallBusinessLedger.Tests.csproj --filter "FullyQualifiedName~InvoicePreviewFailsWhenCalendarLessonHasNoActivePlan" -nodeReuse:false`; `dotnet build ElenaSmallBusinessLedger.sln -nodeReuse:false`; `dotnet test ElenaSmallBusinessLedger.sln -nodeReuse:false`.
  - Results: Use-case tests passed: 11 passed, 0 failed. Targeted no-active-plan regression passed: 1 passed, 0 failed. Full build passed with 0 warnings and 0 errors. Full test suite passed: 30 passed, 0 failed.
  - Deviations: The implemented staged issue order publishes the verified PDF to its final path before saving issued invoice metadata, rather than saving metadata before final publish. This keeps the existing hard requirement precise: the app must not mark an invoice issued unless final PDF storage has already succeeded.
  - Review notes: Code-review pass found and fixed one safety issue before completion: invoice preview now fails closed when a Google Calendar lesson has no active student plan instead of silently omitting that lesson from the draft.
  - Risks/open questions: Stage 5 remains blocked by the SQLite access decision. PDF dependency and Google Calendar UI approach remain open for their later stages. Later Infrastructure should add recovery/cleanup behavior for any final PDF published immediately before a database save failure.

- 2026-06-16:
  - Stage: Stage 3 - Build Core Domain Model And Pure Business Rules
  - Files changed: `src/ElenaSmallBusinessLedger.Core/Common/*`, `src/ElenaSmallBusinessLedger.Core/Students/*`, `src/ElenaSmallBusinessLedger.Core/Plans/*`, `src/ElenaSmallBusinessLedger.Core/Calendar/CalendarLessonEntry.cs`, `src/ElenaSmallBusinessLedger.Core/Money/Money.cs`, `src/ElenaSmallBusinessLedger.Core/Company/*`, `src/ElenaSmallBusinessLedger.Core/Invoices/*`, `tests/ElenaSmallBusinessLedger.Tests/Core/Students/StudentTests.cs`, `tests/ElenaSmallBusinessLedger.Tests/Core/Plans/StudentPlanPeriodTests.cs`, `tests/ElenaSmallBusinessLedger.Tests/Core/Calendar/CalendarLessonEntryTests.cs`, `tests/ElenaSmallBusinessLedger.Tests/Core/Invoices/*`, `.ai/implementation-plans/active/2026-06-16-v1-invoice-printer.md`
  - Summary: Added pure Core domain types and rules for student age validation, student plan periods, strict Google Calendar marker parsing, money/VAT calculation, invoice drafts and immutable issued invoice snapshots, price adjustment validation, and paid/outstanding toggle confirmation.
  - Tests/verification run: Red check: `dotnet test tests\ElenaSmallBusinessLedger.Tests\ElenaSmallBusinessLedger.Tests.csproj --filter "FullyQualifiedName~Core" -nodeReuse:false` failed because planned Core namespaces/types did not exist. Green checks: same Core-filtered command passed with 19 tests; `dotnet build ElenaSmallBusinessLedger.sln -nodeReuse:false`; `dotnet test ElenaSmallBusinessLedger.sln -nodeReuse:false`.
  - Results: Core-filtered tests passed: 19 passed, 0 failed. Full build passed with 0 warnings and 0 errors. Full test suite passed: 19 passed, 0 failed.
  - Deviations: Added an Infrastructure assembly marker and corresponding smoke test during Stage 2; Stage 3 kept implementation in Core only. Used internal type aliases where the `Money` namespace and `Money` type would otherwise collide.
  - Risks/open questions: Stage 4 is blocked by the open invoice issue staging semantics decision because invoice issue use-case contracts depend on it.

- 2026-06-16:
  - Stage: Stage 2 - Scaffold Solution, Projects, And Test Baseline
  - Files changed: `ElenaSmallBusinessLedger.sln`, `.editorconfig`, `README.md`, `src/ElenaSmallBusinessLedger.App/*`, `src/ElenaSmallBusinessLedger.Core/AssemblyMarker.cs`, `src/ElenaSmallBusinessLedger.Core/ElenaSmallBusinessLedger.Core.csproj`, `src/ElenaSmallBusinessLedger.Infrastructure/AssemblyMarker.cs`, `src/ElenaSmallBusinessLedger.Infrastructure/ElenaSmallBusinessLedger.Infrastructure.csproj`, `tests/ElenaSmallBusinessLedger.Tests/Core/SolutionSmokeTests.cs`, `tests/ElenaSmallBusinessLedger.Tests/ElenaSmallBusinessLedger.Tests.csproj`, `.ai/implementation-plans/active/2026-06-16-v1-invoice-printer.md`
  - Summary: Created the classic solution file, WPF App project, Core class library, Infrastructure class library, xUnit test project, project references, editorconfig, and smoke tests. Updated README with verified build/test commands.
  - Tests/verification run: `dotnet --list-sdks`; `dotnet build ElenaSmallBusinessLedger.sln -nodeReuse:false`; `dotnet test ElenaSmallBusinessLedger.sln -nodeReuse:false`
  - Results: .NET SDKs available: 6.0.321, 10.0.109, 10.0.301. Build passed with 0 warnings and 0 errors. Tests passed: 2 passed, 0 failed.
  - Deviations: `dotnet new sln` initially created `.slnx` by default under .NET 10; replaced it with classic `.sln` using `--format sln` to match the plan. The WPF template required `-f net10.0`; it generated `net10.0-windows` in the project file as desired. xUnit restore initially failed in the sandbox and was rerun with approved `dotnet restore` network access.
  - Risks/open questions: SQLite access approach, PDF dependency, invoice issue staging, and Google Calendar UI approach remain open for later stages.

- 2026-06-16:
  - Stage: Stage 1 - Align Project Profile And Tooling Baseline
  - Files changed: `.ai/project-profile.md`, `.ai/implementation-plans/active/2026-06-16-v1-invoice-printer.md`
  - Summary: Confirmed App/Core/Infrastructure solution shape, aligned backup scope to v1 limitations, restored no-CPR hard requirement, and updated Google Calendar hard requirements to the first-line marker model plus immediate failure notification.
  - Tests/verification run: `Get-Content -Raw .ai\project-profile.md`; `rg "02-business-rules|03-requirements|04-data-model|05-implementation-plan|06-open-questions|07-ui-design|08-backend-design|archived" README.md AGENTS.md .ai docs`
  - Results: Profile contents verified. Stale-reference scan found only references inside this active implementation plan.
  - Deviations: None.
  - Risks/open questions: SQLite access approach, PDF dependency, invoice issue staging, and Google Calendar UI approach remain open for later stages.

- 2026-06-16:
  - Stage: Plan creation
  - Files changed: `.ai/implementation-plans/active/2026-06-16-v1-invoice-printer.md`
  - Summary: Created active implementation plan for simplified v1 invoice printer.
  - Tests/verification run: Not run; planning-only change.
  - Results: Plan ready for review.
  - Deviations: Used project plan location instead of generic Superpowers plan location.
  - Risks/open questions: Solution shape, SQLite access approach, PDF dependency, invoice issue staging, and Google Calendar UI approach need confirmation before execution.
