using ElenasBusinessLedger.Core.Invoices;

namespace ElenasBusinessLedger.Core.UseCases.Invoices;

// Returns the proposed draft and its calculated totals together so the preview shows a consistent starting point.
public sealed record InvoicePreviewResult(InvoiceDraft Draft, InvoiceTotals Totals);
