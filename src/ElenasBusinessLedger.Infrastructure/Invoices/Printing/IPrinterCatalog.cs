namespace ElenasBusinessLedger.Infrastructure.Invoices.Printing;

// Supplies installed printer names so Settings and print validation use the same Windows choices.
public interface IPrinterCatalog
{
    Task<IReadOnlyList<string>> ListPrinterNamesAsync(CancellationToken cancellationToken);
}
