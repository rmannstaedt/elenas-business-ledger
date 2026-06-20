using MySmallBusinessLedger.App.Services;
using MySmallBusinessLedger.Core.Calendar;
using MySmallBusinessLedger.Core.Common;
using MySmallBusinessLedger.Core.Company;
using MySmallBusinessLedger.Core.Invoices;
using MySmallBusinessLedger.Core.Lessons;
using MySmallBusinessLedger.Core.Ports;
using MySmallBusinessLedger.Core.Time;
using MySmallBusinessLedger.Core.UseCases.Lessons;
using MySmallBusinessLedger.Core.UseCases.Startup;
using MySmallBusinessLedger.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MySmallBusinessLedger.Tests.App.Services;

public sealed class AppServiceTests
{
    [Fact]
    public async Task StartupDashboardServiceReturnsFailureInsteadOfSampleRowsWhenCalendarIsNotConfigured()
    {
        var query = new StartupDashboardQuery(
            new EmptyStudentRepository(),
            new ThrowingCalendarGateway("Google Calendar is not configured."),
            new EmptyInvoiceRepository(),
            new FixedClock());
        var service = new CoreStartupDashboardService(query);

        var result = await service.LoadAsync(new DateOnly(2026, 6, 1), null, CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, error => error.Code == "Dashboard.RefreshFailed");
        Assert.Contains("Google Calendar is not configured.", result.Errors[0].Message);
    }

    [Fact]
    public async Task ConfiguredCalendarGatewayFailsClosedWhenConfigurationIsMissing()
    {
        var gateway = new ConfiguredGoogleCalendarGateway(
            _ => Task.FromResult<ConfiguredGoogleCalendarClient?>(null),
            TimeZoneInfo.Utc,
            TimeSpan.FromHours(1));

        var result = await gateway.AddLessonAsync(
            new LessonAddRequest("Anna", new DateTimeOffset(2026, 6, 16, 15, 0, 0, TimeSpan.Zero)),
            new DateOnly(2026, 6, 1),
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, error => error.Code == "Calendar.NotConfigured");
    }

    [Fact]
    public async Task AppSettingsServiceReportsSavedGoogleAuthorizationWhenTokenFolderContainsFiles()
    {
        using var database = await TestDatabase.CreateAsync();
        using var appData = new TemporaryDirectory();
        var appPaths = AppPaths.FromAppDataFolder(appData.Path);
        Directory.CreateDirectory(appPaths.GoogleTokenFolderPath);
        await File.WriteAllBytesAsync(Path.Combine(appPaths.GoogleTokenFolderPath, "token.bin"), [1, 2, 3]);
        var service = new CoreAppSettingsService(
            new SqliteCompanyProfileRepository(database.CreateContext),
            new SqliteCalendarSettingsRepository(database.CreateContext),
            appPaths,
            new GoogleOAuthClientConfigurationProvider(Path.Combine(appData.Path, "missing-google-oauth-client.json")));

        var snapshot = await service.LoadAsync(CancellationToken.None);

        Assert.True(snapshot.HasGoogleAuthorizationTokens);
    }

    private sealed class EmptyStudentRepository : IStudentRepository
    {
        public Task<IReadOnlyList<StudentRecord>> ListStudentsAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<StudentRecord>>([]);

        public Task<IReadOnlyList<StudentRecord>> ListActiveStudentsAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<StudentRecord>>([]);

        public Task<StudentRecord?> GetByShortNameAsync(string shortName, CancellationToken cancellationToken) =>
            Task.FromResult<StudentRecord?>(null);

        public Task<OperationResult<StudentRecord>> SaveAsync(StudentRecord student, CancellationToken cancellationToken) =>
            Task.FromResult(OperationResult<StudentRecord>.Success(student));
    }

    private sealed class EmptyInvoiceRepository : IInvoiceRepository
    {
        public Task<IReadOnlyList<InvoiceLessonStatus>> ListLessonStatusesAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<InvoiceLessonStatus>>([]);

        public Task<IReadOnlyList<IssuedInvoiceSummary>> ListInvoicesForStudentAsync(string studentShortName, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<IssuedInvoiceSummary>>([]);

        public Task<bool> HasInvoiceForStudentInMonthAsync(string studentShortName, DateOnly month, CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task<InvoiceNumber> GetNextInvoiceNumberAsync(CancellationToken cancellationToken) =>
            Task.FromResult(new InvoiceNumber(1));

        public Task<OperationResult<IssuedInvoice>> SaveIssuedInvoiceAsync(IssuedInvoice invoice, CancellationToken cancellationToken) =>
            Task.FromResult(OperationResult<IssuedInvoice>.Success(invoice));

        public Task<IssuedInvoice?> GetIssuedInvoiceAsync(InvoiceNumber invoiceNumber, CancellationToken cancellationToken) =>
            Task.FromResult<IssuedInvoice?>(null);

        public Task<PaymentStatus> GetPaymentStatusAsync(InvoiceNumber invoiceNumber, CancellationToken cancellationToken) =>
            Task.FromResult(PaymentStatus.Outstanding);

        public Task SavePaymentStatusAsync(InvoiceNumber invoiceNumber, PaymentStatus paymentStatus, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }

    private sealed class ThrowingCalendarGateway(string message) : IGoogleCalendarGateway
    {
        public Task<IReadOnlyList<CalendarLesson>> ListLessonsAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken) =>
            throw new InvalidOperationException(message);

        public Task<OperationResult<IReadOnlyList<CalendarLesson>>> AddLessonAsync(LessonAddRequest request, DateOnly refreshMonth, CancellationToken cancellationToken) =>
            Task.FromResult(OperationResult<IReadOnlyList<CalendarLesson>>.Failure(new ValidationError("Calendar.NotConfigured", message)));

        public Task<OperationResult<IReadOnlyList<CalendarLesson>>> UpdateLessonAsync(LessonEditRequest request, DateOnly refreshMonth, CancellationToken cancellationToken) =>
            Task.FromResult(OperationResult<IReadOnlyList<CalendarLesson>>.Failure(new ValidationError("Calendar.NotConfigured", message)));

        public Task<OperationResult<IReadOnlyList<CalendarLesson>>> DeleteLessonAsync(string lessonId, DateOnly refreshMonth, CancellationToken cancellationToken) =>
            Task.FromResult(OperationResult<IReadOnlyList<CalendarLesson>>.Failure(new ValidationError("Calendar.NotConfigured", message)));

        public Task<OperationResult<LessonState>> UpdateLessonStateAsync(string lessonId, LessonState state, CancellationToken cancellationToken) =>
            Task.FromResult(OperationResult<LessonState>.Failure(state, new ValidationError("Calendar.NotConfigured", message)));
    }

    private sealed class FixedClock : IClock
    {
        public DateTimeOffset Now => new(2026, 6, 16, 15, 0, 0, TimeSpan.Zero);
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
            var database = new TestDatabase(Path.Combine(Path.GetTempPath(), "MySmallBusinessLedgerAppServiceTests", Guid.NewGuid().ToString("N")));
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

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "MySmallBusinessLedgerAppServiceTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            Directory.Delete(Path, recursive: true);
        }
    }
}
