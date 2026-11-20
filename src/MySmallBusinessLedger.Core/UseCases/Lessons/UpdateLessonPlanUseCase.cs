using MySmallBusinessLedger.Core.Common;
using MySmallBusinessLedger.Core.Lessons;
using MySmallBusinessLedger.Core.Ports;

namespace MySmallBusinessLedger.Core.UseCases.Lessons;

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
                new ValidationError("Lesson.PlanLocked", "Invoiced lessons cannot have their plan changed."));
        }

        return OperationResult<LessonRecord>.Success(lesson);
    }
}
