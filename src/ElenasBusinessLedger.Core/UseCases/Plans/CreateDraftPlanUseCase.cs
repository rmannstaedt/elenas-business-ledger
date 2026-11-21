using ElenasBusinessLedger.Core.Common;
using ElenasBusinessLedger.Core.Plans;
using ElenasBusinessLedger.Core.Ports;

namespace ElenasBusinessLedger.Core.UseCases.Plans;

// Validates and saves new plan terms as a draft so they can be prepared before assignment.
public sealed class CreateDraftPlanUseCase
{
    private readonly IPlanCatalogRepository planCatalogRepository;

    public CreateDraftPlanUseCase(IPlanCatalogRepository planCatalogRepository)
    {
        this.planCatalogRepository = planCatalogRepository;
    }

    // Saves a validated draft that can be edited before it becomes assignable.
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
