namespace ElenasBusinessLedger.Infrastructure.Invoices.Printing;

public interface IPrinterCatalog
{
    Task<IReadOnlyList<string>> ListPrinterNamesAsync(CancellationToken cancellationToken);
}
