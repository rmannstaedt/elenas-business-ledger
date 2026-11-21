using ElenasBusinessLedger.Core.Common;
using ElenasBusinessLedger.Core.Lessons;

namespace ElenasBusinessLedger.Core.Ports;

public interface ILessonRepository
{
    Task<OperationResult<bool>> ClaimCalendarMutationAsync(
        string eventId, CalendarMutationKind kind, CancellationToken cancellationToken) =>
        Task.FromResult(OperationResult<bool>.Success(true));

    Task ClearCalendarMutationAsync(string eventId, CalendarMutationKind kind, CancellationToken cancellationToken) =>
        Task.CompletedTask;

    Task<OperationResult<bool>> ResolveCalendarMutationAsync(
        string eventId, ElenasBusinessLedger.Core.Calendar.CalendarLesson? observed, CancellationToken cancellationToken) =>
        Task.FromResult(OperationResult<bool>.Failure(new ValidationError("Lesson.ReviewUnavailable", "Calendar review is unavailable.")));

    Task<LessonRecord> SaveAsync(LessonRecord lesson, CancellationToken cancellationToken);

    Task<IReadOnlyList<LessonRecord>> ListForStudentMonthAsync(
        string studentShortName,
        DateOnly month,
        CancellationToken cancellationToken,
        int? studentId = null);

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

    Task<IReadOnlyList<LessonRecord>> ListByGoogleCalendarEventIdsAsync(
        IReadOnlyList<string> eventIds, CancellationToken cancellationToken);
}
