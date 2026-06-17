using MySmallBusinessLedger.Core.Common;
using MySmallBusinessLedger.Core.Plans;
using MySmallBusinessLedger.Core.Ports;

namespace MySmallBusinessLedger.Core.UseCases.Plans;

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
