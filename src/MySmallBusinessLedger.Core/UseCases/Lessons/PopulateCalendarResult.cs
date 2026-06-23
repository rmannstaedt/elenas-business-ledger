using MySmallBusinessLedger.Core.Calendar;

namespace MySmallBusinessLedger.Core.UseCases.Lessons;

public sealed record PopulateCalendarResult(
    int CreatedCount,
    int SkippedDuplicateCount,
    IReadOnlyList<CalendarLesson> Lessons);
