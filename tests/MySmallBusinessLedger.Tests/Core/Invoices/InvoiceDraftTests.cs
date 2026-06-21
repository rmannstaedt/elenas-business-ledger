using MySmallBusinessLedger.Core.Invoices;
using MySmallBusinessLedger.Core.Money;

namespace MySmallBusinessLedger.Tests.Core.Invoices;

public sealed class InvoiceDraftTests
{
    [Fact]
    public void CreateRejectsPriceAdjustmentWithoutReason()
    {
        var result = InvoiceDraft.TryCreate(
            invoiceDate: new DateOnly(2026, 6, 16),
            companySnapshot: new CompanySnapshot("Maple Key Studio", "Musikvej 1", null),
            studentSnapshot: new StudentSnapshot("Anna", "Anna Andersen", "Testvej 1"),
            lines:
            [
                new InvoiceLineDraft(
                    lessonDateTime: new DateTimeOffset(2026, 6, 16, 15, 0, 0, TimeSpan.Zero),
                    planName: "Tilbud 3",
                    priceWithoutVat: Money.Dkk(400m))
            ],
            priceAdjustment: new PriceAdjustment(Money.Dkk(-50m), ""));

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, error => error.Code == "Invoice.PriceAdjustmentReasonRequired");
    }

    [Fact]
    public void CreateRejectsInvoiceWithoutLessonLines()
    {
        var result = InvoiceDraft.TryCreate(
            invoiceDate: new DateOnly(2026, 6, 16),
            companySnapshot: new CompanySnapshot("Maple Key Studio", "Musikvej 1", null),
            studentSnapshot: new StudentSnapshot("Anna", "Anna Andersen", "Testvej 1"),
            lines: [],
            priceAdjustment: null);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, error => error.Code == "Invoice.NoLines");
    }

    [Fact]
    public void IssueCreatesImmutableInvoiceSnapshot()
    {
        var draft = InvoiceDraft.Create(
            invoiceDate: new DateOnly(2026, 6, 16),
            companySnapshot: new CompanySnapshot("Maple Key Studio", "Musikvej 1", null),
            studentSnapshot: new StudentSnapshot("Anna", "Anna Andersen", "Testvej 1"),
            lines:
            [
                new InvoiceLineDraft(
                    lessonDateTime: new DateTimeOffset(2026, 6, 16, 15, 0, 0, TimeSpan.Zero),
                    planName: "Tilbud 3",
                    priceWithoutVat: Money.Dkk(400m))
            ],
            priceAdjustment: null);

        var invoice = draft.Issue(
            new InvoiceNumber(1),
            new InvoiceTotals(Money.Dkk(400m), Money.Dkk(0m), Money.Dkk(400m)),
            pdfFilePath: "C:\\Invoices\\Faktura_000001_Anna.pdf");

        Assert.Equal("000001", invoice.InvoiceNumber.ToString());
        Assert.Equal("Anna", invoice.Student.ShortName);
        Assert.Equal("C:\\Invoices\\Faktura_000001_Anna.pdf", invoice.PdfFilePath);
        Assert.Throws<NotSupportedException>(() => ((ICollection<IssuedInvoiceLine>)invoice.Lines).Add(
            new IssuedInvoiceLine(
                new DateTimeOffset(2026, 6, 23, 15, 0, 0, TimeSpan.Zero),
                "Tilbud 3",
                Money.Dkk(400m))));
    }
}
