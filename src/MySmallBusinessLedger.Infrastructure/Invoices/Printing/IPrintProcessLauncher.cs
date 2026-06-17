namespace MySmallBusinessLedger.Infrastructure.Invoices.Printing;

public interface IPrintProcessLauncher
{
    Task PrintAsync(string pdfFilePath, CancellationToken cancellationToken);
}
