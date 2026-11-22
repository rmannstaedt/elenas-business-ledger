using ElenasBusinessLedger.Core.Common;
using ElenasBusinessLedger.Core.Plans;
using ElenasBusinessLedger.Core.Ports;

namespace ElenasBusinessLedger.Core.UseCases.Plans;

// Sets or clears the date used by scheduled plan retirement.
public sealed class UpdatePlanRetireOnUseCase
{
    private readonly IPlanCatalogRepository planCatalogRepository;

    public UpdatePlanRetireOnUseCase(IPlanCatalogRepository planCatalogRepository)
    {
        this.planCatalogRepository = planCatalogRepository;
    }

    // Changes when a catalog plan should stop being offered for new assignments.
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
