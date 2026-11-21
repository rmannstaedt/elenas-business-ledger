using ElenasBusinessLedger.Core.Invoices;

namespace ElenasBusinessLedger.Core.Lessons;

public sealed record LessonRecord(
    int Id,
    int StudentId,
    string StudentShortName,
    int PlanCatalogId,
    string PlanName,
    DateTimeOffset StartsAt,
    int DurationMinutes,
    LessonState State,
    string? GoogleCalendarEventId,
    InvoiceNumber? InvoiceNumber,
    bool HasKnownStartTime = true,
    CalendarMutationKind? PendingCalendarMutation = null);
