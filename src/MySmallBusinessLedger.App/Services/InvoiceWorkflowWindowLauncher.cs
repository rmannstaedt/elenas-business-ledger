using System.Windows;
using MySmallBusinessLedger.App.ViewModels;
using MySmallBusinessLedger.App.Views;
using MySmallBusinessLedger.Core.Invoices;

namespace MySmallBusinessLedger.App.Services;

public sealed class InvoiceWorkflowWindowLauncher : IInvoiceWorkflowLauncher
{
    private readonly IInvoiceWorkflowService invoiceWorkflowService;
    private readonly IAppSettingsService settingsService;
    private readonly IUiClock clock;

    public InvoiceWorkflowWindowLauncher(
        IInvoiceWorkflowService invoiceWorkflowService,
        IAppSettingsService settingsService,
        IUiClock clock)
    {
        this.invoiceWorkflowService = invoiceWorkflowService;
        this.settingsService = settingsService;
        this.clock = clock;
    }

    public async Task<bool> ShowWriteInvoiceAsync(DateOnly displayMonth, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(clock.Now.DateTime);
        var candidateViewModel = new InvoiceCandidateDialogViewModel(invoiceWorkflowService, today, clock.Now);
        await candidateViewModel.LoadAsync();

        var candidateWindow = new InvoiceCandidateDialog(candidateViewModel)
        {
            Owner = Application.Current?.MainWindow
        };

        if (candidateWindow.ShowDialog() != true ||
            !candidateViewModel.WasConfirmed ||
            candidateViewModel.SelectedStudentShortName is null)
        {
            return false;
        }

        var month = new DateOnly(displayMonth.Year, displayMonth.Month, 1);
        var preview = await invoiceWorkflowService.CreatePreviewAsync(
            candidateViewModel.SelectedStudentShortName,
            today,
            month,
            month.AddMonths(1).AddDays(-1),
            cancellationToken);

        if (!preview.Succeeded)
        {
            ShowWarning(preview.Errors.FirstOrDefault()?.Message ?? "Could not create the invoice preview.");
            return false;
        }

        var previewViewModel = new InvoicePreviewViewModel(
            invoiceWorkflowService,
            preview.Value!,
            EnsureInvoicePrinterConfiguredAsync);
        var previewWindow = new InvoicePreviewView(previewViewModel)
        {
            Owner = Application.Current?.MainWindow
        };

        return previewWindow.ShowDialog() == true && previewViewModel.WasIssued;
    }

    public async Task ShowInvoiceViewerAsync(InvoiceNumber invoiceNumber, CancellationToken cancellationToken)
    {
        var invoice = await invoiceWorkflowService.GetIssuedInvoiceAsync(invoiceNumber, cancellationToken);
        if (invoice is null)
        {
            ShowWarning($"Invoice {invoiceNumber} could not be found.");
            return;
        }

        var viewModel = new InvoiceViewerViewModel(
            invoice,
            invoiceWorkflowService,
            EnsureInvoicePrinterConfiguredAsync);
        var window = new InvoiceViewerView(viewModel)
        {
            Owner = Application.Current?.MainWindow
        };
        window.ShowDialog();
    }

    private async Task<bool> EnsureInvoicePrinterConfiguredAsync(CancellationToken cancellationToken)
    {
        var snapshot = await settingsService.LoadAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(snapshot.CompanyProfile?.ConfiguredInvoicePrinterName))
        {
            return true;
        }

        var choice = MessageBox.Show(
            "No invoice printer is configured. Open Settings to choose one now?",
            "Invoice printer",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);
        if (choice != MessageBoxResult.Yes)
        {
            return false;
        }

        var viewModel = new SettingsViewModel(settingsService);
        await viewModel.LoadAsync();
        var window = new SettingsView(viewModel)
        {
            Owner = Application.Current?.MainWindow
        };
        window.ShowDialog();

        snapshot = await settingsService.LoadAsync(cancellationToken);
        return !string.IsNullOrWhiteSpace(snapshot.CompanyProfile?.ConfiguredInvoicePrinterName);
    }

    private static void ShowWarning(string message) =>
        MessageBox.Show(
            message,
            "Invoice",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
}
