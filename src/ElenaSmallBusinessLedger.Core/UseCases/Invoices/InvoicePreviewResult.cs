using MySmallBusinessLedger.Core.Invoices;

namespace MySmallBusinessLedger.Core.UseCases.Invoices;

public sealed record InvoicePreviewResult(InvoiceDraft Draft, InvoiceTotals Totals);
