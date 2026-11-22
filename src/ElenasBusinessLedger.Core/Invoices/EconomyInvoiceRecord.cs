namespace ElenasBusinessLedger.Core.Invoices;

// Projects the issued totals and billing period that Economy uses for reporting from SQLite.
public sealed record EconomyInvoiceRecord(
    InvoiceNumber InvoiceNumber,
    DateOnly InvoiceDate,
    InvoicePeriod Period,
    string PdfFilePath,
    InvoiceTotals Totals);
