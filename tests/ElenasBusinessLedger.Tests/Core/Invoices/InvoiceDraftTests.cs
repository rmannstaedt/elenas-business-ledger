using ElenasBusinessLedger.Core.Invoices;
using ElenasBusinessLedger.Core.Money;

namespace ElenasBusinessLedger.Tests.Core.Invoices;

// Verifies draft validation and the billing details captured when an invoice snapshot is created.
public sealed class InvoiceDraftTests
{
    [Fact]
    public void InvoiceLineDraftRequiresDurableLessonAndPlanIdentity()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new InvoiceLineDraft(
            lessonDateTime: new DateTimeOffset(2026, 6, 16, 15, 0, 0, TimeSpan.Zero),
            planName: "Tilbud 3",
            priceWithoutVat: Money.Dkk(400m),
            planCatalogId: 7,
            lessonId: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new InvoiceLineDraft(
            lessonDateTime: new DateTimeOffset(2026, 6, 16, 15, 0, 0, TimeSpan.Zero),
            planName: "Tilbud 3",
            priceWithoutVat: Money.Dkk(400m),
            planCatalogId: 0,
            lessonId: 123));
    }

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
                    priceWithoutVat: Money.Dkk(400m),
                    planCatalogId: 7,
                    lessonId: 123)
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
                    priceWithoutVat: Money.Dkk(400m),
                    planCatalogId: 7,
                    lessonId: 123)
            ],
            priceAdjustment: null);

        var invoice = draft.Issue(
            new InvoiceNumber(1),
            new InvoiceTotals(Money.Dkk(400m), Money.Dkk(0m), Money.Dkk(400m)),
            pdfFilePath: "C:\\Invoices\\Faktura_000001_Anna.pdf");

        Assert.Equal("001", invoice.InvoiceNumber.ToString());
        Assert.Equal("Anna", invoice.Student.ShortName);
        Assert.Equal("C:\\Invoices\\Faktura_000001_Anna.pdf", invoice.PdfFilePath);
        Assert.Equal(123, invoice.Lines[0].LessonId);
        Assert.Throws<NotSupportedException>(() => ((ICollection<IssuedInvoiceLine>)invoice.Lines).Add(
            new IssuedInvoiceLine(
                new DateTimeOffset(2026, 6, 23, 15, 0, 0, TimeSpan.Zero),
                "Tilbud 3",
                Money.Dkk(400m),
                planCatalogId: 7,
                hasKnownLessonTime: true,
                lessonId: 456)));
    }
}
