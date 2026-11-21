using System.IO;
using ElenasBusinessLedger.App.Services;
using ElenasBusinessLedger.App.ViewModels;
using ElenasBusinessLedger.Core.UseCases.Invoices;
using ElenasBusinessLedger.Core.UseCases.Lessons;
using ElenasBusinessLedger.Core.UseCases.Startup;
using ElenasBusinessLedger.Infrastructure.Persistence;

namespace ElenasBusinessLedger.App.Startup;

// Upgrades the ledger and runs startup jobs before handing a ready main window and its warnings to WPF.
internal sealed class ApplicationBootstrapper
{
    // Upgrades the local database and runs startup jobs before returning the main window and any warnings.
    public async Task<ApplicationStartupResult> StartAsync(CancellationToken cancellationToken)
    {
        var appPaths = AppPaths.CreateDefault();
        Directory.CreateDirectory(appPaths.AppDataFolderPath);

        var createContext = SqliteLedgerDbContextFactory.Create(appPaths.DatabasePath);
        var migrationWarnings = await new LedgerDatabaseMigrator(createContext).MigrateAsync(cancellationToken);

        var services = ApplicationServiceGraph.Create(appPaths, createContext);
        var warnings = await RunStartupJobsAsync(services, cancellationToken);
        warnings = migrationWarnings
            .Select(warning => new ApplicationStartupWarning(warning.Title, warning.Message))
            .Concat(warnings)
            .ToList();
        var viewModel = CreateMainWindowViewModel(services);

        return new ApplicationStartupResult(new MainWindow(viewModel), warnings);
    }

    private static async Task<IReadOnlyList<ApplicationStartupWarning>> RunStartupJobsAsync(
        ApplicationServiceGraph services,
        CancellationToken cancellationToken)
    {
        var warnings = new List<ApplicationStartupWarning>();
        var retireDueResult = await services.PlanCatalogService.RetireDueAsync(
            services.SystemClock.Now,
            cancellationToken);
        if (!retireDueResult.Succeeded)
        {
            warnings.Add(new ApplicationStartupWarning(
                "Plan retirement failed",
                retireDueResult.Errors.FirstOrDefault()?.Message
                    ?? "Scheduled plan retirement could not be completed."));
        }

        return warnings;
    }

    private static MainWindowViewModel CreateMainWindowViewModel(ApplicationServiceGraph services) =>
        new(
            new CoreStartupDashboardService(new LoadDashboardUseCase(
                services.StudentRepository,
                services.CalendarGateway,
                services.InvoiceRepository,
                services.SystemClock,
                lessonRepository: services.LessonRepository,
                planCatalogRepository: services.PlanCatalogRepository)),
            new CoreInvoicePaymentStatusService(new ToggleInvoicePaymentStatusUseCase(services.InvoiceRepository)),
            new MessageBoxConfirmationService(),
            new ExternalBrowserCalendarLauncher(services.CalendarSettingsRepository.GetAsync),
            new StudentOverviewWindowLauncher(
                services.StudentOverviewService,
                services.StudentEditorService,
                services.PlanCatalogService,
                services.InvoiceWorkflowLauncher,
                services.SystemClock),
            new LessonEditorWindowLauncher(services.LessonEditorService),
            services.LessonEditorService,
            new CoreLessonStateService(new ToggleLessonStateUseCase(services.CalendarGateway, services.LessonRepository)),
            services.InvoiceWorkflowLauncher,
            new ShellInvoicePdfLauncher(),
            new SettingsWindowLauncher(services.SettingsService, services.CalendarAuthorization),
            new PlanCatalogWindowLauncher(services.PlanCatalogService),
            new EconomyOverviewWindowLauncher(
                services.EconomyOverviewService,
                new WindowsEconomyOverviewExportDialog(),
                new EconomyOverviewCsvFileService(new EconomyOverviewCsvFormatter()),
                new ShellInvoicePdfLauncher(),
                services.SystemClock),
            new AboutDialogLauncher(),
            services.SystemClock);
}
