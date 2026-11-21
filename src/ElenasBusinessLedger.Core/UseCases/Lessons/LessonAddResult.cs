using ElenasBusinessLedger.Core.Calendar;

namespace ElenasBusinessLedger.Core.UseCases.Lessons;

public sealed record LessonAddResult(
    CalendarLesson CreatedLesson,
    IReadOnlyList<CalendarLesson> Lessons);
