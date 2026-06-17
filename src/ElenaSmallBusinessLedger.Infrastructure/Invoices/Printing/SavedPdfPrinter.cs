using MySmallBusinessLedger.Core.Common;
using MySmallBusinessLedger.Core.Ports;

namespace MySmallBusinessLedger.Infrastructure.Invoices.Printing;

public sealed class SavedPdfPrinter : IInvoicePrinter
{
    private readonly IPrintProcessLauncher processLauncher;

    public SavedPdfPrinter(IPrintProcessLauncher processLauncher)
    {
        this.processLauncher = processLauncher;
    }

    public async Task<OperationResult<string>> PrintAsync(string pdfFilePath, CancellationToken cancellationToken)
    {
        if (!File.Exists(pdfFilePath))
        {
            return OperationResult<string>.Failure(new ValidationError(
                "InvoicePrinter.PdfMissing",
                "The invoice PDF was not found, so printing was not requested."));
        }

        try
        {
            await processLauncher.PrintAsync(pdfFilePath, cancellationToken);
            return OperationResult<string>.Success(pdfFilePath);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return OperationResult<string>.Failure(new ValidationError(
                "InvoicePrinter.PrintRequestFailed",
                "The invoice PDF was saved, but the print request failed."));
        }
    }
}
