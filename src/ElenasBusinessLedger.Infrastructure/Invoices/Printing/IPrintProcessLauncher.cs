namespace ElenasBusinessLedger.Infrastructure.Invoices.Printing;

// Defines how the printer adapter hands a PDF and target printer to the operating system.
public interface IPrintProcessLauncher
{
    Task PrintAsync(string pdfFilePath, string printerName, CancellationToken cancellationToken);
}
