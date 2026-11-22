namespace ElenasBusinessLedger.App.Services;

// Lets invoice views open an existing PDF without depending on the operating system's file launcher.
public interface IInvoicePdfLauncher
{
    Task OpenAsync(string pdfFilePath, CancellationToken cancellationToken);
}
