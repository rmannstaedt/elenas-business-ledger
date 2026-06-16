using ElenaSmallBusinessLedger.Core.Calendar;
using ElenaSmallBusinessLedger.Core.Common;
using ElenaSmallBusinessLedger.Core.UseCases.Lessons;

namespace ElenaSmallBusinessLedger.App.Services;

public interface ILessonEditorService
{
    Task<OperationResult<IReadOnlyList<CalendarLesson>>> AddAsync(
        LessonAddRequest request,
        DateOnly refreshMonth,
        CancellationToken cancellationToken);

    Task<OperationResult<IReadOnlyList<CalendarLesson>>> EditAsync(
        LessonEditRequest request,
        DateOnly refreshMonth,
        CancellationToken cancellationToken);

    Task<OperationResult<IReadOnlyList<CalendarLesson>>> DeleteAsync(
        string lessonId,
        DateOnly refreshMonth,
        CancellationToken cancellationToken);
}
