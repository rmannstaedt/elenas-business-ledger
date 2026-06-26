using MySmallBusinessLedger.Core.Calendar;
using MySmallBusinessLedger.Core.Common;
using MySmallBusinessLedger.Core.UseCases.Lessons;

namespace MySmallBusinessLedger.App.Services;

public sealed class CoreLessonEditorService : ILessonEditorService
{
    private readonly AddLessonUseCase addLessonUseCase;
    private readonly PopulateCalendarUseCase populateCalendarUseCase;
    private readonly EditLessonUseCase editLessonUseCase;
    private readonly DeleteLessonUseCase deleteLessonUseCase;

    public CoreLessonEditorService(
        AddLessonUseCase addLessonUseCase,
        EditLessonUseCase editLessonUseCase,
        DeleteLessonUseCase deleteLessonUseCase,
        PopulateCalendarUseCase populateCalendarUseCase)
    {
        this.addLessonUseCase = addLessonUseCase;
        this.populateCalendarUseCase = populateCalendarUseCase;
        this.editLessonUseCase = editLessonUseCase;
        this.deleteLessonUseCase = deleteLessonUseCase;
    }

    public Task<OperationResult<IReadOnlyList<CalendarLesson>>> AddAsync(
        LessonAddRequest request,
        DateOnly refreshMonth,
        CancellationToken cancellationToken) =>
        addLessonUseCase.ExecuteAsync(request, refreshMonth, cancellationToken);

    public Task<OperationResult<PopulateCalendarResult>> PopulateCalendarAsync(
        string studentShortName,
        DateOnly displayMonth,
        DateOnly today,
        CancellationToken cancellationToken) =>
        populateCalendarUseCase.ExecuteAsync(
            new PopulateCalendarRequest(studentShortName, displayMonth, today),
            cancellationToken);

    public Task<OperationResult<IReadOnlyList<CalendarLesson>>> EditAsync(
        LessonEditRequest request,
        DateOnly refreshMonth,
        CancellationToken cancellationToken) =>
        editLessonUseCase.ExecuteAsync(request, refreshMonth, cancellationToken);

    public Task<OperationResult<IReadOnlyList<CalendarLesson>>> DeleteAsync(
        string lessonId,
        DateOnly refreshMonth,
        CancellationToken cancellationToken) =>
        deleteLessonUseCase.ExecuteAsync(lessonId, refreshMonth, cancellationToken);
}
