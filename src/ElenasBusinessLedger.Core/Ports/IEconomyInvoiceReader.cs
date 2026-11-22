using ElenasBusinessLedger.Core.Invoices;

namespace ElenasBusinessLedger.Core.Ports;

// Supplies issued invoice totals to Economy without exposing invoice issuance or payment writes.
public interface IEconomyInvoiceReader
{
    Task<IReadOnlyList<EconomyInvoiceRecord>> ListIssuedInvoiceOverviewRowsAsync(
        int periodYear,
        CancellationToken cancellationToken);
}
