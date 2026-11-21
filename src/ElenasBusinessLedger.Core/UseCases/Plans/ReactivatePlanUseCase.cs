using ElenasBusinessLedger.Core.Common;
using ElenasBusinessLedger.Core.Plans;
using ElenasBusinessLedger.Core.Ports;

namespace ElenasBusinessLedger.Core.UseCases.Plans;

public sealed class ReactivatePlanUseCase
{
    private readonly IPlanCatalogRepository planCatalogRepository;

    public ReactivatePlanUseCase(IPlanCatalogRepository planCatalogRepository)
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

        if (plan.State != PlanCatalogState.Retired)
        {
            return OperationResult<PlanCatalogItem>.Failure(
                new ValidationError("Plan.ReactivateRequiresRetired", "Only retired plans can be reactivated."));
        }

        return await planCatalogRepository.SaveAsync(plan with { State = PlanCatalogState.Active, RetireOn = null }, cancellationToken);
    }
}
