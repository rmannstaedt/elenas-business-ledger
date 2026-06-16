using ElenaSmallBusinessLedger.Core.Calendar;
using ElenaSmallBusinessLedger.Core.Common;
using ElenaSmallBusinessLedger.Core.UseCases.Lessons;

namespace ElenaSmallBusinessLedger.App.Services;

public sealed class SampleLessonEditorService : ILessonEditorService
{
    public Task<OperationResult<IReadOnlyList<CalendarLesson>>> AddAsync(
        LessonAddRequest request,
        DateOnly refreshMonth,
        CancellationToken cancellationToken) =>
        Task.FromResult(OperationResult<IReadOnlyList<CalendarLesson>>.Success(
            [new CalendarLesson(Guid.NewGuid().ToString("N"), request.StudentShortName, request.StartsAt)]));

    public Task<OperationResult<IReadOnlyList<CalendarLesson>>> EditAsync(
        LessonEditRequest request,
        DateOnly refreshMonth,
        CancellationToken cancellationToken) =>
        Task.FromResult(OperationResult<IReadOnlyList<CalendarLesson>>.Success(
            [new CalendarLesson(request.LessonId, request.StudentShortName, request.StartsAt)]));

    public Task<OperationResult<IReadOnlyList<CalendarLesson>>> DeleteAsync(
        string lessonId,
        DateOnly refreshMonth,
        CancellationToken cancellationToken) =>
        Task.FromResult(OperationResult<IReadOnlyList<CalendarLesson>>.Success([]));
}
