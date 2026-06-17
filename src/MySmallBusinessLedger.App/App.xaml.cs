using System.IO;
using System.Windows;
using MySmallBusinessLedger.App.Services;
using MySmallBusinessLedger.App.ViewModels;
using MySmallBusinessLedger.Core.UseCases.Invoices;
using MySmallBusinessLedger.Core.UseCases.Lessons;
using MySmallBusinessLedger.Core.UseCases.Startup;
using MySmallBusinessLedger.Infrastructure.Calendar;
using MySmallBusinessLedger.Infrastructure.Invoices.Pdf;
using MySmallBusinessLedger.Infrastructure.Invoices.Printing;
using MySmallBusinessLedger.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace MySmallBusinessLedger.App;

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

        new SqliteSchemaInitializer(CreateContext).EnsureCreated();

        var studentRepository = new SqliteStudentRepository(CreateContext);
        var invoiceRepository = new SqliteInvoiceRepository(CreateContext);
        var companyProfileRepository = new SqliteCompanyProfileRepository(CreateContext);
        var calendarSettingsRepository = new SqliteCalendarSettingsRepository(CreateContext);
        var planCatalogRepository = new SqlitePlanCatalogRepository(CreateContext);
        var systemClock = new SystemUiClock();
        var planCatalogService = new CorePlanCatalogService(planCatalogRepository);
        var retireDueResult = planCatalogService.RetireDueAsync(systemClock.Now, CancellationToken.None).GetAwaiter().GetResult();
        if (!retireDueResult.Succeeded)
        {
            MessageBox.Show(
                retireDueResult.Errors.FirstOrDefault()?.Message ?? "Scheduled plan retirement could not be completed.",
                "Plan retirement failed",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }

        var googleOAuthClientConfigurationProvider = new GoogleOAuthClientConfigurationProvider(
            GoogleOAuthClientConfigurationProvider.CreateDefaultConfigurationPath());
        var googleCalendarClientFactory = new GoogleCalendarClientFactory(
            calendarSettingsRepository.GetAsync,
            googleOAuthClientConfigurationProvider,
            appPaths.GoogleTokenFolderPath,
            "My Small Business Ledger",
            TimeZoneInfo.Local.Id);
        var calendarGateway = new ConfiguredGoogleCalendarGateway(
            googleCalendarClientFactory.CreateAsync,
            TimeZoneInfo.Local,
            DefaultLessonDuration);
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
            appPaths,
            googleOAuthClientConfigurationProvider,
            googleCalendarClientFactory);

        var viewModel = new MainWindowViewModel(
            new CoreStartupDashboardService(new StartupDashboardQuery(
                studentRepository,
                calendarGateway,
                invoiceRepository,
                systemClock)),
            new CoreInvoicePaymentStatusService(new ToggleInvoicePaymentStatusUseCase(invoiceRepository)),
            new MessageBoxConfirmationService(),
            new ExternalBrowserCalendarLauncher(calendarSettingsRepository.GetAsync),
            new StudentEditorWindowLauncher(studentEditorService, planCatalogService),
            new LessonEditorWindowLauncher(lessonEditorService),
            lessonEditorService,
            new CoreLessonStateService(new ToggleLessonStateUseCase(calendarGateway)),
            invoiceWorkflowLauncher,
            new SettingsWindowLauncher(settingsService),
            new PlanCatalogWindowLauncher(planCatalogService),
            systemClock);

        var window = new MainWindow(viewModel);
        MainWindow = window;
        window.Show();
    }
}
