using ElenasBusinessLedger.Core.Common;
using ElenasBusinessLedger.Core.Plans;
using ElenasBusinessLedger.Core.Ports;

namespace ElenasBusinessLedger.Core.UseCases.Plans;

// Changes editable draft terms while protecting plans that have already been published.
public sealed class UpdateDraftPlanUseCase
{
    private readonly IPlanCatalogRepository planCatalogRepository;

    public UpdateDraftPlanUseCase(IPlanCatalogRepository planCatalogRepository)
    {
        this.planCatalogRepository = planCatalogRepository;
    }

    // Updates a draft while protecting terms on plans that have already been published.
    public async Task<OperationResult<PlanCatalogItem>> ExecuteAsync(
        int planId,
        PlanDraftInput input,
        CancellationToken cancellationToken)
    {
        var plan = await planCatalogRepository.GetAsync(planId, cancellationToken);
        if (plan is null)
        {
            return OperationResult<PlanCatalogItem>.Failure(
                new ValidationError("Plan.NotFound", "Plan was not found."));
        }

        if (plan.State != PlanCatalogState.Draft)
        {
            return OperationResult<PlanCatalogItem>.Failure(
                new ValidationError("Plan.Immutable", "Only draft plans can be edited."));
        }

        var errors = PlanCatalogValidation.ValidateDraft(input);
        if (errors.Count > 0)
        {
            return OperationResult<PlanCatalogItem>.Failure(errors.ToArray());
        }

        return await planCatalogRepository.SaveAsync(PlanCatalogValidation.UpdateDraft(plan, input), cancellationToken);
    }
}
