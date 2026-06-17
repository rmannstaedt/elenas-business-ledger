using MoneyAmount = MySmallBusinessLedger.Core.Money.Money;

namespace MySmallBusinessLedger.Core.Invoices;

public sealed record PriceAdjustment(MoneyAmount Amount, string Reason);
