using MySmallBusinessLedger.Core.Calendar;
using MySmallBusinessLedger.Core.Common;
using MySmallBusinessLedger.Core.Ports;

namespace MySmallBusinessLedger.Core.UseCases.Lessons;

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
        var calendarResult = await calendarGateway.DeleteLessonAsync(lessonId, refreshMonth, cancellationToken);
        if (!calendarResult.Succeeded)
        {
            return calendarResult;
        }

        try
        {
            var deleted = await lessonRepository.DeleteUninvoicedByGoogleCalendarEventIdAsync(
                lessonId,
                cancellationToken);
            if (!deleted)
            {
                return LocalLedgerFailure(calendarResult.Value!);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return LocalLedgerFailure(calendarResult.Value!);
        }

        return calendarResult;
    }

    private static OperationResult<IReadOnlyList<CalendarLesson>> LocalLedgerFailure(
        IReadOnlyList<CalendarLesson> currentLessons) =>
        OperationResult<IReadOnlyList<CalendarLesson>>.Failure(
            currentLessons,
            new ValidationError(
                "Lesson.LocalDeleteFailed",
                "The lesson was deleted in Google Calendar, but the local billing ledger could not be updated. Refresh Calendar and check the lesson list before invoicing."));
}
