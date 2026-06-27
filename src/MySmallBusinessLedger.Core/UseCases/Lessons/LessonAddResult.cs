using MySmallBusinessLedger.Core.Calendar;

namespace MySmallBusinessLedger.Core.UseCases.Lessons;

public sealed record LessonAddResult(
    CalendarLesson CreatedLesson,
    IReadOnlyList<CalendarLesson> Lessons);
