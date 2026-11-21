using ElenasBusinessLedger.Core.Common;
using ElenasBusinessLedger.Core.Plans;
using ElenasBusinessLedger.Core.Ports;

namespace ElenasBusinessLedger.Core.UseCases.Plans;

// Marks a plan retired so it can remain in history while leaving the active choices.
public sealed class RetirePlanUseCase
{
    private readonly IPlanCatalogRepository planCatalogRepository;

    public RetirePlanUseCase(IPlanCatalogRepository planCatalogRepository)
    {
        this.planCatalogRepository = planCatalogRepository;
    }

    // Removes an active plan from future choices by marking it retired.
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
