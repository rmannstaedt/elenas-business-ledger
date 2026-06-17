using MySmallBusinessLedger.Core.Common;
using MySmallBusinessLedger.Core.Plans;
using MySmallBusinessLedger.Core.Ports;

namespace MySmallBusinessLedger.Core.UseCases.Plans;

public sealed class RetirePlanUseCase
{
    private readonly IPlanCatalogRepository planCatalogRepository;

    public RetirePlanUseCase(IPlanCatalogRepository planCatalogRepository)
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

        if (plan.State != PlanCatalogState.Active)
        {
            return OperationResult<PlanCatalogItem>.Failure(
                new ValidationError("Plan.RetireRequiresActive", "Only active plans can be retired."));
        }

        return await planCatalogRepository.SaveAsync(plan with { State = PlanCatalogState.Retired }, cancellationToken);
    }
}
