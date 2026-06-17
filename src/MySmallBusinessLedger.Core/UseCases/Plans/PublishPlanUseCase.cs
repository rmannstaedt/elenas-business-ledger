using MySmallBusinessLedger.Core.Common;
using MySmallBusinessLedger.Core.Plans;
using MySmallBusinessLedger.Core.Ports;

namespace MySmallBusinessLedger.Core.UseCases.Plans;

public sealed class PublishPlanUseCase
{
    private readonly IPlanCatalogRepository planCatalogRepository;

    public PublishPlanUseCase(IPlanCatalogRepository planCatalogRepository)
    {
        this.planCatalogRepository = planCatalogRepository;
    }

    public async Task<OperationResult<PlanCatalogItem>> ExecuteAsync(int planId, CancellationToken cancellationToken)
    {
        var plan = await planCatalogRepository.GetAsync(planId, cancellationToken);
        if (plan is null)
        {
            return OperationResult<PlanCatalogItem>.Failure(
                new ValidationError("Plan.NotFound", "Plan was not found."));
        }

        if (plan.State != PlanCatalogState.Draft)
        {
            return OperationResult<PlanCatalogItem>.Failure(
                new ValidationError("Plan.PublishRequiresDraft", "Only draft plans can be published."));
        }

        return await planCatalogRepository.SaveAsync(plan with { State = PlanCatalogState.Active }, cancellationToken);
    }
}
