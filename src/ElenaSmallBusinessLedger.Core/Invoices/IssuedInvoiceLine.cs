using MoneyAmount = ElenaSmallBusinessLedger.Core.Money.Money;

namespace ElenaSmallBusinessLedger.Core.Invoices;

public sealed record IssuedInvoiceLine(DateTimeOffset LessonDateTime, string PlanName, MoneyAmount PriceWithoutVat);
