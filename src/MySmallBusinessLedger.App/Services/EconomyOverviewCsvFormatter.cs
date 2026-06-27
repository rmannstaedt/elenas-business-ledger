using System.Globalization;
using System.Text;
using MySmallBusinessLedger.Core.Money;
using MySmallBusinessLedger.Core.UseCases.Economy;

namespace MySmallBusinessLedger.App.Services;

public sealed class EconomyOverviewCsvFormatter
{
    public string Format(EconomyOverview overview)
    {
        var rows = new List<string[]>
        {
            new[] { "Section", "Year", "Label", "InvoiceNumber", "Filename", "Net", "VAT", "Total" }
        };

        rows.AddRange(overview.Invoices.Select(invoice => new[]
        {
            "Invoice",
            overview.Year.ToString(CultureInfo.InvariantCulture),
            string.Empty,
            invoice.InvoiceNumber.ToString(),
            invoice.PdfFileName,
            FormatMoney(invoice.Net),
            FormatMoney(invoice.Vat),
            FormatMoney(invoice.Total)
        }));
        rows.AddRange(overview.Months.Select(month => new[]
        {
            "Month",
            overview.Year.ToString(CultureInfo.InvariantCulture),
            $"{overview.Year:D4}-{month.Month:D2}",
            string.Empty,
            string.Empty,
            FormatNullableMoney(month.Net),
            FormatNullableMoney(month.Vat),
            FormatNullableMoney(month.Total)
        }));
        rows.AddRange(overview.Quarters.Select(quarter => new[]
        {
            "Quarter",
            overview.Year.ToString(CultureInfo.InvariantCulture),
            $"Q{quarter.Quarter}",
            string.Empty,
            string.Empty,
            FormatNullableMoney(quarter.Net),
            FormatNullableMoney(quarter.Vat),
            FormatNullableMoney(quarter.Total)
        }));
        rows.AddRange(overview.Years.Select(year => new[]
        {
            "Year",
            year.Year.ToString(CultureInfo.InvariantCulture),
            year.Year.ToString(CultureInfo.InvariantCulture),
            string.Empty,
            string.Empty,
            FormatNullableMoney(year.Net),
            FormatNullableMoney(year.Vat),
            FormatNullableMoney(year.Total)
        }));

        var builder = new StringBuilder();
        for (var index = 0; index < rows.Count; index++)
        {
            if (index > 0)
            {
                builder.AppendLine();
            }

            builder.Append(string.Join(",", rows[index].Select(Escape)));
        }

        return builder.ToString();
    }

    private static string FormatNullableMoney(Money? money) =>
        money is null ? string.Empty : FormatMoney(money.Value);

    private static string FormatMoney(Money money) =>
        money.Amount.ToString("0.00", CultureInfo.InvariantCulture);

    private static string Escape(string value)
    {
        if (!value.Contains(',') && !value.Contains('"') && !value.Contains('\r') && !value.Contains('\n'))
        {
            return value;
        }

        return $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
    }
}
