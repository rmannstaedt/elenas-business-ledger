using MySmallBusinessLedger.Core.Common;
using MySmallBusinessLedger.Core.Plans;
using MySmallBusinessLedger.Core.Ports;
using MySmallBusinessLedger.Core.UseCases.Plans;

namespace MySmallBusinessLedger.App.Services;

public sealed class CorePlanCatalogService : IPlanCatalogService
{
    private readonly ListPlansUseCase listPlansUseCase;
    private readonly ListAssignablePlansUseCase listAssignablePlansUseCase;
    private readonly CreateDraftPlanUseCase createDraftPlanUseCase;
    private readonly UpdateDraftPlanUseCase updateDraftPlanUseCase;
    private readonly DeleteDraftPlanUseCase deleteDraftPlanUseCase;
    private readonly PublishPlanUseCase publishPlanUseCase;
    private readonly ArchivePlanUseCase archivePlanUseCase;

    public CorePlanCatalogService(IPlanCatalogRepository planCatalogRepository)
    {
        listPlansUseCase = new ListPlansUseCase(planCatalogRepository);
        listAssignablePlansUseCase = new ListAssignablePlansUseCase(planCatalogRepository);
        createDraftPlanUseCase = new CreateDraftPlanUseCase(planCatalogRepository);
        updateDraftPlanUseCase = new UpdateDraftPlanUseCase(planCatalogRepository);
        deleteDraftPlanUseCase = new DeleteDraftPlanUseCase(planCatalogRepository);
        publishPlanUseCase = new PublishPlanUseCase(planCatalogRepository);
        archivePlanUseCase = new ArchivePlanUseCase(planCatalogRepository);
    }

    public async Task<IReadOnlyList<PlanCatalogItem>> ListAsync(CancellationToken cancellationToken)
    {
        var result = await listPlansUseCase.ExecuteAsync(cancellationToken);
        return result.Value ?? [];
    }

    public async Task<IReadOnlyList<PlanCatalogItem>> ListAssignableAsync(CancellationToken cancellationToken)
    {
        var result = await listAssignablePlansUseCase.ExecuteAsync(cancellationToken);
        return result.Value ?? [];
    }

    public Task<OperationResult<PlanCatalogItem>> CreateDraftAsync(
        PlanDraftInput input,
        CancellationToken cancellationToken) =>
        createDraftPlanUseCase.ExecuteAsync(input, cancellationToken);

    public Task<OperationResult<PlanCatalogItem>> UpdateDraftAsync(
        int planId,
        PlanDraftInput input,
        CancellationToken cancellationToken) =>
        updateDraftPlanUseCase.ExecuteAsync(planId, input, cancellationToken);

    public Task<OperationResult<bool>> DeleteDraftAsync(int planId, CancellationToken cancellationToken) =>
        deleteDraftPlanUseCase.ExecuteAsync(planId, cancellationToken);

    public Task<OperationResult<PlanCatalogItem>> PublishAsync(int planId, CancellationToken cancellationToken) =>
        publishPlanUseCase.ExecuteAsync(planId, cancellationToken);

    public Task<OperationResult<PlanCatalogItem>> ArchiveAsync(int planId, CancellationToken cancellationToken) =>
        archivePlanUseCase.ExecuteAsync(planId, cancellationToken);
}
