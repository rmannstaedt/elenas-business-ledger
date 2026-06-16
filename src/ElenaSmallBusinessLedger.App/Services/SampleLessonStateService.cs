using ElenaSmallBusinessLedger.Core.Common;
using ElenaSmallBusinessLedger.Core.Lessons;

namespace ElenaSmallBusinessLedger.App.Services;

public sealed class SampleLessonStateService : ILessonStateService
{
    public Task<OperationResult<LessonState>> ToggleAsync(
        string lessonId,
        LessonState currentState,
        bool confirmReturnToPlanned,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (currentState == LessonState.Completed && !confirmReturnToPlanned)
        {
            return Task.FromResult(OperationResult<LessonState>.ConfirmationRequired(
                currentState,
                new ValidationError("LessonState.ReturnToPlannedRequiresConfirmation", "Returning a completed lesson to planned requires confirmation.")));
        }

        var nextState = currentState == LessonState.Planned
            ? LessonState.Completed
            : LessonState.Planned;

        return Task.FromResult(OperationResult<LessonState>.Success(nextState));
    }
}
