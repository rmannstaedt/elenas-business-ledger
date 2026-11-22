using ElenasBusinessLedger.Core.Invoices;
using ElenasBusinessLedger.Core.Students;

namespace ElenasBusinessLedger.App.Services;

// Lets the dashboard open the issue, recover, or view invoice windows without constructing them itself.
public interface IInvoiceWorkflowLauncher
{
    // Requests a dashboard refresh after an issuance attempt or when a pending reservation blocks the preview.
    Task<bool> ShowWriteInvoiceAsync(
        DateOnly displayMonth,
        StudentChoice student,
        CancellationToken cancellationToken);

    // Returns whether recovery issued the invoice or discarded its reservation.
    Task<bool> ShowResumeInvoiceAsync(CancellationToken cancellationToken);

    Task ShowInvoiceViewerAsync(InvoiceNumber invoiceNumber, CancellationToken cancellationToken);
}
