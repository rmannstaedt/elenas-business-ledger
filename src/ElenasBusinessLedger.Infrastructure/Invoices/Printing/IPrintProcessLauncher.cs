namespace ElenasBusinessLedger.Infrastructure.Invoices.Printing;

public interface IPrintProcessLauncher
{
    Task PrintAsync(string pdfFilePath, string printerName, CancellationToken cancellationToken);
}
