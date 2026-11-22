using ElenasBusinessLedger.Core.Calendar;

namespace ElenasBusinessLedger.Core.UseCases.Lessons;

// Carries the created event separately from the refreshed list so the local link survives a failed refresh.
public sealed record LessonAddResult(
    CalendarLesson CreatedLesson,
    IReadOnlyList<CalendarLesson> Lessons);
