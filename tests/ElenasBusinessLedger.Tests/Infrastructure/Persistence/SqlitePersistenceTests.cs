using ElenasBusinessLedger.Core.Common;
using ElenasBusinessLedger.Core.Company;
using ElenasBusinessLedger.Core.Invoices;
using ElenasBusinessLedger.Core.Lessons;
using ElenasBusinessLedger.Core.Money;
using ElenasBusinessLedger.Core.Plans;
using ElenasBusinessLedger.Core.Ports;
using ElenasBusinessLedger.Core.Students;
using ElenasBusinessLedger.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data.Common;

namespace ElenasBusinessLedger.Tests.Infrastructure.Persistence;

public sealed class SqlitePersistenceTests
{
    [Fact]
    public async Task CompanyProfileCanBeSavedAndLoaded()
    {
        using var database = await TestDatabase.CreateAsync();
        var repository = new SqliteCompanyProfileRepository(database.CreateContext);
        var profile = CompanyProfile.Create(
            "Maple Key Studio",
            "Musikvej 1",
            "12345678",
            "elena@example.test",
            "12345678",
            new DateOnly(2026, 7, 1),
            VatStatus.Registered,
            "C:\\Invoices",
            configuredInvoicePrinterName: "Office Printer",
            bankRegistrationNumber: "1234",
            bankAccountNumber: "5678901234",
            mobilePayAccount: "MobilePay 5678");

        await repository.SaveAsync(profile, CancellationToken.None);

        var loaded = await repository.GetAsync(CancellationToken.None);
        Assert.Equal(profile, loaded);
        Assert.Equal("Office Printer", loaded.ConfiguredInvoicePrinterName);
        Assert.Equal("1234", loaded.BankRegistrationNumber);
        Assert.Equal("5678901234", loaded.BankAccountNumber);
        Assert.Equal("MobilePay 5678", loaded.MobilePayAccount);
    }

    [Fact]
    public async Task CalendarSettingsCanBeSavedAndLoaded()
    {
        using var database = await TestDatabase.CreateAsync();
        var repository = new SqliteCalendarSettingsRepository(database.CreateContext);
        var settings = new CalendarSettings("primary", "elena@example.test");

        await repository.SaveAsync(settings, CancellationToken.None);

        var loaded = await repository.GetAsync(CancellationToken.None);
        Assert.Equal(settings, loaded);
    }

    [Fact]
    public async Task EconomySettingsAndManualMonthOverridesCanBeSavedLoadedUpdatedAndCleared()
    {
        using var database = await TestDatabase.CreateAsync();
        var repository = new SqliteEconomyRepository(database.CreateContext);

        Assert.Null(await repository.GetSettingsAsync(CancellationToken.None));

        await repository.SaveSettingsAsync(
            new EconomySettings(new DateOnly(2026, 7, 15)),
            CancellationToken.None);
        await repository.SaveManualMonthOverridesAsync(
            [
                new EconomyManualMonthOverride(2025, 12, Money.Dkk(1200.125m), Money.Dkk(300.125m)),
                new EconomyManualMonthOverride(2026, 5, Money.Dkk(1000m), Money.Dkk(250m)),
                new EconomyManualMonthOverride(2026, 7, Money.Dkk(900m), Money.Dkk(225m))
            ],
            CancellationToken.None);

        var initialOverrides = await repository.ListManualMonthOverridesAsync(2025, 2026, CancellationToken.None);
        Assert.Equal(2, initialOverrides.Count);
        Assert.DoesNotContain(initialOverrides, manual => manual.Year == 2026 && manual.Month == 7);

        await repository.SaveManualMonthOverridesAsync(
            [
                new EconomyManualMonthOverride(2026, 5, Money.Dkk(1100m), Money.Dkk(275m))
            ],
            CancellationToken.None);
        await repository.SaveManualMonthOverridesAsync(
            [
                new EconomyManualMonthOverride(2025, 12, Money.Dkk(0m), Money.Dkk(0m))
            ],
            CancellationToken.None);

        var settings = await repository.GetSettingsAsync(CancellationToken.None);
        var loaded = await repository.ListManualMonthOverridesAsync(2025, 2026, CancellationToken.None);
        await using var context = database.CreateContext();
        var storedRows = await context.EconomyManualMonthOverrides.AsNoTracking().ToArrayAsync(TestContext.Current.CancellationToken);

        Assert.Equal(new DateOnly(2026, 7, 1), settings?.FirstApplicationInvoiceMonth);
        var overrideRow = Assert.Single(loaded);
        Assert.Equal(2026, overrideRow.Year);
        Assert.Equal(5, overrideRow.Month);
        Assert.Equal(Money.Dkk(1100m), overrideRow.Net);
        Assert.Equal(Money.Dkk(275m), overrideRow.Vat);
        Assert.Single(storedRows);

        await repository.SaveSettingsAsync(
            new EconomySettings(new DateOnly(2026, 5, 1)),
            CancellationToken.None);

        var pruned = await repository.ListManualMonthOverridesAsync(2025, 2026, CancellationToken.None);
        Assert.Empty(pruned);
    }

    [Fact]
    public async Task ManualMonthOverridesRequireSavedCutOver()
    {
        using var database = await TestDatabase.CreateAsync();
        var repository = new SqliteEconomyRepository(database.CreateContext);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repository.SaveManualMonthOverridesAsync(
                [
                    new EconomyManualMonthOverride(2026, 5, Money.Dkk(1000m), Money.Dkk(250m))
                ],
                CancellationToken.None));
        var loaded = await repository.ListManualMonthOverridesAsync(2026, 2026, CancellationToken.None);

        Assert.Contains("cut-over", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(loaded);
    }

    [Fact]
    public async Task StudentWithPlanHistoryCanBeSavedAndLoaded()
    {
        using var database = await TestDatabase.CreateAsync();
        var repository = new SqliteStudentRepository(database.CreateContext);
        var plans = new SqlitePlanCatalogRepository(database.CreateContext);
        var intro = await SaveCatalogPlanAsync(plans, "Intro");
        var standard = await SaveCatalogPlanAsync(plans, "Standard");
        var student = CreateStudentRecord("Anna", introPlanId: intro.Id, currentPlanId: standard.Id);

        await repository.SaveAsync(student, CancellationToken.None);

        var loaded = await repository.GetByShortNameAsync("Anna", CancellationToken.None);
        Assert.NotNull(loaded);
        Assert.Equal("Anna", loaded.Student.ShortName);
        Assert.Equal(2, loaded.PlanPeriods.Count);
        Assert.Equal(intro.Id, loaded.PlanPeriods[0].Plan.PlanCatalogId);
        Assert.Equal("Intro", loaded.PlanPeriods[0].Plan.Name);
        Assert.Equal(new DateOnly(2026, 5, 31), loaded.PlanPeriods[0].EndsOn);
        Assert.Equal(standard.Id, loaded.PlanPeriods[1].Plan.PlanCatalogId);
        Assert.Equal("Standard", loaded.PlanPeriods[1].Plan.Name);
        Assert.Null(loaded.PlanPeriods[1].EndsOn);
        Assert.Single(loaded.Schedule);
        Assert.Equal(DayOfWeek.Tuesday, loaded.Schedule[0].Weekday);
        Assert.Equal(new TimeOnly(15, 0), loaded.Schedule[0].StartsAt);
    }

    [Fact]
    public async Task SavingLoadedStudentWithChangedShortNameRenamesExistingStudent()
    {
        using var database = await TestDatabase.CreateAsync();
        var repository = new SqliteStudentRepository(database.CreateContext);
        var plans = new SqlitePlanCatalogRepository(database.CreateContext);
        var intro = await SaveCatalogPlanAsync(plans, "Intro");
        var standard = await SaveCatalogPlanAsync(plans, "Standard");
        await repository.SaveAsync(CreateStudentRecord("Anna", introPlanId: intro.Id, currentPlanId: standard.Id), CancellationToken.None);
        var loaded = await repository.GetByShortNameAsync("Anna", CancellationToken.None);
        Assert.NotNull(loaded);

        var renamedStudent = Student.Create(
            "Anja",
            loaded.Student.FullName,
            loaded.Student.Address,
            loaded.Student.Phone,
            loaded.Student.Email,
            loaded.Student.AgeBracket,
            loaded.Student.Birthdate,
            loaded.Student.IsActive).Value!;
        var renamed = loaded with { Student = renamedStudent };

        var result = await repository.SaveAsync(renamed, CancellationToken.None);

        Assert.True(result.Succeeded);
        var students = await repository.ListStudentsAsync(CancellationToken.None);
        var student = Assert.Single(students);
        Assert.Equal("Anja", student.Student.ShortName);
        Assert.Equal(loaded.Id, student.Id);
        Assert.Null(await repository.GetByShortNameAsync("Anna", CancellationToken.None));
    }

    [Fact]
    public async Task StudentsCanBeListedIncludingInactiveStudents()
    {
        using var database = await TestDatabase.CreateAsync();
        var repository = new SqliteStudentRepository(database.CreateContext);
        var plans = new SqlitePlanCatalogRepository(database.CreateContext);
        var intro = await SaveCatalogPlanAsync(plans, "Intro");
        var standard = await SaveCatalogPlanAsync(plans, "Standard");
        await repository.SaveAsync(CreateStudentRecord("Anna", introPlanId: intro.Id, currentPlanId: standard.Id), CancellationToken.None);
        await repository.SaveAsync(CreateStudentRecord("Bo", introPlanId: intro.Id, currentPlanId: standard.Id, isActive: false), CancellationToken.None);

        var students = await repository.ListStudentsAsync(CancellationToken.None);

        Assert.Equal(["Anna", "Bo"], students.Select(record => record.Student.ShortName).ToArray());
        Assert.True(students.Single(record => record.Student.ShortName == "Anna").Student.IsActive);
        Assert.False(students.Single(record => record.Student.ShortName == "Bo").Student.IsActive);
    }

    [Fact]
    public async Task PlanCatalogLifecycleCanBeSavedAndListed()
    {
        using var database = await TestDatabase.CreateAsync();
        var repository = new SqlitePlanCatalogRepository(database.CreateContext);

        var draft = (await repository.SaveAsync(CreatePlanCatalogItem("Draft", PlanCatalogState.Draft), CancellationToken.None)).Value!;
        var active = (await repository.SaveAsync(CreatePlanCatalogItem("Active", PlanCatalogState.Active), CancellationToken.None)).Value!;
        var retired = (await repository.SaveAsync(CreatePlanCatalogItem("Retired", PlanCatalogState.Retired) with
        {
            RetireOn = new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.Zero)
        }, CancellationToken.None)).Value!;

        var allPlans = await repository.ListAllAsync(CancellationToken.None);
        var assignablePlans = await repository.ListAssignableAsync(CancellationToken.None);
        await repository.DeleteAsync(draft.Id, CancellationToken.None);
        var afterDraftDelete = await repository.ListAllAsync(CancellationToken.None);

        Assert.Equal(
            [PlanCatalogState.Active, PlanCatalogState.Draft, PlanCatalogState.Retired],
            allPlans.Select(plan => plan.State).OrderBy(state => state.ToString(), StringComparer.Ordinal).ToArray());
        Assert.Equal("Paid monthly in advance", allPlans.Single(plan => plan.Id == active.Id).PaymentTerms);
        Assert.Equal(new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.Zero), allPlans.Single(plan => plan.Id == retired.Id).RetireOn);
        var assignablePlan = Assert.Single(assignablePlans);
        Assert.Equal(active.Id, assignablePlan.Id);
        Assert.DoesNotContain(afterDraftDelete, plan => plan.Id == draft.Id);
        Assert.Contains(afterDraftDelete, plan => plan.Id == active.Id);
        Assert.Contains(afterDraftDelete, plan => plan.Id == retired.Id);
    }

    [Fact]
    public async Task IncompleteDraftPlanCanBeSavedAndLoaded()
    {
        using var database = await TestDatabase.CreateAsync();
        var repository = new SqlitePlanCatalogRepository(database.CreateContext);

        var draft = (await repository.SaveAsync(CreatePlanCatalogItem("", PlanCatalogState.Draft) with
        {
            PriceWithoutVat = null
        }, CancellationToken.None)).Value!;

        var loaded = await repository.GetAsync(draft.Id, CancellationToken.None);

        Assert.NotNull(loaded);
        Assert.Equal(string.Empty, loaded.Name);
        Assert.Null(loaded.PriceWithoutVat);
        Assert.Equal(PlanCatalogState.Draft, loaded.State);
    }

    [Fact]
    public async Task PlanCatalogRenameChangesStudentPlanNameButNotSnapshotTermsOrInvoiceNames()
    {
        using var database = await TestDatabase.CreateAsync();
        var students = new SqliteStudentRepository(database.CreateContext);
        var plans = new SqlitePlanCatalogRepository(database.CreateContext);
        var catalogPlan = (await plans.SaveAsync(CreatePlanCatalogItem("Standard", PlanCatalogState.Active), CancellationToken.None)).Value!;
        var intro = await SaveCatalogPlanAsync(plans, "Intro");
        await students.SaveAsync(CreateStudentRecord("Anna", introPlanId: intro.Id, currentPlanId: catalogPlan.Id, currentPlanName: "Standard"), CancellationToken.None);
        var lesson = await SaveLessonAsync(database, "Anna", catalogPlan, new DateTimeOffset(2026, 6, 16, 15, 0, 0, TimeSpan.Zero));
        var invoices = new SqliteInvoiceRepository(database.CreateContext);
        await SaveIssuedInvoiceViaBusinessFlowAsync(invoices, CreateIssuedInvoice(
            "Anna",
            fullName: "Anna Andersen",
            planName: "Standard",
            planCatalogId: catalogPlan.Id,
            lessonId: lesson.Id), CancellationToken.None);

        var renamed = await plans.RenameAsync(catalogPlan.Id, "Standard 2026", new DateTimeOffset(2026, 6, 20, 12, 0, 0, TimeSpan.Zero), CancellationToken.None);

        Assert.True(renamed.Succeeded);
        var student = await students.GetByShortNameAsync("Anna", CancellationToken.None);
        Assert.NotNull(student);
        var currentPlan = student.PlanPeriods.Single(period => period.EndsOn is null).Plan;
        Assert.Equal(catalogPlan.Id, currentPlan.PlanCatalogId);
        Assert.Equal("Standard 2026", currentPlan.Name);
        Assert.Equal(400m, currentPlan.PriceWithoutVat);
        Assert.Equal(LessonsPerWeek.One, currentPlan.LessonsPerWeek);
        Assert.Equal("Paid monthly in advance", currentPlan.PaymentTerms);
        var invoice = await invoices.GetIssuedInvoiceAsync(new InvoiceNumber(1), CancellationToken.None);
        Assert.NotNull(invoice);
        Assert.Equal(catalogPlan.Id, invoice.Lines[0].PlanCatalogId);
        Assert.Equal("Standard", invoice.Lines[0].PlanName);

        await using var context = database.CreateContext();
        var change = Assert.Single(context.PlanNameChanges.AsNoTracking());
        Assert.Equal(catalogPlan.Id, change.PlanCatalogId);
        Assert.Equal("Standard", change.OldName);
        Assert.Equal("Standard 2026", change.NewName);
        Assert.Equal(new DateTimeOffset(2026, 6, 20, 12, 0, 0, TimeSpan.Zero), change.ChangedAt);
    }

    [Fact]
    public async Task PlanCatalogRenameRejectsCaseInsensitiveDuplicateName()
    {
        using var database = await TestDatabase.CreateAsync();
        var plans = new SqlitePlanCatalogRepository(database.CreateContext);
        await SaveCatalogPlanAsync(plans, "Standard");
        var premium = await SaveCatalogPlanAsync(plans, "Premium");

        var result = await plans.RenameAsync(
            premium.Id,
            "standard",
            new DateTimeOffset(2026, 6, 20, 12, 0, 0, TimeSpan.Zero),
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, error => error.Code == "Plan.DuplicateName");
        var allPlans = await plans.ListAllAsync(CancellationToken.None);
        Assert.Contains(allPlans, plan => plan.Id == premium.Id && plan.Name == "Premium");
    }

    [Fact]
    public async Task MigrationsCreateCurrentSchemaForFreshDatabase()
    {
        using var database = await TestDatabase.CreateAsync();
        await using var context = database.CreateContext();

        var tableNames = await context.Database
            .SqlQueryRaw<string>("SELECT name AS Value FROM sqlite_master WHERE type = 'table'")
            .ToListAsync(TestContext.Current.CancellationToken);

        var repository = new SqlitePlanCatalogRepository(database.CreateContext);
        var result = await repository.SaveAsync(CreatePlanCatalogItem("Standard", PlanCatalogState.Draft), CancellationToken.None);
        var plans = await repository.ListAllAsync(CancellationToken.None);

        Assert.Contains("__EFMigrationsHistory", tableNames);
        Assert.Contains("PlanCatalog", tableNames);
        Assert.Contains("StudentPlanPeriods", tableNames);
        Assert.Contains("IssuedInvoiceLines", tableNames);
        Assert.Contains("EconomySettings", tableNames);
        Assert.Contains("EconomyManualMonthOverrides", tableNames);
        Assert.True(result.Succeeded);
        Assert.Single(plans);
    }

    [Fact]
    public async Task BaselineDatabaseMigratesToCurrentSchema()
    {
        using var database = await TestDatabase.CreateBaselineMigratedAsync();
        var repository = new SqlitePlanCatalogRepository(database.CreateContext);
        var saved = await repository.SaveAsync(CreatePlanCatalogItem("Existing", PlanCatalogState.Active), CancellationToken.None);
        Assert.True(saved.Succeeded);

        var warnings = await database.MigrateAsync();

        var plans = await repository.ListAllAsync(CancellationToken.None);
        await using var context = database.CreateContext();
        var appliedMigrations = await context.Database.GetAppliedMigrationsAsync(TestContext.Current.CancellationToken);

        Assert.Empty(warnings);
        var plan = Assert.Single(plans);
        Assert.Equal("Existing", plan.Name);
        Assert.Equal(PlanCatalogState.Active, plan.State);
        Assert.Equal("Paid monthly in advance", plan.PaymentTerms);
        Assert.Contains(appliedMigrations, migration => migration.EndsWith("_Baseline", StringComparison.Ordinal));
        Assert.Contains(appliedMigrations, migration => migration.EndsWith("_AddInvoiceIssuePrintState", StringComparison.Ordinal));
    }

    [Fact]
    public async Task PreviouslyAppliedPaymentAccountsMigrationSplitsBankAccountFields()
    {
        using var database = await TestDatabase.CreateMigratedAsync("20260625194841_AddInvoicePeriodAndPaymentAccounts");
        await using (var context = database.CreateContext())
        {
            await context.Database.ExecuteSqlRawAsync(
                """
                INSERT INTO CompanyProfiles (
                    Id,
                    Name,
                    Address,
                    Phone,
                    Email,
                    Cvr,
                    VatRegistrationDate,
                    VatStatus,
                    InvoiceFolderPath,
                    ConfiguredInvoicePrinterName,
                    PaymentInstructions,
                    BankAccount,
                    MobilePayAccount)
                VALUES (
                    1,
                    'Maple Key Studio',
                    'Musikvej 1',
                    '12345678',
                    'elena@example.test',
                    NULL,
                    NULL,
                    'NotRegistered',
                    'C:\Invoices',
                    '',
                    'Betales ved modtagelse',
                    '5678901234',
                    'MobilePay 5678');
                """,
                TestContext.Current.CancellationToken);
        }

        await database.MigrateAsync();

        var repository = new SqliteCompanyProfileRepository(database.CreateContext);
        var loaded = await repository.GetAsync(CancellationToken.None);

        Assert.Equal(string.Empty, loaded.BankRegistrationNumber);
        Assert.Equal("5678901234", loaded.BankAccountNumber);
        Assert.Equal("MobilePay 5678", loaded.MobilePayAccount);
    }

    [Fact]
    public async Task ExistingInvoiceRowsMigrateWithWarningAndRemainReadable()
    {
        using var database = await TestDatabase.CreateBaselineMigratedAsync();
        await using (var context = database.CreateContext())
        {
            await context.Database.ExecuteSqlRawAsync(
                """
                INSERT INTO IssuedInvoices (
                    InvoiceNumber,
                    InvoiceDate,
                    CompanyName,
                    CompanyAddress,
                    CompanyCvr,
                    StudentShortName,
                    StudentFullName,
                    StudentAddress,
                    TotalWithoutVatAmount,
                    VatAmount,
                    TotalAmount,
                    Currency,
                    PdfFilePath,
                    PaymentStatus)
                VALUES (
                    1,
                    '2026-06-16',
                    'Maple Key Studio',
                    'Musikvej 1',
                    NULL,
                    'Anna',
                    'Anna Andersen',
                    'Testvej 1',
                    '400.0',
                    '0.0',
                    '400.0',
                    'DKK',
                    'C:\Invoices\000001.pdf',
                    'Outstanding');
                """,
                TestContext.Current.CancellationToken);
            await context.Database.ExecuteSqlRawAsync(
                """
                INSERT INTO IssuedInvoiceLines (
                    IssuedInvoiceEntityId,
                    LessonDateTime,
                    PlanName,
                    PlanCatalogId,
                    PriceWithoutVatAmount,
                    Currency)
                VALUES (
                    1,
                    '2026-06-16T15:00:00.0000000+00:00',
                    'Standard',
                    7,
                    '400.0',
                    'DKK');
                """,
                TestContext.Current.CancellationToken);
        }

        var warnings = await database.MigrateAsync();

        var repository = new SqliteInvoiceRepository(database.CreateContext);
        var loaded = await repository.GetIssuedInvoiceAsync(new InvoiceNumber(1), CancellationToken.None);

        var warning = Assert.Single(warnings);
        Assert.Equal("Invoice history migration warning", warning.Title);
        Assert.Contains("skipped 1 old invoice line", warning.Message, StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(loaded);
        Assert.Equal(InvoiceIssueStatus.Issued, loaded.IssueStatus);
        Assert.Equal(InvoicePrintStatus.NotPrinted, loaded.PrintStatus);
        Assert.Null(loaded.PrintedAt);
        Assert.Null(loaded.LastPrintError);
        Assert.Empty(loaded.Lines);
        Assert.Empty(await database.MigrateAsync());
    }

    [Fact]
    public async Task IssuedInvoiceWithLinesAndPdfPathCanBeSavedAndLoaded()
    {
        using var database = await TestDatabase.CreateAsync();
        var repository = new SqliteInvoiceRepository(database.CreateContext);
        var plan = await SaveCatalogPlanAsync(new SqlitePlanCatalogRepository(database.CreateContext), "Standard");
        var lessonDateTime = new DateTimeOffset(2026, 6, 16, 15, 0, 0, TimeSpan.FromHours(2));
        var lesson = await SaveLessonAsync(database, "Anna", plan, lessonDateTime);
        var invoice = CreateIssuedInvoice("Anna", fullName: "Anna Andersen", planName: "Standard", lessonDateTime, plan.Id, lessonId: lesson.Id);

        await SaveIssuedInvoiceViaBusinessFlowAsync(repository, invoice, CancellationToken.None);

        var loaded = await repository.GetIssuedInvoiceAsync(new InvoiceNumber(1), CancellationToken.None);
        Assert.NotNull(loaded);
        Assert.Equal("Anna", loaded.Student.ShortName);
        Assert.Equal("C:\\Invoices\\000001.pdf", loaded.PdfFilePath);
        Assert.Single(loaded.Lines);
        Assert.Equal(lessonDateTime, loaded.Lines[0].LessonDateTime);
        Assert.Equal(plan.Id, loaded.Lines[0].PlanCatalogId);
        Assert.Equal("Standard", loaded.Lines[0].PlanName);
        Assert.Equal(lesson.Id, loaded.Lines[0].LessonId);
        Assert.Equal(Money.Dkk(400m), loaded.Totals.Total);
    }

    [Fact]
    public async Task InvoiceSummaryUsesStoredInvoicePeriodWhenLessonsSpillOutsideMonth()
    {
        using var database = await TestDatabase.CreateAsync();
        var repository = new SqliteInvoiceRepository(database.CreateContext);
        var plan = await SaveCatalogPlanAsync(new SqlitePlanCatalogRepository(database.CreateContext), "Standard");
        var lessonDateTime = new DateTimeOffset(2026, 7, 30, 15, 0, 0, TimeSpan.FromHours(2));
        var lesson = await SaveLessonAsync(database, "Anna", plan, lessonDateTime);
        var invoice = CreateIssuedInvoice(
            "Anna",
            fullName: "Anna Andersen",
            planName: "Standard",
            lessonDateTime: lessonDateTime,
            planCatalogId: plan.Id,
            period: new InvoicePeriod(new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31)),
            lessonId: lesson.Id);

        await SaveIssuedInvoiceViaBusinessFlowAsync(repository, invoice, CancellationToken.None);

        var summary = Assert.Single(await repository.ListInvoicesForStudentAsync("Anna", CancellationToken.None));
        Assert.Equal(new InvoicePeriod(new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31)), summary.Period);
    }

    [Fact]
    public async Task InvoiceSummaryMatchesStudentShortNameCaseInsensitively()
    {
        using var database = await TestDatabase.CreateAsync();
        var repository = new SqliteInvoiceRepository(database.CreateContext);
        var plan = await SaveCatalogPlanAsync(new SqlitePlanCatalogRepository(database.CreateContext), "Standard");
        var lesson = await SaveLessonAsync(database, "Anna", plan, new DateTimeOffset(2026, 7, 30, 15, 0, 0, TimeSpan.FromHours(2)));
        var invoice = CreateIssuedInvoice("Anna", "Anna Andersen", "Standard", lessonId: lesson.Id);

        await SaveIssuedInvoiceViaBusinessFlowAsync(repository, invoice, CancellationToken.None);

        var summary = Assert.Single(await repository.ListInvoicesForStudentAsync("anna", CancellationToken.None));
        Assert.Equal("Anna", summary.StudentShortName);
    }

    [Fact]
    public async Task EconomyOverviewRowsUsePeriodYearAndExcludeIssuingReservations()
    {
        using var database = await TestDatabase.CreateAsync();
        var repository = new SqliteInvoiceRepository(database.CreateContext);
        var plan = await SaveCatalogPlanAsync(new SqlitePlanCatalogRepository(database.CreateContext), "Standard");
        var includedLesson = await SaveLessonAsync(database, "Anna", plan, new DateTimeOffset(2026, 5, 4, 14, 0, 0, TimeSpan.Zero));
        var previousYearLesson = await SaveLessonAsync(database, "Anna", plan, new DateTimeOffset(2025, 12, 15, 14, 0, 0, TimeSpan.Zero));
        var issuingLesson = await SaveLessonAsync(database, "Anna", plan, new DateTimeOffset(2026, 6, 1, 14, 0, 0, TimeSpan.Zero));

        await SaveIssuedInvoiceViaBusinessFlowAsync(
            repository,
            CreateIssuedInvoice(
                "Anna",
                "Anna Andersen",
                "Standard",
                includedLesson.StartsAt,
                plan.Id,
                new InvoicePeriod(new DateOnly(2026, 5, 1), new DateOnly(2026, 5, 31)),
                includedLesson.Id,
                invoiceDate: new DateOnly(2027, 1, 5),
                totals: new InvoiceTotals(Money.Dkk(800m), Money.Dkk(200m), Money.Dkk(1000m)),
                pdfFilePath: @"C:\Invoices\Invoice 001 Maj.pdf"),
            CancellationToken.None);
        await SaveIssuedInvoiceViaBusinessFlowAsync(
            repository,
            CreateIssuedInvoice(
                "Anna",
                "Anna Andersen",
                "Standard",
                previousYearLesson.StartsAt,
                plan.Id,
                new InvoicePeriod(new DateOnly(2025, 12, 1), new DateOnly(2025, 12, 31)),
                previousYearLesson.Id,
                invoiceDate: new DateOnly(2026, 1, 5),
                totals: new InvoiceTotals(Money.Dkk(400m), Money.Dkk(100m), Money.Dkk(500m)),
                pdfFilePath: @"C:\Invoices\Invoice 002 December.pdf"),
            CancellationToken.None);

        var issuingDraft = CreateInvoiceDraft(
            "Anna",
            "Anna Andersen",
            "Standard",
            issuingLesson.StartsAt,
            plan.Id,
            new InvoicePeriod(new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 30)),
            issuingLesson.Id,
            invoiceDate: new DateOnly(2026, 6, 30));
        var reserved = await repository.ReserveInvoiceAsync(
            issuingDraft,
            new InvoiceTotals(Money.Dkk(400m), Money.Dkk(100m), Money.Dkk(500m)),
            CancellationToken.None);
        Assert.True(reserved.Succeeded);

        var rows = await repository.ListIssuedInvoiceOverviewRowsAsync(2026, CancellationToken.None);

        var row = Assert.Single(rows);
        Assert.Equal(new InvoiceNumber(1), row.InvoiceNumber);
        Assert.Equal(new DateOnly(2027, 1, 5), row.InvoiceDate);
        Assert.Equal(new InvoicePeriod(new DateOnly(2026, 5, 1), new DateOnly(2026, 5, 31)), row.Period);
        Assert.Equal(@"C:\Invoices\Invoice 001 Maj.pdf", row.PdfFilePath);
        Assert.Equal(Money.Dkk(800m), row.Totals.TotalWithoutVat);
        Assert.Equal(Money.Dkk(200m), row.Totals.VatAmount);
        Assert.Equal(Money.Dkk(1000m), row.Totals.Total);
    }

    [Fact]
    public async Task HasInvoiceForStudentInMonthUsesStoredInvoicePeriod()
    {
        using var database = await TestDatabase.CreateAsync();
        var repository = new SqliteInvoiceRepository(database.CreateContext);
        var plan = await SaveCatalogPlanAsync(new SqlitePlanCatalogRepository(database.CreateContext), "Standard");
        var lessonDateTime = new DateTimeOffset(2026, 7, 30, 15, 0, 0, TimeSpan.FromHours(2));
        var lesson = await SaveLessonAsync(database, "Anna", plan, lessonDateTime);
        var invoice = CreateIssuedInvoice(
            "Anna",
            fullName: "Anna Andersen",
            planName: "Standard",
            lessonDateTime: lessonDateTime,
            planCatalogId: plan.Id,
            period: new InvoicePeriod(new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31)),
            lessonId: lesson.Id);

        await SaveIssuedInvoiceViaBusinessFlowAsync(repository, invoice, CancellationToken.None);

        Assert.False(await repository.HasInvoiceForStudentInMonthAsync("Anna", new DateOnly(2026, 7, 1), CancellationToken.None));
        Assert.True(await repository.HasInvoiceForStudentInMonthAsync("Anna", new DateOnly(2026, 8, 1), CancellationToken.None));
    }

    [Fact]
    public async Task HasInvoiceForStudentInMonthMatchesStudentShortNameCaseInsensitively()
    {
        using var database = await TestDatabase.CreateAsync();
        var repository = new SqliteInvoiceRepository(database.CreateContext);
        var plan = await SaveCatalogPlanAsync(new SqlitePlanCatalogRepository(database.CreateContext), "Standard");
        var lesson = await SaveLessonAsync(database, "Anna", plan, new DateTimeOffset(2026, 8, 6, 15, 0, 0, TimeSpan.FromHours(2)));
        var invoice = CreateIssuedInvoice(
            "Anna",
            fullName: "Anna Andersen",
            planName: "Standard",
            planCatalogId: plan.Id,
            period: new InvoicePeriod(new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31)),
            lessonId: lesson.Id);

        await SaveIssuedInvoiceViaBusinessFlowAsync(repository, invoice, CancellationToken.None);

        Assert.True(await repository.HasInvoiceForStudentInMonthAsync("anna", new DateOnly(2026, 8, 1), CancellationToken.None));
    }

    [Fact]
    public async Task InvoiceReservationFinalizationAndPrintStatusRoundTripThroughSQLite()
    {
        using var database = await TestDatabase.CreateAsync();
        var repository = new SqliteInvoiceRepository(database.CreateContext);
        var plan = await SaveCatalogPlanAsync(new SqlitePlanCatalogRepository(database.CreateContext), "Standard");
        var lessonDateTime = new DateTimeOffset(2026, 6, 16, 15, 0, 0, TimeSpan.Zero);
        var lesson = await SaveLessonAsync(database, "Anna", plan, lessonDateTime);
        var draft = CreateInvoiceDraft("Anna", "Anna Andersen", "Standard", lessonDateTime, plan.Id, lessonId: lesson.Id);
        var totals = new InvoiceTotals(Money.Dkk(400m), Money.Dkk(0m), Money.Dkk(400m));

        var reserved = await repository.ReserveInvoiceAsync(draft, totals, CancellationToken.None);

        Assert.True(reserved.Succeeded);
        Assert.Equal(new InvoiceNumber(1), reserved.Value!.InvoiceNumber);
        Assert.Equal(InvoiceIssueStatus.Issuing, reserved.Value.IssueStatus);
        Assert.Equal(InvoicePrintStatus.NotPrinted, reserved.Value.PrintStatus);
        Assert.Null(reserved.Value.PdfFilePath);
        Assert.Null(await repository.GetIssuedInvoiceAsync(new InvoiceNumber(1), CancellationToken.None));
        Assert.Empty(await repository.ListInvoicesForStudentAsync("Anna", CancellationToken.None));

        var issuing = await repository.GetIssuingInvoiceAsync(new InvoiceNumber(1), CancellationToken.None);
        Assert.NotNull(issuing);
        Assert.Equal(InvoiceIssueStatus.Issuing, issuing.IssueStatus);

        var finalized = await repository.FinalizeIssuedInvoiceWithLessonLocksAsync(
            new InvoiceNumber(1),
            "C:\\Invoices\\000001.pdf",
            [lesson.Id],
            CancellationToken.None);

        Assert.True(finalized.Succeeded);
        Assert.Equal(InvoiceIssueStatus.Issued, finalized.Value!.IssueStatus);
        Assert.Equal("C:\\Invoices\\000001.pdf", finalized.Value.PdfFilePath);
        Assert.Null(await repository.GetIssuingInvoiceAsync(new InvoiceNumber(1), CancellationToken.None));

        var printUpdated = await repository.UpdatePrintStatusAsync(
            new InvoiceNumber(1),
            InvoicePrintStatus.PrintFailed,
            printedAt: null,
            lastPrintError: "Printer unavailable.",
            CancellationToken.None);

        Assert.True(printUpdated.Succeeded);
        Assert.Equal(InvoicePrintStatus.PrintFailed, printUpdated.Value!.PrintStatus);
        Assert.Equal("Printer unavailable.", printUpdated.Value.LastPrintError);

        var loaded = await repository.GetIssuedInvoiceAsync(new InvoiceNumber(1), CancellationToken.None);
        var summary = Assert.Single(await repository.ListInvoicesForStudentAsync("Anna", CancellationToken.None));
        Assert.NotNull(loaded);
        Assert.Equal(InvoiceIssueStatus.Issued, loaded.IssueStatus);
        Assert.Equal(InvoicePrintStatus.PrintFailed, loaded.PrintStatus);
        Assert.Equal("Printer unavailable.", loaded.LastPrintError);
        Assert.Equal(new InvoicePeriod(new DateOnly(2026, 6, 16), new DateOnly(2026, 6, 16)), summary.Period);
        Assert.Equal(InvoiceIssueStatus.Issued, summary.IssueStatus);
        Assert.Equal(InvoicePrintStatus.PrintFailed, summary.PrintStatus);
        Assert.Equal("Printer unavailable.", summary.LastPrintError);
    }

    [Fact]
    public async Task InvoiceFinalizationWithLessonLocksIssuesInvoiceAndLocksLessonsTogether()
    {
        using var database = await TestDatabase.CreateAsync();
        var students = new SqliteStudentRepository(database.CreateContext);
        var plans = new SqlitePlanCatalogRepository(database.CreateContext);
        var lessons = new SqliteLessonRepository(database.CreateContext);
        var invoices = new SqliteInvoiceRepository(database.CreateContext);
        var plan = await SaveCatalogPlanAsync(plans, "Standard");
        var student = await students.SaveAsync(
            CreateStudentRecord("Anna", currentPlanId: plan.Id),
            CancellationToken.None);
        Assert.True(student.Succeeded);
        var startsAt = new DateTimeOffset(2026, 6, 16, 15, 0, 0, TimeSpan.Zero);
        var lesson = await lessons.SaveAsync(
            new LessonRecord(
                Id: 0,
                StudentId: student.Value!.Id,
                StudentShortName: "Anna",
                PlanCatalogId: plan.Id,
                PlanName: "Standard",
                StartsAt: startsAt,
                DurationMinutes: 60,
                State: LessonState.Completed,
                GoogleCalendarEventId: "calendar-event-1",
                InvoiceNumber: null),
            CancellationToken.None);
        var draft = CreateInvoiceDraft("Anna", "Anna Andersen", "Standard", startsAt, plan.Id, lessonId: lesson.Id);
        var totals = new InvoiceTotals(Money.Dkk(400m), Money.Dkk(0m), Money.Dkk(400m));

        var reserved = await invoices.ReserveInvoiceAsync(draft, totals, CancellationToken.None);
        var finalized = await invoices.FinalizeIssuedInvoiceWithLessonLocksAsync(
            reserved.Value!.InvoiceNumber,
            "C:\\Invoices\\000001.pdf",
            [lesson.Id],
            CancellationToken.None);
        var loadedLessons = await lessons.ListForStudentMonthAsync("Anna", new DateOnly(2026, 6, 1), CancellationToken.None);

        Assert.True(finalized.Succeeded);
        Assert.Equal(InvoiceIssueStatus.Issued, finalized.Value!.IssueStatus);
        Assert.Equal(new InvoiceNumber(1), Assert.Single(loadedLessons).InvoiceNumber);
        Assert.Null(await invoices.GetIssuingInvoiceAsync(new InvoiceNumber(1), CancellationToken.None));
    }

    [Fact]
    public async Task InvoiceFinalizationWithLessonLocksDoesNotIssueInvoiceWhenLessonAlreadyLocked()
    {
        using var database = await TestDatabase.CreateAsync();
        var students = new SqliteStudentRepository(database.CreateContext);
        var plans = new SqlitePlanCatalogRepository(database.CreateContext);
        var lessons = new SqliteLessonRepository(database.CreateContext);
        var invoices = new SqliteInvoiceRepository(database.CreateContext);
        var plan = await SaveCatalogPlanAsync(plans, "Standard");
        var student = await students.SaveAsync(
            CreateStudentRecord("Anna", currentPlanId: plan.Id),
            CancellationToken.None);
        Assert.True(student.Succeeded);
        var startsAt = new DateTimeOffset(2026, 6, 16, 15, 0, 0, TimeSpan.Zero);
        var lesson = await lessons.SaveAsync(
            new LessonRecord(
                Id: 0,
                StudentId: student.Value!.Id,
                StudentShortName: "Anna",
                PlanCatalogId: plan.Id,
                PlanName: "Standard",
                StartsAt: startsAt,
                DurationMinutes: 60,
                State: LessonState.Completed,
                GoogleCalendarEventId: "calendar-event-1",
                InvoiceNumber: new InvoiceNumber(99)),
            CancellationToken.None);
        var draft = CreateInvoiceDraft("Anna", "Anna Andersen", "Standard", startsAt, plan.Id, lessonId: lesson.Id);
        var totals = new InvoiceTotals(Money.Dkk(400m), Money.Dkk(0m), Money.Dkk(400m));

        var reserved = await invoices.ReserveInvoiceAsync(draft, totals, CancellationToken.None);
        var finalized = await invoices.FinalizeIssuedInvoiceWithLessonLocksAsync(
            reserved.Value!.InvoiceNumber,
            "C:\\Invoices\\000001.pdf",
            [lesson.Id],
            CancellationToken.None);
        var loadedLessons = await lessons.ListForStudentMonthAsync("Anna", new DateOnly(2026, 6, 1), CancellationToken.None);

        Assert.False(finalized.Succeeded);
        Assert.Contains(finalized.Errors, error => error.Code == "Invoice.LessonAlreadyInvoiced");
        Assert.NotNull(await invoices.GetIssuingInvoiceAsync(new InvoiceNumber(1), CancellationToken.None));
        Assert.Null(await invoices.GetIssuedInvoiceAsync(new InvoiceNumber(1), CancellationToken.None));
        Assert.Equal(new InvoiceNumber(99), Assert.Single(loadedLessons).InvoiceNumber);
    }

    [Fact]
    public async Task ReleasedInvoiceReservationDoesNotConsumeInvoiceNumber()
    {
        using var database = await TestDatabase.CreateAsync();
        var repository = new SqliteInvoiceRepository(database.CreateContext);
        var plan = await SaveCatalogPlanAsync(new SqlitePlanCatalogRepository(database.CreateContext), "Standard");
        var lessonDateTime = new DateTimeOffset(2026, 6, 16, 15, 0, 0, TimeSpan.Zero);
        var lesson = await SaveLessonAsync(database, "Anna", plan, lessonDateTime);
        var draft = CreateInvoiceDraft("Anna", "Anna Andersen", "Standard", lessonDateTime, plan.Id, lessonId: lesson.Id);
        var totals = new InvoiceTotals(Money.Dkk(400m), Money.Dkk(0m), Money.Dkk(400m));

        var reserved = await repository.ReserveInvoiceAsync(draft, totals, CancellationToken.None);
        await repository.ReleaseInvoiceReservationAsync(reserved.Value!.InvoiceNumber, CancellationToken.None);
        var retry = await repository.ReserveInvoiceAsync(draft, totals, CancellationToken.None);

        Assert.True(retry.Succeeded);
        Assert.Equal(new InvoiceNumber(1), retry.Value!.InvoiceNumber);
        Assert.Null(await repository.GetIssuedInvoiceAsync(new InvoiceNumber(1), CancellationToken.None));
        Assert.NotNull(await repository.GetIssuingInvoiceAsync(new InvoiceNumber(1), CancellationToken.None));
    }

    [Fact]
    public async Task ReserveInvoiceReusesMatchingIssuingReservationWithoutPublishedPdf()
    {
        using var database = await TestDatabase.CreateAsync();
        var repository = new SqliteInvoiceRepository(database.CreateContext);
        var plan = await SaveCatalogPlanAsync(new SqlitePlanCatalogRepository(database.CreateContext), "Standard");
        var lessonDateTime = new DateTimeOffset(2026, 6, 16, 15, 0, 0, TimeSpan.Zero);
        var lesson = await SaveLessonAsync(database, "Anna", plan, lessonDateTime);
        var draft = CreateInvoiceDraft("Anna", "Anna Andersen", "Standard", lessonDateTime, plan.Id, lessonId: lesson.Id);
        var totals = new InvoiceTotals(Money.Dkk(400m), Money.Dkk(0m), Money.Dkk(400m));

        var reserved = await repository.ReserveInvoiceAsync(draft, totals, CancellationToken.None);
        var retry = await repository.ReserveInvoiceAsync(draft, totals, CancellationToken.None);

        Assert.True(reserved.Succeeded);
        Assert.True(retry.Succeeded);
        Assert.Equal(new InvoiceNumber(1), retry.Value!.InvoiceNumber);
        Assert.Equal(InvoiceIssueStatus.Issuing, retry.Value.IssueStatus);
        Assert.Null(retry.Value.PdfFilePath);
        await using var context = database.CreateContext();
        Assert.Equal(1, await context.IssuedInvoices.CountAsync(TestContext.Current.CancellationToken));
        Assert.Equal(1, await context.IssuedInvoiceLines.CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ReserveInvoiceReturnsMatchingPublishedIssuingReservationForRetry()
    {
        using var database = await TestDatabase.CreateAsync();
        var repository = new SqliteInvoiceRepository(database.CreateContext);
        var plan = await SaveCatalogPlanAsync(new SqlitePlanCatalogRepository(database.CreateContext), "Standard");
        var lessonDateTime = new DateTimeOffset(2026, 6, 16, 15, 0, 0, TimeSpan.Zero);
        var lesson = await SaveLessonAsync(database, "Anna", plan, lessonDateTime);
        var draft = CreateInvoiceDraft("Anna", "Anna Andersen", "Standard", lessonDateTime, plan.Id, lessonId: lesson.Id);
        var totals = new InvoiceTotals(Money.Dkk(400m), Money.Dkk(0m), Money.Dkk(400m));

        var reserved = await repository.ReserveInvoiceAsync(draft, totals, CancellationToken.None);
        await using (var context = database.CreateContext())
        {
            var invoice = await context.IssuedInvoices.SingleAsync(
                storedInvoice => storedInvoice.InvoiceNumber == reserved.Value!.InvoiceNumber.Value,
                TestContext.Current.CancellationToken);
            invoice.PdfFilePath = "C:\\Invoices\\000001.pdf";
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var retry = await repository.ReserveInvoiceAsync(draft, totals, CancellationToken.None);

        Assert.True(retry.Succeeded);
        Assert.Equal(new InvoiceNumber(1), retry.Value!.InvoiceNumber);
        Assert.Equal(InvoiceIssueStatus.Issuing, retry.Value.IssueStatus);
        Assert.Equal("C:\\Invoices\\000001.pdf", retry.Value.PdfFilePath);
        await using var verifyContext = database.CreateContext();
        Assert.Equal(1, await verifyContext.IssuedInvoices.CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task InvoiceLineSchemaStoresOneRoundTripLessonDateTimeValue()
    {
        using var database = await TestDatabase.CreateAsync();
        await using var context = database.CreateContext();

        var entityType = context.Model.FindEntityType(typeof(IssuedInvoiceLineEntity));
        Assert.NotNull(entityType);
        var propertyNames = entityType.GetProperties().Select(property => property.Name).ToArray();

        Assert.Contains("LessonDateTime", propertyNames);
        Assert.Contains("LessonId", propertyNames);
        Assert.Contains("PlanCatalogId", propertyNames);
        Assert.DoesNotContain("LessonDateTimeUtcTicks", propertyNames);
    }

    [Fact]
    public async Task IssuedInvoiceLineSchemaReferencesExactLesson()
    {
        using var database = await TestDatabase.CreateAsync();
        await using var context = database.CreateContext();

        var entityType = context.Model.FindEntityType(typeof(IssuedInvoiceLineEntity));
        Assert.NotNull(entityType);
        var lessonForeignKey = Assert.Single(
            entityType.GetForeignKeys(),
            foreignKey =>
                foreignKey.PrincipalEntityType.ClrType == typeof(LessonEntity) &&
                foreignKey.Properties.Select(property => property.Name).SequenceEqual(["LessonId"]));

        Assert.False(lessonForeignKey.Properties.Single().IsNullable);
        Assert.True(
            lessonForeignKey.DeleteBehavior is DeleteBehavior.Restrict or DeleteBehavior.NoAction,
            $"Expected restrictive delete behavior, but was {lessonForeignKey.DeleteBehavior}.");
    }

    [Fact]
    public async Task IssuedInvoiceLineSchemaAllowsOneIssuedLinePerLesson()
    {
        using var database = await TestDatabase.CreateAsync();
        await using var context = database.CreateContext();

        var entityType = context.Model.FindEntityType(typeof(IssuedInvoiceLineEntity));
        Assert.NotNull(entityType);

        Assert.Contains(
            entityType.GetIndexes(),
            index =>
                index.IsUnique &&
                index.Properties.Select(property => property.Name).SequenceEqual(["LessonId"]));
    }

    [Fact]
    public async Task IssuedInvoiceLineCannotReferenceMissingLesson()
    {
        using var database = await TestDatabase.CreateAsync();
        var repository = new SqliteInvoiceRepository(database.CreateContext);
        var invoice = CreateIssuedInvoice(
            "Anna",
            "Anna Andersen",
            "Standard",
            lessonId: 999);

        await Assert.ThrowsAsync<DbUpdateException>(() =>
            SaveIssuedInvoiceViaBusinessFlowAsync(repository, invoice, CancellationToken.None));
    }

    [Fact]
    public async Task IssuedInvoiceLineCannotReuseLesson()
    {
        using var database = await TestDatabase.CreateAsync();
        var students = new SqliteStudentRepository(database.CreateContext);
        var plans = new SqlitePlanCatalogRepository(database.CreateContext);
        var lessons = new SqliteLessonRepository(database.CreateContext);
        var repository = new SqliteInvoiceRepository(database.CreateContext);
        var plan = await SaveCatalogPlanAsync(plans, "Standard");
        var student = await students.SaveAsync(CreateStudentRecord("Anna", currentPlanId: plan.Id), CancellationToken.None);
        Assert.True(student.Succeeded);
        var lesson = await lessons.SaveAsync(new LessonRecord(
            Id: 0,
            StudentId: student.Value!.Id,
            StudentShortName: "Anna",
            PlanCatalogId: plan.Id,
            PlanName: "Standard",
            StartsAt: new DateTimeOffset(2026, 6, 16, 15, 0, 0, TimeSpan.Zero),
            DurationMinutes: 60,
            State: LessonState.Completed,
            GoogleCalendarEventId: "calendar-event-1",
            InvoiceNumber: null),
            CancellationToken.None);
        var firstInvoice = CreateIssuedInvoice("Anna", "Anna Andersen", "Standard", lessonId: lesson.Id);
        var secondInvoice = CreateIssuedInvoice("Anna", "Anna Andersen", "Standard", lessonId: lesson.Id, invoiceNumber: new InvoiceNumber(2));

        var firstResult = await SaveIssuedInvoiceViaBusinessFlowAsync(repository, firstInvoice, CancellationToken.None);
        Assert.True(firstResult.Succeeded);
        await Assert.ThrowsAsync<DbUpdateException>(() =>
            SaveIssuedInvoiceViaBusinessFlowAsync(repository, secondInvoice, CancellationToken.None));
    }

    [Fact]
    public async Task IssuedInvoiceLinePreventsDeletingReferencedLesson()
    {
        using var database = await TestDatabase.CreateAsync();
        var students = new SqliteStudentRepository(database.CreateContext);
        var plans = new SqlitePlanCatalogRepository(database.CreateContext);
        var lessons = new SqliteLessonRepository(database.CreateContext);
        var repository = new SqliteInvoiceRepository(database.CreateContext);
        var plan = await SaveCatalogPlanAsync(plans, "Standard");
        var student = await students.SaveAsync(CreateStudentRecord("Anna", currentPlanId: plan.Id), CancellationToken.None);
        Assert.True(student.Succeeded);
        var lesson = await lessons.SaveAsync(new LessonRecord(
            Id: 0,
            StudentId: student.Value!.Id,
            StudentShortName: "Anna",
            PlanCatalogId: plan.Id,
            PlanName: "Standard",
            StartsAt: new DateTimeOffset(2026, 6, 16, 15, 0, 0, TimeSpan.Zero),
            DurationMinutes: 60,
            State: LessonState.Completed,
            GoogleCalendarEventId: "calendar-event-1",
            InvoiceNumber: null),
            CancellationToken.None);
        var invoice = CreateIssuedInvoice("Anna", "Anna Andersen", "Standard", lessonId: lesson.Id);
        var result = await SaveIssuedInvoiceViaBusinessFlowAsync(repository, invoice, CancellationToken.None);
        Assert.True(result.Succeeded);

        await using var context = database.CreateContext();
        var storedLesson = await context.Lessons.SingleAsync(
            storedLesson => storedLesson.Id == lesson.Id,
            TestContext.Current.CancellationToken);
        context.Lessons.Remove(storedLesson);

        await Assert.ThrowsAsync<DbUpdateException>(() =>
            context.SaveChangesAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task IssuedInvoiceSnapshotIsNotChangedByStudentOrPlanEdits()
    {
        using var database = await TestDatabase.CreateAsync();
        var students = new SqliteStudentRepository(database.CreateContext);
        var invoices = new SqliteInvoiceRepository(database.CreateContext);
        var plans = new SqlitePlanCatalogRepository(database.CreateContext);
        var intro = await SaveCatalogPlanAsync(plans, "Intro");
        var standard = await SaveCatalogPlanAsync(plans, "Standard");
        var premium = await SaveCatalogPlanAsync(plans, "Premium");

        await students.SaveAsync(CreateStudentRecord("Anna", introPlanId: intro.Id, currentPlanId: standard.Id), CancellationToken.None);
        var lesson = await SaveLessonAsync(database, "Anna", standard, new DateTimeOffset(2026, 6, 16, 15, 0, 0, TimeSpan.Zero));
        await SaveIssuedInvoiceViaBusinessFlowAsync(invoices, CreateIssuedInvoice(
            "Anna",
            fullName: "Anna Andersen",
            planName: "Standard",
            planCatalogId: standard.Id,
            lessonId: lesson.Id), CancellationToken.None);
        var loadedStudent = await students.GetByShortNameAsync("Anna", CancellationToken.None);
        Assert.NotNull(loadedStudent);
        var updatedStudent = CreateStudentRecord("Anna", fullName: "Anna Updated", introPlanId: intro.Id, currentPlanId: premium.Id, currentPlanName: "Premium") with
        {
            Id = loadedStudent.Id
        };
        await students.SaveAsync(updatedStudent, CancellationToken.None);

        var loaded = await invoices.GetIssuedInvoiceAsync(new InvoiceNumber(1), CancellationToken.None);
        Assert.NotNull(loaded);
        Assert.Equal("Anna Andersen", loaded.Student.FullName);
        Assert.Equal(standard.Id, loaded.Lines[0].PlanCatalogId);
        Assert.Equal("Standard", loaded.Lines[0].PlanName);
    }

    [Fact]
    public async Task PaymentStatusCanBeSavedAndQueriedThroughInvoiceRepository()
    {
        using var database = await TestDatabase.CreateAsync();
        var students = new SqliteStudentRepository(database.CreateContext);
        var plans = new SqlitePlanCatalogRepository(database.CreateContext);
        var lessons = new SqliteLessonRepository(database.CreateContext);
        var repository = new SqliteInvoiceRepository(database.CreateContext);
        var intro = await SaveCatalogPlanAsync(plans, "Intro");
        var standard = await SaveCatalogPlanAsync(plans, "Standard");
        var student = await students.SaveAsync(
            CreateStudentRecord("Anna", introPlanId: intro.Id, currentPlanId: standard.Id),
            CancellationToken.None);
        Assert.True(student.Succeeded);
        var startsAt = new DateTimeOffset(2026, 6, 16, 15, 0, 0, TimeSpan.Zero);
        var lesson = await lessons.SaveAsync(
            new LessonRecord(
                Id: 0,
                StudentId: student.Value!.Id,
                StudentShortName: "Anna",
                PlanCatalogId: standard.Id,
                PlanName: "Standard",
                StartsAt: startsAt,
                DurationMinutes: 60,
                State: LessonState.Completed,
                GoogleCalendarEventId: "calendar-event-1",
                InvoiceNumber: new InvoiceNumber(1)),
            CancellationToken.None);
        await SaveIssuedInvoiceViaBusinessFlowAsync(
            repository,
            CreateIssuedInvoice("Anna", "Anna Andersen", "Standard", startsAt, standard.Id, lessonId: lesson.Id),
            CancellationToken.None);

        var initial = await repository.GetPaymentStatusAsync(new InvoiceNumber(1), CancellationToken.None);
        await repository.SavePaymentStatusAsync(new InvoiceNumber(1), PaymentStatus.Paid, CancellationToken.None);
        var updated = await repository.GetPaymentStatusAsync(new InvoiceNumber(1), CancellationToken.None);
        var summaries = await repository.ListInvoicesForStudentAsync("Anna", CancellationToken.None);
        var lessonStatuses = await repository.ListLessonStatusesAsync(new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 30), CancellationToken.None);

        Assert.Equal(PaymentStatus.Outstanding, initial);
        Assert.Equal(PaymentStatus.Paid, updated);
        Assert.Equal(PaymentStatus.Paid, summaries.Single().PaymentStatus);
        Assert.Equal(PaymentStatus.Paid, lessonStatuses.Single().PaymentStatus);
        Assert.Equal(lesson.Id, lessonStatuses.Single().LessonId);
    }

    [Fact]
    public async Task LessonsCanBeSavedAndLoadedForStudentMonth()
    {
        using var database = await TestDatabase.CreateAsync();
        var students = new SqliteStudentRepository(database.CreateContext);
        var plans = new SqlitePlanCatalogRepository(database.CreateContext);
        var lessons = new SqliteLessonRepository(database.CreateContext);
        var intro = await SaveCatalogPlanAsync(plans, "Prøvetime");
        var standard = await SaveCatalogPlanAsync(plans, "Standard");
        var student = await students.SaveAsync(
            CreateStudentRecord("Anna", introPlanId: intro.Id, currentPlanId: standard.Id),
            CancellationToken.None);
        Assert.True(student.Succeeded);
        var startsAt = new DateTimeOffset(2026, 6, 16, 15, 0, 0, TimeSpan.Zero);

        var saved = await lessons.SaveAsync(
            new LessonRecord(
                Id: 0,
                StudentId: student.Value!.Id,
                StudentShortName: "Anna",
                PlanCatalogId: standard.Id,
                PlanName: "Standard",
                StartsAt: startsAt,
                DurationMinutes: 60,
                State: LessonState.Planned,
                GoogleCalendarEventId: "calendar-event-1",
                InvoiceNumber: null),
            CancellationToken.None);
        var loaded = await lessons.ListForStudentMonthAsync("Anna", new DateOnly(2026, 6, 1), CancellationToken.None);

        Assert.True(saved.Id > 0);
        var lesson = Assert.Single(loaded);
        Assert.Equal(saved.Id, lesson.Id);
        Assert.Equal(student.Value.Id, lesson.StudentId);
        Assert.Equal("Anna", lesson.StudentShortName);
        Assert.Equal(standard.Id, lesson.PlanCatalogId);
        Assert.Equal("Standard", lesson.PlanName);
        Assert.Equal(startsAt, lesson.StartsAt);
        Assert.Equal(60, lesson.DurationMinutes);
        Assert.Equal(LessonState.Planned, lesson.State);
        Assert.Equal("calendar-event-1", lesson.GoogleCalendarEventId);
        Assert.Null(lesson.InvoiceNumber);
    }

    [Fact]
    public async Task LessonRepositoryUpdatesUninvoicedTimingByGoogleCalendarEventId()
    {
        using var database = await TestDatabase.CreateAsync();
        var students = new SqliteStudentRepository(database.CreateContext);
        var plans = new SqlitePlanCatalogRepository(database.CreateContext);
        var lessons = new SqliteLessonRepository(database.CreateContext);
        var plan = await SaveCatalogPlanAsync(plans, "Standard");
        var student = await students.SaveAsync(CreateStudentRecord("Anna", currentPlanId: plan.Id), CancellationToken.None);
        Assert.True(student.Succeeded);
        await lessons.SaveAsync(
            new LessonRecord(
                Id: 0,
                StudentId: student.Value!.Id,
                StudentShortName: "Anna",
                PlanCatalogId: plan.Id,
                PlanName: "Standard",
                StartsAt: new DateTimeOffset(2026, 6, 16, 15, 0, 0, TimeSpan.Zero),
                DurationMinutes: 60,
                State: LessonState.Planned,
                GoogleCalendarEventId: "calendar-event-1",
                InvoiceNumber: null),
            CancellationToken.None);
        var updatedStartsAt = new DateTimeOffset(2026, 7, 1, 0, 0, 0, TimeSpan.Zero);

        var updated = await lessons.UpdateUninvoicedTimingByGoogleCalendarEventIdAsync(
            "calendar-event-1",
            updatedStartsAt,
            hasKnownStartTime: false,
            CancellationToken.None);

        Assert.NotNull(updated);
        Assert.Equal(updatedStartsAt, updated.StartsAt);
        Assert.False(updated.HasKnownStartTime);
        var julyLesson = Assert.Single(await lessons.ListForStudentMonthAsync("Anna", new DateOnly(2026, 7, 1), CancellationToken.None));
        Assert.Equal(updatedStartsAt, julyLesson.StartsAt);
        Assert.False(julyLesson.HasKnownStartTime);
    }

    [Fact]
    public async Task LessonRepositoryUpdatesUninvoicedStateByGoogleCalendarEventId()
    {
        using var database = await TestDatabase.CreateAsync();
        var plan = await SaveCatalogPlanAsync(new SqlitePlanCatalogRepository(database.CreateContext), "Standard");
        await SaveLessonAsync(database, "Anna", plan, new DateTimeOffset(2026, 6, 16, 15, 0, 0, TimeSpan.Zero));
        var lessons = new SqliteLessonRepository(database.CreateContext);
        var existing = Assert.Single(await lessons.ListForStudentMonthAsync("Anna", new DateOnly(2026, 6, 1), CancellationToken.None));

        var updated = await lessons.UpdateUninvoicedStateByGoogleCalendarEventIdAsync(
            existing.GoogleCalendarEventId!,
            LessonState.Completed,
            CancellationToken.None);

        Assert.NotNull(updated);
        Assert.Equal(LessonState.Completed, updated.State);
        Assert.Equal(LessonState.Completed, Assert.Single(await lessons.ListForStudentMonthAsync("Anna", new DateOnly(2026, 6, 1), CancellationToken.None)).State);
    }

    [Fact]
    public async Task LessonRepositoryDeletesOnlyUninvoicedLessonByGoogleCalendarEventId()
    {
        using var database = await TestDatabase.CreateAsync();
        var plan = await SaveCatalogPlanAsync(new SqlitePlanCatalogRepository(database.CreateContext), "Standard");
        var uninvoiced = await SaveLessonAsync(database, "Anna", plan, new DateTimeOffset(2026, 6, 16, 15, 0, 0, TimeSpan.Zero));
        var invoiced = await SaveLessonAsync(database, "Anna", plan, new DateTimeOffset(2026, 6, 23, 15, 0, 0, TimeSpan.Zero), new InvoiceNumber(7));
        var lessons = new SqliteLessonRepository(database.CreateContext);

        var deleted = await lessons.DeleteUninvoicedByGoogleCalendarEventIdAsync(
            uninvoiced.GoogleCalendarEventId!,
            CancellationToken.None);
        var invoicedDelete = await lessons.DeleteUninvoicedByGoogleCalendarEventIdAsync(
            invoiced.GoogleCalendarEventId!,
            CancellationToken.None);
        var invoicedUpdate = await lessons.UpdateUninvoicedStateByGoogleCalendarEventIdAsync(
            invoiced.GoogleCalendarEventId!,
            LessonState.Completed,
            CancellationToken.None);

        Assert.True(deleted);
        Assert.False(invoicedDelete);
        Assert.Null(invoicedUpdate);
        var remaining = Assert.Single(await lessons.ListForStudentMonthAsync("Anna", new DateOnly(2026, 6, 1), CancellationToken.None));
        Assert.Equal(invoiced.Id, remaining.Id);
        Assert.Equal(new InvoiceNumber(7), remaining.InvoiceNumber);
    }

    [Fact]
    public async Task LessonRepositoryFiltersStudentMonthByStartsAtInSqlite()
    {
        using var database = await TestDatabase.CreateAsync();
        var plan = await SaveCatalogPlanAsync(new SqlitePlanCatalogRepository(database.CreateContext), "Standard");
        await SaveLessonAsync(database, "Anna", plan, new DateTimeOffset(2026, 5, 31, 15, 0, 0, TimeSpan.Zero));
        var inMonth = await SaveLessonAsync(database, "Anna", plan, new DateTimeOffset(2026, 6, 16, 15, 0, 0, TimeSpan.Zero));
        await SaveLessonAsync(database, "Anna", plan, new DateTimeOffset(2026, 7, 1, 15, 0, 0, TimeSpan.Zero));
        var interceptor = new CapturingCommandInterceptor();
        var repository = CreateLessonRepository(database, interceptor);

        var loaded = await repository.ListForStudentMonthAsync("Anna", new DateOnly(2026, 6, 1), CancellationToken.None);

        var lesson = Assert.Single(loaded);
        Assert.Equal(inMonth.Id, lesson.Id);
        Assert.Contains(interceptor.Commands, command =>
            command.Contains("FROM \"Lessons\"", StringComparison.Ordinal) &&
            command.Contains("\"StartsAt\" >=", StringComparison.Ordinal) &&
            command.Contains("WHERE", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task LessonRepositoryFiltersWindowByStartsAtInSqlite()
    {
        using var database = await TestDatabase.CreateAsync();
        var plan = await SaveCatalogPlanAsync(new SqlitePlanCatalogRepository(database.CreateContext), "Standard");
        await SaveLessonAsync(database, "Anna", plan, new DateTimeOffset(2026, 5, 31, 15, 0, 0, TimeSpan.Zero));
        var inWindow = await SaveLessonAsync(database, "Anna", plan, new DateTimeOffset(2026, 6, 16, 15, 0, 0, TimeSpan.Zero));
        await SaveLessonAsync(database, "Bo", plan, new DateTimeOffset(2026, 7, 1, 15, 0, 0, TimeSpan.Zero));
        var interceptor = new CapturingCommandInterceptor();
        var repository = CreateLessonRepository(database, interceptor);

        var loaded = await repository.ListForWindowAsync(new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 30), CancellationToken.None);

        var lesson = Assert.Single(loaded);
        Assert.Equal(inWindow.Id, lesson.Id);
        Assert.Contains(interceptor.Commands, command =>
            command.Contains("FROM \"Lessons\"", StringComparison.Ordinal) &&
            command.Contains("\"StartsAt\" >=", StringComparison.Ordinal) &&
            command.Contains("WHERE", StringComparison.OrdinalIgnoreCase));
    }

    private static StudentRecord CreateStudentRecord(
        string shortName,
        string? fullName = null,
        int introPlanId = 1,
        int currentPlanId = 2,
        string currentPlanName = "Standard",
        bool isActive = true)
    {
        var student = Student.Create(
            shortName,
            fullName ?? $"{shortName} Andersen",
            "Testvej 1",
            "12345678",
            $"{shortName.ToLowerInvariant()}@example.test",
            AgeBracket.Standard,
            birthdate: null,
            isActive).Value!;

        return new StudentRecord(
            0,
            student,
            [
                new StudentPlanPeriod(
                    new Plan(
                        "Intro",
                        priceWithoutVat: 300m,
                        LessonsPerWeek.One,
                        durationMinutes: 30,
                        PlanFlexibility.MoveOrCancelByAgreement,
                        numberOfStudents: 1,
                        effectiveFrom: new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
                        paymentTerms: "Paid monthly in advance",
                        planCatalogId: introPlanId),
                    new DateOnly(2026, 1, 1),
                    new DateOnly(2026, 5, 31)),
                new StudentPlanPeriod(
                    new Plan(
                        currentPlanName,
                        priceWithoutVat: 400m,
                        LessonsPerWeek.One,
                        durationMinutes: 60,
                        PlanFlexibility.MoveOrCancelByAgreement,
                        numberOfStudents: 1,
                        effectiveFrom: new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero),
                        paymentTerms: "Paid monthly in advance",
                        planCatalogId: currentPlanId),
                    new DateOnly(2026, 6, 1),
                    EndsOn: null)
            ],
            [
                new StudentScheduleEntry(DayOfWeek.Tuesday, new TimeOnly(15, 0))
            ]);
    }

    private static IssuedInvoice CreateIssuedInvoice(
        string studentShortName,
        string fullName,
        string planName,
        DateTimeOffset? lessonDateTime = null,
        int planCatalogId = 7,
        InvoicePeriod? period = null,
        int lessonId = 0,
        InvoiceNumber? invoiceNumber = null,
        DateOnly? invoiceDate = null,
        InvoiceTotals? totals = null,
        string? pdfFilePath = null)
    {
        var draft = CreateInvoiceDraft(studentShortName, fullName, planName, lessonDateTime, planCatalogId, period, lessonId, invoiceDate);

        return draft.Issue(
            invoiceNumber ?? new InvoiceNumber(1),
            totals ?? new InvoiceTotals(Money.Dkk(400m), Money.Dkk(0m), Money.Dkk(400m)),
            pdfFilePath ?? "C:\\Invoices\\000001.pdf");
    }

    private static async Task<OperationResult<IssuedInvoice>> SaveIssuedInvoiceViaBusinessFlowAsync(
        SqliteInvoiceRepository repository,
        IssuedInvoice invoice,
        CancellationToken cancellationToken)
    {
        var draft = InvoiceDraft.Create(
            invoice.InvoiceDate,
            invoice.Company,
            invoice.Student,
            invoice.Lines
                .Select(line => new InvoiceLineDraft(
                    line.LessonDateTime,
                    line.PlanName,
                    line.PriceWithoutVat,
                    line.PlanCatalogId,
                    line.LessonId,
                    line.HasKnownLessonTime))
                .ToArray(),
            priceAdjustment: null,
            invoice.Period);
        var reserved = await repository.ReserveInvoiceAsync(draft, invoice.Totals, cancellationToken);
        if (!reserved.Succeeded)
        {
            return reserved;
        }

        return await repository.FinalizeIssuedInvoiceWithLessonLocksAsync(
            reserved.Value!.InvoiceNumber,
            invoice.PdfFilePath ?? "C:\\Invoices\\000001.pdf",
            draft.Lines.Select(line => line.LessonId).ToArray(),
            cancellationToken);
    }

    private static InvoiceDraft CreateInvoiceDraft(
        string studentShortName,
        string fullName,
        string planName,
        DateTimeOffset? lessonDateTime = null,
        int planCatalogId = 7,
        InvoicePeriod? period = null,
        int lessonId = 0,
        DateOnly? invoiceDate = null) =>
        InvoiceDraft.Create(
            invoiceDate ?? new DateOnly(2026, 6, 16),
            new CompanySnapshot("Maple Key Studio", "Musikvej 1", null),
            new StudentSnapshot(studentShortName, fullName, "Testvej 1"),
            [
                new InvoiceLineDraft(
                    lessonDateTime ?? new DateTimeOffset(2026, 6, 16, 15, 0, 0, TimeSpan.Zero),
                    planName,
                    Money.Dkk(400m),
                    planCatalogId,
                    lessonId)
            ],
            priceAdjustment: null,
            period);

    private static PlanCatalogItem CreatePlanCatalogItem(string name, PlanCatalogState state) =>
        new(
            Id: 0,
            name,
            PriceWithoutVat: 400m,
            LessonsPerWeek.One,
            DurationMinutes: 60,
            PlanFlexibility.MoveOrCancelByAgreement,
            NumberOfStudents: 1,
            EffectiveFrom: new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
            PaymentTerms: "Paid monthly in advance",
            RetireOn: null,
            state);

    private static async Task<PlanCatalogItem> SaveCatalogPlanAsync(SqlitePlanCatalogRepository repository, string name)
    {
        var result = await repository.SaveAsync(CreatePlanCatalogItem(name, PlanCatalogState.Active), CancellationToken.None);
        Assert.True(result.Succeeded);
        return result.Value!;
    }

    private static async Task<LessonRecord> SaveLessonAsync(
        TestDatabase database,
        string studentShortName,
        PlanCatalogItem plan,
        DateTimeOffset startsAt,
        InvoiceNumber? invoiceNumber = null)
    {
        var students = new SqliteStudentRepository(database.CreateContext);
        var lessons = new SqliteLessonRepository(database.CreateContext);
        var student = await students.GetByShortNameAsync(studentShortName, CancellationToken.None);
        if (student is null)
        {
            var savedStudent = await students.SaveAsync(
                CreateStudentRecord(studentShortName, currentPlanId: plan.Id, currentPlanName: plan.Name),
                CancellationToken.None);
            Assert.True(savedStudent.Succeeded);
            student = savedStudent.Value!;
        }

        return await lessons.SaveAsync(
            new LessonRecord(
                Id: 0,
                StudentId: student.Id,
                StudentShortName: studentShortName,
                PlanCatalogId: plan.Id,
                PlanName: plan.Name,
                StartsAt: startsAt,
                DurationMinutes: plan.DurationMinutes,
                State: LessonState.Completed,
                GoogleCalendarEventId: $"calendar-event-{Guid.NewGuid():N}",
                InvoiceNumber: invoiceNumber),
            CancellationToken.None);
    }

    private sealed class TestDatabase : IDisposable
    {
        private readonly string directory;

        private TestDatabase(string directory)
        {
            this.directory = directory;
            DatabasePath = Path.Combine(directory, "ledger.db");
        }

        public string DatabasePath { get; }

        public static async Task<TestDatabase> CreateAsync()
        {
            var database = new TestDatabase(Path.Combine(Path.GetTempPath(), "ElenaLedgerTests", Guid.NewGuid().ToString("N")));
            Directory.CreateDirectory(database.directory);
            await database.MigrateAsync();
            return database;
        }

        public static async Task<TestDatabase> CreateBaselineMigratedAsync()
        {
            return await CreateMigratedAsync("20260622213154_Baseline");
        }

        public static async Task<TestDatabase> CreateMigratedAsync(string targetMigration)
        {
            var database = new TestDatabase(Path.Combine(Path.GetTempPath(), "ElenaLedgerTests", Guid.NewGuid().ToString("N")));
            Directory.CreateDirectory(database.directory);
            await using var context = database.CreateContext();
            await context.Database.MigrateAsync(targetMigration, TestContext.Current.CancellationToken);
            return database;
        }

        public async Task<IReadOnlyList<LedgerMigrationWarning>> MigrateAsync()
        {
            return await new LedgerDatabaseMigrator(CreateContext).MigrateAsync(TestContext.Current.CancellationToken);
        }

        public LedgerDbContext CreateContext()
        {
            return new LedgerDbContext(SqliteLedgerDbContextFactory.CreateOptions(DatabasePath, pooling: false));
        }

        public void Dispose()
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static SqliteLessonRepository CreateLessonRepository(
        TestDatabase database,
        CapturingCommandInterceptor interceptor)
    {
        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = database.DatabasePath,
            Pooling = false
        }.ToString();
        var options = new DbContextOptionsBuilder<LedgerDbContext>()
            .UseSqlite(connectionString)
            .AddInterceptors(interceptor)
            .Options;

        return new SqliteLessonRepository(() => new LedgerDbContext(options));
    }

    private sealed class CapturingCommandInterceptor : DbCommandInterceptor
    {
        public List<string> Commands { get; } = [];

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            Commands.Add(command.CommandText);
            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }
    }
}
