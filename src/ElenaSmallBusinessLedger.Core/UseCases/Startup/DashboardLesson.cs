using ElenaSmallBusinessLedger.Core.Invoices;
using ElenaSmallBusinessLedger.Core.Lessons;

namespace ElenaSmallBusinessLedger.Core.UseCases.Startup;

public sealed record DashboardLesson(
    string Id,
    string StudentShortName,
    DateTimeOffset StartsAt,
    LessonState State,
    bool IsInvoiced,
    PaymentStatus? PaymentStatus);
