namespace MySmallBusinessLedger.Infrastructure.Invoices.Printing;

public interface IPrinterCatalog
{
    Task<IReadOnlyList<string>> ListPrinterNamesAsync(CancellationToken cancellationToken);
}
