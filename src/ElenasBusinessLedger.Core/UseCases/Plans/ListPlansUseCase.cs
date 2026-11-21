using ElenasBusinessLedger.Core.Common;
using ElenasBusinessLedger.Core.Plans;
using ElenasBusinessLedger.Core.Ports;

namespace ElenasBusinessLedger.Core.UseCases.Plans;

// Supplies the full plan catalog for management, including drafts and retired entries.
public sealed class ListPlansUseCase
{
    private readonly IPlanCatalogRepository planCatalogRepository;

    public ListPlansUseCase(IPlanCatalogRepository planCatalogRepository)
    {
        this.planCatalogRepository = planCatalogRepository;
    }

    public async Task<OperationResult<IReadOnlyList<PlanCatalogItem>>> ExecuteAsync(CancellationToken cancellationToken)
    {
        var plans = await planCatalogRepository.ListAllAsync(cancellationToken);
        return OperationResult<IReadOnlyList<PlanCatalogItem>>.Success(plans);
    }
}
