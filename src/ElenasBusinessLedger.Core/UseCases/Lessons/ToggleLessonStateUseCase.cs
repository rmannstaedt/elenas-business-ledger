using ElenasBusinessLedger.Core.Common;
using ElenasBusinessLedger.Core.Lessons;
using ElenasBusinessLedger.Core.Ports;

namespace ElenasBusinessLedger.Core.UseCases.Lessons;

// Changes completion status in the calendar and billing ledger while protecting held or invoiced lessons.
public sealed class ToggleLessonStateUseCase
{
    private readonly IGoogleCalendarGateway calendarGateway;
    private readonly ILessonRepository lessonRepository;

    public ToggleLessonStateUseCase(
        IGoogleCalendarGateway calendarGateway,
        ILessonRepository lessonRepository)
    {
        this.calendarGateway = calendarGateway;
        this.lessonRepository = lessonRepository;
    }

    // Changes the lesson state in Google Calendar and SQLite, asking before reopening a completed lesson.
    public async Task<OperationResult<LessonState>> ExecuteAsync(
        string lessonId,
        LessonState currentState,
        bool confirmReturnToPlanned,
        CancellationToken cancellationToken)
    {
        if (currentState == LessonState.Completed && !confirmReturnToPlanned)
        {
            return OperationResult<LessonState>.ConfirmationRequired(
                currentState,
                new ValidationError(
                    "LessonState.ReturnToPlannedRequiresConfirmation",
                    "Returning a completed lesson to planned requires confirmation."));
        }

        var nextState = currentState == LessonState.Planned
            ? LessonState.Completed
            : LessonState.Planned;

        var claimed = await lessonRepository.ClaimCalendarMutationAsync(lessonId, CalendarMutationKind.ChangeState, cancellationToken);
        if (!claimed.Succeeded)
        {
            return OperationResult<LessonState>.Failure(currentState, claimed.Errors.ToArray());
        }

        var calendarResult = await calendarGateway.UpdateLessonStateAsync(lessonId, nextState, cancellationToken);
        if (!calendarResult.Succeeded)
        {
            if (calendarResult.Errors.Any(error => error.Code is "Calendar.ScopeMismatch" or "Calendar.NotConfigured"))
            {
                await lessonRepository.ClearCalendarMutationAsync(lessonId, CalendarMutationKind.ChangeState, cancellationToken);
            }
            return calendarResult;
        }

        try
        {
            var updated = await lessonRepository.UpdateUninvoicedStateByGoogleCalendarEventIdAsync(
                lessonId,
                nextState,
                cancellationToken);
            if (updated is null)
            {
                return LocalLedgerFailure(currentState);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return LocalLedgerFailure(currentState);
        }

        return calendarResult;
    }

    private static OperationResult<LessonState> LocalLedgerFailure(LessonState currentState) =>
        OperationResult<LessonState>.Failure(
            currentState,
            new ValidationError(
                "Lesson.LocalStateUpdateFailed",
                "The lesson status was updated in Google Calendar, but the local billing ledger could not be updated. Refresh Calendar and check the lesson list before invoicing."));
}
