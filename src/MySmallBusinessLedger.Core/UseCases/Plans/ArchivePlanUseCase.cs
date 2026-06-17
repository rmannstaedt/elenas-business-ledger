using MySmallBusinessLedger.Core.Common;
using MySmallBusinessLedger.Core.Plans;
using MySmallBusinessLedger.Core.Ports;

namespace MySmallBusinessLedger.Core.UseCases.Plans;

public sealed class ArchivePlanUseCase
{
    private readonly IPlanCatalogRepository planCatalogRepository;

    public ArchivePlanUseCase(IPlanCatalogRepository planCatalogRepository)
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
                new ValidationError("Plan.ArchiveRequiresActive", "Only active plans can be archived."));
        }

        return await planCatalogRepository.SaveAsync(plan with { State = PlanCatalogState.Archived }, cancellationToken);
    }
}
