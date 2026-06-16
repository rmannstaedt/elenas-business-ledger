using MoneyAmount = ElenaSmallBusinessLedger.Core.Money.Money;

namespace ElenaSmallBusinessLedger.Core.Invoices;

public sealed record InvoiceTotals(MoneyAmount TotalWithoutVat, MoneyAmount VatAmount, MoneyAmount Total);
