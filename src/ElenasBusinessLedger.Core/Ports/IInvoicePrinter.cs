using ElenasBusinessLedger.Core.Common;

namespace ElenasBusinessLedger.Core.Ports;

// Submits a saved PDF to a printer so issuing and reprinting can record the request outcome.
public interface IInvoicePrinter
{
    Task<OperationResult<string>> PrintAsync(
        string pdfFilePath,
        string configuredPrinterName,
        CancellationToken cancellationToken);
}
