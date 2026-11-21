using ElenasBusinessLedger.Core.Common;
using ElenasBusinessLedger.Core.Invoices;

namespace ElenasBusinessLedger.App.Services;

public interface IInvoicePaymentStatusService
{
    Task<OperationResult<PaymentStatus>> ToggleAsync(
        InvoiceNumber invoiceNumber,
        bool confirmMarkOutstanding,
        CancellationToken cancellationToken);
}
