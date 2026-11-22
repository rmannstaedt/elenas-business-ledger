using ElenasBusinessLedger.Core.Common;
using ElenasBusinessLedger.Core.Invoices;

namespace ElenasBusinessLedger.Core.Ports;

// Defines staging, recovery, and publication operations for PDFs so issuance can verify the artifact across retries.
public interface IInvoiceFileStore
{
    Task<OperationResult<string>> CreateStagingPathAsync(
        InvoiceDraft draft,
        InvoiceNumber invoiceNumber,
        CancellationToken cancellationToken);

    Task<OperationResult<string>> VerifyStagedPdfAsync(string stagingPath, CancellationToken cancellationToken);

    Task<OperationResult<InvoicePdfPublication>> PreparePublicationAsync(
        string stagingPath, InvoiceDraft draft, InvoiceNumber invoiceNumber, CancellationToken cancellationToken);

    Task<OperationResult<string>> CompletePublicationAsync(
        InvoicePdfPublication publication, CancellationToken cancellationToken);

    Task<OperationResult<string>> DiscardStagingPathAsync(string stagingPath, CancellationToken cancellationToken);

    Task<OperationResult<string>> DiscardPublishedPathAsync(string publishedPath, CancellationToken cancellationToken);
}
