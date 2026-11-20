using MySmallBusinessLedger.Core.Invoices;
using MySmallBusinessLedger.Core.Lessons;

namespace MySmallBusinessLedger.Core.UseCases.Startup;

public sealed record DashboardLesson(
    string Id,
    string StudentShortName,
    DateTimeOffset StartsAt,
    LessonState State,
    bool IsInvoiced,
    PaymentStatus? PaymentStatus,
    InvoiceNumber? InvoiceNumber = null,
    int PlanCatalogId = 0,
    string PlanName = "",
    int LocalLessonId = 0,
    bool HasKnownStartTime = true);
