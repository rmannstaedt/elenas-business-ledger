using ElenasBusinessLedger.Core.Calendar;

namespace ElenasBusinessLedger.Core.UseCases.Lessons;

public sealed record PopulateCalendarResult(
    int CreatedCount,
    int SkippedDuplicateCount,
    IReadOnlyList<CalendarLesson> Lessons);
