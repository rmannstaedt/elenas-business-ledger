using ElenasBusinessLedger.Core.Invoices;
using MoneyAmount = ElenasBusinessLedger.Core.Money.Money;

namespace ElenasBusinessLedger.Core.UseCases.Economy;

public sealed record EconomyOverview(
    int Year,
    IReadOnlyList<EconomyOverviewInvoiceRow> Invoices,
    IReadOnlyList<EconomyOverviewMonthTotal> Months,
    IReadOnlyList<EconomyOverviewQuarterTotal> Quarters,
    EconomyOverviewYearToDateTotal YearToDate,
    IReadOnlyList<EconomyOverviewYearTotal> Years,
    DateOnly? FirstApplicationInvoiceMonth = null);

public sealed record EconomyOverviewInvoiceRow(
    InvoiceNumber InvoiceNumber,
    DateOnly InvoiceDate,
    InvoicePeriod Period,
    string PdfFilePath,
    string PdfFileName,
    MoneyAmount Net,
    MoneyAmount Vat,
    MoneyAmount Total);

public sealed record EconomyOverviewMonthTotal(
    int Month,
    bool HasInvoices,
    MoneyAmount? Net,
    MoneyAmount? Vat,
    MoneyAmount? Total,
    EconomyOverviewMonthSource Source = EconomyOverviewMonthSource.None,
    bool IsLegacyManualMonth = false);

public sealed record EconomyOverviewQuarterTotal(
    int Quarter,
    bool HasInvoices,
    MoneyAmount? Net,
    MoneyAmount? Vat,
    MoneyAmount? Total);

public sealed record EconomyOverviewYearToDateTotal(
    MoneyAmount Net,
    MoneyAmount Vat,
    MoneyAmount Total);

public sealed record EconomyOverviewYearTotal(
    int Year,
    bool HasInvoices,
    MoneyAmount? Net,
    MoneyAmount? Vat,
    MoneyAmount? Total);

public enum EconomyOverviewMonthSource
{
    None,
    IssuedInvoices,
    ManualOverride
}
