namespace ElenasBusinessLedger.Core.Invoices;

// Supplies invoice history and status to lists that do not need the full billing snapshot.
public sealed record IssuedInvoiceSummary(
    InvoiceNumber InvoiceNumber,
    string StudentShortName,
    DateOnly InvoiceDate,
    PaymentStatus PaymentStatus,
    string PdfFilePath,
    InvoiceIssueStatus IssueStatus = InvoiceIssueStatus.Issued,
    InvoicePrintStatus PrintStatus = InvoicePrintStatus.NotPrinted,
    DateTimeOffset? PrintedAt = null,
    string? LastPrintError = null,
    InvoicePeriod? Period = null);
