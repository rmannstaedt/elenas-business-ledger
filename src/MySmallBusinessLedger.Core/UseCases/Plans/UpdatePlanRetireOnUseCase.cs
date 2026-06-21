using MySmallBusinessLedger.Core.Common;
using MySmallBusinessLedger.Core.Plans;
using MySmallBusinessLedger.Core.Ports;

namespace MySmallBusinessLedger.Core.UseCases.Plans;

public sealed class UpdatePlanRetireOnUseCase
{
    private readonly IPlanCatalogRepository planCatalogRepository;

    public UpdatePlanRetireOnUseCase(IPlanCatalogRepository planCatalogRepository)
    {
        this.planCatalogRepository = planCatalogRepository;
    }

    public async Task<OperationResult<PlanCatalogItem>> ExecuteAsync(
        int planId,
        DateTimeOffset? retireOn,
        CancellationToken cancellationToken)
    {
        var plan = await planCatalogRepository.GetAsync(planId, cancellationToken);
        if (plan is null)
        {
            return OperationResult<PlanCatalogItem>.Failure(
                new ValidationError("Plan.NotFound", "Plan was not found."));
        }

        if (plan.State is not (PlanCatalogState.Draft or PlanCatalogState.Active))
        {
            return OperationResult<PlanCatalogItem>.Failure(
                new ValidationError("Plan.RetireOnImmutable", "Retire on can only be changed for draft or active plans."));
        }

        return await planCatalogRepository.SaveAsync(plan with { RetireOn = retireOn }, cancellationToken);
    }
}
