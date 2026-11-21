using ElenasBusinessLedger.Core.Common;
using ElenasBusinessLedger.Core.Plans;
using ElenasBusinessLedger.Core.Ports;

namespace ElenasBusinessLedger.Core.UseCases.Plans;

public sealed class DeleteDraftPlanUseCase
{
    private readonly IPlanCatalogRepository planCatalogRepository;

    public DeleteDraftPlanUseCase(IPlanCatalogRepository planCatalogRepository)
    {
        this.planCatalogRepository = planCatalogRepository;
    }

    public async Task<OperationResult<bool>> ExecuteAsync(int planId, CancellationToken cancellationToken)
    {
        var plan = await planCatalogRepository.GetAsync(planId, cancellationToken);
        if (plan is null)
        {
            return OperationResult<bool>.Failure(
                new ValidationError("Plan.NotFound", "Plan was not found."));
        }

        if (plan.State != PlanCatalogState.Draft)
        {
            return OperationResult<bool>.Failure(
                new ValidationError("Plan.DeleteRequiresDraft", "Only draft plans can be deleted."));
        }

        return await planCatalogRepository.DeleteAsync(planId, cancellationToken);
    }
}
