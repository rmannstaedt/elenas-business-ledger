using ElenasBusinessLedger.Core.Common;
using ElenasBusinessLedger.Core.Plans;

namespace ElenasBusinessLedger.Core.Ports;

// Supplies and updates reusable plan terms so assignment and catalog management share the same records.
public interface IPlanCatalogRepository
{
    Task<IReadOnlyList<PlanCatalogItem>> ListAllAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<PlanCatalogItem>> ListAssignableAsync(CancellationToken cancellationToken);

    Task<PlanCatalogItem?> GetAsync(int id, CancellationToken cancellationToken);

    Task<OperationResult<PlanCatalogItem>> SaveAsync(PlanCatalogItem plan, CancellationToken cancellationToken);

    Task<OperationResult<PlanCatalogItem>> RenameAsync(
        int id,
        string newName,
        DateTimeOffset changedAt,
        CancellationToken cancellationToken);

    Task<OperationResult<bool>> DeleteAsync(int id, CancellationToken cancellationToken);
}
