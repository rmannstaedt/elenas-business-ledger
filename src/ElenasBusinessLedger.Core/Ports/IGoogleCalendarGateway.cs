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

    async Task<CalendarWriteReceipt> DeleteWithoutRefreshAsync(
        string lessonId, DateOnly refreshMonth, CancellationToken cancellationToken) =>
        CalendarWriteReceipt.FromPreviousResult(await DeleteLessonAsync(lessonId, refreshMonth, cancellationToken));

    async Task<CalendarWriteReceipt> UpdateWithoutRefreshAsync(
        LessonEditRequest request, DateOnly refreshMonth, CancellationToken cancellationToken) =>
        CalendarWriteReceipt.FromPreviousResult(await UpdateLessonAsync(request, refreshMonth, cancellationToken));

    async Task<OperationResult<IReadOnlyList<CalendarLesson>>> PopulateWithReceiptsAsync(
        IReadOnlyList<LessonAddRequest> requests, DateOnly refreshMonth,
        Func<int, CalendarLesson, CancellationToken, Task<ValidationError?>> saveReceipt,
        CancellationToken cancellationToken)
    {
        var result = await PopulateLessonsAsync(requests, refreshMonth, cancellationToken);
        for (var index = 0; index < requests.Count && result.Value is not null; index++)
        {
            var request = requests[index];
            var created = result.Value.FirstOrDefault(lesson => lesson.StartsAt == request.StartsAt &&
                lesson.HasKnownStartTime == request.HasKnownStartTime &&
                string.Equals(lesson.StudentShortName, request.StudentShortName, StringComparison.OrdinalIgnoreCase));
            if (created is null)
            {
                if (result.Succeeded)
                {
                    return OperationResult<IReadOnlyList<CalendarLesson>>.Failure(result.Value, new ValidationError(
                        "Lesson.LocalSaveFailed", "A created appointment could not be identified for the local ledger."));
                }
                continue;
            }

            var error = await saveReceipt(index, created, cancellationToken);
            if (error is not null)
            {
                return OperationResult<IReadOnlyList<CalendarLesson>>.Failure(result.Value, result.Errors.Append(error).ToArray());
            }
        }

        return result;
    }

    Task<OperationResult<LessonState>> UpdateLessonStateAsync(
        string lessonId,
        LessonState state,
        CancellationToken cancellationToken);
}
