using ElenasBusinessLedger.Core.Common;
using ElenasBusinessLedger.Core.Lessons;

namespace ElenasBusinessLedger.App.Services;

// Lets the dashboard change lesson completion state through the validation and confirmation rules.
public interface ILessonStateService
{
    Task<OperationResult<LessonState>> ToggleAsync(
        string lessonId,
        LessonState currentState,
        bool confirmReturnToPlanned,
        CancellationToken cancellationToken);
}
