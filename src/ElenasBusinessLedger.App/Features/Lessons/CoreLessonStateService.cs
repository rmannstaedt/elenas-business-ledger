using ElenasBusinessLedger.Core.Common;
using ElenasBusinessLedger.Core.Lessons;
using ElenasBusinessLedger.Core.UseCases.Lessons;

namespace ElenasBusinessLedger.App.Services;

// Applies lesson state changes through Core so the dashboard can show the result of marking attendance.
public sealed class CoreLessonStateService : ILessonStateService
{
    private readonly ToggleLessonStateUseCase useCase;

    public CoreLessonStateService(ToggleLessonStateUseCase useCase)
    {
        this.useCase = useCase;
    }

    public async Task<OperationResult<LessonState>> ToggleAsync(
        string lessonId,
        LessonState currentState,
        bool confirmReturnToPlanned,
        CancellationToken cancellationToken)
    {
        try
        {
            return await useCase.ExecuteAsync(lessonId, currentState, confirmReturnToPlanned, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return OperationResult<LessonState>.Failure(
                currentState,
                new ValidationError("LessonState.UpdateFailed", ex.Message));
        }
    }
}
