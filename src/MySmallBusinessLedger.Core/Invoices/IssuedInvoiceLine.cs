using MoneyAmount = MySmallBusinessLedger.Core.Money.Money;

namespace MySmallBusinessLedger.Core.Invoices;

public sealed record IssuedInvoiceLine(
    DateTimeOffset LessonDateTime,
    string PlanName,
    MoneyAmount PriceWithoutVat,
    int PlanCatalogId = 0,
    bool HasKnownLessonTime = true,
    int LessonId = 0);
