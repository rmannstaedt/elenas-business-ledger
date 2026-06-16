using ElenaSmallBusinessLedger.Core.Company;
using ElenaSmallBusinessLedger.Core.Invoices;
using ElenaSmallBusinessLedger.Core.Money;
using ElenaSmallBusinessLedger.Core.Plans;
using ElenaSmallBusinessLedger.Core.Ports;
using ElenaSmallBusinessLedger.Core.Students;
using ElenaSmallBusinessLedger.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ElenaSmallBusinessLedger.Tests.Infrastructure.Persistence;

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
                        effectiveFrom: new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)),
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
                        effectiveFrom: new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero)),
                    new DateOnly(2026, 6, 1),
                    EndsOn: null)
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
