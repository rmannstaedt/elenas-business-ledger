using MySmallBusinessLedger.Core.Invoices;
using MySmallBusinessLedger.Core.Lessons;

namespace MySmallBusinessLedger.Core.UseCases.Startup;

public sealed record DashboardLesson(
    string Id,
    string StudentShortName,
    DateTimeOffset StartsAt,
    LessonState State,
    bool IsInvoiced,
    PaymentStatus? PaymentStatus);
