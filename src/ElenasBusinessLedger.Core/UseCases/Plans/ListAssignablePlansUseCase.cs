using ElenasBusinessLedger.Core.Common;
using ElenasBusinessLedger.Core.Plans;
using ElenasBusinessLedger.Core.Ports;

namespace ElenasBusinessLedger.Core.UseCases.Plans;

public sealed class ListAssignablePlansUseCase
{
    private readonly IPlanCatalogRepository planCatalogRepository;

    public ListAssignablePlansUseCase(IPlanCatalogRepository planCatalogRepository)
    {
        this.planCatalogRepository = planCatalogRepository;
    }

    public async Task<OperationResult<IReadOnlyList<PlanCatalogItem>>> ExecuteAsync(CancellationToken cancellationToken)
    {
        var plans = await planCatalogRepository.ListAssignableAsync(cancellationToken);
        return OperationResult<IReadOnlyList<PlanCatalogItem>>.Success(plans);
    }
}
