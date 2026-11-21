using ElenasBusinessLedger.Core.Calendar;
using ElenasBusinessLedger.Core.Common;
using ElenasBusinessLedger.Core.Lessons;
using ElenasBusinessLedger.Core.Ports;

namespace ElenasBusinessLedger.Core.UseCases.Lessons;

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
        var claimed = await lessonRepository.ClaimCalendarMutationAsync(request.LessonId, CalendarMutationKind.Move, cancellationToken);
        if (!claimed.Succeeded)
        {
            return OperationResult<IReadOnlyList<CalendarLesson>>.Failure(claimed.Errors.ToArray());
        }

        var calendarResult = await calendarGateway.UpdateWithoutRefreshAsync(request, refreshMonth, cancellationToken);
        if (calendarResult.State != CalendarWriteState.Acknowledged)
        {
            if (calendarResult.State == CalendarWriteState.Rejected)
            {
                await lessonRepository.ClearCalendarMutationAsync(request.LessonId, CalendarMutationKind.Move, cancellationToken);
            }
            return OperationResult<IReadOnlyList<CalendarLesson>>.Failure(calendarResult.Errors.ToArray());
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
                "Lesson.LocalUpdateFailed",
                "The lesson was updated in Google Calendar, but the local billing ledger could not be updated. Refresh Calendar and check the lesson list before invoicing."));
}
