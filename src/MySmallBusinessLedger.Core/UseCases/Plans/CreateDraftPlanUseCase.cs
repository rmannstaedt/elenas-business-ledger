using MySmallBusinessLedger.Core.Common;
using MySmallBusinessLedger.Core.Plans;
using MySmallBusinessLedger.Core.Ports;

namespace MySmallBusinessLedger.Core.UseCases.Plans;

public sealed class CreateDraftPlanUseCase
{
    private readonly IPlanCatalogRepository planCatalogRepository;

    public CreateDraftPlanUseCase(IPlanCatalogRepository planCatalogRepository)
    {
        this.planCatalogRepository = planCatalogRepository;
    }

    public Task<OperationResult<PlanCatalogItem>> ExecuteAsync(
        PlanDraftInput input,
        CancellationToken cancellationToken)
    {
        var errors = PlanCatalogValidation.ValidateDraft(input);
        if (errors.Count > 0)
        {
            return Task.FromResult(OperationResult<PlanCatalogItem>.Failure(errors.ToArray()));
        }

        return planCatalogRepository.SaveAsync(PlanCatalogValidation.CreateDraft(input), cancellationToken);
    }
}
