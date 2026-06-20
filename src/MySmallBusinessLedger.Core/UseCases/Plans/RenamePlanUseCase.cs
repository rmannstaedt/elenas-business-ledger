using MySmallBusinessLedger.Core.Common;
using MySmallBusinessLedger.Core.Plans;
using MySmallBusinessLedger.Core.Ports;

namespace MySmallBusinessLedger.Core.UseCases.Plans;

public sealed class RenamePlanUseCase
{
    private readonly IPlanCatalogRepository planCatalogRepository;

    public RenamePlanUseCase(IPlanCatalogRepository planCatalogRepository)
    {
        this.planCatalogRepository = planCatalogRepository;
    }

    public Task<OperationResult<PlanCatalogItem>> ExecuteAsync(
        int planId,
        string newName,
        DateTimeOffset changedAt,
        CancellationToken cancellationToken) =>
        planCatalogRepository.RenameAsync(planId, newName, changedAt, cancellationToken);
}
