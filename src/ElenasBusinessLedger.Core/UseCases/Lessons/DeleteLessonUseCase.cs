using ElenasBusinessLedger.Core.Calendar;
using ElenasBusinessLedger.Core.Common;
using ElenasBusinessLedger.Core.Lessons;
using ElenasBusinessLedger.Core.Ports;

namespace ElenasBusinessLedger.Core.UseCases.Lessons;

public sealed class DeleteLessonUseCase
{
    private readonly IGoogleCalendarGateway calendarGateway;
    private readonly ILessonRepository lessonRepository;

    public DeleteLessonUseCase(
        IGoogleCalendarGateway calendarGateway,
        ILessonRepository lessonRepository)
    {
        this.calendarGateway = calendarGateway;
        this.lessonRepository = lessonRepository;
    }

    public async Task<OperationResult<IReadOnlyList<CalendarLesson>>> ExecuteAsync(
        string lessonId,
        DateOnly refreshMonth,
        CancellationToken cancellationToken)
    {
        var claimed = await lessonRepository.ClaimCalendarMutationAsync(lessonId, CalendarMutationKind.Delete, cancellationToken);
        if (!claimed.Succeeded)
        {
            return OperationResult<IReadOnlyList<CalendarLesson>>.Failure(claimed.Errors.ToArray());
        }

        var calendarResult = await calendarGateway.DeleteWithoutRefreshAsync(lessonId, refreshMonth, cancellationToken);
        if (calendarResult.State != CalendarWriteState.Acknowledged)
        {
            if (calendarResult.State == CalendarWriteState.Rejected)
            {
                await lessonRepository.ClearCalendarMutationAsync(lessonId, CalendarMutationKind.Delete, cancellationToken);
            }
            return OperationResult<IReadOnlyList<CalendarLesson>>.Failure(calendarResult.Errors.ToArray());
        }

        try
        {
            var deleted = await lessonRepository.DeleteUninvoicedByGoogleCalendarEventIdAsync(
                lessonId,
                cancellationToken);
            if (!deleted)
            {
                return LocalLedgerFailure(calendarResult.RefreshedLessons ?? []);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return LocalLedgerFailure(calendarResult.RefreshedLessons ?? []);
        }

        return await calendarResult.RefreshAsync(calendarGateway, refreshMonth, cancellationToken);
    }

    private static OperationResult<IReadOnlyList<CalendarLesson>> LocalLedgerFailure(
        IReadOnlyList<CalendarLesson> currentLessons) =>
        OperationResult<IReadOnlyList<CalendarLesson>>.Failure(
            currentLessons,
            new ValidationError(
                "Lesson.LocalDeleteFailed",
                "The lesson was deleted in Google Calendar, but the local billing ledger could not be updated. Refresh Calendar and check the lesson list before invoicing."));
}
