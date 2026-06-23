using MySmallBusinessLedger.Core.Calendar;
using MySmallBusinessLedger.Core.Common;
using MySmallBusinessLedger.Core.UseCases.Lessons;

namespace MySmallBusinessLedger.App.Services;

public interface ILessonEditorService
{
    Task<OperationResult<IReadOnlyList<CalendarLesson>>> AddAsync(
        LessonAddRequest request,
        DateOnly refreshMonth,
        CancellationToken cancellationToken);

    Task<OperationResult<PopulateCalendarResult>> PopulateCalendarAsync(
        string studentShortName,
        DateOnly displayMonth,
        DateOnly today,
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
