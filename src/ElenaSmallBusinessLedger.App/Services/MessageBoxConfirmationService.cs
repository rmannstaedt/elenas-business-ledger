using System.Windows;
using ElenaSmallBusinessLedger.App.ViewModels;

namespace ElenaSmallBusinessLedger.App.Services;

public sealed class MessageBoxConfirmationService : IConfirmationService
{
    public Task<bool> ConfirmMarkInvoiceOutstandingAsync(
        InvoiceDisplayModel invoice,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var result = MessageBox.Show(
            $"Mark invoice {invoice.InvoiceNumberText} as outstanding?",
            "Confirm payment status",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question,
            MessageBoxResult.No);

        return Task.FromResult(result == MessageBoxResult.Yes);
    }
}
