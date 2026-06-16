using ElenaSmallBusinessLedger.Core.Common;

namespace ElenaSmallBusinessLedger.Core.Ports;

public interface IInvoicePrinter
{
    Task<OperationResult<string>> PrintAsync(string pdfFilePath, CancellationToken cancellationToken);
}
