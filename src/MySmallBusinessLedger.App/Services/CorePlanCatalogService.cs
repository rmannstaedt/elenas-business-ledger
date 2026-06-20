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
    private readonly RenamePlanUseCase renamePlanUseCase;
    private readonly DeleteDraftPlanUseCase deleteDraftPlanUseCase;
    private readonly PublishPlanUseCase publishPlanUseCase;
    private readonly RetirePlanUseCase retirePlanUseCase;
    private readonly ReactivatePlanUseCase reactivatePlanUseCase;
    private readonly UpdatePlanRetireOnUseCase updatePlanRetireOnUseCase;
    private readonly RetireDuePlansUseCase retireDuePlansUseCase;

    public CorePlanCatalogService(IPlanCatalogRepository planCatalogRepository)
    {
        listPlansUseCase = new ListPlansUseCase(planCatalogRepository);
        listAssignablePlansUseCase = new ListAssignablePlansUseCase(planCatalogRepository);
        createDraftPlanUseCase = new CreateDraftPlanUseCase(planCatalogRepository);
        updateDraftPlanUseCase = new UpdateDraftPlanUseCase(planCatalogRepository);
        renamePlanUseCase = new RenamePlanUseCase(planCatalogRepository);
        deleteDraftPlanUseCase = new DeleteDraftPlanUseCase(planCatalogRepository);
        publishPlanUseCase = new PublishPlanUseCase(planCatalogRepository);
        retirePlanUseCase = new RetirePlanUseCase(planCatalogRepository);
        reactivatePlanUseCase = new ReactivatePlanUseCase(planCatalogRepository);
        updatePlanRetireOnUseCase = new UpdatePlanRetireOnUseCase(planCatalogRepository);
        retireDuePlansUseCase = new RetireDuePlansUseCase(planCatalogRepository);
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

    public Task<OperationResult<PlanCatalogItem>> RenameAsync(
        int planId,
        string newName,
        CancellationToken cancellationToken) =>
        renamePlanUseCase.ExecuteAsync(planId, newName, DateTimeOffset.Now, cancellationToken);

    public Task<OperationResult<bool>> DeleteDraftAsync(int planId, CancellationToken cancellationToken) =>
        deleteDraftPlanUseCase.ExecuteAsync(planId, cancellationToken);

    public Task<OperationResult<PlanCatalogItem>> PublishDraftAsync(int planId, CancellationToken cancellationToken) =>
        publishPlanUseCase.ExecuteAsync(planId, cancellationToken);

    public Task<OperationResult<PlanCatalogItem>> RetireAsync(int planId, CancellationToken cancellationToken) =>
        retirePlanUseCase.ExecuteAsync(planId, cancellationToken);

    public Task<OperationResult<PlanCatalogItem>> ReactivateAsync(int planId, CancellationToken cancellationToken) =>
        reactivatePlanUseCase.ExecuteAsync(planId, cancellationToken);

    public Task<OperationResult<PlanCatalogItem>> UpdateRetireOnAsync(
        int planId,
        DateTimeOffset? retireOn,
        CancellationToken cancellationToken) =>
        updatePlanRetireOnUseCase.ExecuteAsync(planId, retireOn, cancellationToken);

    public Task<OperationResult<int>> RetireDueAsync(DateTimeOffset now, CancellationToken cancellationToken) =>
        retireDuePlansUseCase.ExecuteAsync(now, cancellationToken);
}
