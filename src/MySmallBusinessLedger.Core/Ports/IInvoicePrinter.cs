using MySmallBusinessLedger.Core.Common;

namespace MySmallBusinessLedger.Core.Ports;

public interface IInvoicePrinter
{
    Task<OperationResult<string>> PrintAsync(
        string pdfFilePath,
        string configuredPrinterName,
        CancellationToken cancellationToken);
}
