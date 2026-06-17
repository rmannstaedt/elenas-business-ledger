using MoneyAmount = MySmallBusinessLedger.Core.Money.Money;

namespace MySmallBusinessLedger.Core.Invoices;

public sealed record InvoiceTotals(MoneyAmount TotalWithoutVat, MoneyAmount VatAmount, MoneyAmount Total);
