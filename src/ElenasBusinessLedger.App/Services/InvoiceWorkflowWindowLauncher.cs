using System.Windows;
using ElenasBusinessLedger.App.ViewModels;
using ElenasBusinessLedger.App.Views;
using ElenasBusinessLedger.Core.Invoices;
using ElenasBusinessLedger.Core.UseCases.Invoices;

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
        CancellationToken cancellationToken,
        int? studentId = null)
    {
        var today = DateOnly.FromDateTime(clock.Now.DateTime);
        var candidateViewModel = new InvoiceCandidateDialogViewModel(
            invoiceWorkflowService,
            today,
            clock.Now,
            initialStudentShortName,
            studentId);
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
            cancellationToken,
            candidateViewModel.SelectedStudent?.StudentId is > 0 ? candidateViewModel.SelectedStudent.StudentId : null);

        if (!preview.Succeeded)
        {
            ShowWarning(preview.Errors.FirstOrDefault()?.Message ?? "Could not create the invoice preview.");
            return false;
        }

        var previewViewModel = new InvoicePreviewViewModel(
            invoiceWorkflowService,
            preview.Value!,
            EnsureInvoicePrinterConfiguredAsync,
            ConfirmContinueWaitingForInvoiceIssueAsync,
            chooseRecovery: ChooseInvoiceRecoveryAsync);
        var previewWindow = new InvoicePreviewView(previewViewModel)
        {
            Owner = Application.Current?.MainWindow
        };

        previewWindow.ShowDialog();
        return previewViewModel.WasIssued;
    }

    public async Task<bool> ShowResumeInvoiceAsync(CancellationToken cancellationToken)
    {
        try
        {
            var invoice = await invoiceWorkflowService.GetOldestPendingAsync(cancellationToken);
            if (invoice is null)
            {
                ShowWarning("There is no pending invoice.");
                return false;
            }

            var canDiscard = invoice.Publication is null && invoice.PdfFilePath is null;
            var selection = new PendingInvoiceRecovery(invoice, RequestedDraft: null, invoice.Totals, CanRevise: false);
            var dialog = new InvoiceRecoveryDialog(selection, directRecovery: true, canDiscard)
            {
                Owner = Application.Current?.MainWindow,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            dialog.ShowDialog();
            if (dialog.Choice == InvoiceRecoveryChoice.Discard)
            {
                var result = await invoiceWorkflowService.DiscardPendingAsync(invoice, cancellationToken);
                if (!result.Succeeded)
                {
                    ShowWarning(result.Errors[0].Message);
                }

                return false;
            }

            if (dialog.Choice != InvoiceRecoveryChoice.Resume || !await EnsureInvoicePrinterConfiguredAsync(cancellationToken))
            {
                return false;
            }

            var issued = await invoiceWorkflowService.ResumeAsync(invoice.InvoiceNumber, cancellationToken);
            if (!issued.Succeeded)
            {
                ShowWarning(issued.Errors[0].Message);
                return false;
            }

            if (!issued.Value!.PrintSucceeded || issued.Value.PrintErrors.Count > 0)
            {
                ShowWarning(issued.Value.PrintErrors.FirstOrDefault()?.Message ?? "The invoice was saved, but printing failed.");
            }

            return true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            ShowWarning(ex.Message);
            return false;
        }
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

    private static Task<InvoiceRecoveryChoice> ChooseInvoiceRecoveryAsync(
        PendingInvoiceRecovery recovery, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var dialog = new InvoiceRecoveryDialog(recovery);
        var owner = Application.Current?.Windows.OfType<Window>().FirstOrDefault(window => window.IsActive);
        if (owner is not null)
        {
            dialog.Owner = owner;
            dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        }

        dialog.ShowDialog();
        return Task.FromResult(dialog.Choice);
    }

    private static void ShowWarning(string message) =>
        MessageBox.Show(
            message,
            "Invoice",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
}
