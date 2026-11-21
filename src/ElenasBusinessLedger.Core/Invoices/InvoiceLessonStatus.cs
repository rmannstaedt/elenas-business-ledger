namespace ElenasBusinessLedger.Core.Invoices;

// Connects a saved lesson to its issued invoice and payment state for dashboard display.
public sealed record InvoiceLessonStatus(
    string StudentShortName,
    DateTimeOffset LessonDateTime,
    PaymentStatus PaymentStatus,
    InvoiceNumber? InvoiceNumber = null,
    int LessonId = 0);
