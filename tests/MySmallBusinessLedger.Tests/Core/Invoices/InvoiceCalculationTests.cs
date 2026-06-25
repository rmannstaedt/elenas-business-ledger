using MySmallBusinessLedger.Core.Company;
using MySmallBusinessLedger.Core.Invoices;
using MySmallBusinessLedger.Core.Money;

namespace MySmallBusinessLedger.Tests.Core.Invoices;

public sealed class InvoiceCalculationTests
{
    [Fact]
    public void CalculateTotalsDoesNotChargeVatWhenCompanyIsNotRegistered()
    {
        var company = CompanyProfile.Create(
            name: "Maple Key Studio",
            address: "Musikvej 1",
            phone: "12345678",
            email: "elena@example.test",
            cvr: null,
            vatRegistrationDate: null,
            vatStatus: VatStatus.NotRegistered,
            invoiceFolderPath: "C:\\Invoices");
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

        var totals = draft.CalculateTotals(company);

        Assert.Equal(Money.Dkk(400m), totals.TotalWithoutVat);
        Assert.Equal(Money.Dkk(0m), totals.VatAmount);
        Assert.Equal(Money.Dkk(400m), totals.Total);
    }

    [Fact]
    public void CalculateTotalsChargesVatWhenCompanyIsRegisteredAndInvoiceDateIsOnRegistrationDate()
    {
        var company = CompanyProfile.Create(
            name: "Maple Key Studio",
            address: "Musikvej 1",
            phone: "12345678",
            email: "elena@example.test",
            cvr: "12345678",
            vatRegistrationDate: new DateOnly(2026, 6, 16),
            vatStatus: VatStatus.Registered,
            invoiceFolderPath: "C:\\Invoices");
        var draft = InvoiceDraft.Create(
            invoiceDate: new DateOnly(2026, 6, 16),
            companySnapshot: new CompanySnapshot("Maple Key Studio", "Musikvej 1", "12345678"),
            studentSnapshot: new StudentSnapshot("Anna", "Anna Andersen", "Testvej 1"),
            lines:
            [
                new InvoiceLineDraft(
                    lessonDateTime: new DateTimeOffset(2026, 6, 16, 15, 0, 0, TimeSpan.Zero),
                    planName: "Tilbud 3",
                    priceWithoutVat: Money.Dkk(400m))
            ],
            priceAdjustment: null);

        var totals = draft.CalculateTotals(company);

        Assert.Equal(Money.Dkk(400m), totals.TotalWithoutVat);
        Assert.Equal(Money.Dkk(100m), totals.VatAmount);
        Assert.Equal(Money.Dkk(500m), totals.Total);
    }

    [Fact]
    public void CalculateTotalsChargesVatWhenInvoicePeriodStartsAfterRegistrationDateEvenIfInvoiceDateIsEarlier()
    {
        var company = CompanyProfile.Create(
            name: "Maple Key Studio",
            address: "Musikvej 1",
            phone: "12345678",
            email: "elena@example.test",
            cvr: "12345678",
            vatRegistrationDate: new DateOnly(2026, 7, 1),
            vatStatus: VatStatus.Registered,
            invoiceFolderPath: "C:\\Invoices");
        var draft = InvoiceDraft.Create(
            invoiceDate: new DateOnly(2026, 6, 26),
            companySnapshot: new CompanySnapshot("Maple Key Studio", "Musikvej 1", "12345678"),
            studentSnapshot: new StudentSnapshot("Anna", "Anna Andersen", "Testvej 1"),
            lines:
            [
                new InvoiceLineDraft(
                    lessonDateTime: new DateTimeOffset(2026, 8, 3, 15, 0, 0, TimeSpan.Zero),
                    planName: "Tilbud 3",
                    priceWithoutVat: Money.Dkk(400m))
            ],
            priceAdjustment: null,
            period: new InvoicePeriod(new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31)));

        var totals = draft.CalculateTotals(company);

        Assert.Equal(Money.Dkk(400m), totals.TotalWithoutVat);
        Assert.Equal(Money.Dkk(100m), totals.VatAmount);
        Assert.Equal(Money.Dkk(500m), totals.Total);
    }
}
