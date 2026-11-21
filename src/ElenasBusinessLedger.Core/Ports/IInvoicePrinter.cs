using ElenasBusinessLedger.Core.Common;

namespace ElenasBusinessLedger.Core.Ports;

public interface IInvoicePrinter
{
    Task<OperationResult<string>> PrintAsync(
        string pdfFilePath,
        string configuredPrinterName,
        CancellationToken cancellationToken);
}
