using ElenasBusinessLedger.Core.Common;
using ElenasBusinessLedger.Core.Ports;

namespace ElenasBusinessLedger.Infrastructure.Invoices.Printing;

// Checks that the saved file and printer exist before submitting a Windows print request.
public sealed class SavedPdfPrinter : IInvoicePrinter
{
    private readonly IPrintProcessLauncher processLauncher;
    private readonly IPrinterCatalog printerCatalog;

    public SavedPdfPrinter(IPrintProcessLauncher processLauncher, IPrinterCatalog printerCatalog)
    {
        this.processLauncher = processLauncher;
        this.printerCatalog = printerCatalog;
    }

    // Checks that the PDF and configured printer exist before asking Windows to launch a print request.
    public async Task<OperationResult<string>> PrintAsync(
        string pdfFilePath,
        string configuredPrinterName,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(pdfFilePath))
        {
            return OperationResult<string>.Failure(new ValidationError(
                "InvoicePrinter.PdfMissing",
                "The invoice PDF was not found, so printing was not requested."));
        }

        if (string.IsNullOrWhiteSpace(configuredPrinterName))
        {
            return OperationResult<string>.Failure(new ValidationError(
                "InvoicePrinter.NotConfigured",
                "No invoice printer is configured."));
        }

        var printerName = configuredPrinterName.Trim();
        var printerNames = await printerCatalog.ListPrinterNamesAsync(cancellationToken);
        if (!printerNames.Any(name => string.Equals(name, printerName, StringComparison.OrdinalIgnoreCase)))
        {
            return OperationResult<string>.Failure(new ValidationError(
                "InvoicePrinter.Unavailable",
                "The configured invoice printer is not available."));
        }

        try
        {
            await processLauncher.PrintAsync(pdfFilePath, printerName, cancellationToken);
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
