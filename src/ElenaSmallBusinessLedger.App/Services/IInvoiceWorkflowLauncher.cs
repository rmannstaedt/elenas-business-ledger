using ElenaSmallBusinessLedger.Core.Invoices;

namespace ElenaSmallBusinessLedger.App.Services;

public interface IInvoiceWorkflowLauncher
{
    Task<bool> ShowWriteInvoiceAsync(DateOnly displayMonth, CancellationToken cancellationToken);

    Task ShowInvoiceViewerAsync(InvoiceNumber invoiceNumber, CancellationToken cancellationToken);
}
