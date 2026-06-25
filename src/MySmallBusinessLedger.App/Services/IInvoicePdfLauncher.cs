namespace MySmallBusinessLedger.App.Services;

public interface IInvoicePdfLauncher
{
    Task OpenAsync(string pdfFilePath, CancellationToken cancellationToken);
}
