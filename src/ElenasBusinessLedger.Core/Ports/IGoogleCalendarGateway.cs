using ElenasBusinessLedger.Core.Calendar;
using ElenasBusinessLedger.Core.Common;
using ElenasBusinessLedger.Core.Lessons;
using ElenasBusinessLedger.Core.UseCases.Lessons;

namespace ElenasBusinessLedger.Core.Ports;

public interface IGoogleCalendarGateway
{
    Task<IReadOnlyList<CalendarLesson>> ListLessonsAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken);

    Task<OperationResult<LessonAddResult>> AddLessonAsync(
        LessonAddRequest request,
        DateOnly refreshMonth,
        CancellationToken cancellationToken);

    Task<OperationResult<IReadOnlyList<CalendarLesson>>> PopulateLessonsAsync(
        IReadOnlyList<LessonAddRequest> requests,
        DateOnly refreshMonth,
        CancellationToken cancellationToken);

    Task<OperationResult<IReadOnlyList<CalendarLesson>>> UpdateLessonAsync(
        LessonEditRequest request,
        DateOnly refreshMonth,
        CancellationToken cancellationToken);

    Task<OperationResult<IReadOnlyList<CalendarLesson>>> DeleteLessonAsync(
        string lessonId,
        DateOnly refreshMonth,
        CancellationToken cancellationToken);

    Task<OperationResult<LessonState>> UpdateLessonStateAsync(
        string lessonId,
        LessonState state,
        CancellationToken cancellationToken);
}
