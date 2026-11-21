using ElenasBusinessLedger.Core.Common;
using ElenasBusinessLedger.Core.Lessons;
using ElenasBusinessLedger.Core.Ports;

namespace ElenasBusinessLedger.Core.UseCases.Lessons;

public sealed class UpdateLessonPlanUseCase
{
    private readonly ILessonRepository lessonRepository;
    private readonly IPlanCatalogRepository planCatalogRepository;

    public UpdateLessonPlanUseCase(
        ILessonRepository lessonRepository,
        IPlanCatalogRepository planCatalogRepository)
    {
        this.lessonRepository = lessonRepository;
        this.planCatalogRepository = planCatalogRepository;
    }

    public async Task<OperationResult<LessonRecord>> LinkAsync(
        LessonRecord observed, int planCatalogId, CancellationToken cancellationToken)
    {
        if (observed.StudentId <= 0 || string.IsNullOrWhiteSpace(observed.GoogleCalendarEventId))
        {
            return OperationResult<LessonRecord>.Failure(new ValidationError(
                "Lesson.CalendarLinkMissing", "Refresh the dashboard so the appointment and its student can be identified."));
        }

        var linked = await lessonRepository.ListByGoogleCalendarEventIdsAsync([observed.GoogleCalendarEventId], cancellationToken);
        var existing = linked.SingleOrDefault();
        if (existing is not null)
        {
            return existing.StudentId == observed.StudentId
                ? await ExecuteAsync(existing.Id, planCatalogId, cancellationToken)
                : OperationResult<LessonRecord>.Failure(new ValidationError(
                    "Lesson.CalendarLinkChanged", "The appointment is already linked to another student."));
        }

        var plan = await planCatalogRepository.GetAsync(planCatalogId, cancellationToken);
        if (plan is null || !plan.CanBeAssigned)
        {
            return OperationResult<LessonRecord>.Failure(new ValidationError(
                "Lesson.PlanNotAssignable", "Choose an active plan for the appointment."));
        }

        var saved = await lessonRepository.SaveAsync(observed with
        {
            PlanCatalogId = plan.Id,
            PlanName = plan.Name,
            DurationMinutes = plan.DurationMinutes
        }, cancellationToken);
        return OperationResult<LessonRecord>.Success(saved);
    }

    public async Task<OperationResult<LessonRecord>> ExecuteAsync(
        int lessonId,
        int planCatalogId,
        CancellationToken cancellationToken)
    {
        var plan = await planCatalogRepository.GetAsync(planCatalogId, cancellationToken);
        if (plan is null)
        {
            return OperationResult<LessonRecord>.Failure(
                new ValidationError("Lesson.PlanNotFound", "The selected plan was not found."));
        }

        if (!plan.CanBeAssigned)
        {
            return OperationResult<LessonRecord>.Failure(
                new ValidationError("Lesson.PlanNotAssignable", "The selected plan cannot be assigned to a lesson."));
        }

        var lesson = await lessonRepository.UpdatePlanAsync(
            lessonId,
            plan.Id,
            plan.Name,
            plan.DurationMinutes,
            cancellationToken);
        if (lesson is null)
        {
            return OperationResult<LessonRecord>.Failure(
                new ValidationError("Lesson.PlanLocked", "A lesson with a pending invoice or calendar change cannot have its plan changed."));
        }

        return OperationResult<LessonRecord>.Success(lesson);
    }
}
