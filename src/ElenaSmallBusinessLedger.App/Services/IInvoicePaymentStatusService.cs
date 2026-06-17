using MySmallBusinessLedger.Core.Common;
using MySmallBusinessLedger.Core.Invoices;

namespace MySmallBusinessLedger.App.Services;

public interface IInvoicePaymentStatusService
{
    Task<OperationResult<PaymentStatus>> ToggleAsync(
        InvoiceNumber invoiceNumber,
        bool confirmMarkOutstanding,
        CancellationToken cancellationToken);
}
