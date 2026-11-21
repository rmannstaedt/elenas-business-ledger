using ElenasBusinessLedger.Core.Calendar;

namespace ElenasBusinessLedger.Core.UseCases.Lessons;

// Gives the dashboard the saved and duplicate counts along with available lesson rows after a batch.
public sealed record PopulateCalendarResult(
    int CreatedCount,
    int SkippedDuplicateCount,
    IReadOnlyList<CalendarLesson> Lessons);
