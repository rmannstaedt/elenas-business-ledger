using ElenasBusinessLedger.Core.Invoices;
using ElenasBusinessLedger.Core.Students;

namespace ElenasBusinessLedger.App.Services;

// Lets the dashboard open the issue, recover, or view invoice windows without constructing them itself.
public interface IInvoiceWorkflowLauncher
{
    Task<bool> ShowWriteInvoiceAsync(
        DateOnly displayMonth,
        StudentChoice student,
        CancellationToken cancellationToken);

    Task<bool> ShowResumeInvoiceAsync(CancellationToken cancellationToken);

    Task ShowInvoiceViewerAsync(InvoiceNumber invoiceNumber, CancellationToken cancellationToken);
}
