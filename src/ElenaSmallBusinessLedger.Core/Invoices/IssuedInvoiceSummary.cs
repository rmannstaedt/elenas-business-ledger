namespace ElenaSmallBusinessLedger.Core.Invoices;

public sealed record IssuedInvoiceSummary(
    InvoiceNumber InvoiceNumber,
    string StudentShortName,
    DateOnly InvoiceDate,
    PaymentStatus PaymentStatus,
    string PdfFilePath);
