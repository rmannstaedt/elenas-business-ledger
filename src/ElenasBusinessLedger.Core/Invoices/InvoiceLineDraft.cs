using MoneyAmount = ElenasBusinessLedger.Core.Money.Money;

namespace ElenasBusinessLedger.Core.Invoices;

// Holds the price and plan at preview time while retaining the lesson ID needed for the eventual billing lock.
public sealed class InvoiceLineDraft
{
    public InvoiceLineDraft(
        DateTimeOffset lessonDateTime,
        string planName,
        MoneyAmount priceWithoutVat,
        int planCatalogId = 0,
        int lessonId = 0,
        bool hasKnownLessonTime = true)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(planCatalogId, 0);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(lessonId, 0);

        LessonDateTime = lessonDateTime;
        HasKnownLessonTime = hasKnownLessonTime;
        PlanName = planName;
        PriceWithoutVat = priceWithoutVat;
        PlanCatalogId = planCatalogId;
        LessonId = lessonId;
    }

    public DateTimeOffset LessonDateTime { get; }

    public bool HasKnownLessonTime { get; }

    public string PlanName { get; }

    public MoneyAmount PriceWithoutVat { get; }

    public int PlanCatalogId { get; }

    public int LessonId { get; }
}
