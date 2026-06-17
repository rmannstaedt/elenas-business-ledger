using MySmallBusinessLedger.Core.Common;
using MySmallBusinessLedger.Core.Plans;
using MySmallBusinessLedger.Core.UseCases.Plans;

namespace MySmallBusinessLedger.App.Services;

public interface IPlanCatalogService
{
    Task<IReadOnlyList<PlanCatalogItem>> ListAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<PlanCatalogItem>> ListAssignableAsync(CancellationToken cancellationToken);

    Task<OperationResult<PlanCatalogItem>> CreateDraftAsync(PlanDraftInput input, CancellationToken cancellationToken);

    Task<OperationResult<PlanCatalogItem>> UpdateDraftAsync(int planId, PlanDraftInput input, CancellationToken cancellationToken);

    Task<OperationResult<bool>> DeleteDraftAsync(int planId, CancellationToken cancellationToken);

    Task<OperationResult<PlanCatalogItem>> PublishAsync(int planId, CancellationToken cancellationToken);

    Task<OperationResult<PlanCatalogItem>> ArchiveAsync(int planId, CancellationToken cancellationToken);
}
