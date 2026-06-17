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
        var student = CreateStudentRecord("Anna");

        await repository.SaveAsync(student, CancellationToken.None);

        var loaded = await repository.GetByShortNameAsync("Anna", CancellationToken.None);
        Assert.NotNull(loaded);
        Assert.Equal("Anna", loaded.Student.ShortName);
        Assert.Equal(2, loaded.PlanPeriods.Count);
        Assert.Equal("Intro", loaded.PlanPeriods[0].Plan.Name);
        Assert.Equal(new DateOnly(2026, 5, 31), loaded.PlanPeriods[0].EndsOn);
        Assert.Equal("Standard", loaded.PlanPeriods[1].Plan.Name);
        Assert.Null(loaded.PlanPeriods[1].EndsOn);
        Assert.Single(loaded.Schedule);
        Assert.Equal(DayOfWeek.Tuesday, loaded.Schedule[0].Weekday);
        Assert.Equal(new TimeOnly(15, 0), loaded.Schedule[0].StartsAt);
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
    public async Task PlanCatalogLifecycleChangesDoNotChangeStudentPlanPeriodSnapshots()
    {
        using var database = await TestDatabase.CreateAsync();
        var students = new SqliteStudentRepository(database.CreateContext);
        var plans = new SqlitePlanCatalogRepository(database.CreateContext);
        await students.SaveAsync(CreateStudentRecord("Anna", currentPlanName: "Standard"), CancellationToken.None);
        var catalogPlan = (await plans.SaveAsync(CreatePlanCatalogItem("Standard", PlanCatalogState.Active), CancellationToken.None)).Value!;

        await plans.SaveAsync(catalogPlan with { State = PlanCatalogState.Retired }, CancellationToken.None);

        var student = await students.GetByShortNameAsync("Anna", CancellationToken.None);
        Assert.NotNull(student);
        var currentPlan = student.PlanPeriods.Single(period => period.EndsOn is null).Plan;
        Assert.Equal("Standard", currentPlan.Name);
        Assert.Equal(400m, currentPlan.PriceWithoutVat);
        Assert.Equal(LessonsPerWeek.One, currentPlan.LessonsPerWeek);
        Assert.Equal("Paid monthly in advance", currentPlan.PaymentTerms);
    }

    [Fact]
    public async Task SchemaInitializerAddsPlanCatalogTableToExistingDatabase()
    {
        using var database = await TestDatabase.CreateAsync();
        await using (var context = database.CreateContext())
        {
            await context.Database.ExecuteSqlRawAsync("DROP TABLE PlanCatalog");
        }

        new SqliteSchemaInitializer(database.CreateContext).EnsureCreated();

        var repository = new SqlitePlanCatalogRepository(database.CreateContext);
        var result = await repository.SaveAsync(CreatePlanCatalogItem("Standard", PlanCatalogState.Draft), CancellationToken.None);
        var plans = await repository.ListAllAsync(CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Single(plans);
    }

    [Fact]
    public async Task SchemaInitializerAddsRetireOnAndMapsLegacyRetiredState()
    {
        using var database = await TestDatabase.CreateAsync();
        await using (var context = database.CreateContext())
        {
            await context.Database.ExecuteSqlRawAsync("DROP TABLE PlanCatalog");
            await context.Database.ExecuteSqlRawAsync(
                """
                CREATE TABLE PlanCatalog (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL,
                    PriceWithoutVat TEXT NOT NULL,
                    LessonsPerWeek TEXT NOT NULL,
                    DurationMinutes INTEGER NOT NULL,
                    Flexibility TEXT NOT NULL,
                    NumberOfStudents INTEGER NOT NULL,
                    EffectiveFrom TEXT NOT NULL,
                    State TEXT NOT NULL
                );
                """);
            await context.Database.ExecuteSqlRawAsync(
                """
                INSERT INTO PlanCatalog (
                    Name,
                    PriceWithoutVat,
                    LessonsPerWeek,
                    DurationMinutes,
                    Flexibility,
                    NumberOfStudents,
                    EffectiveFrom,
                    State
                ) VALUES (
                    'Legacy',
                    '400',
                    'One',
                    60,
                    'MoveOrCancelByAgreement',
                    1,
                    '2026-01-01 00:00:00+00:00',
                    'Archived'
                );
                """);
        }

        new SqliteSchemaInitializer(database.CreateContext).EnsureCreated();

        var repository = new SqlitePlanCatalogRepository(database.CreateContext);
        var plans = await repository.ListAllAsync(CancellationToken.None);

        var plan = Assert.Single(plans);
        Assert.Equal(PlanCatalogState.Retired, plan.State);
        Assert.Equal("Paid monthly in advance", plan.PaymentTerms);
        Assert.Null(plan.RetireOn);
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
        Assert.DoesNotContain("LessonDateTimeUtcTicks", propertyNames);
    }

    [Fact]
    public async Task IssuedInvoiceSnapshotIsNotChangedByStudentOrPlanEdits()
    {
        using var database = await TestDatabase.CreateAsync();
        var students = new SqliteStudentRepository(database.CreateContext);
        var invoices = new SqliteInvoiceRepository(database.CreateContext);

        await students.SaveAsync(CreateStudentRecord("Anna"), CancellationToken.None);
        await invoices.SaveIssuedInvoiceAsync(CreateIssuedInvoice("Anna", fullName: "Anna Andersen", planName: "Standard"), CancellationToken.None);
        await students.SaveAsync(CreateStudentRecord("Anna", fullName: "Anna Updated", currentPlanName: "Premium"), CancellationToken.None);

        var loaded = await invoices.GetIssuedInvoiceAsync(new InvoiceNumber(1), CancellationToken.None);
        Assert.NotNull(loaded);
        Assert.Equal("Anna Andersen", loaded.Student.FullName);
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
        string currentPlanName = "Standard")
    {
        var student = Student.Create(
            shortName,
            fullName ?? $"{shortName} Andersen",
            "Testvej 1",
            "12345678",
            $"{shortName.ToLowerInvariant()}@example.test",
            AgeBracket.Standard,
            birthdate: null,
            isActive: true).Value!;

        return new StudentRecord(
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
                        paymentTerms: "Paid monthly in advance"),
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
                        paymentTerms: "Paid monthly in advance"),
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
        DateTimeOffset? lessonDateTime = null)
    {
        var draft = InvoiceDraft.Create(
            new DateOnly(2026, 6, 16),
            new CompanySnapshot("Maple Key Studio", "Musikvej 1", null),
            new StudentSnapshot(studentShortName, fullName, "Testvej 1"),
            [
                new InvoiceLineDraft(
                    lessonDateTime ?? new DateTimeOffset(2026, 6, 16, 15, 0, 0, TimeSpan.Zero),
                    planName,
                    Money.Dkk(400m))
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
            await using var context = database.CreateContext();
            await context.Database.EnsureCreatedAsync();
            return database;
        }

        public LedgerDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<LedgerDbContext>()
                .UseSqlite($"Data Source={DatabasePath};Pooling=False")
                .Options;

            return new LedgerDbContext(options);
        }

        public void Dispose()
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
