using MySmallBusinessLedger.Core.Calendar;
using MySmallBusinessLedger.Core.Common;
using MySmallBusinessLedger.Core.Lessons;
using MySmallBusinessLedger.Core.Plans;
using MySmallBusinessLedger.Core.Ports;

namespace MySmallBusinessLedger.Core.UseCases.Lessons;

public sealed class AddLessonUseCase
{
    private readonly IGoogleCalendarGateway calendarGateway;
    private readonly IStudentRepository studentRepository;
    private readonly IPlanCatalogRepository planCatalogRepository;
    private readonly ILessonRepository lessonRepository;

    public AddLessonUseCase(
        IGoogleCalendarGateway calendarGateway,
        IStudentRepository studentRepository,
        IPlanCatalogRepository planCatalogRepository,
        ILessonRepository lessonRepository)
    {
        this.calendarGateway = calendarGateway;
        this.studentRepository = studentRepository;
        this.planCatalogRepository = planCatalogRepository;
        this.lessonRepository = lessonRepository;
    }

    public async Task<OperationResult<IReadOnlyList<CalendarLesson>>> ExecuteAsync(
        LessonAddRequest request,
        DateOnly refreshMonth,
        CancellationToken cancellationToken)
    {
        if (request.PlanCatalogId <= 0)
        {
            return OperationResult<IReadOnlyList<CalendarLesson>>.Failure(
                new ValidationError("Lesson.PlanRequired", "A lesson plan is required."));
        }

        var student = await studentRepository.GetByShortNameAsync(request.StudentShortName, cancellationToken);
        if (student is null)
        {
            return OperationResult<IReadOnlyList<CalendarLesson>>.Failure(
                new ValidationError("Lesson.StudentNotFound", "The selected student was not found."));
        }

        var plan = await planCatalogRepository.GetAsync(request.PlanCatalogId, cancellationToken);
        if (plan is null)
        {
            return OperationResult<IReadOnlyList<CalendarLesson>>.Failure(
                new ValidationError("Lesson.PlanNotFound", "The selected plan was not found."));
        }

        if (!plan.CanBeAssigned)
        {
            return OperationResult<IReadOnlyList<CalendarLesson>>.Failure(
                new ValidationError("Lesson.PlanNotAssignable", "The selected plan cannot be assigned to a lesson."));
        }

        var addResult = await calendarGateway.AddLessonAsync(request, refreshMonth, cancellationToken);
        if (!addResult.Succeeded)
        {
            if (addResult.Value?.CreatedLesson is not null)
            {
                var saveFailure = await TrySaveCreatedLessonAsync(
                    request,
                    student.Id,
                    plan,
                    addResult.Value.CreatedLesson,
                    addResult.Value.Lessons,
                    refreshMonth,
                    cancellationToken);
                if (saveFailure is not null)
                {
                    return saveFailure;
                }

                var currentLessons = addResult.Value.Lessons.Count > 0
                    ? addResult.Value.Lessons
                    : [addResult.Value.CreatedLesson];
                return OperationResult<IReadOnlyList<CalendarLesson>>.Failure(
                    currentLessons,
                    addResult.Errors.ToArray());
            }

            return OperationResult<IReadOnlyList<CalendarLesson>>.Failure(
                addResult.Value?.Lessons ?? [],
                addResult.Errors.ToArray());
        }

        var createdLesson = addResult.Value!.CreatedLesson;
        var successfulSaveFailure = await TrySaveCreatedLessonAsync(
            request,
            student.Id,
            plan,
            createdLesson,
            addResult.Value.Lessons,
            refreshMonth,
            cancellationToken);
        if (successfulSaveFailure is not null)
        {
            return successfulSaveFailure;
        }

        return OperationResult<IReadOnlyList<CalendarLesson>>.Success(addResult.Value.Lessons);
    }

    private async Task<OperationResult<IReadOnlyList<CalendarLesson>>?> TrySaveCreatedLessonAsync(
        LessonAddRequest request,
        int studentId,
        PlanCatalogItem plan,
        CalendarLesson createdLesson,
        IReadOnlyList<CalendarLesson> fallbackLessons,
        DateOnly refreshMonth,
        CancellationToken cancellationToken)
    {
        try
        {
            await lessonRepository.SaveAsync(
                new LessonRecord(
                    Id: 0,
                    StudentId: studentId,
                    StudentShortName: request.StudentShortName,
                    PlanCatalogId: plan.Id,
                    PlanName: plan.Name,
                    StartsAt: request.StartsAt,
                    DurationMinutes: plan.DurationMinutes,
                    State: createdLesson.State,
                    GoogleCalendarEventId: createdLesson.Id,
                    InvoiceNumber: null,
                    HasKnownStartTime: request.HasKnownStartTime),
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            var cleanup = await calendarGateway.DeleteLessonAsync(createdLesson.Id, refreshMonth, cancellationToken);
            var errors = new List<ValidationError>
            {
                new("Lesson.LocalSaveFailed", "The lesson could not be saved locally. The app attempted to remove the Google Calendar entry.")
            };
            errors.AddRange(cleanup.Errors);
            return OperationResult<IReadOnlyList<CalendarLesson>>.Failure(
                cleanup.Value ?? fallbackLessons,
                errors.ToArray());
        }

        return null;
    }
}
