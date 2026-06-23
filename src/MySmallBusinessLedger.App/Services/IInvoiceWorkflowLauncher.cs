using MySmallBusinessLedger.Core.Invoices;

namespace MySmallBusinessLedger.App.Services;

public interface IInvoiceWorkflowLauncher
{
    Task<bool> ShowWriteInvoiceAsync(
        DateOnly displayMonth,
        string? initialStudentShortName,
        CancellationToken cancellationToken);

    Task ShowInvoiceViewerAsync(InvoiceNumber invoiceNumber, CancellationToken cancellationToken);
}
