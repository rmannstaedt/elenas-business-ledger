using ElenaSmallBusinessLedger.Core.Common;
using ElenaSmallBusinessLedger.Core.Lessons;

namespace ElenaSmallBusinessLedger.App.Services;

public interface ILessonStateService
{
    Task<OperationResult<LessonState>> ToggleAsync(
        string lessonId,
        LessonState currentState,
        bool confirmReturnToPlanned,
        CancellationToken cancellationToken);
}
