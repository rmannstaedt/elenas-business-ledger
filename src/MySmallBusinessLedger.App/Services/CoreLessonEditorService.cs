using MySmallBusinessLedger.Core.Calendar;
using MySmallBusinessLedger.Core.Common;
using MySmallBusinessLedger.Core.Lessons;
using MySmallBusinessLedger.Core.UseCases.Lessons;

namespace MySmallBusinessLedger.App.Services;

public sealed class CoreLessonEditorService : ILessonEditorService
{
    private readonly AddLessonUseCase addLessonUseCase;
    private readonly PopulateCalendarUseCase populateCalendarUseCase;
    private readonly EditLessonUseCase editLessonUseCase;
    private readonly DeleteLessonUseCase deleteLessonUseCase;
    private readonly UpdateLessonPlanUseCase updateLessonPlanUseCase;

    public CoreLessonEditorService(
        AddLessonUseCase addLessonUseCase,
        EditLessonUseCase editLessonUseCase,
        DeleteLessonUseCase deleteLessonUseCase,
        PopulateCalendarUseCase populateCalendarUseCase,
        UpdateLessonPlanUseCase updateLessonPlanUseCase)
    {
        this.addLessonUseCase = addLessonUseCase;
        this.populateCalendarUseCase = populateCalendarUseCase;
        this.editLessonUseCase = editLessonUseCase;
        this.deleteLessonUseCase = deleteLessonUseCase;
        this.updateLessonPlanUseCase = updateLessonPlanUseCase;
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

    public Task<OperationResult<LessonRecord>> UpdatePlanAsync(
        int lessonId,
        int planCatalogId,
        CancellationToken cancellationToken) =>
        updateLessonPlanUseCase.ExecuteAsync(lessonId, planCatalogId, cancellationToken);
}
