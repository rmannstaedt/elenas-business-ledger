# V1 Invoice Printer Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `plan-runner` for project-stage execution. If using Superpowers execution workflows, use `superpowers:subagent-driven-development` or `superpowers:executing-plans` task-by-task. Steps use checkbox syntax for tracking.

**Goal:** Build the first usable WPF/.NET 10 local app for Elena to maintain students and plans, read/edit relevant Google Calendar lessons, write immutable Danish invoices, save invoice PDFs, print them, and mark invoices paid/outstanding.

**Architecture:** Use a small three-project shape: WPF `App`, business `Core`, and technical `Infrastructure`. Keep invoice, VAT, plan, lesson, and payment rules in Core; keep WPF, SQLite, Google Calendar, PDF generation, printing, and filesystem details outside Core.

**Tech Stack:** .NET 10, WPF/MVVM, xUnit, SQLite, Google Calendar API, QuestPDF or an approved PDF alternative.

Status: Active
Created: 2026-06-16
Last updated: 2026-06-16
Owner/context: Ruben and Codex; implementation has not started yet.

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

- The repository contains a scaffolded `.sln`, App/Core/Infrastructure projects, and an xUnit test project.
- `.ai/project-profile.md` has been aligned to the App/Core/Infrastructure solution shape.
- There is no active implementation plan other than this file.
- Build and test commands are verified for the scaffolded solution.

## Desired End State

- The repository contains a buildable .NET 10 solution with App/Core/Infrastructure projects and one xUnit test project.
- Core contains the v1 business model and use cases for students, plans, lessons, invoices, VAT calculation, invoice issue, and payment status.
- Infrastructure persists students, plans, company settings, issued invoices, invoice lessons, and paid/outstanding state in SQLite.
- Infrastructure reads and updates Google Calendar entries using the strict `$ short-name` rule.
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

- [ ] Confirm PDF dependency.
  - Recommended/default answer: Use QuestPDF if the license is acceptable for this app.
  - Alternatives: PdfSharp/MigraDoc or another approved PDF library.
  - Why it matters: Invoice PDF generation is safety-critical and production dependency choice must be deliberate.
  - Exact answer needed: "Use QuestPDF" or name the selected alternative.

- [x] Decide invoice issue staging semantics.
  - Accepted answer: Render PDF to a staging path, verify it, publish/move the PDF to the final path, persist issued invoice metadata only after final PDF storage succeeds, then request print. If any pre-persistence step fails, show failure and do not display the invoice as issued.
  - Original recommended/default answer: Render PDF to a staging path, verify it, persist issued invoice metadata, then publish/move the PDF to final path; if any step fails, show failure and do not display the invoice as issued.
  - Alternatives: Save final PDF first, then persist invoice data; or persist draft first, then save final PDF, then mark issued.
  - Why it matters: SQLite transactions cannot include filesystem writes, so false success and orphan PDFs must be handled deliberately.
  - Exact answer received: Use the staged workflow; implementation orders final PDF publish before issued metadata persistence to preserve the hard requirement that a final stored PDF exists before the invoice is marked issued.

- [ ] Decide how much Google Calendar UI is embedded.
  - Recommended/default answer: Open Google Calendar in the user's browser for v1; revisit embedded pane later only if technically easy and reliable.
  - Alternatives: Embedded WebView2 pane inside the WPF app.
  - Why it matters: Embedded browser adds dependency, auth/session, layout, and support complexity.
  - Exact answer needed: "External browser" or "Embedded WebView2".

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
Status: Not started

Goal:

Read and edit relevant Google Calendar entries using the strict first-line marker rule.

Affected files/classes/schemas/commands/UI surfaces:

- `src/ElenaSmallBusinessLedger.Infrastructure/Calendar/`
- `tests/ElenaSmallBusinessLedger.Tests/Infrastructure/Calendar/`
- WPF settings fields later for selected Google account/calendar

Concrete steps:

- [ ] Add approved Google Calendar packages after dependency review.
- [ ] Implement OAuth/token storage using Windows secure credential storage or an approved secure local mechanism.
- [ ] Implement selected-calendar configuration storage through existing settings/persistence.
- [ ] Implement calendar read for previous/current/next month window.
- [ ] Ignore every event whose first line does not match `$ <short name>` or `$<short name>`.
- [ ] Implement event update for date/time changes.
- [ ] Implement event delete for lesson cancellation.
- [ ] Implement event create for new lesson.
- [ ] Map Calendar/API failures into Core result failures that the UI can display immediately.
- [ ] Add fake gateway tests in Core and adapter tests around marker parsing/request mapping.

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
Status: Not started

Goal:

Generate Danish invoice PDFs, save them safely, and print only after a successful save.

Affected files/classes/schemas/commands/UI surfaces:

- `src/ElenaSmallBusinessLedger.Infrastructure/Invoices/Pdf/`
- `src/ElenaSmallBusinessLedger.Infrastructure/Invoices/Printing/`
- `tests/ElenaSmallBusinessLedger.Tests/Infrastructure/Invoices/`
- configured invoice folder setting

Concrete steps:

- [ ] Apply the PDF dependency decision from Open Questions.
- [ ] Add approved PDF package.
- [ ] Implement Danish invoice rendering from immutable invoice preview/snapshot data.
- [ ] Implement invoice folder validation.
- [ ] Implement stable file naming with sanitized student short/full name and invoice number.
- [ ] Implement no-silent-overwrite behavior.
- [ ] Implement staged PDF save according to the confirmed issue workflow.
- [ ] Implement printer adapter that prints only saved PDFs.
- [ ] Write tests for PDF save failure blocking invoice save.
- [ ] Write tests for file name collision handling.
- [ ] Write a PDF smoke test that verifies a file is created and non-empty.

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
Status: Not started

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

- [ ] Add dependency injection composition in App startup.
- [ ] Create main window with current student short name.
- [ ] Show lesson list for previous/current/next month.
- [ ] Show each lesson's date/time, planned/completed state, invoiced flag, and paid/outstanding when invoiced.
- [ ] Add Previous/Next student commands based on lesson date/time.
- [ ] Add Previous/Next month commands.
- [ ] Add Student menu with Add Student and Edit Student.
- [ ] Add Student Invoices pane with Paid/Outstanding flag.
- [ ] Implement Outstanding to Paid direct toggle.
- [ ] Implement Paid to Outstanding confirmation dialog.
- [ ] Add double-click invoice open command.
- [ ] Add Write Invoice button.
- [ ] Add inline error display for Google Calendar update failures.
- [ ] Add ViewModel tests for command availability and state transitions.

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
Status: Not started

Goal:

Allow Ruben/Elena to maintain students, plan history, schedule, and uninvoiced Google Calendar lessons.

Affected files/classes/schemas/commands/UI surfaces:

- `src/ElenaSmallBusinessLedger.App/Views/StudentEditorView.xaml`
- `src/ElenaSmallBusinessLedger.App/ViewModels/StudentEditorViewModel.cs`
- `src/ElenaSmallBusinessLedger.App/Views/LessonEditorView.xaml`
- `src/ElenaSmallBusinessLedger.App/ViewModels/LessonEditorViewModel.cs`
- matching ViewModel tests

Concrete steps:

- [ ] Add student create/edit dialog with short name, full name, address, phone, email, age bracket, optional/required birthdate, active flag.
- [ ] Add plan fields: name, price without VAT, lessons per week, duration, flexibility, number of students, effective date.
- [ ] Add schedule fields: weekday/time list.
- [ ] Enforce one active plan per student at any date through Core use case.
- [ ] Add direct lesson edit for uninvoiced lessons.
- [ ] Add lesson cancellation/delete for uninvoiced lessons.
- [ ] Add new lesson command for Google Calendar.
- [ ] Display immediate failure when Calendar update fails.
- [ ] Preserve user input after validation failure.

Behavior that must remain unchanged:

- Invoiced lessons cannot be edited.
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
Status: Not started

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

- [ ] Implement candidate dialog listing students with lessons today who have not had an invoice printed this month.
- [ ] Preselect the current-time student when one exists.
- [ ] Allow selecting any student in the database.
- [ ] Implement invoice preview using company info, student info, matching Google Calendar lessons, VAT when relevant, and price information.
- [ ] Allow editing invoice lesson list before issue.
- [ ] Allow price adjustment only with mandatory reason.
- [ ] Implement Cancel so no invoice/PDF/local lesson data is saved.
- [ ] Implement Print Invoice command through the Core issue use case.
- [ ] Display PDF save failure and keep invoice unissued.
- [ ] Save invoice data and local invoice lessons only after PDF save succeeds according to confirmed workflow.
- [ ] Request print after successful save.
- [ ] Close form and refresh main view after successful save.
- [ ] Open saved invoice in invoice viewer on double-click.
- [ ] Prevent editing saved invoice data.

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
Status: Not started

Goal:

Make the first usable app coherent, verifiable, and ready for Ruben/Elena pilot testing.

Affected files/classes/schemas/commands/UI surfaces:

- `README.md`
- `.ai/project-profile.md`
- `.ai/context/HANDOFF.md`
- source and tests changed in prior stages

Concrete steps:

- [ ] Run full build.
- [ ] Run full test suite.
- [ ] Run formatter check if available and not noisy.
- [ ] Review package dependencies and licenses.
- [ ] Review logs/errors for secrets and sensitive data.
- [ ] Run code review skill for Core/Infrastructure safety.
- [ ] Run UI review skill for WPF flows.
- [ ] Update README with verified commands and app setup notes.
- [ ] Update project profile if commands, solution shape, or dependency decisions changed.
- [ ] Create or update `.ai/context/HANDOFF.md`.
- [ ] Record remaining limitations and manual test notes.

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
