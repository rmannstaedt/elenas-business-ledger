using MySmallBusinessLedger.Core.Common;
using MySmallBusinessLedger.Core.Invoices;
using MySmallBusinessLedger.Core.UseCases.Invoices;

namespace MySmallBusinessLedger.App.Services;

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
        CancellationToken cancellationToken);

    Task<OperationResult<InvoiceTotals>> CalculateTotalsAsync(
        InvoiceDraft draft,
        CancellationToken cancellationToken);

    Task<OperationResult<IssueInvoiceResult>> IssueAsync(
        InvoiceDraft draft,
        CancellationToken cancellationToken);

    Task<OperationResult<ReprintInvoiceResult>> ReprintAsync(
        InvoiceNumber invoiceNumber,
        CancellationToken cancellationToken);

    Task<IssuedInvoice?> GetIssuedInvoiceAsync(
        InvoiceNumber invoiceNumber,
        CancellationToken cancellationToken);
}
