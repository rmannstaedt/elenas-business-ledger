using ElenasBusinessLedger.Core.Invoices;

namespace ElenasBusinessLedger.Core.UseCases.Invoices;

public sealed record InvoicePreviewResult(InvoiceDraft Draft, InvoiceTotals Totals);
