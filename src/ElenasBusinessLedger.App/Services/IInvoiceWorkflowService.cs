using ElenasBusinessLedger.Core.Common;
using ElenasBusinessLedger.Core.Invoices;
using ElenasBusinessLedger.Core.UseCases.Invoices;

namespace ElenasBusinessLedger.App.Services;

// Supplies invoice choices, preview totals, issuance, recovery, and reprint operations to the WPF views.
public interface IInvoiceWorkflowService
{
    Task<OperationResult<InvoiceCandidateSelectionResult>> LoadCandidatesAsync(
        DateOnly date,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task<OperationResult<InvoicePreviewResult>> CreatePreviewAsync(
        string studentShortName,
        DateOnly invoiceDate,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken,
        int? studentId = null);

    Task<OperationResult<InvoiceTotals>> CalculateTotalsAsync(
        InvoiceDraft draft,
        CancellationToken cancellationToken);

    Task<OperationResult<IssueInvoiceResult>> IssueAsync(
        InvoiceDraft draft,
        CancellationToken cancellationToken,
        Func<PendingInvoiceRecovery, CancellationToken, Task<InvoiceRecoveryChoice>>? chooseRecovery = null);

    Task<OperationResult<ReprintInvoiceResult>> ReprintAsync(
        InvoiceNumber invoiceNumber,
        CancellationToken cancellationToken);

    Task<IssuedInvoice?> GetOldestPendingAsync(CancellationToken cancellationToken);

    Task<OperationResult<IssueInvoiceResult>> ResumeAsync(
        InvoiceNumber invoiceNumber, CancellationToken cancellationToken);

    Task<OperationResult<InvoiceNumber>> DiscardPendingAsync(
        IssuedInvoice invoice, CancellationToken cancellationToken);

    Task<IssuedInvoice?> GetIssuedInvoiceAsync(
        InvoiceNumber invoiceNumber,
        CancellationToken cancellationToken);
}
