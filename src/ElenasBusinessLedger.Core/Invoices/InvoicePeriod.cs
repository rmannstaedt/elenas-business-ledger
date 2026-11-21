namespace ElenasBusinessLedger.Core.Invoices;

// Represents the date range an invoice covers so billing and reporting agree on its period.
public sealed record InvoicePeriod(DateOnly From, DateOnly To)
{
    // Uses the earliest and latest lesson dates when a caller has not supplied a billing period.
    public static InvoicePeriod FromLines(IReadOnlyList<InvoiceLineDraft> lines)
    {
        var dates = lines.Select(line => DateOnly.FromDateTime(line.LessonDateTime.DateTime)).ToArray();
        return new InvoicePeriod(dates.Min(), dates.Max());
    }
}
