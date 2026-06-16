using System.Windows;
using ElenaSmallBusinessLedger.App.ViewModels;
using ElenaSmallBusinessLedger.App.Views;
using ElenaSmallBusinessLedger.Core.Invoices;

namespace ElenaSmallBusinessLedger.App.Services;

public sealed class InvoiceWorkflowWindowLauncher : IInvoiceWorkflowLauncher
{
    private readonly IInvoiceWorkflowService invoiceWorkflowService;
    private readonly IUiClock clock;

    public InvoiceWorkflowWindowLauncher(IInvoiceWorkflowService invoiceWorkflowService, IUiClock clock)
    {
        this.invoiceWorkflowService = invoiceWorkflowService;
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

        var previewViewModel = new InvoicePreviewViewModel(invoiceWorkflowService, preview.Value!);
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

        var viewModel = new InvoiceViewerViewModel(invoice);
        var window = new InvoiceViewerView(viewModel)
        {
            Owner = Application.Current?.MainWindow
        };
        window.ShowDialog();
    }

    private static void ShowWarning(string message) =>
        MessageBox.Show(
            message,
            "Invoice",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
}
