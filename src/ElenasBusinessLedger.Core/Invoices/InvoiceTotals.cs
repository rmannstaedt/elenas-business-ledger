using MoneyAmount = ElenasBusinessLedger.Core.Money.Money;

namespace ElenasBusinessLedger.Core.Invoices;

// Carries net, VAT, and gross amounts together from calculation through issuance and reporting.
public sealed record InvoiceTotals(MoneyAmount TotalWithoutVat, MoneyAmount VatAmount, MoneyAmount Total);
