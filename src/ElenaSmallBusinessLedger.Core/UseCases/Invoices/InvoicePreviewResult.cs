using ElenaSmallBusinessLedger.Core.Invoices;

namespace ElenaSmallBusinessLedger.Core.UseCases.Invoices;

public sealed record InvoicePreviewResult(InvoiceDraft Draft, InvoiceTotals Totals);
