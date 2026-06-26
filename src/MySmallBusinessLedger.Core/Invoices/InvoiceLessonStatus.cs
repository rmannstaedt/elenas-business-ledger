namespace MySmallBusinessLedger.Core.Invoices;

public sealed record InvoiceLessonStatus(
    string StudentShortName,
    DateTimeOffset LessonDateTime,
    PaymentStatus PaymentStatus,
    InvoiceNumber? InvoiceNumber = null,
    int LessonId = 0);
