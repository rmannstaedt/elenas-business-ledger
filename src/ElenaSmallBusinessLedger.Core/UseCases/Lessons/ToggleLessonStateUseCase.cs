using ElenaSmallBusinessLedger.Core.Common;
using ElenaSmallBusinessLedger.Core.Lessons;
using ElenaSmallBusinessLedger.Core.Ports;

namespace ElenaSmallBusinessLedger.Core.UseCases.Lessons;

public sealed class ToggleLessonStateUseCase
{
    private readonly IGoogleCalendarGateway calendarGateway;

    public ToggleLessonStateUseCase(IGoogleCalendarGateway calendarGateway)
    {
        this.calendarGateway = calendarGateway;
    }

    public Task<OperationResult<LessonState>> ExecuteAsync(
        string lessonId,
        LessonState currentState,
        bool confirmReturnToPlanned,
        CancellationToken cancellationToken)
    {
        if (currentState == LessonState.Completed && !confirmReturnToPlanned)
        {
            return Task.FromResult(OperationResult<LessonState>.ConfirmationRequired(
                currentState,
                new ValidationError(
                    "LessonState.ReturnToPlannedRequiresConfirmation",
                    "Returning a completed lesson to planned requires confirmation.")));
        }

        var nextState = currentState == LessonState.Planned
            ? LessonState.Completed
            : LessonState.Planned;

        return calendarGateway.UpdateLessonStateAsync(lessonId, nextState, cancellationToken);
    }
}
