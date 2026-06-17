using MySmallBusinessLedger.Core.Common;
using MySmallBusinessLedger.Core.Lessons;
using MySmallBusinessLedger.Core.UseCases.Lessons;

namespace MySmallBusinessLedger.App.Services;

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
