using ElenasBusinessLedger.Core.Invoices;

namespace ElenasBusinessLedger.Core.Lessons;

// Stores the lesson's student, plan, and calendar link so billing can use the local record.
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
