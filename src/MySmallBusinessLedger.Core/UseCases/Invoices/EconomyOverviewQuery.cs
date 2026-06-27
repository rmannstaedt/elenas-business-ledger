using MySmallBusinessLedger.Core.Invoices;
using MySmallBusinessLedger.Core.Ports;
using MoneyAmount = MySmallBusinessLedger.Core.Money.Money;

namespace MySmallBusinessLedger.Core.UseCases.Economy;

public sealed class EconomyOverviewQuery
{
    private readonly IInvoiceRepository invoiceRepository;

    public EconomyOverviewQuery(IInvoiceRepository invoiceRepository)
    {
        this.invoiceRepository = invoiceRepository;
    }

    public async Task<EconomyOverview> LoadAsync(int year, CancellationToken cancellationToken)
    {
        var records = await invoiceRepository.ListIssuedInvoiceOverviewRowsAsync(year, cancellationToken);
        var orderedRecords = records
            .OrderBy(record => record.InvoiceNumber.Value)
            .ToArray();
        var invoices = orderedRecords
            .Select(record => new EconomyOverviewInvoiceRow(
                record.InvoiceNumber,
                record.InvoiceDate,
                record.Period,
                record.PdfFilePath,
                GetFileName(record.PdfFilePath),
                record.Totals.TotalWithoutVat,
                record.Totals.VatAmount,
                record.Totals.Total))
            .ToArray();
        var months = Enumerable.Range(1, 12)
            .Select(month => CreateMonthTotal(
                month,
                orderedRecords.Where(record => record.Period.From.Month == month).ToArray()))
            .ToArray();
        var quarters = Enumerable.Range(1, 4)
            .Select(quarter => CreateQuarterTotal(
                quarter,
                orderedRecords.Where(record => GetQuarter(record.Period.From.Month) == quarter).ToArray()))
            .ToArray();
        var yearToDate = CreateYearToDateTotal(orderedRecords);

        return new EconomyOverview(year, invoices, months, quarters, yearToDate);
    }

    private static EconomyOverviewMonthTotal CreateMonthTotal(int month, IReadOnlyList<EconomyInvoiceRecord> records)
    {
        var totals = Sum(records);
        return new EconomyOverviewMonthTotal(
            month,
            records.Count > 0,
            totals?.Net,
            totals?.Vat,
            totals?.Total);
    }

    private static EconomyOverviewQuarterTotal CreateQuarterTotal(int quarter, IReadOnlyList<EconomyInvoiceRecord> records)
    {
        var totals = Sum(records);
        return new EconomyOverviewQuarterTotal(
            quarter,
            records.Count > 0,
            totals?.Net,
            totals?.Vat,
            totals?.Total);
    }

    private static EconomyOverviewYearToDateTotal CreateYearToDateTotal(IReadOnlyList<EconomyInvoiceRecord> records)
    {
        var totals = Sum(records);
        return totals is null
            ? new EconomyOverviewYearToDateTotal(MoneyAmount.Dkk(0m), MoneyAmount.Dkk(0m), MoneyAmount.Dkk(0m))
            : new EconomyOverviewYearToDateTotal(totals.Net, totals.Vat, totals.Total);
    }

    private static EconomyOverviewYearToDateTotal? Sum(IReadOnlyList<EconomyInvoiceRecord> records)
    {
        if (records.Count == 0)
        {
            return null;
        }

        var net = records[0].Totals.TotalWithoutVat;
        var vat = records[0].Totals.VatAmount;
        var total = records[0].Totals.Total;
        foreach (var record in records.Skip(1))
        {
            net += record.Totals.TotalWithoutVat;
            vat += record.Totals.VatAmount;
            total += record.Totals.Total;
        }

        return new EconomyOverviewYearToDateTotal(net, vat, total);
    }

    private static int GetQuarter(int month) =>
        ((month - 1) / 3) + 1;

    private static string GetFileName(string path)
    {
        var normalized = path.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
        return Path.GetFileName(normalized);
    }
}
