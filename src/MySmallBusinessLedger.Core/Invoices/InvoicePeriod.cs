namespace MySmallBusinessLedger.Core.Invoices;

public sealed record InvoicePeriod(DateOnly From, DateOnly To)
{
    public static InvoicePeriod FromLines(IReadOnlyList<InvoiceLineDraft> lines)
    {
        var dates = lines.Select(line => DateOnly.FromDateTime(line.LessonDateTime.DateTime)).ToArray();
        return new InvoicePeriod(dates.Min(), dates.Max());
    }
}
