using ElenasBusinessLedger.Core.Invoices;
using ElenasBusinessLedger.Core.Money;
using ElenasBusinessLedger.Infrastructure.Invoices.Pdf;

namespace ElenasBusinessLedger.Tests.Infrastructure.Invoices;

public sealed class InvoicePdfDocumentModelTests
{
    [Fact]
    public void CreateGroupsLessonsByPlanAndUsesDraftInvoicePeriod()
    {
        var draft = InvoiceDraft.Create(
            new DateOnly(2026, 6, 23),
            new CompanySnapshot(
                "Maple Key Studio",
                "18 Example Lane, 0000 Example City",
                "12345678",
                "Betales ved modtagelse.",
                "1234",
                "5678901234",
                "MobilePay 5678"),
            new StudentSnapshot("Mira", "Mira Vale", "42 Sample Lane" + Environment.NewLine + "0000 Example City"),
            [
                CreateLine(2026, 6, 1, "Tilbud 2 - 2026: 2 lektioner/uge"),
                CreateLine(2026, 6, 4, "Tilbud 2 - 2026: 2 lektioner/uge"),
                CreateLine(2026, 6, 8, "Tilbud 2 - 2026: 2 lektioner/uge"),
                CreateLine(2026, 6, 11, "Tilbud 2 - 2026: 2 lektioner/uge"),
                CreateLine(2026, 6, 15, "Tilbud 2 - 2026: 2 lektioner/uge"),
                CreateLine(2026, 6, 18, "Tilbud 2 - 2026: 2 lektioner/uge"),
                CreateLine(2026, 6, 25, "Tilbud 2 - 2026: 2 lektioner/uge"),
                CreateLine(2026, 6, 29, "Tilbud 2 - 2026: 2 lektioner/uge")
            ],
            priceAdjustment: null,
            period: new InvoicePeriod(new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 30)));
        var totals = new InvoiceTotals(Money.Dkk(2800m), Money.Dkk(0m), Money.Dkk(2800m));

        var model = InvoicePdfDocumentModel.Create(draft, totals, new InvoiceNumber(1));

        Assert.Equal("001", model.InvoiceNumber);
        Assert.Equal("Juni 2026", model.Period);
        Assert.Equal("Betales ved modtagelse.", model.PaymentInstructions);
        Assert.Equal("1234", model.BankRegistrationNumber);
        Assert.Equal("5678901234", model.BankAccountNumber);
        Assert.Equal("MobilePay 5678", model.MobilePayAccount);
        Assert.Equal(["18 Example Lane", "0000 Example City"], model.CompanyAddressLines);
        var service = Assert.Single(model.Services);
        Assert.Equal("Klaverundervisning - Tilbud 2", service.Description);
        Assert.Equal("Individuel undervisning, 2 lektioner pr. uge", service.Detail);
        Assert.Equal(8, service.Quantity);
        Assert.Equal("lektioner", service.Unit);
        Assert.Equal(Money.Dkk(350m), service.UnitPriceWithoutVat);
        Assert.Equal(Money.Dkk(2800m), service.TotalWithoutVat);
        Assert.Equal(
            [
                "01-06-2026 14:00",
                "04-06-2026 14:00",
                "08-06-2026 14:00",
                "11-06-2026 14:00",
                "15-06-2026 14:00",
                "18-06-2026 14:00",
                "25-06-2026 14:00",
                "29-06-2026 14:00"
            ],
            model.IncludedLessons);
        Assert.Empty(model.PlanChangeNotes);
    }

    [Fact]
    public void CreateAddsCutoverNoteWhenInvoiceCoversMultiplePlans()
    {
        var draft = InvoiceDraft.Create(
            new DateOnly(2026, 6, 23),
            new CompanySnapshot("Maple Key Studio", "18 Example Lane", null, ""),
            new StudentSnapshot("Mira", "Mira Vale", "42 Sample Lane"),
            [
                CreateLine(2026, 6, 1, "Tilbud 1 - 2026: 1 lektion/uge"),
                CreateLine(2026, 6, 8, "Tilbud 1 - 2026: 1 lektion/uge"),
                CreateLine(2026, 6, 15, "Tilbud 2 - 2026: 2 lektioner/uge"),
                CreateLine(2026, 6, 18, "Tilbud 2 - 2026: 2 lektioner/uge")
            ],
            priceAdjustment: null);
        var totals = new InvoiceTotals(Money.Dkk(1400m), Money.Dkk(0m), Money.Dkk(1400m));

        var model = InvoicePdfDocumentModel.Create(draft, totals, new InvoiceNumber(2));

        Assert.Equal("Juni 2026", model.Period);
        Assert.Equal(2, model.Services.Count);
        Assert.Equal(
            "Plan skiftede fra Tilbud 1 - 2026: 1 lektion/uge til Tilbud 2 - 2026: 2 lektioner/uge den 15-06-2026.",
            Assert.Single(model.PlanChangeNotes));
    }

    private static InvoiceLineDraft CreateLine(int year, int month, int day, string planName) =>
        new(
            new DateTimeOffset(year, month, day, 14, 0, 0, TimeSpan.FromHours(2)),
            planName,
            Money.Dkk(350m),
            planName.Contains('1', StringComparison.Ordinal) ? 1 : 2,
            year * 10000 + month * 100 + day);
}
