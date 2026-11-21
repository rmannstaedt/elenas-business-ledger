using ElenasBusinessLedger.Core.Invoices;

namespace ElenasBusinessLedger.App.Services;

// Lets the dashboard open the issue, recover, or view invoice windows without constructing them itself.
public interface IInvoiceWorkflowLauncher
{
    Task<bool> ShowWriteInvoiceAsync(
        DateOnly displayMonth,
        string? initialStudentShortName,
        CancellationToken cancellationToken,
        int? studentId = null);

    Task<bool> ShowResumeInvoiceAsync(CancellationToken cancellationToken);

    Task ShowInvoiceViewerAsync(InvoiceNumber invoiceNumber, CancellationToken cancellationToken);
}
