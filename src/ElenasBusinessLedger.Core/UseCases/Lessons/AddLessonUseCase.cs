using ElenasBusinessLedger.Core.Calendar;
using ElenasBusinessLedger.Core.Common;
using ElenasBusinessLedger.Core.Lessons;
using ElenasBusinessLedger.Core.Plans;
using ElenasBusinessLedger.Core.Ports;

namespace ElenasBusinessLedger.Core.UseCases.Lessons;

// Creates a calendar appointment and records its selected plan in the local ledger for billing.
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

    // Creates a Google Calendar appointment and records the chosen plan and event ID in the local billing ledger.
    public async Task<OperationResult<LessonAddResult>> ExecuteAsync(
        LessonAddRequest request,
        DateOnly refreshMonth,
        CancellationToken cancellationToken)
    {
        if (request.PlanCatalogId <= 0)
        {
            return OperationResult<LessonAddResult>.Failure(
                new ValidationError("Lesson.PlanRequired", "A lesson plan is required."));
        }

        var student = request.StudentId is { } id
            ? await studentRepository.GetByIdAsync(id, cancellationToken)
            : await studentRepository.GetByShortNameAsync(request.StudentShortName, cancellationToken);
        if (student is null)
        {
            return OperationResult<LessonAddResult>.Failure(
                new ValidationError("Lesson.StudentNotFound", "The selected student was not found."));
        }

        var plan = await planCatalogRepository.GetAsync(request.PlanCatalogId, cancellationToken);
        if (plan is null)
        {
            return OperationResult<LessonAddResult>.Failure(
                new ValidationError("Lesson.PlanNotFound", "The selected plan was not found."));
        }

        if (!plan.CanBeAssigned)
        {
            return OperationResult<LessonAddResult>.Failure(
                new ValidationError("Lesson.PlanNotAssignable", "The selected plan cannot be assigned to a lesson."));
        }

        request = request with { StudentShortName = student.Student.ShortName };
        var addResult = await calendarGateway.AddLessonAsync(request, refreshMonth, cancellationToken);
        if (addResult.Value is not { } calendarResult || string.IsNullOrWhiteSpace(calendarResult.CreatedLesson.Id))
        {
            return OperationResult<LessonAddResult>.Failure(addResult.Errors.ToArray());
        }

        var saveFailure = await TrySaveCreatedLessonAsync(
            request,
            student.Id,
            plan,
            calendarResult.CreatedLesson,
            refreshMonth,
            cancellationToken);

        return saveFailure ?? addResult;
    }

    private async Task<OperationResult<LessonAddResult>?> TrySaveCreatedLessonAsync(
        LessonAddRequest request,
        int studentId,
        PlanCatalogItem plan,
        CalendarLesson createdLesson,
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
            return OperationResult<LessonAddResult>.Failure(errors.ToArray());
        }

        return null;
    }
}
