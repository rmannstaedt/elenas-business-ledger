using ElenasBusinessLedger.Core.Calendar;
using ElenasBusinessLedger.Core.Common;
using ElenasBusinessLedger.Core.Lessons;
using ElenasBusinessLedger.Core.UseCases.Lessons;

namespace ElenasBusinessLedger.App.Services;

public interface ILessonEditorService
{
    Task<OperationResult<LessonAddResult>> AddAsync(
        LessonAddRequest request,
        DateOnly refreshMonth,
        CancellationToken cancellationToken);

    Task<OperationResult<PopulateCalendarResult>> PopulateCalendarAsync(
        string studentShortName,
        DateOnly displayMonth,
        CancellationToken cancellationToken);

    Task<OperationResult<IReadOnlyList<CalendarLesson>>> EditAsync(
        LessonEditRequest request,
        DateOnly refreshMonth,
        CancellationToken cancellationToken);

    Task<OperationResult<IReadOnlyList<CalendarLesson>>> DeleteAsync(
        string lessonId,
        DateOnly refreshMonth,
        CancellationToken cancellationToken);

    Task<OperationResult<LessonRecord>> UpdatePlanAsync(
        int lessonId,
        int planCatalogId,
        CancellationToken cancellationToken);
}
