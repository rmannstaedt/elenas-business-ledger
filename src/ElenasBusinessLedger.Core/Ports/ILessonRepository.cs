using ElenasBusinessLedger.Core.Lessons;

namespace ElenasBusinessLedger.Core.Ports;

public interface ILessonRepository
{
    Task<LessonRecord> SaveAsync(LessonRecord lesson, CancellationToken cancellationToken);

    Task<IReadOnlyList<LessonRecord>> ListForStudentMonthAsync(
        string studentShortName,
        DateOnly month,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<LessonRecord>> ListForWindowAsync(
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken);

    Task<LessonRecord?> UpdatePlanAsync(
        int lessonId,
        int planCatalogId,
        string planName,
        int durationMinutes,
        CancellationToken cancellationToken);

    Task<LessonRecord?> UpdateUninvoicedTimingByGoogleCalendarEventIdAsync(
        string googleCalendarEventId,
        DateTimeOffset startsAt,
        bool hasKnownStartTime,
        CancellationToken cancellationToken);

    Task<LessonRecord?> UpdateUninvoicedStateByGoogleCalendarEventIdAsync(
        string googleCalendarEventId,
        LessonState state,
        CancellationToken cancellationToken);

    Task<bool> DeleteUninvoicedByGoogleCalendarEventIdAsync(
        string googleCalendarEventId,
        CancellationToken cancellationToken);

    Task DeleteUninvoicedAsync(IReadOnlyList<int> lessonIds, CancellationToken cancellationToken);
}
