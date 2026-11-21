using ElenasBusinessLedger.Core.Invoices;
using ElenasBusinessLedger.Core.Lessons;

namespace ElenasBusinessLedger.Core.UseCases.Startup;

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
    bool HasKnownStartTime = true,
    int? StudentId = null,
    bool NeedsCalendarReview = false,
    bool NeedsPlan = false);
