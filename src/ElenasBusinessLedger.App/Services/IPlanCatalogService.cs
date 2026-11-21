using ElenasBusinessLedger.Core.Common;
using ElenasBusinessLedger.Core.Plans;
using ElenasBusinessLedger.Core.UseCases.Plans;

namespace ElenasBusinessLedger.App.Services;

public interface IPlanCatalogService
{
    Task<IReadOnlyList<PlanCatalogItem>> ListAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<PlanCatalogItem>> ListAssignableAsync(CancellationToken cancellationToken);

    Task<OperationResult<PlanCatalogItem>> CreateDraftAsync(PlanDraftInput input, CancellationToken cancellationToken);

    Task<OperationResult<PlanCatalogItem>> UpdateDraftAsync(int planId, PlanDraftInput input, CancellationToken cancellationToken);

    Task<OperationResult<PlanCatalogItem>> RenameAsync(int planId, string newName, CancellationToken cancellationToken);

    Task<OperationResult<bool>> DeleteDraftAsync(int planId, CancellationToken cancellationToken);

    Task<OperationResult<PlanCatalogItem>> PublishDraftAsync(int planId, CancellationToken cancellationToken);

    Task<OperationResult<PlanCatalogItem>> RetireAsync(int planId, CancellationToken cancellationToken);

    Task<OperationResult<PlanCatalogItem>> ReactivateAsync(int planId, CancellationToken cancellationToken);

    Task<OperationResult<PlanCatalogItem>> UpdateRetireOnAsync(
        int planId,
        DateTimeOffset? retireOn,
        CancellationToken cancellationToken);

    Task<OperationResult<int>> RetireDueAsync(DateTimeOffset now, CancellationToken cancellationToken);
}
