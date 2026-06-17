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

        if (plan.State is not (PlanCatalogState.Draft or PlanCatalogState.Archived))
        {
            return OperationResult<PlanCatalogItem>.Failure(
                new ValidationError("Plan.PublishRequiresDraftOrArchived", "Only draft or archived plans can be published."));
        }

        return await planCatalogRepository.SaveAsync(plan with { State = PlanCatalogState.Active }, cancellationToken);
    }
}
