using ElenasBusinessLedger.Core.Common;
using ElenasBusinessLedger.Core.Invoices;

namespace ElenasBusinessLedger.App.Services;

// Exposes payment changes and their confirmation results to the dashboard.
public interface IInvoicePaymentStatusService
{
    Task<OperationResult<PaymentStatus>> ToggleAsync(
        InvoiceNumber invoiceNumber,
        bool confirmMarkOutstanding,
        CancellationToken cancellationToken);
}
