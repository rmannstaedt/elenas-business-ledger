using MySmallBusinessLedger.Core.Common;
using MySmallBusinessLedger.Core.Invoices;

namespace MySmallBusinessLedger.Core.Ports;

public interface IInvoiceFileStore
{
    Task<OperationResult<string>> CreateStagingPathAsync(
        InvoiceDraft draft,
        InvoiceNumber invoiceNumber,
        CancellationToken cancellationToken);

    Task<OperationResult<string>> VerifyStagedPdfAsync(string stagingPath, CancellationToken cancellationToken);

    Task<OperationResult<string>> PublishAsync(
        string stagingPath,
        InvoiceDraft draft,
        InvoiceNumber invoiceNumber,
        CancellationToken cancellationToken);

    Task<OperationResult<string>> DiscardStagingPathAsync(string stagingPath, CancellationToken cancellationToken);

    Task<OperationResult<string>> DiscardPublishedPathAsync(string publishedPath, CancellationToken cancellationToken);
}
