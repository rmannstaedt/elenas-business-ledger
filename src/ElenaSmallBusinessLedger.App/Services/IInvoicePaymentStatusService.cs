using ElenaSmallBusinessLedger.Core.Common;
using ElenaSmallBusinessLedger.Core.Invoices;

namespace ElenaSmallBusinessLedger.App.Services;

public interface IInvoicePaymentStatusService
{
    Task<OperationResult<PaymentStatus>> ToggleAsync(
        InvoiceNumber invoiceNumber,
        bool confirmMarkOutstanding,
        CancellationToken cancellationToken);
}
