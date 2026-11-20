using MySmallBusinessLedger.Core.Calendar;
using MySmallBusinessLedger.Core.Common;
using MySmallBusinessLedger.Core.Ports;

namespace MySmallBusinessLedger.Core.UseCases.Lessons;

public sealed class EditLessonUseCase
{
    private readonly IGoogleCalendarGateway calendarGateway;
    private readonly ILessonRepository lessonRepository;

    public EditLessonUseCase(
        IGoogleCalendarGateway calendarGateway,
        ILessonRepository lessonRepository)
    {
        this.calendarGateway = calendarGateway;
        this.lessonRepository = lessonRepository;
    }

    public async Task<OperationResult<IReadOnlyList<CalendarLesson>>> ExecuteAsync(
        LessonEditRequest request,
        DateOnly refreshMonth,
        CancellationToken cancellationToken)
    {
        var calendarResult = await calendarGateway.UpdateLessonAsync(request, refreshMonth, cancellationToken);
        if (!calendarResult.Succeeded)
        {
            return calendarResult;
        }

        try
        {
            var updated = await lessonRepository.UpdateUninvoicedTimingByGoogleCalendarEventIdAsync(
                request.LessonId,
                request.StartsAt,
                request.HasKnownStartTime,
                cancellationToken);
            if (updated is null)
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
                "Lesson.LocalUpdateFailed",
                "The lesson was updated in Google Calendar, but the local billing ledger could not be updated. Refresh Calendar and check the lesson list before invoicing."));
}
