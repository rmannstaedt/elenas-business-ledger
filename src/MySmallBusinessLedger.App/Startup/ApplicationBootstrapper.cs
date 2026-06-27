using System.IO;
using MySmallBusinessLedger.App.Services;
using MySmallBusinessLedger.App.ViewModels;
using MySmallBusinessLedger.Core.UseCases.Invoices;
using MySmallBusinessLedger.Core.UseCases.Lessons;
using MySmallBusinessLedger.Core.UseCases.Startup;
using MySmallBusinessLedger.Infrastructure.Persistence;

namespace MySmallBusinessLedger.App.Startup;

internal sealed class ApplicationBootstrapper
{
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
            new CoreStartupDashboardService(new StartupDashboardQuery(
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
            new CoreLessonStateService(new ToggleLessonStateUseCase(services.CalendarGateway)),
            services.InvoiceWorkflowLauncher,
            new ShellInvoicePdfLauncher(),
            new SettingsWindowLauncher(services.SettingsService),
            new PlanCatalogWindowLauncher(services.PlanCatalogService),
            services.SystemClock);
}
