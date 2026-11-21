using System.Windows;
using ElenasBusinessLedger.App.ViewModels;
using ElenasBusinessLedger.App.Views;
using ElenasBusinessLedger.Core.Invoices;

namespace ElenasBusinessLedger.App.Services;

public sealed class InvoiceWorkflowWindowLauncher : IInvoiceWorkflowLauncher
{
    private readonly IInvoiceWorkflowService invoiceWorkflowService;
    private readonly IInvoicePrinterConfigurationService printerConfigurationService;
    private readonly IUiClock clock;

    public InvoiceWorkflowWindowLauncher(
        IInvoiceWorkflowService invoiceWorkflowService,
        IInvoicePrinterConfigurationService printerConfigurationService,
        IUiClock clock)
    {
        this.invoiceWorkflowService = invoiceWorkflowService;
        this.printerConfigurationService = printerConfigurationService;
        this.clock = clock;
    }

    public async Task<bool> ShowWriteInvoiceAsync(
        DateOnly displayMonth,
        string? initialStudentShortName,
        CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(clock.Now.DateTime);
        var candidateViewModel = new InvoiceCandidateDialogViewModel(
            invoiceWorkflowService,
            today,
            clock.Now,
            initialStudentShortName);
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
            EnsureInvoicePrinterConfiguredAsync,
            ConfirmContinueWaitingForInvoiceIssueAsync);
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
        var result = await printerConfigurationService.EnsureConfiguredAsync(cancellationToken);
        if (!result.Succeeded)
        {
            ShowWarning(result.Errors.FirstOrDefault()?.Message ?? "The invoice printer could not be configured.");
            return false;
        }

        return result.Value;
    }

    private static Task<bool> ConfirmContinueWaitingForInvoiceIssueAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var result = MessageBox.Show(
            "The invoice is still being written and printed. The invoice window needs to stay open until the result is known. Keep waiting?",
            "Invoice",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question,
            MessageBoxResult.Yes);

        return Task.FromResult(result == MessageBoxResult.Yes);
    }

    private static void ShowWarning(string message) =>
        MessageBox.Show(
            message,
            "Invoice",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
}
