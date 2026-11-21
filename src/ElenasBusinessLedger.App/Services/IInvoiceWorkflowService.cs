using ElenasBusinessLedger.Core.Common;
using ElenasBusinessLedger.Core.Invoices;
using ElenasBusinessLedger.Core.UseCases.Invoices;

namespace ElenasBusinessLedger.App.Services;

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
