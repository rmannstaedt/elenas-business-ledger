using ElenasBusinessLedger.Core.Calendar;
using ElenasBusinessLedger.Core.Common;
using ElenasBusinessLedger.Core.Lessons;
using ElenasBusinessLedger.Core.UseCases.Lessons;

namespace ElenasBusinessLedger.App.Services;

// Supplies the local and calendar lesson workflows behind dashboard and editor actions.
public interface ILessonEditorService
{
    Task<OperationResult<LessonAddResult>> AddAsync(
        LessonAddRequest request,
        DateOnly refreshMonth,
        CancellationToken cancellationToken);

    Task<OperationResult<PopulateCalendarResult>> PopulateCalendarAsync(
        string studentShortName,
        DateOnly displayMonth,
        CancellationToken cancellationToken,
        int? studentId = null);

    Task<OperationResult<bool>> ReviewCalendarMutationAsync(
        string eventId, DateOnly month, bool confirmRemoved, CancellationToken cancellationToken) =>
        Task.FromResult(OperationResult<bool>.Failure(new ValidationError("Lesson.ReviewUnavailable", "Calendar review is unavailable.")));

    Task<OperationResult<IReadOnlyList<CalendarLesson>>> EditAsync(
        LessonEditRequest request,
        DateOnly refreshMonth,
        CancellationToken cancellationToken);

    Task<OperationResult<IReadOnlyList<CalendarLesson>>> DeleteAsync(
        string lessonId,
        DateOnly refreshMonth,
        CancellationToken cancellationToken);

    Task<OperationResult<LessonRecord>> LinkToPlanAsync(
        LessonRecord observed, int planCatalogId, CancellationToken cancellationToken) =>
        Task.FromResult(OperationResult<LessonRecord>.Failure(new ValidationError("Lesson.LinkUnavailable", "Calendar linking is unavailable.")));

    Task<OperationResult<LessonRecord>> UpdatePlanAsync(
        int lessonId,
        int planCatalogId,
        CancellationToken cancellationToken);
}
