using System.IO;
using System.Windows;
using ElenaSmallBusinessLedger.App.Services;
using ElenaSmallBusinessLedger.App.ViewModels;
using ElenaSmallBusinessLedger.Core.UseCases.Invoices;
using ElenaSmallBusinessLedger.Core.UseCases.Lessons;
using ElenaSmallBusinessLedger.Core.UseCases.Startup;
using ElenaSmallBusinessLedger.Infrastructure.Calendar;
using ElenaSmallBusinessLedger.Infrastructure.Invoices.Pdf;
using ElenaSmallBusinessLedger.Infrastructure.Invoices.Printing;
using ElenaSmallBusinessLedger.Infrastructure.Persistence;
using Google.Apis.Auth.OAuth2;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ElenaSmallBusinessLedger.App;

public partial class App : Application
{
    private static readonly TimeSpan DefaultLessonDuration = TimeSpan.FromMinutes(60);

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var appPaths = AppPaths.CreateDefault();
        Directory.CreateDirectory(appPaths.AppDataFolderPath);

        LedgerDbContext CreateContext()
        {
            var connectionString = new SqliteConnectionStringBuilder { DataSource = appPaths.DatabasePath }.ToString();
            var options = new DbContextOptionsBuilder<LedgerDbContext>()
                .UseSqlite(connectionString)
                .Options;
            return new LedgerDbContext(options);
        }

        using (var context = CreateContext())
        {
            context.Database.EnsureCreated();
        }

        var studentRepository = new SqliteStudentRepository(CreateContext);
        var invoiceRepository = new SqliteInvoiceRepository(CreateContext);
        var companyProfileRepository = new SqliteCompanyProfileRepository(CreateContext);
        var calendarSettingsRepository = new SqliteCalendarSettingsRepository(CreateContext);
        var calendarGateway = new ConfiguredGoogleCalendarGateway(
            cancellationToken => LoadConfiguredCalendarClientAsync(appPaths, calendarSettingsRepository, cancellationToken),
            TimeZoneInfo.Local,
            DefaultLessonDuration);
        var systemClock = new SystemUiClock();

        var studentEditorService = new CoreStudentEditorService(studentRepository);
        var lessonEditorService = new CoreLessonEditorService(
            new AddLessonUseCase(calendarGateway),
            new EditLessonUseCase(calendarGateway),
            new DeleteLessonUseCase(calendarGateway));
        var invoiceWorkflowService = new CoreInvoiceWorkflowService(
            new InvoiceCandidateSelectionUseCase(studentRepository, calendarGateway, invoiceRepository),
            new InvoicePreviewUseCase(studentRepository, companyProfileRepository, calendarGateway),
            companyProfileRepository,
            invoiceRepository,
            new QuestPdfInvoicePdfWriter(),
            new SavedPdfPrinter(new ShellPrintProcessLauncher()));
        var invoiceWorkflowLauncher = new InvoiceWorkflowWindowLauncher(invoiceWorkflowService, systemClock);
        var settingsService = new CoreAppSettingsService(
            companyProfileRepository,
            calendarSettingsRepository,
            appPaths);

        var viewModel = new MainWindowViewModel(
            new CoreStartupDashboardService(new StartupDashboardQuery(
                studentRepository,
                calendarGateway,
                invoiceRepository,
                systemClock)),
            new CoreInvoicePaymentStatusService(new ToggleInvoicePaymentStatusUseCase(invoiceRepository)),
            new MessageBoxConfirmationService(),
            new ExternalBrowserCalendarLauncher(),
            new StudentEditorWindowLauncher(studentEditorService),
            new LessonEditorWindowLauncher(lessonEditorService),
            lessonEditorService,
            new CoreLessonStateService(new ToggleLessonStateUseCase(calendarGateway)),
            invoiceWorkflowLauncher,
            new SettingsWindowLauncher(settingsService),
            systemClock);

        var window = new MainWindow(viewModel);
        MainWindow = window;
        window.Show();
    }

    private static async Task<ConfiguredGoogleCalendarClient?> LoadConfiguredCalendarClientAsync(
        AppPaths appPaths,
        SqliteCalendarSettingsRepository calendarSettingsRepository,
        CancellationToken cancellationToken)
    {
        var settings = await calendarSettingsRepository.GetAsync(cancellationToken);
        if (settings is null ||
            string.IsNullOrWhiteSpace(settings.SelectedCalendarId) ||
            string.IsNullOrWhiteSpace(settings.GoogleAccountEmail))
        {
            return null;
        }

        if (!File.Exists(appPaths.GoogleClientSecretsPath))
        {
            return null;
        }

        await using var secretsStream = File.OpenRead(appPaths.GoogleClientSecretsPath);
        var clientSecrets = GoogleClientSecrets.FromStream(secretsStream).Secrets;
        var tokenStore = new WindowsProtectedFileDataStore(appPaths.GoogleTokenFolderPath);
        var credentialFactory = new GoogleCalendarCredentialFactory(
            clientSecrets,
            tokenStore,
            "Elena's Small Business Ledger");
        var calendarService = await credentialFactory.CreateCalendarServiceAsync(settings.GoogleAccountEmail, cancellationToken);

        return new ConfiguredGoogleCalendarClient(
            settings.SelectedCalendarId,
            new GoogleCalendarServiceEventClient(calendarService, TimeZoneInfo.Local.Id));
    }
}
