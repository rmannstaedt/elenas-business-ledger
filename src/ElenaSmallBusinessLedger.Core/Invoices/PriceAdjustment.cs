using MoneyAmount = ElenaSmallBusinessLedger.Core.Money.Money;

namespace ElenaSmallBusinessLedger.Core.Invoices;

public sealed record PriceAdjustment(MoneyAmount Amount, string Reason);
