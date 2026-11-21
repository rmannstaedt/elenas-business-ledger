using ElenasBusinessLedger.Core.Common;
using ElenasBusinessLedger.Core.Invoices;

namespace ElenasBusinessLedger.Core.Ports;

public interface IInvoicePdfWriter
{
    Task<OperationResult<string>> WriteAsync(
        InvoiceDraft draft,
        InvoiceTotals totals,
        InvoiceNumber invoiceNumber,
        string stagingPath,
        CancellationToken cancellationToken);
}
