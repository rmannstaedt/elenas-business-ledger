using ElenasBusinessLedger.Core.Invoices;
using MoneyAmount = ElenasBusinessLedger.Core.Money.Money;

namespace ElenasBusinessLedger.Core.UseCases.Economy;

// Holds the selected year's invoice rows and summaries that the Economy screen and CSV export share.
public sealed record EconomyOverview(
    int Year,
    IReadOnlyList<EconomyOverviewInvoiceRow> Invoices,
    IReadOnlyList<EconomyOverviewMonthTotal> Months,
    IReadOnlyList<EconomyOverviewQuarterTotal> Quarters,
    EconomyOverviewYearToDateTotal YearToDate,
    IReadOnlyList<EconomyOverviewYearTotal> Years,
    DateOnly? FirstApplicationInvoiceMonth = null);

// Holds the issued amount, dates, and PDF reference for a row in Economy.
public sealed record EconomyOverviewInvoiceRow(
    InvoiceNumber InvoiceNumber,
    DateOnly InvoiceDate,
    InvoicePeriod Period,
    string PdfFilePath,
    string PdfFileName,
    MoneyAmount Net,
    MoneyAmount Vat,
    MoneyAmount Total);

// Carries a month's totals and source so Economy can distinguish manual, issued, and empty months.
public sealed record EconomyOverviewMonthTotal(
    int Month,
    bool HasInvoices,
    MoneyAmount? Net,
    MoneyAmount? Vat,
    MoneyAmount? Total,
    EconomyOverviewMonthSource Source = EconomyOverviewMonthSource.None,
    bool IsLegacyManualMonth = false);

// Holds quarter totals for display and export based on the report's month values.
public sealed record EconomyOverviewQuarterTotal(
    int Quarter,
    bool HasInvoices,
    MoneyAmount? Net,
    MoneyAmount? Vat,
    MoneyAmount? Total);

// Holds the aggregate shown for the selected reporting year.
public sealed record EconomyOverviewYearToDateTotal(
    MoneyAmount Net,
    MoneyAmount Vat,
    MoneyAmount Total);

// Supplies one annual summary for the multi-year section of the Economy report.
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
