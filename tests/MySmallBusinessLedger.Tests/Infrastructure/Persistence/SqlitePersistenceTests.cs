using MySmallBusinessLedger.Core.Company;
using MySmallBusinessLedger.Core.Invoices;
using MySmallBusinessLedger.Core.Money;
using MySmallBusinessLedger.Core.Plans;
using MySmallBusinessLedger.Core.Ports;
using MySmallBusinessLedger.Core.Students;
using MySmallBusinessLedger.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MySmallBusinessLedger.Tests.Infrastructure.Persistence;

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
            "C:\\Invoices");

        await repository.SaveAsync(profile, CancellationToken.None);

        var loaded = await repository.GetAsync(CancellationToken.None);
        Assert.Equal(profile, loaded);
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
        var invoices = new SqliteInvoiceRepository(database.CreateContext);
        await invoices.SaveIssuedInvoiceAsync(CreateIssuedInvoice(
            "Anna",
            fullName: "Anna Andersen",
            planName: "Standard",
            planCatalogId: catalogPlan.Id), CancellationToken.None);

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
        Assert.True(result.Succeeded);
        Assert.Single(plans);
    }

    [Fact]
    public async Task BaselineMigrationAdoptsExistingCurrentSchemaDatabase()
    {
        using var database = await TestDatabase.CreateLegacyEnsureCreatedAsync();
        var repository = new SqlitePlanCatalogRepository(database.CreateContext);
        var saved = await repository.SaveAsync(CreatePlanCatalogItem("Existing", PlanCatalogState.Active), CancellationToken.None);
        Assert.True(saved.Succeeded);

        await database.MigrateAsync();

        var plans = await repository.ListAllAsync(CancellationToken.None);
        await using var context = database.CreateContext();
        var appliedMigrations = await context.Database.GetAppliedMigrationsAsync(TestContext.Current.CancellationToken);

        var plan = Assert.Single(plans);
        Assert.Equal("Existing", plan.Name);
        Assert.Equal(PlanCatalogState.Active, plan.State);
        Assert.Equal("Paid monthly in advance", plan.PaymentTerms);
        Assert.Contains(appliedMigrations, migration => migration.EndsWith("_Baseline", StringComparison.Ordinal));
    }

    [Fact]
    public async Task IssuedInvoiceWithLinesAndPdfPathCanBeSavedAndLoaded()
    {
        using var database = await TestDatabase.CreateAsync();
        var repository = new SqliteInvoiceRepository(database.CreateContext);
        var lessonDateTime = new DateTimeOffset(2026, 6, 16, 15, 0, 0, TimeSpan.FromHours(2));
        var invoice = CreateIssuedInvoice("Anna", fullName: "Anna Andersen", planName: "Standard", lessonDateTime);

        await repository.SaveIssuedInvoiceAsync(invoice, CancellationToken.None);

        var loaded = await repository.GetIssuedInvoiceAsync(new InvoiceNumber(1), CancellationToken.None);
        Assert.NotNull(loaded);
        Assert.Equal("Anna", loaded.Student.ShortName);
        Assert.Equal("C:\\Invoices\\000001.pdf", loaded.PdfFilePath);
        Assert.Single(loaded.Lines);
        Assert.Equal(lessonDateTime, loaded.Lines[0].LessonDateTime);
        Assert.Equal(7, loaded.Lines[0].PlanCatalogId);
        Assert.Equal("Standard", loaded.Lines[0].PlanName);
        Assert.Equal(Money.Dkk(400m), loaded.Totals.Total);
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
        Assert.Contains("PlanCatalogId", propertyNames);
        Assert.DoesNotContain("LessonDateTimeUtcTicks", propertyNames);
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
        await invoices.SaveIssuedInvoiceAsync(CreateIssuedInvoice(
            "Anna",
            fullName: "Anna Andersen",
            planName: "Standard",
            planCatalogId: standard.Id), CancellationToken.None);
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
        var repository = new SqliteInvoiceRepository(database.CreateContext);
        await repository.SaveIssuedInvoiceAsync(CreateIssuedInvoice("Anna", "Anna Andersen", "Standard"), CancellationToken.None);

        var initial = await repository.GetPaymentStatusAsync(new InvoiceNumber(1), CancellationToken.None);
        await repository.SavePaymentStatusAsync(new InvoiceNumber(1), PaymentStatus.Paid, CancellationToken.None);
        var updated = await repository.GetPaymentStatusAsync(new InvoiceNumber(1), CancellationToken.None);
        var summaries = await repository.ListInvoicesForStudentAsync("Anna", CancellationToken.None);
        var lessonStatuses = await repository.ListLessonStatusesAsync(new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 30), CancellationToken.None);

        Assert.Equal(PaymentStatus.Outstanding, initial);
        Assert.Equal(PaymentStatus.Paid, updated);
        Assert.Equal(PaymentStatus.Paid, summaries.Single().PaymentStatus);
        Assert.Equal(PaymentStatus.Paid, lessonStatuses.Single().PaymentStatus);
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
        int planCatalogId = 7)
    {
        var draft = InvoiceDraft.Create(
            new DateOnly(2026, 6, 16),
            new CompanySnapshot("Maple Key Studio", "Musikvej 1", null),
            new StudentSnapshot(studentShortName, fullName, "Testvej 1"),
            [
                new InvoiceLineDraft(
                    lessonDateTime ?? new DateTimeOffset(2026, 6, 16, 15, 0, 0, TimeSpan.Zero),
                    planName,
                    Money.Dkk(400m),
                    planCatalogId)
            ],
            priceAdjustment: null);

        return draft.Issue(
            new InvoiceNumber(1),
            new InvoiceTotals(Money.Dkk(400m), Money.Dkk(0m), Money.Dkk(400m)),
            "C:\\Invoices\\000001.pdf");
    }

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

        public static async Task<TestDatabase> CreateLegacyEnsureCreatedAsync()
        {
            var database = new TestDatabase(Path.Combine(Path.GetTempPath(), "ElenaLedgerTests", Guid.NewGuid().ToString("N")));
            Directory.CreateDirectory(database.directory);
            await using var context = database.CreateContext();
            await context.Database.EnsureCreatedAsync();
            return database;
        }

        public async Task MigrateAsync()
        {
            await new LedgerDatabaseMigrator(CreateContext).MigrateAsync(TestContext.Current.CancellationToken);
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
}
