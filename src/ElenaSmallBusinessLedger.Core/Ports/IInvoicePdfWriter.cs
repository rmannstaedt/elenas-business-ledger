using MySmallBusinessLedger.Core.Common;
using MySmallBusinessLedger.Core.Invoices;

namespace MySmallBusinessLedger.Core.Ports;

public interface IInvoicePdfWriter
{
    Task<OperationResult<string>> WriteAsync(
        InvoiceDraft draft,
        InvoiceTotals totals,
        InvoiceNumber invoiceNumber,
        string stagingPath,
        CancellationToken cancellationToken);
}
