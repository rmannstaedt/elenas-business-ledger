using System.Windows;
using ElenasBusinessLedger.App.ViewModels;
using ElenasBusinessLedger.App.Views;

namespace ElenasBusinessLedger.App.Services;

public sealed class EconomyOverviewWindowLauncher : IEconomyOverviewLauncher
{
    private readonly IEconomyOverviewService economyOverviewService;
    private readonly IEconomyOverviewExportDialog exportDialog;
    private readonly IEconomyOverviewCsvFileService csvFileService;
    private readonly IInvoicePdfLauncher invoicePdfLauncher;
    private readonly IUiClock clock;

    public EconomyOverviewWindowLauncher(
        IEconomyOverviewService economyOverviewService,
        IEconomyOverviewExportDialog exportDialog,
        IEconomyOverviewCsvFileService csvFileService,
        IInvoicePdfLauncher invoicePdfLauncher,
        IUiClock clock)
    {
        this.economyOverviewService = economyOverviewService;
        this.exportDialog = exportDialog;
        this.csvFileService = csvFileService;
        this.invoicePdfLauncher = invoicePdfLauncher;
        this.clock = clock;
    }

    public async Task ShowEconomyOverviewAsync(bool isAdminMode, CancellationToken cancellationToken)
    {
        var viewModel = new EconomyOverviewViewModel(
            economyOverviewService,
            exportDialog,
            csvFileService,
            invoicePdfLauncher,
            clock,
            isAdminMode);
        await viewModel.LoadAsync(cancellationToken);
        var window = new EconomyOverviewView(viewModel)
        {
            Owner = Application.Current?.MainWindow
        };
        window.ShowDialog();
    }
}
