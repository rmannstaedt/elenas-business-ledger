using MoneyAmount = MySmallBusinessLedger.Core.Money.Money;

namespace MySmallBusinessLedger.Core.Invoices;

public sealed class InvoiceLineDraft
{
    public InvoiceLineDraft(DateTimeOffset lessonDateTime, string planName, MoneyAmount priceWithoutVat)
    {
        LessonDateTime = lessonDateTime;
        PlanName = planName;
        PriceWithoutVat = priceWithoutVat;
    }

    public DateTimeOffset LessonDateTime { get; }

    public string PlanName { get; }

    public MoneyAmount PriceWithoutVat { get; }
}
