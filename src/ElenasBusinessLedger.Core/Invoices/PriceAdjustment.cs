using MoneyAmount = ElenasBusinessLedger.Core.Money.Money;

namespace ElenasBusinessLedger.Core.Invoices;

public sealed record PriceAdjustment(MoneyAmount Amount, string Reason);
