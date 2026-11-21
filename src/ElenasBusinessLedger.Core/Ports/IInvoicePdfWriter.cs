using ElenasBusinessLedger.Core.Common;
using ElenasBusinessLedger.Core.Invoices;

namespace ElenasBusinessLedger.Core.Ports;

// Renders a numbered billing snapshot to PDF through a contract independent of the rendering library.
public interface IInvoicePdfWriter
{
    Task<OperationResult<string>> WriteAsync(
        InvoiceDraft draft,
        InvoiceTotals totals,
        InvoiceNumber invoiceNumber,
        string stagingPath,
        CancellationToken cancellationToken);
}
