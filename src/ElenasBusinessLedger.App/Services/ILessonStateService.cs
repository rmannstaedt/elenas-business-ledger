using ElenasBusinessLedger.Core.Common;
using ElenasBusinessLedger.Core.Lessons;

namespace ElenasBusinessLedger.App.Services;

public interface ILessonStateService
{
    Task<OperationResult<LessonState>> ToggleAsync(
        string lessonId,
        LessonState currentState,
        bool confirmReturnToPlanned,
        CancellationToken cancellationToken);
}
