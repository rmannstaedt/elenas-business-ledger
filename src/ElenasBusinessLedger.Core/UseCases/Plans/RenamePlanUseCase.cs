using ElenasBusinessLedger.Core.Common;
using ElenasBusinessLedger.Core.Plans;
using ElenasBusinessLedger.Core.Ports;

namespace ElenasBusinessLedger.Core.UseCases.Plans;

// Changes a plan's display name without changing the ID used by existing links.
public sealed class RenamePlanUseCase
{
    private readonly IPlanCatalogRepository planCatalogRepository;

    public RenamePlanUseCase(IPlanCatalogRepository planCatalogRepository)
    {
        this.planCatalogRepository = planCatalogRepository;
    }

    // Updates the plan label through the catalog repository so the change can be recorded under the same plan ID.
    public Task<OperationResult<PlanCatalogItem>> ExecuteAsync(
        int planId,
        string newName,
        DateTimeOffset changedAt,
        CancellationToken cancellationToken) =>
        planCatalogRepository.RenameAsync(planId, newName, changedAt, cancellationToken);
}
