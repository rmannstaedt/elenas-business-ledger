using ElenasBusinessLedger.Core.Lessons;

namespace ElenasBusinessLedger.Core.Calendar;

// Carries a calendar appointment into the ledger with its scoped event ID, time, and any known student ID.
public sealed record CalendarLesson(
    string Id,
    string StudentShortName,
    DateTimeOffset StartsAt,
    LessonState State = LessonState.Planned,
    bool HasKnownStartTime = true,
    string? LegacyEventId = null,
    int? StudentId = null);
