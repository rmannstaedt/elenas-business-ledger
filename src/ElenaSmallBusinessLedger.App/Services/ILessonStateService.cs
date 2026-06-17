using MySmallBusinessLedger.Core.Common;
using MySmallBusinessLedger.Core.Lessons;

namespace MySmallBusinessLedger.App.Services;

public interface ILessonStateService
{
    Task<OperationResult<LessonState>> ToggleAsync(
        string lessonId,
        LessonState currentState,
        bool confirmReturnToPlanned,
        CancellationToken cancellationToken);
}
