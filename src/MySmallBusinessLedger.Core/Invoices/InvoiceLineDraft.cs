using MoneyAmount = MySmallBusinessLedger.Core.Money.Money;

namespace MySmallBusinessLedger.Core.Invoices;

public sealed class InvoiceLineDraft
{
    public InvoiceLineDraft(
        DateTimeOffset lessonDateTime,
        string planName,
        MoneyAmount priceWithoutVat,
        int planCatalogId = 0,
        int lessonId = 0)
    {
        LessonDateTime = lessonDateTime;
        PlanName = planName;
        PriceWithoutVat = priceWithoutVat;
        PlanCatalogId = planCatalogId;
        LessonId = lessonId;
    }

    public DateTimeOffset LessonDateTime { get; }

    public string PlanName { get; }

    public MoneyAmount PriceWithoutVat { get; }

    public int PlanCatalogId { get; }

    public int LessonId { get; }
}
