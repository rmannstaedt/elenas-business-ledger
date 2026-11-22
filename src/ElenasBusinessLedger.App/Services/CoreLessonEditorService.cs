using ElenasBusinessLedger.Core.Calendar;
using ElenasBusinessLedger.Core.Common;
using ElenasBusinessLedger.Core.Lessons;
using ElenasBusinessLedger.Core.UseCases.Lessons;

namespace ElenasBusinessLedger.App.Services;

// Exposes lesson editing, batch population, and calendar review workflows to the WPF lesson controls.
public sealed class CoreLessonEditorService : ILessonEditorService
{
    private readonly AddLessonUseCase addLessonUseCase;
    private readonly PopulateCalendarUseCase populateCalendarUseCase;
    private readonly EditLessonUseCase editLessonUseCase;
    private readonly DeleteLessonUseCase deleteLessonUseCase;
    private readonly UpdateLessonPlanUseCase updateLessonPlanUseCase;
    private readonly ReviewCalendarMutationUseCase? reviewCalendarMutationUseCase;

    public CoreLessonEditorService(
        AddLessonUseCase addLessonUseCase,
        EditLessonUseCase editLessonUseCase,
        DeleteLessonUseCase deleteLessonUseCase,
        PopulateCalendarUseCase populateCalendarUseCase,
        UpdateLessonPlanUseCase updateLessonPlanUseCase,
        ReviewCalendarMutationUseCase? reviewCalendarMutationUseCase = null)
    {
        this.addLessonUseCase = addLessonUseCase;
        this.populateCalendarUseCase = populateCalendarUseCase;
        this.editLessonUseCase = editLessonUseCase;
        this.deleteLessonUseCase = deleteLessonUseCase;
        this.updateLessonPlanUseCase = updateLessonPlanUseCase;
        this.reviewCalendarMutationUseCase = reviewCalendarMutationUseCase;
    }

    public Task<OperationResult<LessonAddResult>> AddAsync(
        LessonAddRequest request,
        DateOnly refreshMonth,
        CancellationToken cancellationToken) =>
        addLessonUseCase.ExecuteAsync(request, refreshMonth, cancellationToken);

    // Creates missing lessons from the selected student's schedule and reports how much of the batch succeeded.
    public async Task<OperationResult<PopulateCalendarResult>> PopulateCalendarAsync(
        string studentShortName,
        DateOnly displayMonth,
        CancellationToken cancellationToken,
        int? studentId = null)
    {
        try
        {
            return await populateCalendarUseCase.ExecuteAsync(
                new PopulateCalendarRequest(studentShortName, displayMonth, studentId),
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return OperationResult<PopulateCalendarResult>.Failure(new ValidationError(
                "Calendar.PopulateFailed",
                "Could not populate Google Calendar. Refresh the lesson list and try again."));
        }
    }

    // Resolves a lesson held for calendar review or returns the confirmation the dashboard needs.
    public async Task<OperationResult<bool>> ReviewCalendarMutationAsync(
        string eventId, DateOnly month, bool confirmRemoved, CancellationToken cancellationToken)
    {
        try
        {
            return reviewCalendarMutationUseCase is null
                ? OperationResult<bool>.Failure(new ValidationError("Lesson.ReviewUnavailable", "Calendar review is unavailable."))
                : await reviewCalendarMutationUseCase.ExecuteAsync(eventId, month, confirmRemoved, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return OperationResult<bool>.Failure(new ValidationError("Lesson.ReviewFailed", ex.Message));
        }
    }

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

    // Assigns a plan to an unlinked calendar row so it can enter the local billing ledger.
    public async Task<OperationResult<LessonRecord>> LinkToPlanAsync(
        LessonRecord observed, int planCatalogId, CancellationToken cancellationToken)
    {
        try
        {
            return await updateLessonPlanUseCase.LinkAsync(observed, planCatalogId, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return OperationResult<LessonRecord>.Failure(new ValidationError("Lesson.LinkFailed", ex.Message));
        }
    }

    public Task<OperationResult<LessonRecord>> UpdatePlanAsync(
        int lessonId,
        int planCatalogId,
        CancellationToken cancellationToken) =>
        updateLessonPlanUseCase.ExecuteAsync(lessonId, planCatalogId, cancellationToken);
}
