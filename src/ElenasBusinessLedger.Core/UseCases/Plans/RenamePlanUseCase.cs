using ElenasBusinessLedger.Core.Common;
using ElenasBusinessLedger.Core.Plans;
using ElenasBusinessLedger.Core.Ports;

namespace ElenasBusinessLedger.Core.UseCases.Plans;

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
