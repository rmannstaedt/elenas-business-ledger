using MoneyAmount = ElenasBusinessLedger.Core.Money.Money;

namespace ElenasBusinessLedger.Core.Invoices;

public sealed record InvoiceTotals(MoneyAmount TotalWithoutVat, MoneyAmount VatAmount, MoneyAmount Total);
