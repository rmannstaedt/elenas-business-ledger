using MoneyAmount = ElenasBusinessLedger.Core.Money.Money;

namespace ElenasBusinessLedger.Core.Invoices;

public sealed record IssuedInvoiceLine
{
    public IssuedInvoiceLine(
        DateTimeOffset lessonDateTime,
        string planName,
        MoneyAmount priceWithoutVat,
        int planCatalogId = 0,
        bool hasKnownLessonTime = true,
        int lessonId = 0)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(planCatalogId, 0);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(lessonId, 0);

        LessonDateTime = lessonDateTime;
        PlanName = planName;
        PriceWithoutVat = priceWithoutVat;
        PlanCatalogId = planCatalogId;
        HasKnownLessonTime = hasKnownLessonTime;
        LessonId = lessonId;
    }

    public DateTimeOffset LessonDateTime { get; }

    public string PlanName { get; }

    public MoneyAmount PriceWithoutVat { get; }

    public int PlanCatalogId { get; }

    public bool HasKnownLessonTime { get; }

    public int LessonId { get; }
}
