using ElenaSmallBusinessLedger.App.ViewModels;

namespace ElenaSmallBusinessLedger.App.Services;

public interface IConfirmationService
{
    Task<bool> ConfirmMarkInvoiceOutstandingAsync(
        InvoiceDisplayModel invoice,
        CancellationToken cancellationToken);
}
