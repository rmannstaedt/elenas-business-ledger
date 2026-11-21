using ElenasBusinessLedger.App.Services;
using ElenasBusinessLedger.Core.Calendar;
using ElenasBusinessLedger.Core.Common;
using ElenasBusinessLedger.Core.Company;
using ElenasBusinessLedger.Core.Invoices;
using ElenasBusinessLedger.Core.Lessons;
using ElenasBusinessLedger.Core.Ports;
using ElenasBusinessLedger.Core.Time;
using ElenasBusinessLedger.Core.UseCases.Lessons;
using ElenasBusinessLedger.Core.UseCases.Startup;
using ElenasBusinessLedger.Infrastructure.Calendar;
using ElenasBusinessLedger.Infrastructure.Invoices.Printing;
using ElenasBusinessLedger.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ElenasBusinessLedger.Tests.App.Services;

// Checks dashboard failures, Google authorization status, and Settings behavior at the app service boundary.
public sealed class AppServiceTests
{
    [Fact]
    public async Task StartupDashboardServiceReturnsFailureInsteadOfSampleRowsWhenCalendarIsNotConfigured()
    {
        var query = new LoadDashboardUseCase(
            new EmptyStudentRepository(),
            new ThrowingCalendarGateway("Google Calendar is not configured."),
            new EmptyInvoiceRepository(),
            new FixedClock(),
            new EmptyLessonRepository());
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
            new LessonAddRequest("Anna", new DateTimeOffset(2026, 6, 16, 15, 0, 0, TimeSpan.Zero), PlanCatalogId: 42),
            new DateOnly(2026, 6, 1),
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, error => error.Code == "Calendar.NotConfigured");
    }

    [Fact]
    public async Task AppSettingsServiceDoesNotReportSavedGoogleAuthorizationWhenOnlyUnrelatedTokenFilesExist()
    {
        using var database = await TestDatabase.CreateAsync();
        using var appData = new TemporaryDirectory();
        var appPaths = AppPaths.FromAppDataFolder(appData.Path);
        Directory.CreateDirectory(appPaths.GoogleTokenFolderPath);
        await File.WriteAllBytesAsync(
            Path.Combine(appPaths.GoogleTokenFolderPath, "token.bin"),
            [1, 2, 3],
            TestContext.Current.CancellationToken);
        var service = CreateSettingsService(database, new SqliteCalendarSettingsRepository(database.CreateContext), appPaths);

        var snapshot = await service.LoadAsync(CancellationToken.None);

        Assert.False(snapshot.HasGoogleAuthorizationTokens);
    }

    [Fact]
    public async Task AppSettingsServiceReportsSavedGoogleAuthorizationForConfiguredAccountToken()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using var database = await TestDatabase.CreateAsync();
        using var appData = new TemporaryDirectory();
        var appPaths = AppPaths.FromAppDataFolder(appData.Path);
        var calendarSettingsRepository = new SqliteCalendarSettingsRepository(database.CreateContext);
        await calendarSettingsRepository.SaveAsync(
            new CalendarSettings("primary", "elena@example.test"),
            CancellationToken.None);
        var tokenStore = new WindowsProtectedFileDataStore(appPaths.GoogleTokenFolderPath);
        await tokenStore.StoreAsync("elena@example.test", new StoredToken("saved-token"));
        var service = CreateSettingsService(database, calendarSettingsRepository, appPaths);

        var snapshot = await service.LoadAsync(CancellationToken.None);

        Assert.True(snapshot.HasGoogleAuthorizationTokens);
    }

    [Fact]
    public async Task AppSettingsServiceReportsSavedGoogleAuthorizationForConfiguredAccountLegacyToken()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using var database = await TestDatabase.CreateAsync();
        using var appData = new TemporaryDirectory();
        var appPaths = AppPaths.FromAppDataFolder(appData.Path);
        var calendarSettingsRepository = new SqliteCalendarSettingsRepository(database.CreateContext);
        await calendarSettingsRepository.SaveAsync(
            new CalendarSettings("primary", "elena@example.test"),
            CancellationToken.None);
        var tokenStore = new WindowsProtectedFileDataStore(appPaths.GoogleTokenFolderPath);
        await tokenStore.StoreAsync("elena@example.test", new StoredToken("saved-token"));
        var hashedFile = Assert.Single(Directory.GetFiles(appPaths.GoogleTokenFolderPath));
        File.Move(
            hashedFile,
            Path.Combine(appPaths.GoogleTokenFolderPath, LegacyEscapedFileName("elena@example.test")));
        var service = CreateSettingsService(database, calendarSettingsRepository, appPaths);

        var snapshot = await service.LoadAsync(CancellationToken.None);

        Assert.True(snapshot.HasGoogleAuthorizationTokens);
    }

    [Fact]
    public async Task AppSettingsServiceLoadsAndSavesEconomySettings()
    {
        using var database = await TestDatabase.CreateAsync();
        using var appData = new TemporaryDirectory();
        var appPaths = AppPaths.FromAppDataFolder(appData.Path);
        var economyRepository = new SqliteEconomyRepository(database.CreateContext);
        await economyRepository.SaveSettingsAsync(
            new EconomySettings(new DateOnly(2026, 7, 15)),
            CancellationToken.None);
        var service = CreateSettingsService(database, new SqliteCalendarSettingsRepository(database.CreateContext),
            appPaths, economyRepository);

        var snapshot = await service.LoadAsync(CancellationToken.None);
        var save = await service.SaveAsync(
            CompanyProfile.Create(
                "Maple Key Studio",
                "Musikvej 1",
                "12345678",
                "elena@example.test",
                cvr: null,
                vatRegistrationDate: null,
                vatStatus: VatStatus.NotRegistered,
                invoiceFolderPath: @"C:\Invoices"),
            new CalendarSettings("primary", "elena@example.test"),
            new EconomySettings(new DateOnly(2026, 8, 20)),
            CancellationToken.None);
        var savedEconomySettings = await economyRepository.GetSettingsAsync(CancellationToken.None);

        Assert.Equal(new DateOnly(2026, 7, 1), snapshot.EconomySettings?.FirstApplicationInvoiceMonth);
        Assert.True(save.Succeeded);
        Assert.Equal(new DateOnly(2026, 8, 1), save.Value?.EconomySettings?.FirstApplicationInvoiceMonth);
        Assert.Equal(new DateOnly(2026, 8, 1), savedEconomySettings?.FirstApplicationInvoiceMonth);
    }

    private static CoreAppSettingsService CreateSettingsService(
        TestDatabase database,
        SqliteCalendarSettingsRepository calendars,
        AppPaths paths,
        IEconomyRepository? economyRepository = null)
    {
        var authorization = new GoogleCalendarAuthorizationService(
            new GoogleOAuthClientConfigurationProvider(Path.Combine(paths.AppDataFolderPath, "missing-google-oauth-client.json")),
            (_, _) => Task.FromResult<ConfiguredGoogleCalendarClient?>(null),
            account => WindowsProtectedFileDataStore.ContainsStoredKey(paths.GoogleTokenFolderPath, account),
            () => new WindowsProtectedFileDataStore(paths.GoogleTokenFolderPath).ClearAsync());
        return new CoreAppSettingsService(
            new SqliteCompanyProfileRepository(database.CreateContext),
            calendars.GetAsync,
            new SqliteAppSettingsWriter(database.CreateContext),
            paths,
            authorization.GetStatus,
            new WindowsPrinterCatalog(),
            economyRepository);
    }

    // Supplies an empty student catalog so dashboard tests isolate the behavior of calendar failures.
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

    // Supplies empty invoice reads so dashboard service tests run without billing records.
    private sealed class EmptyInvoiceRepository : IInvoiceRepository
    {
        public Task<IReadOnlyList<InvoiceLessonStatus>> ListLessonStatusesAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken,
            IReadOnlyList<int>? includeLessonIds = null) =>
            Task.FromResult<IReadOnlyList<InvoiceLessonStatus>>([]);

        public Task<IReadOnlyList<IssuedInvoiceSummary>> ListInvoicesForStudentAsync(int studentId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<IssuedInvoiceSummary>>([]);

        public Task<bool> HasInvoiceForStudentInMonthAsync(int studentId, DateOnly month, CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task<InvoiceNumber> GetNextInvoiceNumberAsync(CancellationToken cancellationToken) =>
            Task.FromResult(new InvoiceNumber(1));

        public Task<OperationResult<IssuedInvoice>> ReserveInvoiceAsync(
            InvoiceDraft draft,
            InvoiceTotals totals,
            CancellationToken cancellationToken) =>
            Task.FromResult(OperationResult<IssuedInvoice>.Success(draft.Issue(
                new InvoiceNumber(1),
                totals,
                pdfFilePath: null,
                InvoiceIssueStatus.Issuing,
                InvoicePrintStatus.NotPrinted,
                printedAt: null,
                lastPrintError: null)));

        public Task<OperationResult<IssuedInvoice>> FinalizeIssuedInvoiceWithLessonLocksAsync(
            InvoiceNumber invoiceNumber,
            string pdfFilePath,
            IReadOnlyList<int> lessonIds,
            CancellationToken cancellationToken) =>
            Task.FromResult(OperationResult<IssuedInvoice>.Failure(
                new ValidationError("Invoice.NotConfigured", "Invoice finalization is not configured.")));

        public Task<OperationResult<IssuedInvoice>> MarkInvoicePdfPublishedAsync(
            InvoiceNumber invoiceNumber,
            string pdfFilePath,
            CancellationToken cancellationToken) =>
            Task.FromResult(OperationResult<IssuedInvoice>.Failure(
                new ValidationError("Invoice.NotConfigured", "Invoice PDF publication is not configured.")));

        public Task<OperationResult<IssuedInvoice>> ReviseInvoiceReservationAsync(
            IssuedInvoice savedInvoice, InvoiceDraft draft, InvoiceTotals totals, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<OperationResult<IssuedInvoice>> PrepareInvoicePublicationAsync(
            IssuedInvoice savedInvoice, InvoicePdfPublication publication, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task ReleaseInvoiceReservationAsync(
            InvoiceNumber invoiceNumber, CancellationToken cancellationToken, string? expectedRevision = null) =>
            Task.CompletedTask;

        public Task<OperationResult<IssuedInvoice>> UpdatePrintStatusAsync(
            InvoiceNumber invoiceNumber,
            InvoicePrintStatus printStatus,
            DateTimeOffset? printedAt,
            string? lastPrintError,
            CancellationToken cancellationToken) =>
            Task.FromResult(OperationResult<IssuedInvoice>.Failure(
                new ValidationError("Invoice.NotConfigured", "Invoice print status is not configured.")));

        public Task<IssuedInvoice?> GetIssuedInvoiceAsync(InvoiceNumber invoiceNumber, CancellationToken cancellationToken) =>
            Task.FromResult<IssuedInvoice?>(null);

        public Task<IssuedInvoice?> GetOldestPendingAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IssuedInvoice?>(null);

        public Task<OperationResult<InvoiceNumber>> DiscardPendingAsync(
            IssuedInvoice savedInvoice, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<IssuedInvoice?> GetIssuingInvoiceAsync(InvoiceNumber invoiceNumber, CancellationToken cancellationToken) =>
            Task.FromResult<IssuedInvoice?>(null);

        public Task<PaymentStatus> GetPaymentStatusAsync(InvoiceNumber invoiceNumber, CancellationToken cancellationToken) =>
            Task.FromResult(PaymentStatus.Outstanding);

        public Task SavePaymentStatusAsync(InvoiceNumber invoiceNumber, PaymentStatus paymentStatus, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }

    // Supplies empty lesson reads so service tests do not need a populated billing ledger.
    private sealed class EmptyLessonRepository : ILessonRepository
    {
        public Task<LessonRecord> SaveAsync(LessonRecord lesson, CancellationToken cancellationToken) =>
            Task.FromResult(lesson);

        public Task<IReadOnlyList<LessonRecord>> ListForStudentMonthAsync(
            string studentShortName,
            DateOnly month,
            CancellationToken cancellationToken,
            int? studentId = null) =>
            Task.FromResult<IReadOnlyList<LessonRecord>>([]);

        public Task<IReadOnlyList<LessonRecord>> ListForWindowAsync(
            DateOnly from,
            DateOnly to,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<LessonRecord>>([]);

        public Task<LessonRecord?> UpdatePlanAsync(
            int lessonId,
            int planCatalogId,
            string planName,
            int durationMinutes,
            CancellationToken cancellationToken) =>
            Task.FromResult<LessonRecord?>(null);

        public Task<LessonRecord?> UpdateUninvoicedTimingByGoogleCalendarEventIdAsync(
            string googleCalendarEventId,
            DateTimeOffset startsAt,
            bool hasKnownStartTime,
            CancellationToken cancellationToken) =>
            Task.FromResult<LessonRecord?>(null);

        public Task<LessonRecord?> UpdateUninvoicedStateByGoogleCalendarEventIdAsync(
            string googleCalendarEventId,
            LessonState state,
            CancellationToken cancellationToken) =>
            Task.FromResult<LessonRecord?>(null);

        public Task<bool> DeleteUninvoicedByGoogleCalendarEventIdAsync(
            string googleCalendarEventId,
            CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task<IReadOnlyList<LessonRecord>> ListByGoogleCalendarEventIdsAsync(
            IReadOnlyList<string> eventIds, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<LessonRecord>>([]);
    }

    // Simulates a calendar exception so the dashboard's failure result can be asserted.
    private sealed class ThrowingCalendarGateway(string message) : IGoogleCalendarGateway
    {
        public Task<IReadOnlyList<CalendarLesson>> ListLessonsAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken) =>
            throw new InvalidOperationException(message);

        public Task<OperationResult<LessonAddResult>> AddLessonAsync(LessonAddRequest request, DateOnly refreshMonth, CancellationToken cancellationToken) =>
            Task.FromResult(OperationResult<LessonAddResult>.Failure(
                new LessonAddResult(
                    new CalendarLesson(
                        string.Empty,
                        request.StudentShortName,
                        request.StartsAt,
                        LessonState.Planned,
                        request.HasKnownStartTime),
                    []),
                new ValidationError("Calendar.NotConfigured", message)));

        public Task<OperationResult<IReadOnlyList<CalendarLesson>>> PopulateLessonsAsync(
            IReadOnlyList<LessonAddRequest> requests,
            DateOnly refreshMonth,
            CancellationToken cancellationToken) =>
            Task.FromResult(OperationResult<IReadOnlyList<CalendarLesson>>.Failure(new ValidationError("Calendar.NotConfigured", message)));

        public Task<OperationResult<IReadOnlyList<CalendarLesson>>> UpdateLessonAsync(LessonEditRequest request, DateOnly refreshMonth, CancellationToken cancellationToken) =>
            Task.FromResult(OperationResult<IReadOnlyList<CalendarLesson>>.Failure(new ValidationError("Calendar.NotConfigured", message)));

        public Task<OperationResult<IReadOnlyList<CalendarLesson>>> DeleteLessonAsync(string lessonId, DateOnly refreshMonth, CancellationToken cancellationToken) =>
            Task.FromResult(OperationResult<IReadOnlyList<CalendarLesson>>.Failure(new ValidationError("Calendar.NotConfigured", message)));

        public Task<OperationResult<LessonState>> UpdateLessonStateAsync(string lessonId, LessonState state, CancellationToken cancellationToken) =>
            Task.FromResult(OperationResult<LessonState>.Failure(state, new ValidationError("Calendar.NotConfigured", message)));
    }

    // Gives dashboard tests a stable date without depending on the system clock.
    private sealed class FixedClock : IClock
    {
        public DateTimeOffset Now => new(2026, 6, 16, 15, 0, 0, TimeSpan.Zero);
    }

    // Creates a disposable SQLite ledger so Settings tests exercise the real repositories.
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
            var database = new TestDatabase(Path.Combine(Path.GetTempPath(), "ElenasBusinessLedgerAppServiceTests", Guid.NewGuid().ToString("N")));
            Directory.CreateDirectory(database.directory);
            await new LedgerDatabaseMigrator(database.CreateContext).MigrateAsync(TestContext.Current.CancellationToken);
            return database;
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

    // Provides an isolated location for token and configuration fixtures and removes it after the test.
    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "ElenasBusinessLedgerAppServiceTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            Directory.Delete(Path, recursive: true);
        }
    }

    // Supplies minimal credential data for tests of saved authorization detection.
    private sealed record StoredToken(string AccessToken);

    private static string LegacyEscapedFileName(string key) =>
        $"{Uri.EscapeDataString(key).Replace("%", "_")}.bin";
}
