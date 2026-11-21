using MoneyAmount = ElenasBusinessLedger.Core.Money.Money;

namespace ElenasBusinessLedger.Core.Invoices;

// Pairs an invoice adjustment with its reason so the saved total can be explained later.
public sealed record PriceAdjustment(MoneyAmount Amount, string Reason);
