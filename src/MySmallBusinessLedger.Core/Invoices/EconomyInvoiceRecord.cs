namespace MySmallBusinessLedger.Core.Invoices;

public sealed record EconomyInvoiceRecord(
    InvoiceNumber InvoiceNumber,
    DateOnly InvoiceDate,
    InvoicePeriod Period,
    string PdfFilePath,
    InvoiceTotals Totals);
